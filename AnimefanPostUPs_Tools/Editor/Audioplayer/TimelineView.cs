namespace AnimefanPostUPs_Tools.TimelineView
{
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
    using AnimefanPostUPs_Tools.AudioTrack;
    using AnimefanPostUPs_Tools.AudioTrackManager;
    //get Texturetypes
    //using AnimefanPostUPs_Tools.ColorTextureItem.TexItemType;
    using AnimefanPostUPs_Tools.MP3Reader;
    using AnimefanPostUPs_Tools.WavReader;
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
                    amgr.createMix(targetfolder, filename + "_auto");
                    amgr.SaveJson(targetfolder+"/"+filename+"_autosave.json");
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
                                    if (draggedObject is AudioClip)
                                    {
                                        //Create a new AudioTrack and add it to the AudioTrackManager (audiotrackmgr
                                        AudioTrack newAudioTrack = new AudioTrack(draggedObject as AudioClip);
                                        audiotrackmgr.addAudioTrack(newAudioTrack);
                                    }
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


                    timelinePosition_Offset.x += (xPos + (widthTimeline) / 2 - current.mousePosition.x) / 100;
                    timelinePosition_Offset.y += ((300) / 2 - current.mousePosition.y) / 100;
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


            int scaling = 10;
            //if (timelinezoom.x > 300) scaling = 20;
            //if (timelinezoom.x > 500) scaling = 30;
            //if (timelinezoom.x > 800) scaling = 40;
            //if (timelinezoom.x > 900) scaling = 50;
            //Draw gray background 
            //EditorGUI.DrawRect(new Rect(0, 0, maxTime*100, trackHeight * trackCount), GetRGBA(ColorRGBA.grayscale_016));
            GUILayout.BeginArea(new Rect(xPos + timelinePosition_Offset.x, timelinePosition_Offset.y, maxTime * timelinezoom.x, (trackHeight + 10) * (trackCount + 10)));

            //Draw a line for the timeline every 10 unitys
            for (int i = 0; i < maxTime; i++)
            {


                if (i % scaling / 5 == 0 || i % scaling == 0)
                {
                    EditorGUI.DrawRect(new Rect(i * timelinezoom.x / scaling, 0, timelinezoom.x / scaling, (trackHeight + 10) * (trackCount + 20)), GetRGBA(ColorRGBA.grayscale_032));
                }
                else EditorGUI.DrawRect(new Rect(i * timelinezoom.x / scaling, 0, timelinezoom.x / scaling, (trackHeight + 10) * (trackCount + 20)), GetRGBA(ColorRGBA.grayscale_025));

                if (i % scaling == 0)
                {
                    //Draw Label with time
                    EditorGUI.DrawRect(new Rect(i * timelinezoom.x / scaling, 0, 1, (trackHeight + 10) * (trackCount + 10)), GetRGBA(ColorRGBA.lightgreyred));
                }


                // Draw Label with time
                if (i % scaling == 0)
                    EditorGUI.LabelField(new Rect(1 + (i * timelinezoom.x / scaling), 2, 25, 14), (i / scaling).ToString());


            }

            timelinePosition = GUILayout.BeginScrollView(timelinePosition, GUILayout.Width((int)(maxTime * 10) * timelinezoom.x), GUILayout.Height(trackCount * (trackHeight * 2 + 2)));
            GUILayout.BeginHorizontal(GUILayout.Width(maxTime * timelinezoom.x));

            GUIStyle boxstyle = new GUIStyle(GUI.skin.box);
            boxstyle.normal.background = colormgr.LoadTexture(TexItemType.Bordered, ColorRGBA.grayscale_032, ColorRGBA.grayscale_064, 2);
            boxstyle.hover.background = colormgr.LoadTexture(TexItemType.Gradient_Horizontal, ColorRGBA.grayscale_048, ColorRGBA.grayscale_032, 32);


            //Draw the audiomanager.buildmix waveform
            if (amgr.buildTrackpreview != null)
            {
                //Draw Image / Waveform
                //Check if first pixel is white
                if (amgr.displayPreview)
                {
                    
                    EditorGUI.DrawPreviewTexture(new Rect(0, 15, amgr.previewLength * timelinezoom.x, trackHeight -15), amgr.buildTrackpreview, null, ScaleMode.StretchToFill);
                    //Add a Button to the Start to X

                    EditorGUI.LabelField(new Rect(19, 18, 100, 16), "Preview");
                    if (GUI.Button(new Rect(4, 18, 20, 20), "X"
                    , new GUIStyle() { normal = new GUIStyleState() { textColor = Color.white } }
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
                    EditorGUI.DrawRect(rect, GetRGBA(ColorRGBA.grayscale_048));
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



                float maxvalue = Mathf.Pow(2, audiotrackmgr.audioTracks[i].targetBitDepth) - 1;

                //Draw Image / Waveform
                if (audiotrackmgr.audioTracks[i].previewImage != null)
                    EditorGUI.DrawPreviewTexture(new Rect(_x, _y + 15, _width, Math.Max(_height - 15, 1)), audiotrackmgr.audioTracks[i].previewImage, null, ScaleMode.StretchToFill);


                EditorGUI.LabelField(new Rect(_x, _y, Math.Max(_width - 5, 1), 20), audiotrackmgr.audioTracks[i].clip.name
              //Make color black and bold
              , new GUIStyle() { normal = new GUIStyleState() { textColor = GetRGBA(ColorRGBA.white) }, fontStyle = FontStyle.Bold, fontSize = 12, clipping = TextClipping.Clip }
              );

            }

            //Draw a line for the timeline every 10 unitys
            for (int i = 0; i < maxTime * timelinezoom.x; i++)
            {



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


            //Draw line on playbackPosition
            if (displayPlayback)
            {
                EditorGUI.DrawRect(new Rect(playbackPosition * timelinezoom.x, 0, 2, (trackHeight + 10) * (trackCount + 10)), GetRGBA(ColorRGBA.orange));
            }



            GUILayout.EndHorizontal();

                //200 units spacing
                GUILayout.Space(200);
            GUILayout.EndScrollView();

            GUILayout.EndArea();




            if (doOverlay)
            {
                //Draw 2 White Boxes to indicate the position
                EditorGUI.DrawRect(new Rect(xPos + (widthTimeline) / 2 - 1, 0, 2, 900), GetRGBA(ColorRGBA.white));
                EditorGUI.DrawRect(new Rect(0, (150) - 1, 2000, 2), GetRGBA(ColorRGBA.white));
            }

            return doRepaint;
        }


    }

}