namespace AnimefanPostUPs_Tools.CustomPopup
{

    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;
    using System;
    using AnimefanPostUPs_Tools.SmartColorUtility;
    using AnimefanPostUPs_Tools.ColorTextureItem;
    using AnimefanPostUPs_Tools.ColorTextureManager;
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

}