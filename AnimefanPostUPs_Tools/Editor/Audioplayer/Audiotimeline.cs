

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using System;
using System.Text;
using System.Collections;
using System.Reflection;
using Unity.EditorCoroutines.Editor;
using System.Linq;
using System.IO;
using System.Threading.Tasks;

using NAudio;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;


//import colors and guilayouts from AnimefanPostUPs-Tools
using AnimefanPostUPs_Tools.SmartColorUtility;
using AnimefanPostUPs_Tools.ColorTextureItem;
using AnimefanPostUPs_Tools.ColorTextureManager;
using AnimefanPostUPs_Tools.GUI_LayoutElements;
//get Texturetypes
//using AnimefanPostUPs_Tools.ColorTextureItem.TexItemType;
using AnimefanPostUPs_Tools.MP3Reader;
using AnimefanPostUPs_Tools.WavReader;
using AnimefanPostUPs_Tools.AudioMixUtils;
using AnimefanPostUPs_Tools.TimelineView;
using AnimefanPostUPs_Tools.AudioTrackManager;
using AnimefanPostUPs_Tools.AudioTrack;
using AnimefanPostUPs_Tools.CustomPopup;

//----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
//CLass Header


public class Audiotimeline : EditorWindow
{

    private static Color GetRGBA(ColorRGBA color)
    {
        return SmartColorUtility.GetRGBA(color);
    }

    //clip slot
    const string CacheFolder = "Assets/AnimefanPostUPs-Tools/AnimefanPostUPs_Tools/Editor/Textures_Internal/";
    public AudioClip audioClip;
    public string filename = "Mix";
    public string targetfolder = "Assets/AnimTools/";


    private ColorTextureManager colorTextureManager = new ColorTextureManager();
    private TimelineView timelineView;
    private AudiotrackManager audioManager;
    public Splitviewer splitviewer = new Splitviewer();
    public DropdownMenu dropdownMenu;
    public float lastTime = 0;
    public bool isPlaying = false;
    bool firstTime = true;
    bool change = false;
    int changetime = 0;

    //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

    //Create window
    [MenuItem("Animtools/Audio Timeline")]
    public static void ShowWindow()
    {
        GetWindow<Audiotimeline>("Audio Timeline");
    }


    //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    //Unity Calls


    public void checkSave()
    {
        if (audioManager.autosave)
        {
            //Check if folder exists
            if (Directory.Exists(targetfolder))
            {
                audioManager.SaveJson(targetfolder + "/" + filename + "_autosave.json");
            }
        }
    }

    private void OnEnable()
    {
        init_audiomanager();
        //Init color texture manager


        colorTextureManager.CacheFolder = CacheFolder;


        dropdownMenu = new DropdownMenu("Mainmenu", colorTextureManager, new Vector2(5, 5));
        setDropdownTextures(dropdownMenu);
        dropdownMenu.AddItem("New", () => { New(); });
        dropdownMenu.AddItem("Load Json", () => { LoadFrom(); });
        dropdownMenu.AddItem("Save", () => { Save(); });
        dropdownMenu.AddItem("Save As", () => { SaveAt(); });
        dropdownMenu.AddItem("Create Backup", () => { SaveBackup(); });
        dropdownMenu.AddItem("Render", () => { Render(); });
        dropdownMenu.AddItem("Render As", () => { RenderWavAs(); });
        dropdownMenu.AddItem("Settings", () => { setRendersettings(); });
        dropdownMenu.AddItem("Synch", false);

        //Load the last folder and filename
        string _targetfolder = PlayerPrefs.GetString("AudioTimelineFolder", targetfolder);
        string _filename = PlayerPrefs.GetString("AudioTimelineFilename", filename);

        bool checkFile = true;

        //check if they are valid
        if (Directory.Exists(_targetfolder))
        {
            targetfolder = _targetfolder;

        }
        else { checkFile = false; }

        if (File.Exists(targetfolder + "/" + _filename + "_autosave.json"))
        {
            filename = _filename;
            //remove _autosave from the filename

            audioManager.LoadJson(targetfolder + "/" + filename + "_autosave.json");
        }
        else if (File.Exists(targetfolder + "/" + _filename + ".json"))
        {
            filename = _filename;
            audioManager.LoadJson(targetfolder + "/" + filename + ".json");
        }
        else { checkFile = false; }

    }

    private void OnDisable()
    {
        colorTextureManager.Unload();

        //store the current folder and filename
        PlayerPrefs.SetString("AudioTimelineFolder", targetfolder);
        PlayerPrefs.SetString("AudioTimelineFilename", filename);
    }


    private void OnGUI()
    {

        if (audioManager.internalupdate)
        {
            //Save the autosave
            audioManager.SaveJson(targetfolder + "/" + filename + "_autosave.json");
        }

        //Audiologic
        bool synch = dropdownMenu.getValue("Synch");
        playAudioLogic(synch);


        //Repainter
        Rect windowRect = new Rect(0, 0, position.width, position.height);
        if (windowRect.Contains(Event.current.mousePosition) || synch)
        {
            Repaint();
        }

        //Dropdown
        dropdownMenu.checkMouseEvents(Event.current);

        //Timelinescrolling
        if (Event.current.type == EventType.ScrollWheel && (Event.current.control || Event.current.alt))
        {
            float old = timelineView.timelinezoom.x;
            timelineView.timelinezoom.x -= (Event.current.delta.y * 5);
            timelineView.timelinezoom.x = Mathf.Clamp(timelineView.timelinezoom.x, 1, 1000);

            //get mouse position
            float mousepos = Event.current.mousePosition.x;
            //calculate relative to window
            float relative = (mousepos - splitviewer.splitPosition / 2) * 100;

            // Adjust the offset based on the change in zoom level and the zoom center
            float a = (((position.width - splitviewer.splitPosition - (timelineView.timelinePosition_Offset.x)) / 2)) / 100 * old;
            float b = (((position.width - splitviewer.splitPosition - (timelineView.timelinePosition_Offset.x)) / 2)) / 100 * timelineView.timelinezoom.x;
            timelineView.timelinePosition_Offset.x += a - b;
            Event.current.Use();
            Repaint();
        }


        //Main Drawing
        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical(GUILayout.Width(splitviewer.splitPosition));
        GUILayout.EndVertical();

        //Splitter Element
        if (splitviewer.drawSplit(Event.current, position)) Repaint();

        //UI
        DrawTimelineUI(position.width - splitviewer.splitPosition);
        GUILayout.EndHorizontal();
        DrawSidePanel(splitviewer.splitPosition);

        //Draw Dropdown (Fixed Position)
        dropdownMenu.Draw(Event.current, new Vector2((splitviewer.splitPosition / 8 * 6), 25));
        //Create a Button at fixed Position


        Texture2D buttonTexture = colorTextureManager.LoadTexture(TexItemType.Gradient_Vertical, ColorRGBA.grayscale_048, ColorRGBA.grayscale_064, 8);
        GUIStyle buttonStyle = new GUIStyle();
        buttonStyle.normal.background = buttonTexture;
        //Center content
        buttonStyle.alignment = TextAnchor.MiddleCenter;
        //White text
        buttonStyle.normal.textColor = Color.white;

        GUIContent buttonContent = EditorGUIUtility.IconContent("d_Profiler.Memory");

        if (GUI.Button(new Rect((splitviewer.splitPosition / 8 * 6) + 7, 6, 25, 25), buttonContent, buttonStyle))
        {
            Render();
            checkSave();
        }

    }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    //INITS
    void init_audiomanager()
    {
        audioManager = new AudiotrackManager();
        timelineView = new TimelineView(1000, 50, audioManager);
    }


    void setDropdownTextures(DropdownMenu menu)
    {
        menu.itemButtonActive = new ColorTextureManager.ColorTextureDummy(TexItemType.Gradient_Radial, ColorRGBA.grayscale_064, ColorRGBA.grayscale_025, 8);
        menu.itemButtonNormal = new ColorTextureManager.ColorTextureDummy(TexItemType.Solid, ColorRGBA.grayscale_025, ColorRGBA.none, 8);
        menu.itemButtonHover = new ColorTextureManager.ColorTextureDummy(TexItemType.Gradient_Radial, ColorRGBA.grayscale_064, ColorRGBA.grayscale_025, 8);

        menu.mainButtonNormal = new ColorTextureManager.ColorTextureDummy(TexItemType.Gradient_Vertical, ColorRGBA.grayscale_048, ColorRGBA.grayscale_064, 8);
        menu.mainButtonHover = new ColorTextureManager.ColorTextureDummy(TexItemType.Solid, ColorRGBA.grayscale_064, ColorRGBA.grayscale_032, 8);
        menu.mainButtonActive = new ColorTextureManager.ColorTextureDummy(TexItemType.Gradient_Radial, ColorRGBA.grayscale_128, ColorRGBA.grayscale_064, 8);

        menu.backgroundTexture = new ColorTextureManager.ColorTextureDummy(TexItemType.Bordered, ColorRGBA.grayscale_032, ColorRGBA.grayscale_016, 8);
    }


    //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    //Logic for Audio Playback
    void playAudioLogic(bool synch)
    {
        //If aniamtion synch is on
        if (synch)
        {
            //Check if the filepath is valid, if not open a folder dialog
            if (!Directory.Exists(targetfolder))
            {
                targetfolder = EditorUtility.OpenFolderPanel("Select Project Folder", targetfolder, "");
                //targetfolder is still not valid set the value to the default
                if (!Directory.Exists(targetfolder))
                {
                    dropdownMenu.setValue("Synch", false);
                }
            }

            float currentTime = GetCurrentTime();

            //Set timeline playback to the current time
            timelineView.displayPlayback = true;
            timelineView.playbackPosition = currentTime;

            //detect if the time has changed negative or not at all
            if (currentTime == lastTime)
            {
                if (isPlaying != IsAnimationPlaying())
                {
                    if (isPlaying)
                    {
                        timeStopped();
                    }
                    else
                    {
                        timeStarted();
                    }
                }
            }
            else if (currentTime > lastTime)
            {
                timeStarted();
            }
            else if (currentTime < lastTime)
            {
                timeChanged();

            }

            lastTime = currentTime;


            //set the splitviewer to the current position to the property width
            splitviewer.splitPosition = GetPropertyWidth();

            //Get the shown area of the animation window
            Rect shownArea = GetTimelineScaling();

            //set the position and timeline position to the shown area
            float widthTimeline = position.width - splitviewer.splitPosition;

            timelineView.timelinezoom = new Vector2((widthTimeline * 10) / (shownArea.width * 10), timelineView.timelinezoom.y);
            timelineView.timelinePosition_Offset = new Vector2(((-shownArea.x * timelineView.timelinezoom.x)), timelineView.timelinePosition_Offset.y);

        }
        else
        {
            timelineView.displayPlayback = false;
            //Disable autobuild
            firstTime = true;

            timeStopped();
            isPlaying = false;
        }
    }




    //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    //Saving Functions / Popups




    //function to Load new Json
    public void New()
    {

        //Ask if they are sure, then ask a second time if they want to save the current project if theres any tracks
        if (EditorUtility.DisplayDialog("Load New Json", "Are you sure you want to create a new Json?", "Yes", "No"))
        {
            if (audioManager.audioTracks.Count > 0)
            {
                if (EditorUtility.DisplayDialog("Save Current Project", "Do you want to save the current project?", "Yes", "No"))
                {
                    SaveAt();
                }
            }

            //open file dialog to create a new json
            string path = EditorUtility.SaveFilePanel("Save Timeline", Application.dataPath, "NewTimeline", "json");
            if (path.Length > 0)
            {
                //save the target folder and filename
                filename = Path.GetFileNameWithoutExtension(path);
                targetfolder = Path.GetDirectoryName(path);

                //Clear the audio manager
                audioManager = new AudiotrackManager();
                timelineView = new TimelineView(1000, 50, audioManager);
            }

        }

    }

    public void setRendersettings()
    {

        int existingSampleRate = audioManager.targetSampleRate;
        int existingBitDepth = audioManager.targetBitDepth;
        int existingChannels = audioManager.targetChannels;

        //Normalization settings values
        bool setting_doNormalizeInput = audioManager.setting_doNormalizeInput;
        float setting_normalizeInput = audioManager.setting_normalizationFac_Input;

        bool setting_doNormalizeOutput = audioManager.setting_doNormalizeOutput;
        float setting_normalizeOutput = audioManager.setting_normalizationFac_Output;

        float targetgain_In = audioManager.targetgain_In;
        float targetgain_Out = audioManager.targetgain_Out;

        bool setting_autobuild = audioManager.autobuild;
        bool setting_autosave = audioManager.autosave;
        bool setting_snapView = audioManager.snapView;
        bool setting_optimizedBuild = audioManager.optimizedBuild;

        CustomPopup.ShowWindow((samplerate, bitrate, channels, doNormalizeInput, normalizeThreshold, doNormalizeOutput, normalizeOutputThreshold, targetgain_In, targetgain_Out, autobuild, autosave, snapView, optimizedBuild) =>
        {
            audioManager.renderSettings(samplerate, bitrate, channels, doNormalizeInput, normalizeThreshold, doNormalizeOutput, normalizeOutputThreshold, targetgain_In, targetgain_Out, autobuild, autosave, snapView, optimizedBuild);
        }, existingSampleRate, existingBitDepth, existingChannels, setting_doNormalizeInput, setting_normalizeInput, setting_doNormalizeOutput, setting_normalizeOutput,
        targetgain_In, targetgain_Out, setting_autobuild, setting_autosave, setting_snapView, setting_optimizedBuild);

        if (audioManager.autosave)
        {
            Render();
        }

        if (audioManager.autobuild)
        {
            Render();
        }


    }


    public void PingOutputFolder()
    {
        //Focus in project window
        //Get relative path
        string relativepath = targetfolder.Replace(Application.dataPath, "Assets");
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(relativepath));
    }

    public void RenderWavAs()
    {
        //Create File
        audioManager.createMix(targetfolder, filename);


        //Open file dialog and ask if overwrite
        string path = EditorUtility.SaveFilePanel("Save Wavefile", targetfolder, filename, "wav");
        if (path.Length > 0)
        {
            //if file exists
            if (File.Exists(path))
            {
                //Open dialog for yes or no
                if (EditorUtility.DisplayDialog("File Exists", "Do you want to overwrite the file?", "Yes", "No"))
                {
                    audioManager.createMix(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path));
                }
            }
            else
            {
                audioManager.createMix(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path));
            }
        }
    }

    //Render function to render the audio mix
    public void Render()
    {
        if (audioManager.audioTracks.Count > 0)
        {
            //Check if folder exists
            if (!Directory.Exists(targetfolder))
            {
                targetfolder = EditorUtility.OpenFolderPanel("Select Project Folder", targetfolder, "");
            }

            //Create File
            string n = "";

            if (audioManager.autosave)
            {
                n = filename + "_auto";
            }
            else
            {
                n = filename;
            }

            //If file exists
            if (File.Exists(targetfolder + "/" + n + ".wav") && false)
            {
                //Open dialog for yes or no
                if (EditorUtility.DisplayDialog("File Exists", "Do you want to overwrite the file?", "Yes", "No"))
                {
                    audioManager.createMix(targetfolder, n);
                }
            }
            else
            {
                audioManager.createMix(targetfolder, n);
            }
        }
    }

    public void SelectOutputFolder()
    {
        targetfolder = EditorUtility.OpenFolderPanel("Select Folder", targetfolder, "");
    }
    public void SaveBackup()
    {
        //Check if folder exists
        if (!Directory.Exists(targetfolder))
            targetfolder = EditorUtility.OpenFolderPanel("Select Project Folder", targetfolder, "");

        audioManager.formattedSave(targetfolder, filename);
        dropdownMenu.isOpen = false;
    }

    //LoadFrom function to load Json from a file using a file dialog
    public void LoadFrom()
    {
        string path = EditorUtility.OpenFilePanel("Load Timeline", Application.dataPath, "json");
        if (path.Length > 0)
        {
            //Check if file exists
            if (File.Exists(path))
                audioManager.LoadJson(path);

            //set the current filename to the loaded file
            filename = Path.GetFileNameWithoutExtension(path);

            //set the folder
            targetfolder = Path.GetDirectoryName(path);
        }
        dropdownMenu.isOpen = false;
    }

    //Save button that saves to the current folder with the current filename
    public void Save()
    {
        //Check if folder exists
        if (!Directory.Exists(targetfolder))
        {
            targetfolder = EditorUtility.OpenFolderPanel("Select Project Folder", targetfolder, "");


        }
        else
        {
            if (audioManager.autosave)
            {
                audioManager.SaveJson(targetfolder + "/" + filename + "_autosave.json");
            }
        }

        if (File.Exists(targetfolder + "/" + filename + ".json"))
        {
            //Open dialog for yes or no
            if (EditorUtility.DisplayDialog("File Exists", "Do you want to overwrite the file?", "Yes", "No"))
            {
                audioManager.SaveJson(targetfolder + "/" + filename + ".json");
            }
        }
        else
        {
            audioManager.SaveJson(targetfolder + "/" + filename + ".json");
        }

        //Save autosave aswell





        dropdownMenu.isOpen = false;
    }

    //Save at function to open a file dialog and save the file at the selected location
    public void SaveAt()
    {
        //Open file dialog and ask if overwrite
        string path = EditorUtility.SaveFilePanel("Save Timeline", targetfolder, filename, "json");
        if (path.Length > 0)
        {
            if (File.Exists(path))
            {
                //Open dialog for yes or no
                if (EditorUtility.DisplayDialog("File Exists", "Do you want to overwrite the file?", "Yes", "No"))
                {
                    audioManager.SaveJson(path);
                    filename = Path.GetFileNameWithoutExtension(path);
                    targetfolder = Path.GetDirectoryName(path);
                }
            }
            else
            {
                audioManager.SaveJson(path);
                filename = Path.GetFileNameWithoutExtension(path);
                targetfolder = Path.GetDirectoryName(path);
            }
        }
        dropdownMenu.isOpen = false;
    }


    //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    //Unity Reflections

    public Rect GetTimelineScaling()
    {
        var editor = typeof(Editor).Assembly;
        var windowtype = editor.GetType("UnityEditor.AnimEditor");
        var dopesheetmethod = windowtype.GetMethod("get_dopeSheetEditor", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);

        var windows = Resources.FindObjectsOfTypeAll(windowtype);

        foreach (var window in windows)
        {
            var obj = dopesheetmethod.Invoke(window, null);
            var type = editor.GetType("UnityEditorInternal.DopeSheetEditor");

            if (type == null) return new Rect();
            var viewRect = type.GetProperty("shownArea", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);

            return (Rect)viewRect.GetValue(obj, null);
        }

        return new Rect();
    }

    public float GetPropertyWidth()
    {
        var editor = typeof(Editor).Assembly;
        var windowtype = editor.GetType("UnityEditor.AnimEditor");
        if (windowtype == null) return 5.0f;
        var getWidthMethod = windowtype.GetMethod("get_hierarchyWidth", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);

        var windowInstances = Resources.FindObjectsOfTypeAll(windowtype);

        foreach (var windowInstance in windowInstances)
        {
            return (float)getWidthMethod.Invoke(windowInstance, null);
        }

        return 0;
    }

    public float GetCurrentTime()
    {
        var editorAssembly = typeof(Editor).Assembly;
        var animWindowStateType = editorAssembly.GetType("UnityEditorInternal.AnimationWindowState");

        if (animWindowStateType == null)
        {
            return 0; // Return a default time if there's no AnimationWindowState
        }

        var timeProperty = animWindowStateType.GetProperty("currentTime", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
        var windowInstances = Resources.FindObjectsOfTypeAll(animWindowStateType);

        foreach (var windowInstance in windowInstances)
        {
            return (float)timeProperty.GetValue(windowInstance, null);
        }

        return 0;
    }


    public bool IsAnimationPlaying()
    {
        var editorAssembly = typeof(Editor).Assembly;
        var animWindowStateType = editorAssembly.GetType("UnityEditorInternal.AnimationWindowState");

        if (animWindowStateType == null)
        {
            return false; // Return false if there's no AnimationWindowState
        }

        var playingProperty = animWindowStateType.GetProperty("playing", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
        var windowInstances = Resources.FindObjectsOfTypeAll(animWindowStateType);

        foreach (var windowInstance in windowInstances)
        {
            return (bool)playingProperty.GetValue(windowInstance, null);
        }

        return false;
    }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    //Audio Utility Copy


    public static void PlayClip(AudioClip clip)
    {
        Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
        Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");

        MethodInfo method = audioUtilClass.GetMethod(
            "PlayPreviewClip",
            BindingFlags.Static | BindingFlags.Public,
            null,
            new System.Type[] {
                typeof(AudioClip), typeof(int), typeof(bool)
        },
        null
        );

        method.Invoke(
            null,
            new object[] {
                clip, 0, false
        }
        );
    }

    public static void StopAllClips()
    {
        Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
        Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
        MethodInfo method = audioUtilClass.GetMethod(
            "StopAllPreviewClips",
            BindingFlags.Static | BindingFlags.Public
            );

        //Debug.Log(audioUtilClass);

        method.Invoke(
            null,
            null
            );
    }


    public static void SetClipSamplePosition(AudioClip clip, int iSamplePosition)
    {
        Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
        Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
        MethodInfo method = audioUtilClass.GetMethod(
            "SetPreviewClipSamplePosition",
            BindingFlags.Static | BindingFlags.Public
            );

        method.Invoke(
            null,
            new object[] {
                clip,
                iSamplePosition
        }
        );
    }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    //Timeline Audio Stopper/Starter/Changer


    public void stopAudio()
    {
        StopAllClips();
    }


    private void timeStopped()
    {
        if (isPlaying)
        {
            isPlaying = false;
            stopAudio();
            //Debug.Log("Stopped");
        }
    }

    private void timeStarted()
    {
        if (!isPlaying)
        {

            AudioClip clip = getMixClip();
            if (clip != null)
            {
                isPlaying = true;
                PlayClip(clip);
                SetClipSamplePosition(clip, (int)((clip.samples * (GetCurrentTime() / clip.length))));

                //Debug.Log("Playing");
            }
        }
    }

    private void timeChanged()
    {
        if (isPlaying)
        {
            StopAllClips();
            AudioClip clip = getMixClip();
            if (clip != null)
            {
                PlayClip(clip);
                SetClipSamplePosition(clip, (int)((clip.samples * (GetCurrentTime() / clip.length))));

                //Debug.Log("Playing" + GetCurrentTime());
            }
            else
            {
                isPlaying = false;
                //Debug.Log("No Clip Found");
            }
        }
    }

    private AudioClip getMixClip()
    {

        string addition = "";

        if (audioManager.autobuild) addition = "_auto";

        if (firstTime)
        {
            firstTime = false;
            audioManager.createMix(targetfolder, filename + addition);
        }

        //find LiveMixTemp.wav in the folderpath
        string path = targetfolder + "/" + filename + addition + ".wav";
        //get relative path
        string relativepath = path.Replace(Application.dataPath, "Assets");

        //cut to the lastes "Assets" folder string using index of
        if (relativepath.IndexOf("Assets") > 0)
            relativepath = relativepath.Substring(relativepath.IndexOf("Assets"));


        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(relativepath);


        return clip;
    }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    //GUI Drawing
    public void DrawTimelineUI(float widthTimeline)
    {
        //Get the current Animation Window if it exists

        GUILayout.BeginHorizontal(GUILayout.Width(widthTimeline));
        if (timelineView.DrawTimeline(audioManager, widthTimeline, splitviewer.splitPosition, colorTextureManager, Event.current, targetfolder, filename)) Repaint();
        GUILayout.EndHorizontal();

    }

    Vector2 scrollPos;

    private void DrawSidePanel(float widthSidePanel)
    {


        GUILayout.BeginVertical();
        //Scrollview vertical
        EditorGUI.DrawRect(new Rect(0, 0, widthSidePanel, position.height), GetRGBA(ColorRGBA.grayscale_032));
        GUILayout.Space(45); //Spacer for Dropdown

        Vector2 oldscrollPos = scrollPos;
        scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Width(widthSidePanel));
        if (oldscrollPos != scrollPos)
        {
            if (audioManager.snapView)
                timelineView.timelinePosition_Offset.y = -scrollPos.y;

        }

        //reset scrollview if shift is pressed
        if (Event.current.shift)
        {
            scrollPos = new Vector2(0, 0);
            timelineView.timelinePosition_Offset.y = 0;
        }

        int halfWidth = (int)(widthSidePanel - 20);

        //Create button style
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.box);
        buttonStyle.fixedWidth = widthSidePanel - 10;
        buttonStyle.fixedHeight = 20;
        buttonStyle.normal.background = colorTextureManager.LoadTexture(TexItemType.Gradient_Vertical, ColorRGBA.grayscale_032, ColorRGBA.grayscale_048, 16);
        buttonStyle.hover.background = colorTextureManager.LoadTexture(TexItemType.Solid, ColorRGBA.grayscale_048, ColorRGBA.grayscale_064, 8);
        buttonStyle.hover.textColor = Color.white;
        //Create button style

        //Create button style
        GUIStyle buttonStyletab = new GUIStyle(GUI.skin.box);
        buttonStyletab.fixedWidth = widthSidePanel - 5;
        buttonStyletab.fixedHeight = 55;
        buttonStyletab.normal.background = colorTextureManager.LoadTexture(TexItemType.Gradient_Vertical, ColorRGBA.grayscale_032, ColorRGBA.grayscale_048, 16);
        buttonStyletab.hover.background = colorTextureManager.LoadTexture(TexItemType.Solid, ColorRGBA.grayscale_048, ColorRGBA.grayscale_064, 8);
        buttonStyletab.hover.textColor = Color.white;


        //Create button style
        GUIStyle buttonStyleicon = new GUIStyle(GUI.skin.box);
        buttonStyleicon.fixedWidth = 20;
        buttonStyleicon.fixedHeight = 25;
        buttonStyleicon.normal.background = colorTextureManager.LoadTexture(TexItemType.Gradient_Vertical, ColorRGBA.grayscale_048, ColorRGBA.grayscale_064, 16);
        buttonStyleicon.hover.background = colorTextureManager.LoadTexture(TexItemType.Solid, ColorRGBA.grayscale_016, ColorRGBA.none, 8);
        buttonStyleicon.hover.textColor = Color.white;

        GUIStyle buttonStyle2 = new GUIStyle(GUI.skin.box);
        buttonStyle2.fixedWidth = widthSidePanel - 10;
        buttonStyle2.fixedHeight = 20;
        buttonStyle2.normal.background = colorTextureManager.LoadTexture(TexItemType.Gradient_Vertical, ColorRGBA.lightgreyred, ColorRGBA.duskred, 16);
        buttonStyle2.margin.right = 0;
        buttonStyle2.padding.right = 0;


        GUIStyle buttonStyleText = new GUIStyle(GUI.skin.box);
        buttonStyleText.fixedWidth = widthSidePanel - 110;
        buttonStyleText.fixedHeight = 25;
        buttonStyleText.normal.background = colorTextureManager.LoadTexture(TexItemType.Solid, ColorRGBA.grayscale_048, ColorRGBA.grayscale_064, 16);
        buttonStyleText.margin.right = 0;
        buttonStyleText.padding.right = 0;

        //General button style
        GUIStyle buttonStyle3 = new GUIStyle(GUI.skin.label);
        buttonStyle3.fixedWidth = widthSidePanel - 10;
        buttonStyle3.fixedHeight = 20;
        //Set background gradient
        buttonStyle3.normal.background = colorTextureManager.LoadTexture(TexItemType.Gradient_Vertical, ColorRGBA.grayscale_025, ColorRGBA.grayscale_032, 16);
        //set highlight gradient
        buttonStyle3.hover.background = colorTextureManager.LoadTexture(TexItemType.Solid, ColorRGBA.grayscale_032, ColorRGBA.grayscale_048, 8);
        //Bold font when hover
        buttonStyle3.hover.textColor = Color.white;


        GUILayout.BeginHorizontal(GUILayout.Width(55));
        //Vertical
        GUILayout.BeginVertical();

        //30 unity spacer
        AudioTrack todelet = null;
        int currentsystime = System.DateTime.Now.Second;
        bool changetrigger = false;
        //loop through all audio tracks
        foreach (var track in audioManager.audioTracks)
        {
            //Horizontal
            GUILayout.BeginVertical(
                //draw a box
                buttonStyletab
            );
            GUILayout.BeginHorizontal();
            //Write 100width label with name
            GUILayout.BeginHorizontal();



            if (GUILayout.Button(track.clip.name, buttonStyleText))
            {
                //Ping the clip in the project window
                EditorGUIUtility.PingObject(track.clip);

            }

            //Inputfield for init time

            float oldinitTime = track.initTime;
            //track.initTime = EditorGUILayout.FloatField("", track.initTime, GUILayout.Width(50));
            GUILayout.EndHorizontal();


            //reload the audio data if the init time has changed
            if (Mathf.Abs(oldinitTime - track.initTime) > 0.001f)
            {
                if (audioManager.autobuild)
                {
                    Render();
                }

            }
            // Icons for buttons
            GUIContent removeIcon = EditorGUIUtility.IconContent("d_P4_DeletedLocal"); // Replace with your icon
            GUIContent reloadIcon = EditorGUIUtility.IconContent("d_RotateTool"); // Replace with your icon
            GUIContent clampIcon = EditorGUIUtility.IconContent("d_AudioMixerSnapshot Icon"); // Replace with your icon
            GUIContent muted = EditorGUIUtility.IconContent("d_scenevis_hidden_hover"); // Replace with your icon
            GUIContent notmuted = EditorGUIUtility.IconContent("d_scenevis_visible_hover"); // Replace with your icon

            if (GUILayout.Button(track.muted ? muted : notmuted, buttonStyleicon))
            {
                track.muted = !track.muted;
            }

            if (GUILayout.Button(removeIcon, buttonStyleicon))
            {
                todelet = track;
            }

            if (GUILayout.Button(reloadIcon, buttonStyleicon))
            {
                track.marked_dirty_normalization = true;
                track.marked_dirty_preview = true;
                track.marked_dirty_time = true;
                track.marked_dirty_settings = true;

                track.checkUpdate();
            }

            if (GUILayout.Button(clampIcon, buttonStyleicon))
            {
                track.initTime = 0;
            }





            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            float oldtargetgain = track.targetgain;

            track.targetgain = GUILayout.HorizontalSlider(track.targetgain, 0, 3, GUILayout.Width(widthSidePanel - 85));
            //Label with red background

            GUILayout.Label(track.targetgain.ToString("0.00"), buttonStyle3);

            if (Mathf.Abs(oldtargetgain - track.targetgain) > 0.001f)
            {
                if (audioManager.autobuild)
                {
                    if (!change)
                    {
                        //track.oldtargetgain = track.targetgain;
                        changetime = System.DateTime.Now.Second;
                        change = true;
                    }

                    changetrigger = true;

                }
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        //Add Slider for targetgain of the track


        if (todelet != null)
        {
            audioManager.audioTracks.Remove(todelet);
        }

        GUILayout.EndVertical();


        GUILayout.EndHorizontal();
        GUILayout.Space(15);

        int index2 = targetfolder.IndexOf("Assets");
        string   folderpathrelative = targetfolder;
        if (index2 >= 0)
        {
            folderpathrelative=targetfolder.Substring(index2);
            GUILayout.Label("\\" + folderpathrelative);
        } 
                
        GUILayout.Label(targetfolder);

        if (GUILayout.Button("" + filename, buttonStyle))
        {
            string relativePath= targetfolder.Replace(Application.dataPath, "Assets");
            //ping the json file if it exists in the target folder
            if (File.Exists(targetfolder + "/" + filename + "_autosave.json"))
            {
                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(folderpathrelative + "/" + filename + "_autosave.json"));
            }
            else if (File.Exists(targetfolder + "/" + filename + ".json"))
            {
                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(folderpathrelative + "/" + filename + ".json"));
            }
        }


        //30 spacer
        GUILayout.Space(15);


        if (audioClip != null)
        {
            AudioTrack newAudioTrack = new AudioTrack(audioClip);
            audioManager.addAudioTrack(newAudioTrack);
            audioManager.reloadAudioData();
            audioClip = null;
        }


        //button to find the output folder
        if (GUILayout.Button("Ping Folder", buttonStyle))
        {
            string relativepath = targetfolder.Replace(Application.dataPath, "Assets");

            if (Directory.Exists(targetfolder))
            {
                int index = targetfolder.IndexOf("Assets");
                relativepath = targetfolder.Substring(index);

                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(relativepath));

            }
        }

        //button to find the file in the project window
        if (GUILayout.Button("Ping Render", buttonStyle))
        {
            //get relative path
            string relativepath = targetfolder.Replace(Application.dataPath, "Assets");
            //use substring and index of
            int ind = targetfolder.IndexOf("Assets");
            if (ind >= 0)
            {
                //cut the string
                relativepath = targetfolder.Substring(ind);
            }

            //check if path is valid


            if (Directory.Exists(targetfolder))
            {
                //focus in project window
                //check if file exists
                if (audioManager.autobuild)
                {
                    if (File.Exists(targetfolder + "/" + filename + "_auto.wav"))
                    {
                        //focus in project window
                        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(relativepath + "/" + filename + "_auto.wav"));
                    }
                    else if (File.Exists(targetfolder + "/" + filename + ".wav"))
                    {
                        //focus in project window
                        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(relativepath + "/" + filename + ".wav"));
                    }

                }
                else
                if (File.Exists(targetfolder + "/" + filename + ".wav"))
                {
                    //focus in project window
                    EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(relativepath + "/" + filename + ".wav"));
                }
            }
        }

        //Get current startup time 

        if (change && audioManager.autobuild)
        {
            if (!changetrigger)
            {
                if (currentsystime > changetime + 1)
                {
                    if (audioManager.autosave)
                    {
                        //Check if folder exists
                        if (Directory.Exists(targetfolder))
                        {
                            audioManager.SaveJson(targetfolder + "/" + filename + "_autosave.json");
                        }
                    }
                    audioManager.createMix(targetfolder, filename + "_auto");
                    change = false;

                }


            }
            else
            {
                changetime = currentsystime;
            }

        }


        //End Scrollview
        GUILayout.EndScrollView();
        GUILayout.EndVertical();

    }

}
//----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
//TIMELINE CLASS

//----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
//AUDIOMANAGER

