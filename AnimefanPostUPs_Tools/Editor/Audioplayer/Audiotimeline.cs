using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using System;
using System.Text;
using System.Collections;
using System.Reflection;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.IO;
using System.Threading.Tasks;

using NAudio;
using NAudio.Wave;

//import colors and guilayouts from AnimefanPostUPs-Tools
using AnimefanPostUPs_Tools.SmartColorUtility;
using AnimefanPostUPs_Tools.ColorTextureItem;
using AnimefanPostUPs_Tools.ColorTextureManager;
using AnimefanPostUPs_Tools.GUI_LayoutElements;
//get Texturetypes
using static AnimefanPostUPs_Tools.ColorTextureItem.TexItemType;


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

    //Create color mgr
    private ColorTextureManager colorTextureManager = new ColorTextureManager();
    private TimelineView timelineView;

    //Audio Manager
    private AudiotrackManager audioManager;

    //Create Split
    public Splitviewer splitviewer = new Splitviewer();

    //Create window
    [MenuItem("Animtools/Audio Timeline")]
    public static void ShowWindow()
    {
        GetWindow<Audiotimeline>("Audio Timeline");
    }

    //On start
    private void OnEnable()
    {
        init_audiomanager();
        //Init color texture manager
        colorTextureManager.CacheFolder = CacheFolder;
    }

    void init_audiomanager()
    {
        audioManager = new AudiotrackManager();
        timelineView = new TimelineView(500, 50, audioManager);
    }

    //on disable
    private void OnDisable()
    {
    }


    public enum SampleRate
    {
        _8000 = 8000,
        _16000 = 16000,
        _32000 = 32000,
        _44100 = 44100,
        _48000 = 48000,
        _96000 = 96000
    }

    public enum BitDepth
    {
        _8 = 8,
        _16 = 16,
        _24 = 24,
        _32 = 32,
        _48 = 48,
        _64 = 64
    }


    private void OnGUI()
    {

        //Check if even is control and scrollwheel
        if (Event.current.type == EventType.ScrollWheel && (Event.current.control || Event.current.alt))
        {
            timelineView.timelinezoom.x -= (Event.current.delta.y * 4);

            //Clamp value between 1 and 1000
            timelineView.timelinezoom.x = Mathf.Clamp(timelineView.timelinezoom.x, 1, 1000);

            //use Event
            Event.current.Use();
            Repaint();
        }

        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical(GUILayout.Width(splitviewer.splitPosition));

        GUILayout.EndVertical();

        if (splitviewer.drawSplit(Event.current, position)) Repaint();

        DrawTimelineUI(position.width - splitviewer.splitPosition);
        GUILayout.EndHorizontal();

        DrawSidePanel(splitviewer.splitPosition);
    }

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

        scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Width(widthSidePanel));
        EditorGUI.DrawRect(new Rect(0, 0, widthSidePanel, position.height), GetRGBA(ColorRGBA.grayscale_064));

        int halfWidth = (int)(widthSidePanel - 20);

        //Create button style
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fixedWidth = widthSidePanel - 10;
        buttonStyle.fixedHeight = 25;

        //Create button style
        GUIStyle buttonStyle2 = new GUIStyle(GUI.skin.button);
        buttonStyle.fixedWidth = widthSidePanel - 10;
        buttonStyle.fixedHeight = 25;

        audioClip = EditorGUILayout.ObjectField("", audioClip, typeof(AudioClip), false, GUILayout.Width(100)) as AudioClip;
        //Call//Unity field for folder

        //Buttons to toggle bool variable in audiomanager
        if (GUILayout.Button("Auto Update Mix", GUILayout.Width(130)))
        {
            audioManager.autobuild = !audioManager.autobuild;
        }

        //Field to Set the Name
        //label
        string oldfilename = filename;
        GUILayout.Label("Filename:", GUILayout.Width(halfWidth));
        filename = EditorGUILayout.TextField("", filename, GUILayout.Width(halfWidth));




        string oldfolder = targetfolder;
        targetfolder = EditorGUILayout.TextField("", targetfolder, GUILayout.Width(halfWidth));
        if (GUILayout.Button("Select", GUILayout.Width(halfWidth)))
        {
            targetfolder = EditorUtility.OpenFolderPanel("Select Folder", targetfolder, "");
        }

        if (GUILayout.Button("Try Find Json", GUILayout.Width(halfWidth)))
        {
            audioManager.checkForJson(targetfolder, filename);
        }





        void CreateEnumButton<T>(T enumValue) where T : Enum
        {
            if (GUILayout.Button(enumValue.ToString().TrimStart('_'), buttonStyle))
            {
                if (typeof(T) == typeof(SampleRate))
                {
                    audioManager.targetSampleRate = (int)(object)enumValue;
                }
                else if (typeof(T) == typeof(BitDepth))
                {
                    audioManager.targetBitDepth = (int)(object)enumValue;
                }
                audioManager.reloadAudioData();
            }
        }

        SampleRate sampleRate = (SampleRate)audioManager.targetSampleRate;
        BitDepth bitDepth = (BitDepth)audioManager.targetBitDepth;

        GUILayout.Label("Sample Rate: " + audioManager.targetSampleRate);
        sampleRate = (SampleRate)EditorGUILayout.EnumPopup("", sampleRate, GUILayout.Width(halfWidth));
        audioManager.targetSampleRate = (int)sampleRate;

        GUILayout.Label("Bit Depth: " + audioManager.targetBitDepth);
        bitDepth = (BitDepth)EditorGUILayout.EnumPopup("", bitDepth, GUILayout.Width(halfWidth));
        audioManager.targetBitDepth = (int)bitDepth;

        //Debug timeline variabled ALL
        //Debug.Log("Timeline: " + timelineView.maxTime + " Tracks: " + audioManager.audioTracks.Count + " Track Height: " + timelineView.trackHeight + " Position: " + timelineView.timelinePosition + " Zoom: " + timelineView.timelinezoom);

        GUILayout.Label("Channelcount: " + audioManager.targetChannels, GUILayout.Width(halfWidth));

        GUILayout.BeginHorizontal(GUILayout.Width(halfWidth));
        //Create 2 Toggling buttons for 1 or 2 channels
        if (GUILayout.Button("1", buttonStyle2))
        {
            audioManager.targetChannels = 1;
        }
        if (GUILayout.Button("2", buttonStyle2))
        {
            audioManager.targetChannels = 2;
        }
        GUILayout.EndHorizontal();




        GUILayout.BeginHorizontal(GUILayout.Width(50));
        //Focus the folder
        if (GUILayout.Button("Find", buttonStyle2))
        {
            //Focus in project window
            //Get relative path
            string relativepath = targetfolder.Replace(Application.dataPath, "Assets");
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(relativepath));
        }
        //Create File
        if (GUILayout.Button("CREATE MIX", buttonStyle2))
        {
            audioManager.createMix(targetfolder, filename);
        }

        GUILayout.EndHorizontal();

        if (audioClip != null)
        {
            AudioTrack newAudioTrack = new AudioTrack(audioClip);
            audioManager.addAudioTrack(newAudioTrack);
            audioManager.reloadAudioData();
            audioClip = null;
        }
        GUILayout.BeginHorizontal(GUILayout.Width(50));


        //Vertical
        GUILayout.BeginVertical();
        //loop through all audio tracks
        foreach (var track in audioManager.audioTracks)
        {
            //Horizontal
            GUILayout.BeginHorizontal();

            //Write 100width label with name
            GUILayout.Label(track.clip.name, GUILayout.Width(25));

            //Inputfield for init time

            float oldinitTime = track.initTime;
            track.initTime = EditorGUILayout.FloatField("", track.initTime, GUILayout.Width(25));

            //reload the audio data if the init time has changed
            if (Mathf.Abs(oldinitTime - track.initTime) > 0.001f)
            {
                track.LoadAudioData();
            }
            //button to remove track
            if (GUILayout.Button("X", GUILayout.Width(widthSidePanel / 12)))
            {
                audioManager.removeAudioTrack(track);
            }

            //button to load audio data
            if (GUILayout.Button("Reload", GUILayout.Width(widthSidePanel / 4)))
            {
                track.LoadAudioData();
            }

            //button to load audio data
            if (GUILayout.Button("Clamp", GUILayout.Width(widthSidePanel / 4)))
            {
                //set Track inittime to 0
                track.initTime = 0;
            }


            //Button to print the first 200 values of audioCurve
            if (GUILayout.Button("C", GUILayout.Width(widthSidePanel / 4)))
            {
                for (int i = 0; i < 200; i++)
                {
                    if (i < track.audioCurve[0].Length)
                    {
                        Debug.Log("Curve: " + track.audioCurve[0][i] + " " + track.audioCurve[1][i]);
                    }
                }

                //Debug the min and max
                float min = 10000;
                float max = -10000;
                for (int i = 0; i < track.audioCurve.Length; i++)
                {
                    if (track.audioCurve[0][i] < min) min = track.audioCurve[0][i];
                    if (track.audioCurve[0][i] > max) max = track.audioCurve[0][i];


                }

                Debug.Log("Min: " + min + " Max: " + max);

            }


            GUILayout.EndHorizontal();
        }

        GUILayout.EndVertical();


        GUILayout.EndHorizontal();


        //End Scrollview
        GUILayout.EndScrollView();
        GUILayout.EndVertical();
    }


}

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

    public float trackHeightOffset = 20;

    AudiotrackManager audiotrackmgr;

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
                amgr.createMix(targetfolder, filename + ".wav");
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
        GUILayout.BeginArea(new Rect(xPos + timelinePosition_Offset.x, timelinePosition_Offset.y, maxTime * timelinezoom.x, (trackHeight + 10) * (trackCount + 2)));

        timelinePosition = GUILayout.BeginScrollView(timelinePosition, GUILayout.Width((int)(maxTime * 10) * timelinezoom.x), GUILayout.Height(trackCount * (trackHeight * 2 + 2)));
        GUILayout.BeginHorizontal(GUILayout.Width(maxTime * timelinezoom.x));

        GUIStyle boxstyle = new GUIStyle(GUI.skin.box);
        boxstyle.normal.background = colormgr.LoadTexture(TexItemType.Gradient_Horizontal, ColorRGBA.lightred, ColorRGBA.brightred, 32);

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
                EditorGUI.DrawRect(rect, GetRGBA(ColorRGBA.grayscale_144));
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
            EditorGUI.DrawPreviewTexture(new Rect(_x, _y + 20, _width, _height - 20), audiotrackmgr.audioTracks[i].previewImage, null, ScaleMode.StretchToFill);


            EditorGUI.LabelField(new Rect(_x, _y, 20, 12), audiotrackmgr.audioTracks[i].clip.name
          //Make color black and bold
          , new GUIStyle() { normal = new GUIStyleState() { textColor = Color.black }, fontStyle = FontStyle.Bold, fontSize = 12 }
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


public class AudiotrackManager
{

    public List<AudioTrack> audioTracks;

    //Target Settings
    public int targetSampleRate = 44100; // Target sample rate in Hz
    public int targetBitDepth = 16; // Target bit depth in bits
    public int targetChannels = 2; // Target number of channels

    public bool autobuild = false;

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
    [Serializable]
    public class AudioData
    {
        public string[] ClipPaths;
        public float[] InitTimes ;
        public string OutputFilePath ;
        public string OutputFileName ;
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
                audioTrack.initTime = audioData.InitTimes[i];
                audioTracks.Add(audioTrack);
            }
        }
    }

    public void createMix(string filePath, string filename)
    {

        //Load all audio data
        //reloadAudioData();
        byte[][] mixedData = MixAudioData();
        CreateWaveFile(mixedData, filePath, filename+ ".wav", targetSampleRate, targetChannels, targetBitDepth);

        AudioData audioData = new AudioData
        {
            ClipPaths = new string[audioTracks.Count],
            InitTimes = new float[audioTracks.Count],
            OutputFilePath = filePath,
            OutputFileName = filename
        };

        for (int i = 0; i < audioTracks.Count; i++)
        {
            audioData.ClipPaths[i] = AssetDatabase.GetAssetPath(audioTracks[i].clip);
            audioData.InitTimes[i] = audioTracks[i].initTime;
        }

        // Store clips in json
        string json = JsonUtility.ToJson(audioData);

        // Write the Json File
        File.WriteAllText(filePath + "/" + filename + ".json", json);


        //Get date and time
        string date = DateTime.Now.ToString("yyyy-MM-dd_HH-mm");

        // Save copy
        File.WriteAllText(filePath + "/" + date + "_" + filename + "_.json", json);

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

        // Write the Json File
        File.WriteAllText(filePath + "/" + filename + ".json", json);

        // Reload the Folder
        AssetDatabase.Refresh();
    }

    public int getMixLength(int channel)
    {
        int maxLength = 0;
        foreach (var audiotrack in audioTracks)
        {
            if ((int)((audiotrack.audioData[channel].Length + ((audiotrack.initTime) * audiotrack._targetSampleRate * (audiotrack._targetBitDepth / 8)))) > maxLength)
            {
                maxLength = (int)(audiotrack.audioData[channel].Length + ((audiotrack.initTime) * audiotrack._targetSampleRate * (audiotrack._targetBitDepth / 8)));
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
                int trackendsample = +(int)(trackstartsample) + ((audioTracks[i].audioData[ch].Length) / bytedepth);

                //Initialize all with 0
                for (int b = 0; b < mixedData[ch][i].Length; b++)
                {
                    mixedData[ch][i][b] = 0;
                }

                for (int s = 0; s < mixedData[ch][i].Length; s = s + bytedepth) //Iterate Samples
                {
                    int b = s * bytedepth;
                    //Write 0 if before or after the track
                    if (s >= trackstartsample && s < trackendsample)
                    {
                        for (int byteIndex = 0; byteIndex < bytedepth; byteIndex++) //Iterate Bytes
                        {
                            mixedData[ch][i][b + byteIndex] = audioTracks[i].audioData[ch][b + byteIndex - (trackstartsample * bytedepth)];
                        }

                    }

                }
            }
        }

        ReportAudioData(mixedData[0][0], "Collected CH1");
        ReportAudioData(mixedData[1][0], "Collected CH2");

        return MixAudioBytes(mixedData, targetBitDepth);
    }

    // Combine all audio tracks into one byte array
    //Combine all audio tracks into one byte array
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

        float amplitudeFactor = 0f; // Increase the amplitude by 50%

        for (int j = 0; j < numTracks; j++) //Tracks
        {
            for (int ch = 0; ch < numChannels; ch++) //Channels
            {
                for (int i = 0; i < numBytes; i += bitDepth / 8) //Samples
                {
                    for (int b = 0; b < bitDepth / 8; b++) // combine and wise
                    {
                        if (i + b < mixedData[ch].Length)
                        {
                            if (b == mixedData[ch].Length - 1)
                            {
                                // Store the most significant bit
                                bool firstbit = (audiolines[ch][j][i + b] & 0x80) == 0x80;

                                // Shift all bits to the right by one

                                audiolines[ch][j][i + b] = (byte)((audiolines[ch][j][i + b] >> 1) & 0x7F);

                                // Add the most significant bit again on the same position

                                if (firstbit)
                                {
                                    audiolines[ch][j][i + b] |= (byte)(0x80);
                                }
                            }
                            mixedData[ch][i + b] = (byte)(mixedData[ch][i + b] | audiolines[ch][j][i + b]);
                        }
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


        fileStream.Close();
        Debug.Log("File Written: " + filePath);
    }

    //report average and max from audio data byte array
    public void ReportAudioData(byte[] bytedata, string name = "")
    {
        AudioTrack.ReportAudioData(bytedata, name);
    }

}

//Class that containes an audiotrack, and the time it should start playing, if it is playing
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

    Dictionary<string, int> header;


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


    public void renderPreviewImage()
    {

        DrawWaveform(audioCurve[0], 512, 250);

    }

    void DrawWaveform(float[] samples, int width, int height)
    {
        // Create a new texture for the waveform using transparent defaultcolor

        //Definining Curve Color
        Color curveColor = new Color(0.5f, 0.2f, 0.2f, 0.1f);

        //Defining background gracient color
        Color backgroundColor_a = new Color(0.3f, 0.1f, 0.1f, 0f);
        Color backgroundColor_b = new Color(0.3f, 0.2f, 0.2f, 0.1f);

        previewImage = new Texture2D(width, height);

        //Draw a gradient from top to bottom
        for (int y = 0; y < height; y++)
        {
            float t = (float)y / height;
            Color backgroundColor = Color.Lerp(backgroundColor_a, backgroundColor_b, t);
            for (int x = 0; x < width; x++)
            {
                previewImage.SetPixel(x, y, backgroundColor);
            }
        }
        previewImage.Apply();

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
                previewImage.SetPixel(x, y, curveColor);
            }
        }

        // Apply the changes to the texture
        previewImage.Apply();
    }

    public void getFloatArrayFromSamples()
    {
        // Initialize the float array for storing samples
        float[][] samples = new float[clip.channels][];

        //Calculate the max value from byte
        float maxPossibleAmplitude = (float)Math.Pow(2, _oldBitDepth - 1);

        //Debug and report if audio array is empty
        if (audioData == null || audioData.Length == 0)
        {
            Debug.LogError("Audio Data is empty");
            return;
        }

        // Iterate over channels
        for (int ch = 0; ch < clip.channels; ch++)
        {
            // Initialize the array for the current channel
            samples[ch] = new float[clip.samples];

            // Iterate over samples
            for (int i = 0; i < clip.samples; i++)
            {
                // Initialize the sample value
                int sampleValue = 0;

                //Store the bytes of each sample
                for (int b = 0; b < _oldBitDepth / 8; b++)
                {
                    // Extract the byte value from audioData
                    if (audioData[ch].Length <= i * (_oldBitDepth / 8) + b)
                    {
                        continue;
                    }

                    byte byteValue = audioData[ch][i * (_oldBitDepth / 8) + b];
                    if (b == _oldBitDepth / 8 - 1 && (byteValue & 0x80) > 0) // If the byte is the last one and its sign bit is set
                    {
                        sampleValue |= byteValue << (b * 8) | ~((1 << (_oldBitDepth - 1)) - 1); // Extend the sign bit
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

        audioCurve = new float[_targetChannels][];
        for (int ch = 0; ch < _targetChannels; ch++)
        {
            //continue if one is null
            if (samples[ch] == null || samples[ch].Length == 0)
            {
                continue;
            }
            //init audiocurves channel
            audioCurve[ch] = new float[samples[ch].Length];
            audioCurve[ch] = samples[ch];
        }
    }
    public void InitializeData()
    {

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


        getFloatArrayFromSamples();

        renderPreviewImage();


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
            header = ReadWavHeader(fileData);
            audioBytes = GetAudioDataFromWav(fileData, clip.length, header["BitsPerSample"] / 8, header["SampleRate"], header["Channels"]);
            ReportAudioData(audioBytes, "Read WAV Audio Bytes");
        }
        else if (Path.GetExtension(absolutePath) == ".mp3")
        {
            header = ReadMp3Header(absolutePath);
            byte[] newWavData = GetAudioDataFromMp3(absolutePath);
            header = ReadWavHeader(newWavData);
            audioBytes = GetAudioDataFromWav(newWavData, clip.length, header["BitsPerSample"] / 8, header["SampleRate"], header["Channels"]);
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
    public Dictionary<string, int> ReadWavHeader(byte[] fileData)
    {
        using (var stream = new MemoryStream(fileData))
        using (var reader = new BinaryReader(stream))
        {
            // Read "RIFF" header
            string riff = new string(reader.ReadChars(4));
            if (riff != "RIFF")
            {
                throw new Exception("Not a WAV file - no RIFF header.");
            }

            // Skip next 4 bytes (file size)
            reader.ReadInt32();

            // Read "WAVE" identifier
            string wave = new string(reader.ReadChars(4));
            if (wave != "WAVE")
            {
                throw new Exception("Not a WAV file - no WAVE identifier.");
            }

            // Read chunks until "fmt " chunk is found
            string chunkId;
            int chunkSize;
            do
            {
                chunkId = new string(reader.ReadChars(4));
                chunkSize = reader.ReadInt32();
                if (chunkId != "fmt ")
                {
                    reader.ReadBytes(chunkSize); // Skip chunk data
                }
            } while (chunkId != "fmt ");

            // Read audio format information
            short audioFormat = reader.ReadInt16();
            short numChannels = reader.ReadInt16();
            int sampleRate = reader.ReadInt32();
            int byteRate = reader.ReadInt32();
            short blockAlign = reader.ReadInt16();
            short bitsPerSample = reader.ReadInt16();

            Dictionary<string, int> header = new Dictionary<string, int>
        {
            { "Channels", numChannels },
            { "SampleRate", sampleRate },
            { "ByteRate", byteRate },
            { "BlockAlign", blockAlign },
            { "BitsPerSample", bitsPerSample }
        };

            return header;
        }
    }

    public byte[] GetAudioDataFromWav(byte[] wavFile, double audioLength, int byteDepth, int sampleRate, int channels)
    {
        // Calculate the size of the audio data
        int dataSize = (int)Math.Round(audioLength * sampleRate * byteDepth * channels);

        // Check if the calculated size is valid
        if (dataSize <= 0 || dataSize > wavFile.Length)
        {
            throw new Exception("Invalid audio data size.");
        }

        // Copy the audio data to a new array
        byte[] audioData = new byte[dataSize];
        Array.Copy(wavFile, wavFile.Length - dataSize, audioData, 0, dataSize);

        return audioData;
    }
    public byte[] GetAudioDataFromMp3(string filePath)
    {
        /*
        using (var reader = new NAudio.Wave.Mp3FileReader(filePath))
        using (var pcmStream = new NAudio.Wave.WaveFormatConversionStream(new NAudio.Wave.WaveFormat(44100, 2), reader))
        {
            var data = new byte[pcmStream.Length];
            int totalBytesRead = 0;
            while (totalBytesRead < data.Length)
            {
                totalBytesRead += pcmStream.Read(data, totalBytesRead, data.Length - totalBytesRead);
            }

            

            // Check the number of channels and sample rate
            int channels = pcmStream.WaveFormat.Channels;
            int sampleRate = pcmStream.WaveFormat.SampleRate;
            Console.WriteLine($"Channels: {channels}, Sample Rate: {sampleRate}");

*/

        //store as tmp.wav in the CacheFolder and read it as bytes
        ConvertMp3ToWav(filePath, Application.temporaryCachePath + "/tmp.wav");

        //return the bytes
        byte[] data = File.ReadAllBytes(Application.temporaryCachePath + "/tmp.wav");
        return data;
    }


    public void ConvertMp3ToWav(string mp3FilePath, string wavFilePath)
    {
        using (var reader = new NAudio.Wave.Mp3FileReader(mp3FilePath))
        using (var writer = new NAudio.Wave.WaveFileWriter(wavFilePath, reader.WaveFormat))
        {
            byte[] buffer = new byte[reader.WaveFormat.AverageBytesPerSecond * 4];
            int bytesRead;
            while ((bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
            {
                writer.Write(buffer, 0, bytesRead);
            }
        }
    }
    public Dictionary<string, int> ReadMp3Header(string filePath)
    {
        using (var reader = new NAudio.Wave.Mp3FileReader(filePath))
        {
            var format = reader.Mp3WaveFormat;

            int bitsPerSample = 0;
            if (reader.WaveFormat.Encoding == NAudio.Wave.WaveFormatEncoding.Pcm)
            {
                bitsPerSample = reader.WaveFormat.BitsPerSample;
            }

            Dictionary<string, int> header = new Dictionary<string, int>
    {
        { "Channels", format.Channels },
        { "SampleRate", format.SampleRate },
        { "ByteRate", format.AverageBytesPerSecond },
        { "BlockAlign", format.AverageBytesPerSecond*format.Channels },
        { "BitsPerSample", bitsPerSample }
    };

            return header;
        }
    }


    //Writers
    public void WriteWavFile(string filePath, int sampleRate, int channels, int bitsPerSample, int durationInSeconds)
    {
        using (var writer = new WaveFileWriter(filePath, WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels)))
        {
            double amplitude = 0.25 * short.MaxValue;
            double frequency = 1000;

            int samples = sampleRate * channels * durationInSeconds;
            for (int n = 0; n < samples; n++)
            {
                float sample = (float)(amplitude * Math.Sin((2 * Math.PI * frequency * n) / sampleRate));
                writer.WriteSample(sample);
            }
        }
    }

}
