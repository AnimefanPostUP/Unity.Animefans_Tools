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
        timelineView = new TimelineView(500, 20, audioManager);
    }

    //on disable
    private void OnDisable()
    {
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
        if (timelineView.DrawTimeline(widthTimeline, splitviewer.splitPosition, colorTextureManager, Event.current)) Repaint();
        GUILayout.EndHorizontal();

    }

    Vector2 scrollPos;

    private void DrawSidePanel(float widthSidePanel)
    {
        GUILayout.BeginVertical();
        //Scrollview vertical
        scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Width(widthSidePanel));
        EditorGUI.DrawRect(new Rect(0, 0, widthSidePanel, position.height), GetRGBA(ColorRGBA.grayscale_064));

        int halfWidth = (int)(widthSidePanel / 2);

        //Create button style
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fixedWidth = widthSidePanel / 2 - 10;
        buttonStyle.fixedHeight = 25;

        //Create button style
        GUIStyle buttonStyle2 = new GUIStyle(GUI.skin.button);
        buttonStyle.fixedWidth = widthSidePanel / 3 - 10;
        buttonStyle.fixedHeight = 25;

        audioClip = EditorGUILayout.ObjectField("", audioClip, typeof(AudioClip), false, GUILayout.Width(100)) as AudioClip;
        //Call//Unity field for folder



        GUILayout.BeginHorizontal();
        targetfolder = EditorGUILayout.TextField("", targetfolder, GUILayout.Width(halfWidth / 2));
        if (GUILayout.Button("Select", GUILayout.Width(halfWidth)))
        {
            targetfolder = EditorUtility.OpenFolderPanel("Select Folder", targetfolder, "");
        }
        GUILayout.EndHorizontal();


        //function for buttons setting sample rate
        void CreateSampleRateButtons(string sampleRate)
        {
            if (GUILayout.Button(sampleRate, buttonStyle))
            {
                audioManager.targetSampleRate = int.Parse(sampleRate);
                audioManager.reloadAudioData();
            }
        }

        //Create the function
        void CreateBitDepthButton(string bitDepth)
        {
            if (GUILayout.Button(bitDepth, buttonStyle))
            {
                audioManager.targetBitDepth = int.Parse(bitDepth);
                audioManager.reloadAudioData();
            }
        }

        GUILayout.Label("Sample Rate: " + audioManager.targetSampleRate);
        GUILayout.BeginHorizontal();
        CreateSampleRateButtons("8000");
        CreateSampleRateButtons("16000");
        CreateSampleRateButtons("32000");
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        CreateSampleRateButtons("44100");
        CreateSampleRateButtons("48000");
        CreateSampleRateButtons("96000");
        GUILayout.EndHorizontal();




        GUILayout.Label("Bit Depth: " + audioManager.targetBitDepth);
        GUILayout.BeginHorizontal();
        CreateBitDepthButton("8");
        CreateBitDepthButton("16");
        CreateBitDepthButton("24");
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        CreateBitDepthButton("32");
        CreateBitDepthButton("48");
        CreateBitDepthButton("64");
        GUILayout.EndHorizontal();

        //Debug timeline variabled ALL
        Debug.Log("Timeline: " + timelineView.maxTime + " Tracks: " + audioManager.audioTracks.Count + " Track Height: " + timelineView.trackHeight + " Position: " + timelineView.timelinePosition + " Zoom: " + timelineView.timelinezoom);

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
        if (GUILayout.Button("Focus Folder", buttonStyle2))
        {
            //Focus in project window
            //Get relative path
            string relativepath = targetfolder.Replace(Application.dataPath, "Assets");
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(relativepath));
        }
        //Create File
        if (GUILayout.Button("Create Mix", buttonStyle2))
        {
            audioManager.createMix(targetfolder, filename + ".wav");
        }

        GUILayout.EndHorizontal();

        if (audioClip != null)
        {
            AudioTrack newAudioTrack = new AudioTrack(audioClip);
            audioManager.addAudioTrack(newAudioTrack);
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

            int oldinitTime = (int)track.initTime;
            track.initTime = EditorGUILayout.FloatField("", track.initTime, GUILayout.Width(25));

            //reload the audio data if the init time has changed
            if (oldinitTime != (int)track.initTime)
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
    public float trackHeight = 40;
    public Vector2 timelinePosition = new Vector2(0, 0);

    public Vector2 timelinePosition_Offset = new Vector2(0, 0);
    public Vector2 timelinezoom = new Vector2(100, 100);

    public int grabbedElement = -1;

    public float mousePositionX = 0;

    public float trackHeightOffset = 20;

    AudiotrackManager audiotrackmgr;

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
    public bool DrawTimeline(float widthTimeline, float xPos, ColorTextureManager colormgr, Event current)
    {
        //create style for box
        GUIStyle boxstylebg = new GUIStyle(GUI.skin.box);
        boxstylebg.normal.background = colormgr.LoadTexture(TexItemType.Gradient_Vertical, ColorRGBA.grayscale_016, ColorRGBA.grayscale_032, 32);
        GUI.Box(new Rect(xPos, 0, xPos + widthTimeline, 1000), "", boxstylebg);

        bool doRepaint = false;
        bool doOverlay = false;


        //set grab element to -1 if mouse is up or position is out of bounds of widthTimeline and Xpos or height with a margin of 10
        if (current.type == EventType.MouseUp || current.mousePosition.x < xPos || current.mousePosition.x > xPos + widthTimeline || current.mousePosition.y < 0 || current.mousePosition.y > trackHeight * (trackCount + 10))
        {
            grabbedElement = -1;
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
                            if (Path.GetExtension(AssetDatabase.GetAssetPath(draggedObject)) == ".wav")
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
            audiotrackmgr.audioTracks[grabbedElement].LoadAudioData();
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
        GUILayout.BeginArea(new Rect(xPos + timelinePosition_Offset.x, timelinePosition_Offset.y, maxTime * timelinezoom.x, (trackHeight + 10) * (trackCount + 50)));
        //Draw the timeline

        //Draw Gradient Background


        timelinePosition = GUILayout.BeginScrollView(timelinePosition, GUILayout.Width((int)(maxTime * 10) * timelinezoom.x), GUILayout.Height(200));
        GUILayout.BeginHorizontal(GUILayout.Width(maxTime * timelinezoom.x));

        GUIStyle boxstyle = new GUIStyle(GUI.skin.box);
        boxstyle.normal.background = colormgr.LoadTexture(TexItemType.Gradient_Horizontal, ColorRGBA.yellow, ColorRGBA.orange, 32);

        //Draw a line for the timeline every 10 unitys
        for (int i = 0; i < maxTime; i++)
        {

            if (i % 10 == 0)
            {
                //Draw Label with time
                EditorGUI.DrawRect(new Rect(i * timelinezoom.x / 10, 0, 1, (40) * trackCount), GetRGBA(ColorRGBA.red));
            }
            else
                EditorGUI.DrawRect(new Rect(i * timelinezoom.x / 10, 0, 3, (40) * trackCount), GetRGBA(ColorRGBA.grayscale_048));
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






            //Draw the Waveform using the bit depth relatively and create a box
            float resolution = 250*audiotrackmgr.audioTracks[i].clip.length * timelinezoom.x / 1000;
            float sumsampling=12;
            //get the max value that the bit depth can reach
            float maxvalue = Mathf.Pow(2, audiotrackmgr.audioTracks[i]._targetBitDepth) - 1;
            //iterate 100 Times 
            for (float j = 0; j < resolution*sumsampling; j++)
            {


                float sampleValue = 0;
                //Loo for channels
                for (int ch = 0; ch < audiotrackmgr.audioTracks[i].audioData.Length; ch++)
                {
                    //getting the next closest starting byte of a sample using modulo
                    int sampleIndex = (int)(audiotrackmgr.audioTracks[i].audioData[ch].Length * (j / (resolution*sumsampling))) - (((int)(audiotrackmgr.audioTracks[i].audioData[ch].Length * (j / (resolution*sumsampling)))) % (audiotrackmgr.audioTracks[i]._targetBitDepth / 8));

                    int samplebuffer = 0;

                    //Loop through the bit depth
                    for (int b = 0; b < audiotrackmgr.audioTracks[i]._targetBitDepth / 8; b++)
                    {
                        //Shift the bits to the left and add the next byte
                        samplebuffer = samplebuffer << 8;
                        samplebuffer += audiotrackmgr.audioTracks[i].audioData[ch][sampleIndex + b];
                    }

              
                        sampleValue = (samplebuffer +(sampleValue*1))/2;
                    

                }

                if (j%sumsampling==0 && false){
                EditorGUI.DrawRect(new Rect(
                    (int)(_x + ((j / (resolution)) * (_width)))/sumsampling, 
                    (int)(_y + (_height/2)-(sampleValue / (maxvalue)) * _height/2),
                    (int)((_width) / (resolution)), 
                    (int)((sampleValue / (maxvalue)) * _height)
                    
                ),GetRGBA(ColorRGBA.red));
                }
                //Testrect
                //EditorGUI.DrawRect(new Rect(_x , _y, _width, _height), GetRGBA(ColorRGBA.red));





            }


            //Draw box with size and position of rect

            EditorGUI.LabelField(new Rect(_x, _y, 20, 12), audiotrackmgr.audioTracks[i].clip.name
            //Make color black and bold
            , new GUIStyle() { normal = new GUIStyleState() { textColor = Color.black }, fontStyle = FontStyle.Bold, fontSize = 12 }
            );

        }

        //Draw a line for the timeline every 10 unitys
        for (int i = 0; i < maxTime; i++)
        {

            if (i % 10 == 0)
                EditorGUI.LabelField(new Rect(trackHeightOffset + i * 10, 0, 20, 12), (i / 10).ToString());

            //If this is the grabbed element draw a the Star and end Time
            if (grabbedElement == i)
            {
                //Draw Box behind the label
                EditorGUI.DrawRect(new Rect(audiotrackmgr.audioTracks[i].initTime * 100, i * (trackHeight + 10) + 40, trackHeightOffset + audiotrackmgr.audioTracks[i].clip.length * 100, 30), GetRGBA(ColorRGBA.grayscale_064));

                EditorGUI.LabelField(new Rect(audiotrackmgr.audioTracks[i].initTime * 100, trackHeightOffset + i * (trackHeight + 10) + 30, 100, 12), audiotrackmgr.audioTracks[i].initTime.ToString());
                EditorGUI.LabelField(new Rect(audiotrackmgr.audioTracks[i].initTime * 100 + (audiotrackmgr.audioTracks[i].clip.length / 2) * 100, trackHeightOffset + i * (trackHeight + 10) + 30, 100, 12), (audiotrackmgr.audioTracks[i].initTime + audiotrackmgr.audioTracks[i].clip.length).ToString());
            }



        }



        GUILayout.EndHorizontal();
        GUILayout.EndScrollView();

        GUILayout.EndArea();


        if (doOverlay)
        {
            //Draw 2 White Boxes to indicate the position
            EditorGUI.DrawRect(new Rect(xPos + (widthTimeline) / 2 - 1, 0, 2, 900), GetRGBA(ColorRGBA.white));
            EditorGUI.DrawRect(new Rect(0, (250) - 1, 2000, 2), GetRGBA(ColorRGBA.white));
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

    public void createMix(string filePath, string filename)
    {

        //Load all audio data
        reloadAudioData();
        byte[][] mixedData = MixAudioData();
        WriteWavFile(mixedData, filePath, filename, targetSampleRate, targetChannels, targetBitDepth);
    }

    public int getMixLength(int channel)
    {
        int maxLength = 0;
        foreach (var audiotrack in audioTracks)
        {
            if (audiotrack.audioData[channel].Length > maxLength)
            {
                maxLength = audiotrack.audioData[channel].Length + (int)audiotrack.initTime * audiotrack._targetSampleRate;
            }
        }
        return maxLength;
    }

    public byte[][] MixAudioData()
    {
        byte[][][] mixedData = new byte[2][][]; //CHANNEL TRACK BYTES

        //set amount of tracks
        for (int ch = 0; ch < 2; ch++)
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

        for (int ch = 0; ch < 2; ch++)
        {
            for (int i = 0; i < audioTracks.Count; i++) //Iterate Tracks
            {
                for (int b = 0; b < mixedData[ch][i].Length; b++) //Iterate Bytes
                {
                    //Write 0 if before or after the track
                    if (b >= (int)(audioTracks[i].initTime * targetSampleRate) && b < (int)(audioTracks[i].initTime * targetSampleRate) + audioTracks[i].audioData[ch].Length)
                    {
                        mixedData[ch][i][b] = audioTracks[i].audioData[ch][b - (int)(audioTracks[i].initTime * targetSampleRate)];
                    }
                    else
                    {
                        mixedData[ch][i][b] = 0;
                    }
                }
            }
        }

        ReportAudioData(mixedData[0][0], "Collected CH1");
        ReportAudioData(mixedData[1][0], "Collected CH2");

        return MixAudioBytes(mixedData);
    }

    //Combine all audio tracks into one byte array
    public byte[][] MixAudioBytes(byte[][][] audiolines)
    {
        int numChannels = audiolines.Length;
        int numTracks = audiolines[0].Length;
        int numBytes = audiolines[0][0].Length;

        byte[][] mixedData = new byte[numChannels][];

        for (int ch = 0; ch < numChannels; ch++)
        {
            mixedData[ch] = new byte[numBytes];

            for (int i = 0; i < numBytes; i++)
            {
                int product = 1;
                for (int j = 0; j < numTracks; j++)
                {
                    product *= audiolines[ch][j][i];
                }
                mixedData[ch][i] = (byte)product;
            }
        }

        // Normalize the mixed data
        for (int ch = 0; ch < numChannels; ch++)
        {
            byte originalMax = mixedData[ch].Max();
            byte multipliedMax = mixedData[ch].Max();

            for (int i = 0; i < numBytes; i++)
            {
                if (multipliedMax != 0)
                {
                    mixedData[ch][i] = (byte)(mixedData[ch][i] * originalMax / multipliedMax);
                }
                else
                {
                    mixedData[ch][i] = 0;
                }
            }
        }

        //Debug
        Debug.Log("Mixed Data CH1: " + mixedData[0].Length);
        Debug.Log("Mixed Data CH2: " + mixedData[1].Length);

        ReportAudioData(mixedData[0], "Mixed CH1");
        ReportAudioData(mixedData[1], "Mixed CH2");

        return mixedData;
    }


    //Get C

    public void WriteWavFile(
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

        //RIFF chunk descriptor
        header[0] = 82; //R
        header[1] = 73; //I
        header[2] = 70; //F
        header[3] = 70; //F

        //file size
        int audioDataSize = 0;

        //Iterate channels
        for (int ch = 0; ch < numChannels; ch++)
        {
            audioDataSize += audioData[ch].Length;
        }

        int fileSize = audioDataSize + 44;
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
        Debug.Log("File Written: " + filePath);
        Debug.Log("Data Size: " + dataSize);
        Debug.Log("File Size: " + fileSize);

        ReportAudioData(audioData[0], "Output All");


        fileStream.Close();

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
    //Unity Interal Data
    public AudioClip clip;
    public float startTime = 0;
    public string absolutePath;
    public float initTime = 0;
    public byte[][] audioData;

    private int _oldSampleRate = 0;
    private int _oldChannels = 0;
    private int _oldBitDepth = 0;
    private float _length = 0;


    public int _targetSampleRate = 44100; // Target sample rate in Hz
    public int _targetBitDepth = 16; // Target bit depth in bits
    public int _targetChannels = 2; // Target number of channels

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
    public AudioTrack(AudioClip clip)
    {
        this.clip = clip;
        this.absolutePath = AssetDatabase.GetAssetPath(clip);

        LoadAudioData();
    }


    public void LoadAudioData()
    {
        // Get the audio data for each channel
        byte[][] channelData = GetChannels();

        //initialize the arrays
        audioData = new byte[2][];
        this.audioData[0] = GetSampledAudioData(channelData[0], _oldSampleRate, _oldBitDepth, _targetSampleRate, _targetBitDepth);
        this.audioData[1] = GetSampledAudioData(channelData[1], _oldSampleRate, _oldBitDepth, _targetSampleRate, _targetBitDepth);

        //Debug count of audio data
        Debug.Log("Audio Data CH1: " + audioData[0].Length);
        Debug.Log("Audio Data CH2: " + audioData[1].Length);
    }

    public byte[] uncompressData(byte[] audioBytes)
    {
        // Now you can read the uncompressed audio data from "temp.wav"


        return audioBytes;
    }

    public byte[] GetAudioDataFromWav(byte[] wavFile)
    {
        int i = 0;

        // Convert 4 bytes to a string
        Func<int, string> getString = startIndex => Encoding.ASCII.GetString(wavFile, startIndex, 4);

        // Convert 4 bytes to an int
        Func<int, int> getInt = startIndex => BitConverter.ToInt32(wavFile, startIndex);

        // Skip the RIFF header
        i += 4;

        // Skip the file size
        i += 4;

        // Skip the WAVE header
        i += 4;

        while (getString(i) != "data")
        {
            // Skip the chunk ID
            i += 4;

            // Get the chunk size
            int chunkSize = getInt(i);

            // Skip the chunk size
            i += 4;

            // Skip the chunk data
            i += chunkSize;
        }

        // Skip the 'data' chunk ID
        i += 4;

        // Get the 'data' chunk size
        int dataSize = getInt(i);

        // Skip the 'data' chunk size
        i += 4;

        // Copy the audio data to a new array
        byte[] audioData = new byte[dataSize];
        Array.Copy(wavFile, i, audioData, 0, dataSize);

        return audioData;
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
        byte[] audioBytes = readAudioBytes();

        Debug.Log("Total Bytes: " + audioBytes.Length);

        audioBytes = GetAudioDataFromWav(audioBytes);

        ReportAudioData(audioBytes, "Read Source Bytes");

        //Get Audiodata
        int totalBytes = audioBytes.Length; // Subtract 44 to exclude the .wav header
        float durationInSeconds = clip.length;
        Debug.Log("durationInSeconds: " + durationInSeconds);

        //Read Header
        Dictionary<string, int> header = ReadWavHeader(absolutePath);

        int sampleRate = header["SampleRate"];
        int bitDepth = header["BitsPerSample"];
        int channels = header["Channels"];
        int blockSize = header["BlockAlign"];

        int bytesPerSample = bitDepth / 8;

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
        int validateSamplecount = ((audioBytes.Length) / (bytesPerSample) / channels);
        if (validateSamplecount != Math.Round(sampleRate * durationInSeconds))
        {
            //Debug warning with samplecount and expected samplecount
            Debug.LogWarning("Invalid Sample Count");
            Debug.LogWarning("Sample Count: " + validateSamplecount);
            Debug.LogWarning("Expected Sample Count: " + sampleRate * durationInSeconds);
            //Try if Channelcount was wrong
            channels = 1;
            validateSamplecount = ((audioBytes.Length) / (bytesPerSample) / channels);
            if (validateSamplecount != sampleRate * durationInSeconds)
            {

                Debug.LogError("Invalid Sample Count");
                return null;
            }
        }


        //Data Init End ---------------------------------------------


        // Create a jagged array to hold the audio data for each channel
        byte[][] channelData = new byte[channels][];

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

        ReportAudioData(channelData[0], ("Separated CH1"));
        ReportAudioData(channelData[1], ("Separated CH2"));

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

    public Dictionary<string, int> ReadWavHeader(string filePath)
    {
        using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        using (var reader = new BinaryReader(fileStream))
        {
            // Read the RIFF header
            string riff = new string(reader.ReadChars(4));
            if (riff != "RIFF")
                throw new Exception("Invalid file format");

            // Skip file size
            reader.ReadInt32();

            // Read the WAVE header
            string wave = new string(reader.ReadChars(4));
            if (wave != "WAVE")
                throw new Exception("Invalid file format");

            // Read the fmt chunk
            string fmt = new string(reader.ReadChars(4));
            if (fmt != "fmt ")
                throw new Exception("Invalid file format");

            // Skip chunk size
            reader.ReadInt32();

            // Read audio format (should be 1 for PCM)
            short audioFormat = reader.ReadInt16();
            if (audioFormat != 1)
                throw new Exception("Invalid file format");

            // Read number of channels
            short numChannels = reader.ReadInt16();

            // Read sample rate
            int sampleRate = reader.ReadInt32();

            // Skip byte rate
            reader.ReadInt32();

            // Read block align
            short blockAlign = reader.ReadInt16();

            // Read bits per sample
            short bitsPerSample = reader.ReadInt16();

            Debug.Log($"Channels: {numChannels}, Sample Rate: {sampleRate}, Bits Per Sample: {bitsPerSample}, Block Align: {blockAlign}");

            //Create return dict
            Dictionary<string, int> header = new Dictionary<string, int>();

            header.Add("Channels", numChannels);
            header.Add("SampleRate", sampleRate);
            header.Add("BitsPerSample", bitsPerSample);
            header.Add("BlockAlign", blockAlign);

            return header;
        }

    }

}
