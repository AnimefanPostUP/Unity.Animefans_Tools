

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

    //Create window
    [MenuItem("Animtools/Audio Timeline")]
    public static void ShowWindow()
    {
        GetWindow<Audiotimeline>("Audio Timeline");
    }


    //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    //Unity Calls

    private void OnEnable()
    {
        init_audiomanager();
        //Init color texture manager


        colorTextureManager.CacheFolder = CacheFolder;


        dropdownMenu = new DropdownMenu("Mainmenu", colorTextureManager, new Vector2(5, 5));
        setDropdownTextures(dropdownMenu);
        dropdownMenu.AddItem("New", () => { New(); });
        dropdownMenu.AddItem("Load Json", () => { LoadFrom(); });
        dropdownMenu.AddItem("Quick Save", () => { Save(); });
        dropdownMenu.AddItem("Save As", () => { SaveAt(); });
        dropdownMenu.AddItem("Create Backup", () => { SaveBackup(); });
        dropdownMenu.AddItem("Quick Render", () => { Render(); });
        dropdownMenu.AddItem("Render As", () => { RenderWavAs(); });
        dropdownMenu.AddItem("Audiosettings", () => { setRendersettings(); });
        dropdownMenu.AddItem("Animationsynch", false);

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

        if (File.Exists(targetfolder + "/" + _filename + ".json"))
        {
            filename = _filename;
        }
        else { checkFile = false; }

        if (checkFile)
        {
            audioManager.LoadJson(targetfolder + "/" + filename + ".json");
        }
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

        //Audiologic
        bool synch = dropdownMenu.getValue("Animationsynch");
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
            timelineView.timelinezoom.x -= (Event.current.delta.y * 4);

            //Clamp value between 1 and 1000
            timelineView.timelinezoom.x = Mathf.Clamp(timelineView.timelinezoom.x, 1, 1000);

            //use Event
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


        Texture2D buttonTexture = colorTextureManager.LoadTexture(TexItemType.Gradient_Vertical, ColorRGBA.duskred, ColorRGBA.darkred, 8);
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
        menu.itemButtonActive = new ColorTextureManager.ColorTextureDummy(TexItemType.Gradient_Radial, ColorRGBA.lightred, ColorRGBA.grayscale_025, 8);
        menu.itemButtonNormal = new ColorTextureManager.ColorTextureDummy(TexItemType.Solid, ColorRGBA.grayscale_025, ColorRGBA.none, 8);
        menu.itemButtonHover = new ColorTextureManager.ColorTextureDummy(TexItemType.Gradient_Radial, ColorRGBA.lightgreyred, ColorRGBA.grayscale_025, 8);

        menu.mainButtonNormal = new ColorTextureManager.ColorTextureDummy(TexItemType.Gradient_Vertical, ColorRGBA.duskred, ColorRGBA.darkred, 32);
        menu.mainButtonHover = new ColorTextureManager.ColorTextureDummy(TexItemType.Solid, ColorRGBA.lightgreyred, ColorRGBA.darkred, 8);
        menu.mainButtonActive = new ColorTextureManager.ColorTextureDummy(TexItemType.Gradient_Radial, ColorRGBA.lightgreyred, ColorRGBA.grayscale_064, 8);

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
                    dropdownMenu.setValue("Animationsynch", false);
                }
            }

            float currentTime = GetCurrentTime();

            //Set timeline playback to the current time
            timelineView.displayPlayback = true;
            audioManager.autobuild = true;
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
            audioManager.autobuild = false;

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




        CustomPopup.ShowWindow((samplerate, bitrate, channels, doNormalizeInput, normalizeThreshold, doNormalizeOutput, normalizeOutputThreshold, targetgain_In, targetgain_Out) =>
        {
            audioManager.renderSettings(samplerate, bitrate, channels, doNormalizeInput, normalizeThreshold, doNormalizeOutput, normalizeOutputThreshold, targetgain_In, targetgain_Out);
        }, existingSampleRate, existingBitDepth, existingChannels, setting_doNormalizeInput, setting_normalizeInput, setting_doNormalizeOutput, setting_normalizeOutput,
        targetgain_In, targetgain_Out);


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

            //If file exists
            if (File.Exists(targetfolder + "/" + filename + ".wav"))
            {
                //Open dialog for yes or no
                if (EditorUtility.DisplayDialog("File Exists", "Do you want to overwrite the file?", "Yes", "No"))
                {
                    audioManager.createMix(targetfolder, filename);
                }
            }
            else
            {
                audioManager.createMix(targetfolder, filename);
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

        Debug.Log(audioUtilClass);

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
            Debug.Log("Stopped");
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

                Debug.Log("Playing");
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

                Debug.Log("Playing" + GetCurrentTime());
            }
            else
            {
                isPlaying = false;
                Debug.Log("No Clip Found");
            }
        }
    }

    private AudioClip getMixClip()
    {
        if (firstTime)
        {
            firstTime = false;
            audioManager.createMix(targetfolder, "LiveMixTemp");
        }

        //find LiveMixTemp.wav in the folderpath
        string path = targetfolder + "/" + "LiveMixTemp" + ".wav";
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
        scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Width(widthSidePanel));


        int halfWidth = (int)(widthSidePanel - 20);

        //Create button style
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.box);
        buttonStyle.fixedWidth = widthSidePanel - 10;
        buttonStyle.fixedHeight = 20;
        buttonStyle.normal.background = colorTextureManager.LoadTexture(TexItemType.Gradient_Vertical, ColorRGBA.duskred, ColorRGBA.darkred, 16);
        buttonStyle.hover.background = colorTextureManager.LoadTexture(TexItemType.Solid, ColorRGBA.lightgreyred, ColorRGBA.darkred, 8);
        buttonStyle.hover.textColor = Color.white;
        //Create button style

        //Create button style
        GUIStyle buttonStyletab = new GUIStyle(GUI.skin.box);
        buttonStyletab.fixedWidth = widthSidePanel - 5;
        buttonStyletab.fixedHeight = 55;
        buttonStyletab.normal.background = colorTextureManager.LoadTexture(TexItemType.Gradient_Vertical, ColorRGBA.duskred, ColorRGBA.darkred, 16);
        buttonStyletab.hover.background = colorTextureManager.LoadTexture(TexItemType.Solid, ColorRGBA.lightgreyred, ColorRGBA.darkred, 8);
        buttonStyletab.hover.textColor = Color.white;


        //Create button style
        GUIStyle buttonStyleicon = new GUIStyle(GUI.skin.box);
        buttonStyleicon.fixedWidth = 25;
        buttonStyleicon.fixedHeight = 25;
        buttonStyleicon.normal.background = colorTextureManager.LoadTexture(TexItemType.Gradient_Vertical, ColorRGBA.duskred, ColorRGBA.darkred, 16);
        buttonStyleicon.hover.background = colorTextureManager.LoadTexture(TexItemType.Solid, ColorRGBA.lightgreyred, ColorRGBA.darkred, 8);
        buttonStyleicon.hover.textColor = Color.white;

        GUIStyle buttonStyle2 = new GUIStyle(GUI.skin.box);
        buttonStyle2.fixedWidth = widthSidePanel - 10;
        buttonStyle2.fixedHeight = 20;
        buttonStyle2.normal.background = colorTextureManager.LoadTexture(TexItemType.Gradient_Vertical, ColorRGBA.darkred, ColorRGBA.duskred, 16);
        buttonStyle2.margin.right = 0;
        buttonStyle2.padding.right = 0;

        //General button style
        GUIStyle buttonStyle3 = new GUIStyle(GUI.skin.label);
        buttonStyle3.fixedWidth = widthSidePanel - 10;
        buttonStyle3.fixedHeight = 20;
        //Set background gradient
        buttonStyle3.normal.background = colorTextureManager.LoadTexture(TexItemType.Gradient_Vertical, ColorRGBA.duskred, ColorRGBA.darkred, 16);
        //set highlight gradient
        buttonStyle3.hover.background = colorTextureManager.LoadTexture(TexItemType.Solid, ColorRGBA.lightgreyred, ColorRGBA.darkred, 8);
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
            GUILayout.Label(track.clip.name, GUILayout.Width(
                Mathf.Max(
                widthSidePanel - 105, 5)
            ));

            //Inputfield for init time

            float oldinitTime = track.initTime;
            //track.initTime = EditorGUILayout.FloatField("", track.initTime, GUILayout.Width(50));
            GUILayout.EndHorizontal();


            //reload the audio data if the init time has changed
            if (Mathf.Abs(oldinitTime - track.initTime) > 0.001f)
            {
                if (audioManager.autobuild)
                    track.LoadAudioData();
            }
            // Icons for buttons
            GUIContent removeIcon = EditorGUIUtility.IconContent("d_P4_DeletedLocal"); // Replace with your icon
            GUIContent reloadIcon = EditorGUIUtility.IconContent("d_RotateTool"); // Replace with your icon
            GUIContent clampIcon = EditorGUIUtility.IconContent("d_AudioMixerSnapshot Icon"); // Replace with your icon


            if (GUILayout.Button(removeIcon, buttonStyleicon))
            {
                todelet = track;
            }

            if (GUILayout.Button(reloadIcon, buttonStyleicon))
            {
                track.LoadAudioData();
            }

            if (GUILayout.Button(clampIcon, buttonStyleicon))
            {
                track.initTime = 0;
            }





            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            float oldtargetgain = track.targetgain;

            track.targetgain = GUILayout.HorizontalSlider(track.targetgain, 0, 1, GUILayout.Width(widthSidePanel - 85));
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

        int index = targetfolder.IndexOf("Assets");
        if (index >= 0)
        {
            GUILayout.Label("\\" + targetfolder.Substring(index));
        }

        GUILayout.Label("" + filename);

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
                //focus in project window
                //check if file exists
                if (File.Exists(targetfolder + "/" + filename + ".json"))
                {
                    //focus in project window
                    EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(relativepath));
                }
            }
        }

        //button to find the file in the project window
        if (GUILayout.Button("Ping Render", buttonStyle))
        {
            //get relative path
            string relativepath = targetfolder.Replace(Application.dataPath, "Assets");

            //check if path is valid


            if (Directory.Exists(targetfolder))
            {
                //focus in project window
                //check if file exists
                if (File.Exists(targetfolder + "/" + filename + ".wav"))
                {
                    //focus in project window
                    EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(relativepath + "/" + filename + ".wav"));
                }
            }
        }

        //Get current startup time 

        if (change)
        {
            if (!changetrigger)
            {
                if (currentsystime > changetime + 1)
                {
                    audioManager.createMix(targetfolder, "LiveMixTemp");
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

//Class to manage and draw a timeline
public class TimelineView
{

    //Variables for Max Sizes, Track count and Track Height and a List of Elements of "TimelineTrack" aswell as the Position of the Scrollview
    public float maxTime = 100;
    public int trackCount => audiotrackmgr.audioTracks.Count;
    public float trackHeight = 150;
    public Vector2 timelinePosition = new Vector2(0, 0);

    public Vector2 timelinePosition_Offset = new Vector2(0, 0);
    public Vector2 timelinezoom = new Vector2(100, 100);

    public int grabbedElement = -1;

    public float mousePositionX = 0;

    public float trackHeightOffset = 57;

    AudiotrackManager audiotrackmgr;

    public bool displayPlayback = false;
    public float playbackPosition = 0;

    //String for Projectfile

    private static Color GetRGBA(ColorRGBA color)
    {
        return SmartColorUtility.GetRGBA(color);
    }

    //Constructor
    public TimelineView(float maxTime, float trackHeight, AudiotrackManager audiotrackmgr)
    {
        this.maxTime = maxTime;
        this.trackHeight = trackHeight;
        this.audiotrackmgr = audiotrackmgr;
    }

    //Draw the Timeline
    public bool DrawTimeline(AudiotrackManager amgr, float widthTimeline, float xPos, ColorTextureManager colormgr, Event current, string targetfolder, string filename)
    {
        //create style for box
        GUIStyle boxstylebg = new GUIStyle(GUI.skin.box);
        boxstylebg.normal.background = colormgr.LoadTexture(TexItemType.Gradient_Vertical, ColorRGBA.darkgrey, ColorRGBA.grayscale_016, 128);
        GUI.Box(new Rect(xPos, 0, xPos + widthTimeline, 1000), "", boxstylebg);

        bool doRepaint = false;
        bool doOverlay = false;


        //set grab element to -1 if mouse is up or position is out of bounds of widthTimeline and Xpos or height with a margin of 10
        if (grabbedElement != -1 && (current.type == EventType.MouseUp || current.mousePosition.x < xPos || current.mousePosition.x > xPos + widthTimeline || current.mousePosition.y < 0 || current.mousePosition.y > trackHeight * (trackCount + 10)))
        {
            grabbedElement = -1;
            //Mix the audio data
            if (audiotrackmgr.audioTracks.Count > 0 && amgr.autobuild)
            {
                //amgr.createMix(targetfolder, filename + ".wav");
                amgr.createMix(targetfolder, "LiveMixTemp");
                AssetDatabase.Refresh();
            }
        }
        else
        {

            //Check if its a File Drop Event
            if (current.type == EventType.DragUpdated || current.type == EventType.DragPerform)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                //Draw lightlightbox to display a Drop Position
                EditorGUI.DrawRect(new Rect(current.mousePosition.x - 10, current.mousePosition.y - 10, 20, 20), GetRGBA(ColorRGBA.grayscale_144));

                if (current.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();

                    foreach (var draggedObject in DragAndDrop.objectReferences)
                    {
                        if (draggedObject is AudioClip)
                        {
                            //check if file is .wav
                            if (Path.GetExtension(AssetDatabase.GetAssetPath(draggedObject)) == ".wav" || Path.GetExtension(AssetDatabase.GetAssetPath(draggedObject)) == ".mp3")
                            {
                                AudioTrack newAudioTrack = new AudioTrack(draggedObject as AudioClip);
                                audiotrackmgr.addAudioTrack(newAudioTrack);
                            }
                        }
                    }
                }
            }

        }

        //if theres a grabbed element then move it to the current mouse position
        if (grabbedElement != -1)
        {

            //if control i pressed limit the movement to 10 unitys
            if (current.control)
            {
                audiotrackmgr.audioTracks[grabbedElement].initTime = ((current.mousePosition.x - mousePositionX - xPos) - ((current.mousePosition.x - mousePositionX - xPos) % 10f)) / timelinezoom.x;
            }
            else if (current.shift)
            {

                audiotrackmgr.audioTracks[grabbedElement].initTime = ((current.mousePosition.x - mousePositionX - xPos) - ((((audiotrackmgr.audioTracks[grabbedElement].clip.length * 100) % 10) + current.mousePosition.x - mousePositionX - xPos) % 10f)) / timelinezoom.x;
            }
            else if (current.alt)
            {
                //no function yet
            }
            else
            {
                audiotrackmgr.audioTracks[grabbedElement].initTime = (current.mousePosition.x - mousePositionX - xPos) / timelinezoom.x;
            }
            //audiotrackmgr.audioTracks[grabbedElement].LoadAudioData();
            doRepaint = true;
        }
        else
        {

            //if control is pressed move timetime to the left by setting the timeline position
            if (current.alt)
            {

                doOverlay = true;


                timelinePosition_Offset.x += (xPos + (widthTimeline) / 2 - current.mousePosition.x) / 200f;
                timelinePosition_Offset.y += ((500) / 2 - current.mousePosition.y) / 200f;
                //maxTime * timelinezoom.x
                timelinePosition_Offset.x = (float)Mathf.Clamp(timelinePosition_Offset.x, -(maxTime * timelinezoom.x), (maxTime * timelinezoom.x));
                doRepaint = true;
            }

            //reset if shift
            if (current.shift)
            {
                timelinePosition_Offset.x = 0;
                timelinePosition_Offset.y = 0;
                doRepaint = true;

            }


        }





        //Draw gray background 
        //EditorGUI.DrawRect(new Rect(0, 0, maxTime*100, trackHeight * trackCount), GetRGBA(ColorRGBA.grayscale_016));
        GUILayout.BeginArea(new Rect(xPos + timelinePosition_Offset.x, timelinePosition_Offset.y, maxTime * timelinezoom.x, (trackHeight + 10) * (trackCount + 10)));

        timelinePosition = GUILayout.BeginScrollView(timelinePosition, GUILayout.Width((int)(maxTime * 10) * timelinezoom.x), GUILayout.Height(trackCount * (trackHeight * 2 + 2)));
        GUILayout.BeginHorizontal(GUILayout.Width(maxTime * timelinezoom.x));

        GUIStyle boxstyle = new GUIStyle(GUI.skin.box);
        boxstyle.normal.background = colormgr.LoadTexture(TexItemType.Gradient_Horizontal, ColorRGBA.lightred, ColorRGBA.brightred, 32);
        boxstyle.hover.background = colormgr.LoadTexture(TexItemType.Gradient_Horizontal, ColorRGBA.lightgreyred, ColorRGBA.brightred, 32);




        //Draw a line for the timeline every 10 unitys
        for (int i = 0; i < maxTime; i++)
        {


            if (i % 2 == 0 || i % 10 == 0)
            {
                EditorGUI.DrawRect(new Rect(i * timelinezoom.x / 10, 0, timelinezoom.x / 10, (trackHeight + 10) * (trackCount + 10)), GetRGBA(ColorRGBA.grayscale_032));
            }
            else EditorGUI.DrawRect(new Rect(i * timelinezoom.x / 10, 0, timelinezoom.x / 10, (trackHeight + 10) * (trackCount + 10)), GetRGBA(ColorRGBA.grayscale_025));

            if (i % 10 == 0)
            {
                //Draw Label with time
                EditorGUI.DrawRect(new Rect(i * timelinezoom.x / 10, 0, 1, (trackHeight + 10) * (trackCount + 10)), GetRGBA(ColorRGBA.red));
            }
        }

        //Draw line on playbackPosition
        if (displayPlayback)
        {
            EditorGUI.DrawRect(new Rect(playbackPosition * timelinezoom.x, 0, 2, (trackHeight + 10) * (trackCount + 10)), GetRGBA(ColorRGBA.orange));
        }

        //Draw the audiomanager.buildmix waveform
        if (amgr.buildTrackpreview != null)
        {
            //Draw Image / Waveform
            //Check if first pixel is white
            if (amgr.displayPreview)
            {
                EditorGUI.DrawPreviewTexture(new Rect(0, 0, amgr.previewLength * timelinezoom.x, trackHeight + 3), amgr.buildTrackpreview, null, ScaleMode.StretchToFill);
                //Add a Button to the Start to X
                if (GUI.Button(new Rect(4, 18, 20, 20), "X"
                , new GUIStyle() { normal = new GUIStyleState() { textColor = Color.grey } }
                ))
                {
                    amgr.displayPreview = false;
                }
            }

        }


        //draw boxes for tracks
        foreach (int i in Enumerable.Range(0, audiotrackmgr.audioTracks.Count))
        {
            //Create style with horizontal gradient

            //continue if position x is below 0


            float _x = audiotrackmgr.audioTracks[i].initTime * timelinezoom.x;
            float _y = trackHeightOffset + i * (trackHeight + 10);
            float _width = audiotrackmgr.audioTracks[i].clip.length * timelinezoom.x;
            float _height = trackHeight;

            boxstyle.fixedWidth = _width;
            boxstyle.fixedHeight = _height;

            //Rect for the box
            Rect rect = new Rect(_x, _y, _width, _height);

            //check if mouse is over the box
            if (rect.Contains(Event.current.mousePosition))
            {
                EditorGUI.DrawRect(rect, GetRGBA(ColorRGBA.brightred));
                doRepaint = true;
                //if mousedown
                if (current.type == EventType.MouseDown)
                {
                    //set the grabbed element to the current element
                    grabbedElement = i;

                    //set the mouse position and remove the offset relative to the box
                    mousePositionX = current.mousePosition.x - _x + timelinePosition_Offset.x;
                    //use the event
                    doRepaint = true;
                }

            }
            else
            {
                GUI.Box(rect, "", boxstyle);
            }



            float maxvalue = Mathf.Pow(2, audiotrackmgr.audioTracks[i]._targetBitDepth) - 1;


            // Draw the Waveform using the bit depth relatively and create a box
            float resolution = 250 * audiotrackmgr.audioTracks[i].clip.length * timelinezoom.x / 1000;



            // Calculate the width of each box
            float boxWidth = _width / resolution;

            //Draw Image / Waveform
            if (audiotrackmgr.audioTracks[i].previewImage != null)
                EditorGUI.DrawPreviewTexture(new Rect(_x, _y + 20, _width, Math.Max(_height - 20, 1)), audiotrackmgr.audioTracks[i].previewImage, null, ScaleMode.StretchToFill);


            EditorGUI.LabelField(new Rect(_x + 2, _y, Math.Max(_width - 5, 1), 20), audiotrackmgr.audioTracks[i].clip.name
          //Make color black and bold
          , new GUIStyle() { normal = new GUIStyleState() { textColor = GetRGBA(ColorRGBA.darkred) }, fontStyle = FontStyle.Bold, fontSize = 12, clipping = TextClipping.Clip }
          );

        }

        //Draw a line for the timeline every 10 unitys
        for (int i = 0; i < maxTime * timelinezoom.x; i++)
        {

            if (i % 10 == 0)
                EditorGUI.LabelField(new Rect(1 + (i * timelinezoom.x / 10), 2, 25, 14), (i / 10).ToString());

            //If this is the grabbed element draw a the Star and end Time
            if (grabbedElement == i)
            {
                //Draw Box behind the label
                //EditorGUI.DrawRect(new Rect(audiotrackmgr.audioTracks[i].initTime * 100, i * (trackHeight + 10) + 40, trackHeightOffset + audiotrackmgr.audioTracks[i].clip.length * 100, 30), GetRGBA(ColorRGBA.grayscale_064));

                EditorGUI.LabelField(new Rect(audiotrackmgr.audioTracks[i].initTime * timelinezoom.x, trackHeightOffset + i * (trackHeight + 10) + 25, 100, 12), Math.Round(audiotrackmgr.audioTracks[i].initTime, 2).ToString()
                , new GUIStyle() { normal = new GUIStyleState() { textColor = Color.white }, fontStyle = FontStyle.Bold, fontSize = 14 }
                );

                EditorGUI.LabelField(new Rect(audiotrackmgr.audioTracks[i].initTime * timelinezoom.x + (int)(audiotrackmgr.audioTracks[i].clip.length * 0.92) * timelinezoom.x, trackHeightOffset + i * (trackHeight + 10) + 25, 100, 12),
                (Math.Round(audiotrackmgr.audioTracks[i].initTime + audiotrackmgr.audioTracks[i].clip.length, 2)).ToString()
                , new GUIStyle() { normal = new GUIStyleState() { textColor = Color.white }, fontStyle = FontStyle.Bold, fontSize = 14 }
                );
            }
        }



        GUILayout.EndHorizontal();
        GUILayout.EndScrollView();

        GUILayout.EndArea();


        if (doOverlay)
        {
            //Draw 2 White Boxes to indicate the position
            EditorGUI.DrawRect(new Rect(xPos + (widthTimeline) / 2 - 1, 0, 2, 900), GetRGBA(ColorRGBA.white));
            EditorGUI.DrawRect(new Rect(0, (190) - 1, 2000, 2), GetRGBA(ColorRGBA.white));
        }

        return doRepaint;
    }


}

//----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
//AUDIOMANAGER
public class AudiotrackManager
{

    public List<AudioTrack> audioTracks;

    //Target Settings
    public int targetSampleRate = 44100; // Target sample rate in Hz
    public int targetBitDepth = 16; // Target bit depth in bits
    public int targetChannels = 2; // Target number of channels
    public bool autobuild = false;
    public Texture2D buildTrackpreview = new Texture2D(2048, 255);
    public float previewLength = 0;
    public bool displayPreview = false;
    public float setting_normalizationFac_Input = 1.0f;
    public float setting_normalizationFac_Output = 0.5f;
    public bool setting_doNormalizeInput = true;
    public bool setting_doNormalizeOutput = false;



    //targetgain
    public float targetgain_In = 0.6f;

    public float targetgain_Out = 0.8f;

    //init  
    public AudiotrackManager()
    {
        audioTracks = new List<AudioTrack>();
    }

    public void reloadAudioData()
    {
        foreach (var track in audioTracks)
        {
            track._targetSampleRate = targetSampleRate;
            track._targetBitDepth = targetBitDepth;
            track._targetChannels = targetChannels;
            track.LoadAudioData();
        }
    }

    public void renderSettings(int samplerate, int bitrate, int channels, bool normalizeInput, float normalizationFacInput, bool normalizeOutput, float normalizationFacOutput, float targetgain_In, float targetgain_Out)
    {
        setting_normalizationFac_Input = normalizationFacInput;
        setting_normalizationFac_Output = normalizationFacOutput;
        setting_doNormalizeInput = normalizeInput;
        setting_doNormalizeOutput = normalizeOutput;

        targetSampleRate = samplerate;
        targetBitDepth = bitrate;
        targetChannels = channels;

        this.targetgain_In = targetgain_In;
        this.targetgain_Out = targetgain_Out;


        reloadAudioData();
    }

    //addAudiotrack
    public void addAudioTrack(AudioTrack track)
    {
        audioTracks.Add(track);
        //Set the target settings
        track._targetSampleRate = targetSampleRate;
        track._targetBitDepth = targetBitDepth;
        track._targetChannels = targetChannels;
    }

    //Remove
    public void removeAudioTrack(AudioTrack track)
    {
        audioTracks.Remove(track);
    }

    public void checkForJson(string filePath, string filename)
    {
        //Try to find the json file in the current path using the filename
        if (File.Exists(filePath + "/" + filename + ".json"))
        {
            //Load the json file
            LoadJson(filePath + "/" + filename + ".json");
        }
    }

    //AudioData Class for storing Json Data
    [Serializable]
    public class AudioData
    {
        public string[] ClipPaths;
        public float[] InitTimes;
        public float[] targetgains;
        public string OutputFilePath;
        public string OutputFileName;

    }

    //Load Json
    public void LoadJson(string path)
    {
        // Load the Json File
        string json = File.ReadAllText(path);
        AudioData audioData = JsonUtility.FromJson<AudioData>(json);

        // Clear the audioTracks list
        audioTracks.Clear();

        // Try to find and add the Clips
        for (int i = 0; i < audioData.ClipPaths.Length; i++)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(audioData.ClipPaths[i]);
            if (clip != null)
            {
                AudioTrack audioTrack = new AudioTrack(clip);
                if (audioData.InitTimes.Length > i)
                    audioTrack.initTime = audioData.InitTimes[i];
                if (audioData.targetgains != null)
                    if (audioData.targetgains.Length > i)
                        audioTrack.targetgain = audioData.targetgains[i];
                audioTracks.Add(audioTrack);
            }
        }
    }

    public void createMix(string filePath, string filename)
    {
        //Load all audio data
        //reloadAudioData();
        byte[][] mixedData = MixAudioData();

        if (setting_doNormalizeOutput)
        {
            int maxvalue = (int)Math.Pow(2, targetBitDepth) - 1;
            for (int i = 0; i < mixedData.Length; i++)
                mixedData[i] = Normalize(mixedData[i], targetBitDepth, targetBitDepth > 8, setting_normalizationFac_Output * maxvalue, setting_normalizationFac_Output);
        }

        int samples = getMixLength(0) / (targetBitDepth / 8);
        previewLength = (float)samples / targetSampleRate;
        AudioTrack.DrawWaveform(AudioTrack.getFloatArrayFromSamples(targetBitDepth, mixedData, targetChannels, samples), buildTrackpreview.width, buildTrackpreview.height, buildTrackpreview);
        this.displayPreview = true;

        CreateWaveFile(mixedData, filePath, filename + ".wav", targetSampleRate, targetChannels, targetBitDepth);

        // Reload the Folder
        AssetDatabase.Refresh();
    }

    //Autosave
    public void formattedSave(string filePath, string filename)
    {

        string date = DateTime.Now.ToString("yyyy-MM-dd_HH-mm");
        SaveJson(filePath + "/" + date + "_" + filename + "_.json");
    }

    public void autocleanup(string filePath, string filename)
    {
        // Delete the one with the oldest date if there's more than 5
        // Get all files in the directory
        string[] files = Directory.GetFiles(filePath, "*.json");

        // Filter by containing the filename
        files = files.Where(x => x.Contains("_" + filename + "_")).ToArray();

        // Sort files by creation time in descending order
        Array.Sort(files, (x, y) => File.GetCreationTime(y).CompareTo(File.GetCreationTime(x)));

        // Group files by day
        var filesGroupedByDay = files.GroupBy(x => File.GetCreationTime(x).Date);

        foreach (var group in filesGroupedByDay)
        {
            // For each day, keep the file with the latest creation time
            var filesToKeep = group.OrderByDescending(x => File.GetCreationTime(x)).Take(1);

            // For the current day, also keep the latest file of each hour
            if (group.Key.Date == DateTime.Today)
            {
                filesToKeep = filesToKeep.Concat(group.GroupBy(x => File.GetCreationTime(x).Hour)
                    .Select(g => g.OrderByDescending(x => File.GetCreationTime(x)).First()));
            }

            // Delete all other files
            foreach (var file in group.Except(filesToKeep))
            {
                File.Delete(file);
            }
        }
    }

    public void SaveJson(string filePath)
    {

        AudioData audioData = new AudioData
        {
            ClipPaths = new string[audioTracks.Count],
            InitTimes = new float[audioTracks.Count],
            OutputFilePath = filePath,
            OutputFileName = Path.GetFileNameWithoutExtension(filePath)
        };

        for (int i = 0; i < audioTracks.Count; i++)
        {
            audioData.ClipPaths[i] = AssetDatabase.GetAssetPath(audioTracks[i].clip);
            audioData.InitTimes[i] = audioTracks[i].initTime;
            audioData.targetgains[i] = audioTracks[i].targetgain;
        }

        // Store clips in json
        string json = JsonUtility.ToJson(audioData);

        // Write the Json File
        File.WriteAllText(filePath, json);


        // Write the Json File
        File.WriteAllText(filePath, json);

    }

    public int getMixLength(int channel) //Gets the length per channel based on the longest clip
    {
        int maxLength = 0;
        float maxtime = 0;
        foreach (var audiotrack in audioTracks)
        {
            if (Math.Round((audiotrack.clip.length + (audiotrack.initTime)), 3) > maxtime)
            {
                maxtime = (float)Math.Round((audiotrack.clip.length + (audiotrack.initTime)), 3);
                maxLength = (int)Math.Round(((audiotrack.clip.length + (audiotrack.initTime)) * audiotrack._targetSampleRate * (targetBitDepth / 8)));
            }
        }
        return maxLength;
    }

    public byte[][] MixAudioData()
    {
        byte[][][] mixedData = new byte[2][][]; //CHANNEL TRACK BYTES

        //set amount of tracks
        for (int ch = 0; ch < targetChannels; ch++)
        {
            mixedData[ch] = new byte[audioTracks.Count][];

            //Set amount of bytes
            for (int i = 0; i < audioTracks.Count; i++)
            {
                int length = getMixLength(ch);
                //Debug
                Debug.Log("Order Tracks Input CH" + ch + " Bytes:" + length);
                mixedData[ch][i] = new byte[length];
            }
        }


        int bytedepth = (targetBitDepth / 8);

        for (int ch = 0; ch < targetChannels; ch++)
        {
            for (int i = 0; i < audioTracks.Count; i++) //Iterate Tracks
            {

                int trackstartsample = (int)(audioTracks[i].initTime * targetSampleRate);
                int trackendsample = (int)((trackstartsample) + ((audioTracks[i].clip.length) * targetSampleRate));

                //Initialize all with 0
                for (int b = 0; b < mixedData[ch][i].Length; b++)
                {
                    mixedData[ch][i][b] = 0;
                }

                for (int s = 0; s < mixedData[ch][i].Length; s = s + bytedepth) //Iterate Samples
                {
                    if (s >= trackstartsample)
                    {
                        for (int byteIndex = 0; byteIndex < bytedepth; byteIndex++) //Iterate Bytes
                        {
                            int current_source_Index = (int)((s) + byteIndex) - (trackstartsample * bytedepth);
                            int current_dest_Index = (int)((s) + byteIndex);

                            //Logical Check for Limit
                            if (current_source_Index >= 0 && current_dest_Index >= 0)
                                if (ch < audioTracks[i].audioData.Length &&
                                    i < audioTracks.Count &&
                                    ch < mixedData.Length &&
                                    i < mixedData[ch].Length &&
                                    i < audioTracks[i].audioData[ch].Length &&
                                    current_source_Index < audioTracks[i].audioData[ch].Length &&
                                    current_dest_Index < mixedData[ch][i].Length)
                                {
                                    mixedData[ch][i][current_dest_Index] = audioTracks[i].audioData[ch][current_source_Index];
                                }
                        }

                    }

                }
            }
        }

        ReportAudioData(mixedData[0][0], "Collected CH1");
        ReportAudioData(mixedData[1][0], "Collected CH2");

        return MixAudioBytesSimple(mixedData, targetBitDepth);
    }

    // Combine all audio tracks into one byte array
    //Combine all audio tracks into one byte array

    public byte[] Normalize(byte[] audioData, int bitDepth, bool considerSignBit, float targetMax, float strength)
    {
        int bytesPerSample = bitDepth / 8;
        float[] audioDataFloat = new float[audioData.Length / bytesPerSample];
        for (int i = 0; i < audioDataFloat.Length; i++)
        {
            int value = 0;
            for (int j = 0; j < bytesPerSample; j++)
            {
                value |= (audioData[i * bytesPerSample + j] & 0xFF) << (j * 8);
            }
            if (considerSignBit && (value & (1 << (bitDepth - 1))) != 0)
            {
                value |= ~((1 << (bitDepth - 1)) - 1); // Extend the sign bit
            }
            audioDataFloat[i] = value;
        }

        float currentMax = audioDataFloat.Max(Math.Abs);
        float normalizationFactor = 1 + strength * (targetMax / currentMax - 1);
        for (int i = 0; i < audioDataFloat.Length; i++)
        {
            audioDataFloat[i] *= normalizationFactor;
        }

        // Convert back to byte array
        byte[] normalizedAudioData = new byte[audioData.Length];
        for (int i = 0; i < audioDataFloat.Length; i++)
        {
            int value = (int)audioDataFloat[i];
            for (int j = 0; j < bitDepth / 8; j++)
            {
                normalizedAudioData[i * bitDepth / 8 + j] = (byte)(value >> (j * 8));
            }
        }

        return normalizedAudioData;
    }


    public byte[][] MixAudioBytesSimple(byte[][][] audiolines, int bitDepth)
    {
        int numChannels = audiolines.Length;
        int numTracks = audiolines[0].Length;
        int numBytes = audiolines[0][0].Length;

        //get the max value of bytedepth
        int maxvalue = (int)Math.Pow(2, bitDepth) - 1;

        //Normalize all audio data
        if (setting_doNormalizeInput)
            for (int ch = 0; ch < numChannels; ch++)
            {
                for (int j = 0; j < numTracks; j++)
                {
                    float targetMax = maxvalue * targetgain_In;
                    if (j < audioTracks.Count) targetMax = targetMax * audioTracks[j].targetgain;
                    audiolines[ch][j] = Normalize(audiolines[ch][j], bitDepth, bitDepth > 8, targetMax, setting_normalizationFac_Input);
                }
            }

        byte[][] mixedData = new byte[numChannels][];

        for (int ch = 0; ch < numChannels; ch++)
        {
            mixedData[ch] = new byte[numBytes];

            for (int i = 0; i < numBytes; i += 2)
            {
                int sum = 0;

                for (int j = 0; j < numTracks; j++)
                {
                    //check if the track 
                    if ((i+1<audiolines[ch][j].Length)){
                    short sample = BitConverter.ToInt16(audiolines[ch][j], i);
                    sum += sample;
                    } else sum += 0;
                }

                short mixedSample = (short)(sum / numTracks);
                byte[] mixedSampleBytes = BitConverter.GetBytes(mixedSample);

                if (i + 1 < mixedData[ch].Length)
                {
                    mixedData[ch][i] = mixedSampleBytes[0];
                    mixedData[ch][i + 1] = mixedSampleBytes[1];
                }
            }
        }

        return mixedData;
    }

    public byte[][] MixAudioBytes(byte[][][] audiolines, int bitDepth)
    {
        int numChannels = audiolines.Length;
        int numTracks = audiolines[0].Length;
        int numBytes = audiolines[0][0].Length;

        byte[][] mixedData = new byte[numChannels][];
        for (int ch = 0; ch < numChannels; ch++)
        {
            mixedData[ch] = new byte[numBytes];
        }

        //get the max value of bytedepth
        int maxvalue = (int)Math.Pow(2, bitDepth) - 1;

        if (setting_doNormalizeInput)
            //Normalize all audio data
            for (int ch = 0; ch < numChannels; ch++)
            {
                for (int j = 0; j < numTracks; j++)
                {
                    audiolines[ch][j] = Normalize(audiolines[ch][j], bitDepth, bitDepth > 8, maxvalue * 0.3f, setting_normalizationFac_Input);
                }
            }


        for (int j = 0; j < numTracks; j++) //Tracks
        {
            for (int ch = 0; ch < numChannels; ch++) //Channels
            {

                for (int i = 0; i < numBytes; i += bitDepth / 8) //Samples
                {
                    float value_dest = 0;
                    float value_source = 0;

                    bool alwaysPositive = bitDepth > 8;
                    // Get the value of the sign bit
                    byte signBit_source = (byte)(audiolines[ch][j][i] & 0x80);
                    byte signBit_dest = (byte)(mixedData[ch][i] & 0x80);

                    //Iterate the Bytes
                    for (int b = 0; b < bitDepth / 8; b++)
                    {
                        value_source += (audiolines[ch][j][i + b] & 0xFF) << (b * 8);
                        value_dest += (mixedData[ch][i + b] & 0xFF) << (b * 8);

                        if (signBit_source == 0 || alwaysPositive)
                        {
                            value_source += (audiolines[ch][j][i + b] & 0xFF) << (b * 8);
                        }
                        else
                        {
                            value_source -= (audiolines[ch][j][i + b] & 0xFF) << (b * 8);
                        }

                        if (signBit_dest == 0 || alwaysPositive)
                        {
                            value_dest += (mixedData[ch][i + b] & 0xFF) << (b * 8);
                        }
                        else
                        {
                            value_dest -= (mixedData[ch][i + b] & 0xFF) << (b * 8);
                        }

                    }

                    //get the average of the two values
                    float average = (value_dest + value_source) / 2;


                    if (average < 0)
                    {
                        average = 0;
                    }
                    else if (average > Math.Pow(2, bitDepth) - 1)
                    {
                        average = (float)Math.Pow(2, bitDepth) - 1;
                    }


                    uint average_uint = (uint)average;

                    if (i % 5000 == 0 && false)
                    {
                        Debug.Log("Value Dest: " + value_dest);
                        Debug.Log("Value Source: " + value_source);
                        Debug.Log("Averageuint: " + average_uint);

                    }

                    //set the value to the mixed data
                    for (int b = 0; b < bitDepth / 8; b++)
                    {
                        mixedData[ch][i + b] = (byte)(average_uint >> (b * 8));
                    }
                }
            }
        }

        Debug.Log("Mixed Data CH1: " + mixedData[0].Length);
        Debug.Log("Mixed Data CH2: " + mixedData[1].Length);

        ReportAudioData(mixedData[0], "Mixed CH1");
        ReportAudioData(mixedData[1], "Mixed CH2");

        return mixedData;
    }
    //Get C
    public byte[] CreateWavFileHeader(
         byte[] header,
            int sampleRate,
            int numChannels,
            int bitDepth,
            int audioDataSize,
            int fileSize)
    {

        //RIFF chunk descriptor
        header[0] = 82; //R
        header[1] = 73; //I
        header[2] = 70; //F
        header[3] = 70; //F


        header[4] = (byte)(fileSize & 0xff);
        header[5] = (byte)((fileSize >> 8) & 0xff);
        header[6] = (byte)((fileSize >> 16) & 0xff);
        header[7] = (byte)((fileSize >> 24) & 0xff);

        //WAVE header
        header[8] = 87; //W
        header[9] = 65; //A
        header[10] = 86; //V
        header[11] = 69; //E

        //fmt chunk
        header[12] = 102; //f
        header[13] = 109; //m
        header[14] = 116; //t
        header[15] = 32; //space

        //chunk size
        header[16] = 16; //16
        header[17] = 0;
        header[18] = 0;
        header[19] = 0;

        //audio format
        header[20] = 1; //1
        header[21] = 0;

        //number of channels
        header[22] = (byte)numChannels;
        header[23] = 0;

        //sample rate
        header[24] = (byte)(sampleRate & 0xff);
        header[25] = (byte)((sampleRate >> 8) & 0xff);
        header[26] = (byte)((sampleRate >> 16) & 0xff);
        header[27] = (byte)((sampleRate >> 24) & 0xff);

        //byte rate
        int byteRate = sampleRate * numChannels * bitDepth / 8;
        header[28] = (byte)(byteRate & 0xff);
        header[29] = (byte)((byteRate >> 8) & 0xff);
        header[30] = (byte)((byteRate >> 16) & 0xff);
        header[31] = (byte)((byteRate >> 24) & 0xff);

        //block align
        header[32] = (byte)(numChannels * bitDepth / 8);
        header[33] = 0;

        //bits per sample
        header[34] = (byte)bitDepth;
        header[35] = 0;

        //data chunk
        header[36] = 100; //d
        header[37] = 97; //a
        header[38] = 116; //t
        header[39] = 97; //a

        //data size
        int dataSize = audioDataSize;
        header[40] = (byte)(dataSize & 0xff);
        header[41] = (byte)((dataSize >> 8) & 0xff);
        header[42] = (byte)((dataSize >> 16) & 0xff);
        header[43] = (byte)((dataSize >> 24) & 0xff);


        Debug.Log("Data Size: " + dataSize);
        Debug.Log("File Size: " + fileSize);

        return header;
    }


    public void CreateWaveFile(
            byte[][] audioData,
            string filePath,
            string filename,
                int sampleRate,
                int numChannels,
                int bitDepth)
    {
        //Create Header for wav file 
        //16bit, 44100hz, 2 channels

        byte[] header = new byte[44];
        int audioDataSize = 0;

        for (int ch = 0; ch < numChannels; ch++)
        {
            audioDataSize += audioData[ch].Length;
        }

        int fileSize = audioDataSize + 44;

        header = CreateWavFileHeader(header, sampleRate, numChannels, bitDepth, audioDataSize, fileSize);

        //combine file path and filename
        filePath = Path.Combine(filePath, filename);

        //Write the bytes to a file
        FileStream fileStream = new FileStream(filePath, FileMode.Create);
        fileStream.Write(header, 0, header.Length);

        int channelSize = audioData[0].Length;
        for (int i = 0; i < channelSize; i += (int)bitDepth / 8)
        {
            for (int ch = 0; ch < numChannels; ch++)
            {
                for (int b = 0; b < (int)bitDepth / 8; b++)
                {
                    if (i + b < audioData[ch].Length)
                    {
                        fileStream.WriteByte(audioData[ch][i + b]);
                    }
                }
            }
        }

        //Debug


        ReportAudioData(audioData[0], "Output All");

        init_MixedClipData(filePath, filename);


        fileStream.Close();
        Debug.Log("File Written: " + filePath);
    }

    public void init_MixedClipData(string filePath, string filename)
    {
        //Load the file
        //Get relative path
        string relativePath = filePath.Substring(filePath.IndexOf("Assets"));
        //AudioClip buildClip = AssetDatabase.LoadAssetAtPath<AudioClip>(relativePath);
        //Add the clip to the list
        //if (buildClip != null)
        //{
        //AudioTrack newAudioTrack = new AudioTrack(buildClip);
        //}
    }

    //report average and max from audio data byte array
    public void ReportAudioData(byte[] bytedata, string name = "")
    {
        AudioTrack.ReportAudioData(bytedata, name);
    }

}

//----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
//Audiotrack
public class AudioTrack
{
    //Main Variables
    public AudioClip clip;
    public string absolutePath;
    public float initTime = 0;
    public byte[][] audioData; //Final

    //Databuffer of sourcefile
    private int _oldSampleRate = 0;
    private int _oldChannels = 0;
    private int _oldBitDepth = 0;
    private float _length = 0;
    public byte[][] sampledChannels;

    public float targetgain = 0.7f;

    Dictionary<string, int> header;

    public float oldtargetgain = 0;


    public float[][] audioCurve; //channel, sample
                                 //Previewimage
    public Texture2D previewImage;
    public int curveResolution = 250;

    //Targetsettings
    public int _targetSampleRate = 44100; // Target sample rate in Hz
    public int _targetBitDepth = 16; // Target bit depth in bits
    public int _targetChannels = 2; // Target number of channels



    public AudioTrack(AudioClip clip)
    {
        this.clip = clip;
        this.absolutePath = AssetDatabase.GetAssetPath(clip);
        InitializeData();
        LoadAudioData();
    }

    //Samples to int array

    public static void ReportAudioData(byte[] bytedata, string name = "")
    {
        int max = 0;
        int average = 0;
        for (int i = 0; i < bytedata.Length; i++)
        {
            average += bytedata[i];
            if (bytedata[i] > max)
            {
                max = bytedata[i];
            }
        }
        //Total size
        Debug.Log("[" + name + "] Bytes: " + bytedata.Length + "// Max: " + max + "// Median: " + average / bytedata.Length);
    }

    //Downsampling for preview Waveform Image
    private float[] Downsample(float[] samples, int desiredSamplesCount, int byteDepth)
    {
        int originalSamplesCount = samples.Length;
        float[] downsampledSamples = new float[desiredSamplesCount];
        float factor = (float)originalSamplesCount / desiredSamplesCount;

        // Calculate maximum possible amplitude based on the byte depth
        float maxPossibleAmplitude = (float)Math.Pow(2, byteDepth * 8 - 1);

        for (int i = 0; i < desiredSamplesCount; i++)
        {
            int start = (int)Math.Floor(i * factor);
            int end = (int)Math.Min(Math.Ceiling((i + 1) * factor), originalSamplesCount);

            // Calculate max and min values within the downsampling interval
            float min = samples[start];
            float max = samples[start];

            for (int j = start + 1; j < end; j++)
            {
                if (samples[j] < min)
                    min = samples[j];
                if (samples[j] > max)
                    max = samples[j];
            }

            // Calculate amplitude modifier
            float amplitudeModifier = maxPossibleAmplitude != 0 ? Math.Max(Math.Abs(max), Math.Abs(min)) / maxPossibleAmplitude : 0;

            // Calculate difference modifier
            float totalDifference = 0;
            for (int j = start; j < end - 1; j++)
            {
                totalDifference += Math.Abs(samples[j + 1] - samples[j]); // Taking absolute of difference
            }
            float differenceModifier = (end - start - 1) != 0 ? totalDifference / (maxPossibleAmplitude * (end - start - 1)) : 0;

            // Calculate curve value
            downsampledSamples[i] = amplitudeModifier * differenceModifier;
        }

        return downsampledSamples;
    }

    //Function that loads the audio data from the clip
    //Will execute on loading but can also be called from outside
    public void renderPreviewImage()
    {

        DrawWaveform(audioCurve[0], 512, 250, previewImage);
    }

    public static void DrawWaveform(float[] samples, int width, int height, Texture2D targetTexture)
    {
        // Create a new texture for the waveform using transparent defaultcolor

        //Definining Curve Color
        Color curveColor = new Color(0.6f, 0.3f, 0.3f, 0.1f);

        //Defining background gracient color
        Color backgroundColor_a = new Color(0.3f, 0.1f, 0.1f, 0f);
        Color backgroundColor_b = new Color(0.3f, 0.2f, 0.2f, 0.1f);

        //Draw a gradient from top to bottom
        for (int y = 0; y < height; y++)
        {
            float t = (float)y / height;
            Color backgroundColor = Color.Lerp(backgroundColor_a, backgroundColor_b, t);
            for (int x = 0; x < width; x++)
            {
                targetTexture.SetPixel(x, y, backgroundColor);
            }
        }
        targetTexture.Apply();

        // Calculate the number of samples per pixel
        int samplesPerPixel = samples.Length / width;

        for (int x = 0; x < width; x++)
        {
            // Calculate the start and end sample index for this pixel
            int startSampleIndex = x * samplesPerPixel;
            int endSampleIndex = startSampleIndex + samplesPerPixel;

            // Find the minimum and maximum sample in this range
            float minSample = samples[startSampleIndex];
            float maxSample = samples[startSampleIndex];
            for (int i = startSampleIndex + 1; i < endSampleIndex; i++)
            {
                minSample = Mathf.Min(minSample, samples[i]);
                maxSample = Mathf.Max(maxSample, samples[i]);
            }

            // Map the minimum and maximum sample to the height of the texture
            int minY = (int)((minSample + 1) / 2 * height);
            int maxY = (int)((maxSample + 1) / 2 * height);

            // Draw the waveform for this pixel
            for (int y = minY; y < maxY; y++)
            {
                targetTexture.SetPixel(x, y, curveColor);
            }
        }

        // Apply the changes to the texture
        targetTexture.Apply();
    }

    public static float[] getFloatArrayFromSamples(int bitDepth, byte[][] data, int channels, int samplerate)
    {
        // Initialize the float array for storing samples
        float[][] samples = new float[channels][];

        //Calculate the max value from byte
        float maxPossibleAmplitude = (float)Math.Pow(2, bitDepth - 1);

        //Debug and report if audio array is empty
        if (data == null || data.Length == 0)
        {
            Debug.LogError("Audio Data is empty");
            return null;
        }

        // Iterate over channels
        for (int ch = 0; ch < channels; ch++)
        {
            // Initialize the array for the current channel
            samples[ch] = new float[samplerate];

            // Iterate over samples
            for (int i = 0; i < samplerate; i++)
            {
                // Initialize the sample value
                int sampleValue = 0;

                //Store the bytes of each sample
                for (int b = 0; b < bitDepth / 8; b++)
                {
                    // Extract the byte value from audioData
                    if (data[ch].Length <= i * (bitDepth / 8) + b)
                    {
                        continue;
                    }

                    byte byteValue = data[ch][i * (bitDepth / 8) + b];
                    if (b == bitDepth / 8 - 1 && (byteValue & 0x80) > 0) // If the byte is the last one and its sign bit is set
                    {
                        sampleValue |= byteValue << (b * 8) | ~((1 << (bitDepth - 1)) - 1); // Extend the sign bit
                    }
                    else
                    {
                        sampleValue |= byteValue << (b * 8); // Shift the byte value and combine it with the sample value
                    }
                }

                // Convert the sample value to a float and store it
                samples[ch][i] = (float)sampleValue / maxPossibleAmplitude;
            }
        }

        float[][] floatdata = new float[channels][];
        for (int ch = 0; ch < channels; ch++)
        {
            //continue if one is null
            if (samples[ch] == null || samples[ch].Length == 0)
            {
                continue;
            }
            //init audiocurves channel
            floatdata[ch] = new float[samples[ch].Length];
            floatdata[ch] = samples[ch];
        }

        return floatdata[0];
    }
    public void InitializeData()
    {
        previewImage = new Texture2D(512, 250);
    }
    public void LoadAudioData()
    {
        // Get the audio data for each channel
        byte[][] channelData = GetChannels();

        //Initialize audioData Based on Channel count
        audioData = new byte[_targetChannels][];

        //initialize mixed data
        sampledChannels = new byte[_targetChannels][];

        //initialize the channels data
        for (int i = 0; i < _targetChannels; i++)
        {

            //store data in mixedAudioData
            this.sampledChannels[i] = GetSampledAudioData(channelData[i], _oldSampleRate, _oldBitDepth, _targetSampleRate, _targetBitDepth);
            this.audioData[i] = sampledChannels[i];
        }


        ;

        if (previewImage != null)
            DrawWaveform(getFloatArrayFromSamples(_oldBitDepth, audioData, clip.channels, clip.samples), 512, 250, previewImage);


        //Debug count of audio data
        Debug.Log("Audio Data CH1: " + audioData[0].Length);
        Debug.Log("Audio Data CH2: " + audioData[1].Length);
    }

    public byte[] uncompressData(byte[] audioBytes)
    {
        // Now you can read the uncompressed audio data from "temp.wav"


        return audioBytes;
    }


    public byte[] readAudioBytes()
    {
        //Get clip absolute path
        string path = AssetDatabase.GetAssetPath(clip);
        string absolutePath = Path.GetFullPath(path);
        return uncompressData(System.IO.File.ReadAllBytes(absolutePath));
    }

    public byte[][] GetChannels()
    {
        //Data Init ------------------------------------------------

        byte[] fileData = File.ReadAllBytes(absolutePath);
        byte[] audioBytes = new byte[1];

        //Check for File extension
        if (Path.GetExtension(absolutePath) == ".wav")
        {
            header = WavReader.ReadWavHeader(fileData);
            audioBytes = WavReader.GetAudioDataFromWav(fileData, clip.length, header["BitsPerSample"] / 8, header["SampleRate"], header["Channels"]);
            ReportAudioData(audioBytes, "Read WAV Audio Bytes");
        }
        else if (Path.GetExtension(absolutePath) == ".mp3")
        {
            header = MP3Reader.ReadMp3Header(absolutePath);
            byte[] newWavData = MP3Reader.GetAudioDataFromMp3(absolutePath);
            header = WavReader.ReadWavHeader(newWavData);
            audioBytes = WavReader.GetAudioDataFromWav(newWavData, clip.length, header["BitsPerSample"] / 8, header["SampleRate"], header["Channels"]);
            ReportAudioData(audioBytes, "Read MP3 Audio Bytes");
        }

        else return null;

        //Initialize Variables 

        int totalBytes = audioBytes.Length; // Subtract 44 to exclude the .wav header
        float durationInSeconds = clip.length;

        Debug.Log("durationInSeconds: " + durationInSeconds);

        int channels = header["Channels"];
        int sampleRate = header["SampleRate"];

        int blockSize = header["BlockAlign"];
        int bitDepth = header["BitsPerSample"];
        int bytesPerSample = bitDepth / 8;

        //Debug all Variables
        Debug.Log("Total Bytes: " + totalBytes);
        Debug.Log("Duration: " + durationInSeconds);
        Debug.Log("Channels: " + channels);
        Debug.Log("Sample Rate: " + sampleRate);
        Debug.Log("Block Size: " + blockSize);
        Debug.Log("Bit Depth: " + bitDepth);
        Debug.Log("Bytes Per Sample: " + bytesPerSample);


        //Abort if 0
        if (sampleRate == 0 || bitDepth == 0 || channels == 0)
        {
            Debug.LogError("Invalid audio data");
            return null;
        }

        //Store for Later Processing
        _oldSampleRate = sampleRate;
        _oldChannels = channels;
        _oldBitDepth = bitDepth;
        _length = clip.length;

        //Validate Samplecount
        int validateSamplecount = ((audioBytes.Length) / (bytesPerSample));
        if (validateSamplecount != Math.Round(sampleRate * durationInSeconds * channels))
        {
            //Debug warning with samplecount and expected samplecount
            Debug.LogWarning("Invalid Sample Count");
            Debug.LogWarning("Sample Count: " + validateSamplecount);
            Debug.LogWarning("Expected Sample Count: " + sampleRate * durationInSeconds * channels);
            //Try if Channelcount was wrong
            channels = 1;
            validateSamplecount = (int)Math.Round((double)audioBytes.Length / bytesPerSample / channels);
            if (validateSamplecount != (int)Math.Round(sampleRate * durationInSeconds))
            {
                Debug.LogWarning("Invalid Sample Count...recreating placeholderdata");
                //recreate the audio data with empty data on the end fitting the exspected length
                byte[] newAudioBytes = new byte[1 + (int)Math.Round(sampleRate * durationInSeconds) * bytesPerSample * channels];

                int copiedBytes = 0;
                for (int i = 0; i < newAudioBytes.Length; i++)
                {
                    //If end of audioBytes is reached fill with 0
                    if (i >= audioBytes.Length || i >= newAudioBytes.Length)
                    {
                        newAudioBytes[i] = 0;
                    }
                    else
                    {
                        copiedBytes++;
                        newAudioBytes[i] = audioBytes[i];
                    }
                }
                Debug.LogWarning("Copied Bytes: " + copiedBytes);

                audioBytes = newAudioBytes;
                //Debug new audiobytes
                Debug.Log(audioBytes.Length.ToString() + " - Recreated Audio Bytes");
            }
        }


        //Data Init End ---------------------------------------------


        // Create a jagged array to hold the audio data for each channel
        byte[][] channelData = new byte[_targetChannels][];

        // Calculate the number of samples
        int numSamples = totalBytes / blockSize;

        for (int i = 0; i < channels; i++)
        {
            channelData[i] = new byte[numSamples * bytesPerSample];
        }

        for (int i = 0; i < numSamples; i++) // Iterate each block (sample)
        {
            for (int j = 0; j < channels; j++) // Iterate each channel within the block
            {
                for (int k = 0; k < bytesPerSample; k++) // Iterate each byte within the channel's sample
                {
                    // Calculate the index into the audioBytes array
                    int index = i * blockSize + j * bytesPerSample + k;

                    // Make sure the index is within the bounds of the array
                    if (index < audioBytes.Length)
                    {
                        channelData[j][i * bytesPerSample + k] = audioBytes[index];
                    }
                }
            }
        }

        //if channel was just 1 copy amounts of targetchannels
        if (channels == 1)
        {
            for (int i = 1; i < _targetChannels; i++)
            {
                channelData[i] = channelData[0];
            }
        }

        //iterate Channels
        for (int i = 0; i < channels; i++)
        {
            ReportAudioData(channelData[i], ("Separated CH" + i));
        }

        return channelData;
    }
    private byte[] GetSampledAudioData(byte[] audioBytes, int oldSampleRate, int oldBitDepth, int newSampleRate, int newBitDepth)
    {
        // Calculate the number of samples in the original and resampled audio data
        //Debug newBit


        int newSamples = (int)(_length * newSampleRate);

        int oldSamples = audioBytes.Length / (oldBitDepth / 8);

        int oldDepthBytes = oldBitDepth / 8;
        int newDepthBytes = newBitDepth / 8;


        Debug.Log(
            "#Samps: " + newSamples +
            "#TByte: " + newSamples * newDepthBytes +
            " #Bit: " + newBitDepth +
            " #Rate: " + newSampleRate +
            " _Samps: " + oldSamples +
            " _TByte: " + oldSamples * oldDepthBytes +
            " _Bit : " + oldBitDepth +
            " _Rate: " + oldSampleRate
         );


        int newbytecount = (int)newSamples * newDepthBytes;
        byte[] resampledBytes = new byte[newbytecount];


        for (int i = 0; i < resampledBytes.Length; i += newDepthBytes)
        {
            // Get current ratio
            double ratio = (double)i / (resampledBytes.Length);

            // Get the closest first byte of a sample
            int oldIndex = ((int)(ratio * audioBytes.Length) - (int)(ratio * audioBytes.Length) % oldDepthBytes);

            uint value = 0;

            // Copy the entire sample
            for (int j = 0; j < oldDepthBytes; j++)
            {
                if (oldIndex + j < audioBytes.Length)
                {
                    // Reverse the order of the bytes for big-endian data
                    //value |= (uint)(audioBytes[oldIndex + j] << (8 * (oldDepthBytes - 1 - j)));
                    //Non reversed
                    value |= (uint)(audioBytes[oldIndex + j] << (8 * j));
                }
            }

            int shiftamount = newBitDepth - oldBitDepth;

            // Shift the value if necessary
            if (shiftamount > 0)
            {
                value <<= shiftamount;
            }
            else if (shiftamount < 0)
            {
                value >>= -shiftamount;
            }

            // Copy the value to the resampled audio data
            for (int j = 0; j < newDepthBytes; j++)
            {
                // Reverse the order of the bytes for big-endian data
                //resampledBytes[i + j] = (byte)(value >> (8 * (newDepthBytes - 1 - j)));

                //Non reversed
                resampledBytes[i + j] = (byte)(value >> (8 * j));
            }
        }

        ReportAudioData(resampledBytes, "Resampled Data");

        return resampledBytes;
    }

    //Readers







}

//----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
//Popup for setting values

public class CustomPopup : EditorWindow
{

    public enum SampleRate
    {
        _8000Hz = 8000,
        _16000Hz = 16000,
        _32000Hz = 32000,
        _44100Hz = 44100,
        _48000Hz = 48000,
        _96000Hz = 96000
    }

    public enum BitDepth
    {
        _8bit = 8,
        _16bit = 16,
        _24bit = 24,
        _32bit = 32,
        _48bit = 48,
        _64bit = 64
    }

    private int samplerate;
    private int bitrate;
    private int channels;

    private bool doNormalizeInput;
    private float normalizeThreshold;

    private bool doNormalizeOutput;
    private float normalizeOutputThreshold;

    private float targetgain_In;
    private float targetgain_Out;

    Vector2 scrollPos = new Vector2(0, 0);


    private Action<int, int, int, bool, float, bool, float, float, float> callback;

    private ColorTextureManager colorTextureManager = new ColorTextureManager();

    public static void ShowWindow(Action<int, int, int, bool, float, bool, float, float, float> callback, int samplerate = 0, int bitrate = 0, int channels = 0, bool doNormalizeInput = false, float normalizeThreshold = 0, bool doNormalizeOutput = false, float normalizeOutputThreshold = 0, float targetgain_In = 0, float targetgain_Out = 0)
    {
        var window = GetWindow<CustomPopup>("Set Values");
        window.callback = callback;
        window.samplerate = samplerate;
        window.bitrate = bitrate;
        window.channels = channels;
        window.doNormalizeInput = doNormalizeInput;
        window.normalizeThreshold = normalizeThreshold;
        window.doNormalizeOutput = doNormalizeOutput;
        window.normalizeOutputThreshold = normalizeOutputThreshold;
        window.targetgain_In = targetgain_In;
        window.targetgain_Out = targetgain_Out;
    }


    //On Enable and Disable setup the color texture manager
    private void OnEnable()
    {
        colorTextureManager.CacheFolder = "Assets/AnimefanPostUPs-Tools/AnimefanPostUPs_Tools/Editor/Textures_Internal/";
    }

    private void OnDisable()
    {
        colorTextureManager.Unload();
    }

    private void OnGUI()
    {

        //Create box with the size of the window hovering in the background
        Rect windowRect = new Rect(0, 0, position.width, position.height);
        //repaint if Rect contains mouse position
        //Draw Gradient Background using a texture
        GUI.DrawTexture(new Rect(0, 0, position.width, position.height), colorTextureManager.LoadTexture(TexItemType.Gradient_Vertical, ColorRGBA.grayscale_032, ColorRGBA.grayscale_016, 128));

        if (windowRect.Contains(Event.current.mousePosition))
        {
            Repaint();
        }

        //Create scroll vertical
        // Begin scroll view
        scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Width(position.width), GUILayout.Height(position.height));

        //Begin scrollview


        GUIStyle buttonStyle2 = new GUIStyle(GUI.skin.box);
        buttonStyle2.fixedWidth = position.width - 10;
        buttonStyle2.fixedHeight = 25;
        buttonStyle2.normal.background = colorTextureManager.LoadTexture(TexItemType.Gradient_Vertical, ColorRGBA.darkred, ColorRGBA.duskred, 16);
        buttonStyle2.margin.right = 0;
        buttonStyle2.padding.right = 0;

        void CreateEnumButton<T>(T enumValue) where T : Enum
        {
            if (GUILayout.Button(enumValue.ToString().TrimStart('_') + "", buttonStyle2))
            {
                if (typeof(T) == typeof(SampleRate))
                {
                    samplerate = (int)(object)enumValue;
                }
                else if (typeof(T) == typeof(BitDepth))
                {
                    bitrate = (int)(object)enumValue;
                }
                //audioManager.reloadAudioData();
            }
        }

        GUILayout.Label("Target Settings:", GUILayout.Width(position.width - 10));

        SampleRate sampleRate = (SampleRate)samplerate;
        BitDepth bitDepth = (BitDepth)bitrate;
        sampleRate = (SampleRate)EditorGUILayout.EnumPopup("", sampleRate, buttonStyle2);
        samplerate = (int)sampleRate;

        GUILayout.Space(5);

        bitDepth = (BitDepth)EditorGUILayout.EnumPopup("", bitDepth, buttonStyle2);
        bitrate = (int)bitDepth;

        GUILayout.Space(5);

        //Debug timeline variabled ALL
        //Debug.Log("Timeline: " + timelineView.maxTime + " Tracks: " + audioManager.audioTracks.Count + " Track Height: " + timelineView.trackHeight + " Position: " + timelineView.timelinePosition + " Zoom: " + timelineView.timelinezoom);

        //Create 2 Toggling buttons for 1 or 2 channels
        if (GUILayout.Button("Current:  " + (channels == 1 ? "MONO" : "STEREO"), buttonStyle2))
        {
            if (channels == 1)
            {
                channels = 2;
            }
            else
                channels = 1;

        }

        //Spacer 30
        GUILayout.Space(30);


        //Normalize settings
        GUILayout.Label("[" + normalizeThreshold + "]" + "Global Normalize Input:", GUILayout.Width(position.width - 10));

        GUILayout.Label("(Applied before Mixing)", GUILayout.Width(position.width - 10));
        GUILayout.Label("(Used for Gain! )", GUILayout.Width(position.width - 10));
        GUILayout.Label("Default: On)", GUILayout.Width(position.width - 10));
        //Toggle
        if (GUILayout.Button("Normalize Input: " + (doNormalizeInput ? "ON" : "OFF"), buttonStyle2))
        {
            doNormalizeInput = !doNormalizeInput;
        }

        GUILayout.Space(5);

        //Slider to set the threshold
        normalizeThreshold = GUILayout.HorizontalSlider(normalizeThreshold, 0, 1, GUILayout.Width(position.width - 10));


        GUILayout.Space(12);

        //Output
        GUILayout.Label("[" + normalizeOutputThreshold + "]" + "Global Normalize Output:", GUILayout.Width(position.width - 10));
        GUILayout.Label("(Applied on Result)", GUILayout.Width(position.width - 10));
        GUILayout.Label("Default: Off)", GUILayout.Width(position.width - 10));

        //Toggle
        if (GUILayout.Button("[" + normalizeOutputThreshold + "]" + "Normalize Output: " + (doNormalizeOutput ? "ON" : "OFF"), buttonStyle2))
        {
            doNormalizeOutput = !doNormalizeOutput;
        }

        GUILayout.Space(5);

        //Slider to set the threshold
        normalizeOutputThreshold = GUILayout.HorizontalSlider(normalizeOutputThreshold, 0, 1, GUILayout.Width(position.width - 10));

        GUILayout.BeginVertical();
        {
            GUILayout.Space(12);

            //setting for Targetgain
            GUILayout.Label("Modifier Gain Input:", GUILayout.Width(position.width / 2 - 20));
            //Splider
            targetgain_In = GUILayout.HorizontalSlider(targetgain_In, -1, 1, GUILayout.Width(position.width / 2 - 20));
        }
        GUILayout.EndVertical();


        GUILayout.Space(5);



        //Vertical
        GUILayout.BeginVertical();
        {
            //setting for Targetgain
            GUILayout.Label("Target Gain Output:", GUILayout.Width(position.width / 2 - 20));

            //Splider
            targetgain_Out = GUILayout.HorizontalSlider(targetgain_Out, -1, 1, GUILayout.Width(position.width / 2 - 20));
        }
        GUILayout.EndVertical();


        GUILayout.Space(12);


        GUILayout.Label("Functions", GUILayout.Width(position.width - 10));

        /*
            //Buttons to toggle bool variable in audiomanager
            if (GUILayout.Button("Auto Update Mix: " + (audioManager.autobuild ? "ON" : "OFF"), buttonStyle3))
            {
                audioManager.autobuild = !audioManager.autobuild;
            }

            GUILayout.Space(5);
            */

        //Focus the folder
        if (GUILayout.Button("OK", buttonStyle2))
        {
            callback?.Invoke(samplerate, bitrate, channels, doNormalizeInput, normalizeThreshold, doNormalizeOutput, normalizeOutputThreshold, targetgain_In, targetgain_Out);
            Close();
        }
        GUILayout.EndScrollView();

    }
}
