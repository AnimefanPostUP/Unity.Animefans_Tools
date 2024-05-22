namespace AnimefanPostUPs_Tools.AudioTrackManager
{

    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;

    using System;
    using System.Text;
    using System.Collections;
    using System.Reflection;
    //using Unity.EditorCoroutines.Editor;
    using System.Linq;
    using System.IO;
    using System.Threading.Tasks;

    using NAudio;
    using NAudio.Wave;
    using NAudio.Wave.SampleProviders;

    using AnimefanPostUPs_Tools.AudioMixUtils;

    using AnimefanPostUPs_Tools.AudioTrack;

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
        public bool snapView = false;
        public float setting_normalizationFac_Input = 1.0f;
        public float setting_normalizationFac_Output = 0.5f;
        public bool setting_doNormalizeInput = true;
        public bool setting_doNormalizeOutput = true;
        public bool autosave = false;
        public bool optimizedBuild = false;

        public bool internalupdate = false;

        public byte[][] mixedData;
        public int totallength = 0;

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
                track.targetSampleRate = targetSampleRate;
                track.targetBitDepth = targetBitDepth;
                track.targetChannels = targetChannels;
                track.setting_doNormalizeInput = setting_doNormalizeInput;
                track.setting_normalizationFac_Input = setting_normalizationFac_Input;
                track.targetgain_scaling = targetgain_In;

                //Mark everything dirty
                track.marked_dirty_normalization = true;
                track.marked_dirty_preview = true;
                track.marked_dirty_settings = true;
                track.marked_dirty_time = true;
                track.marked_dirty_render = true;

                track.checkUpdate();
            }
        }

        public void renderSettings(int samplerate, int bitrate, int channels, bool normalizeInput, float normalizationFacInput, bool normalizeOutput, float normalizationFacOutput, float targetgain_In, float targetgain_Out, bool autobuild, bool snapView, bool autosave, bool optimizedBuild)
        {
            this.autobuild = autobuild;
            this.snapView = snapView;
            this.autosave = autosave;
            this.optimizedBuild = optimizedBuild;

            this.setting_normalizationFac_Input = normalizationFacInput;
            this.setting_normalizationFac_Output = normalizationFacOutput;
            this.setting_doNormalizeInput = normalizeInput;
            this.setting_doNormalizeOutput = normalizeOutput;

            this.targetSampleRate = samplerate;
            this.targetBitDepth = bitrate;
            this.targetChannels = channels;

            this.targetgain_In = targetgain_In;
            this.targetgain_Out = targetgain_Out;

            if (autosave)
            {
                internalupdate = true;
            }

            reloadAudioData();
        }


        //addAudiotrack
        public void addAudioTrack(AudioTrack track)
        {
            audioTracks.Add(track);
            //Set the target settings
            track.targetSampleRate = targetSampleRate;
            track.targetBitDepth = targetBitDepth;
            track.targetChannels = targetChannels;
        }

        //Remove
        public void removeAudioTrack(AudioTrack track)
        {
            audioTracks.Remove(track);
        }

        public void checkForJson(string filePath, string filename)
        {
            //Try to find the json file in the current path using the filename
            if (File.Exists(filePath + "/" + filename + "_autosave.json"))
            {
                //Load the json file
                LoadJson(filePath + "/" + filename + "_autosave.json");
            }
            else if (File.Exists(filePath + "/" + filename + ".json"))
            {
                //Load the json file
                LoadJson(filePath + "/" + filename + ".json");
            }
        }



        public void createMix(string filePath, string filename)
        {
            //Load all audio data

            int counter = 0;
            int startingsample = int.MaxValue;
            int endsample = int.MinValue;
            bool optimized = optimizedBuild;


            //iterate Tracks to Check for Updates

            for (int i = 0; i < audioTracks.Count; i++)
            {
                audioTracks[i].checkUpdate();
            }

            //Iterate all tracks in a loop and debug thier count
            if (optimized && mixedData != null)
            {
                startingsample = int.MaxValue; ;
                endsample = int.MinValue;
                for (int i = 0; i < audioTracks.Count; i++)
                {
                    //Debug.Log("Checking track" + i + " for render dirty");
                    if (audioTracks[i].marked_dirty_render)
                    {
                        //Debug.Log("marked render dirty");
                        if (audioTracks[i].old_initTime != audioTracks[i].initTime)
                        {
                            //Debug.Log("inittime");
                            audioTracks[i].marked_dirty_time = false;
                            counter++;

                            //Get Old and New init time
                            float oldinitTime = audioTracks[i].old_initTime;
                            float newinitTime = audioTracks[i].initTime;


                            startingsample = Math.Min((int)(newinitTime * audioTracks[i].targetSampleRate), startingsample);
                            startingsample = Math.Min((int)(oldinitTime * audioTracks[i].targetSampleRate), startingsample);
                            endsample = Math.Max((int)((newinitTime + audioTracks[i].clip.length) * audioTracks[i].targetSampleRate), endsample);
                            endsample = Math.Max((int)((oldinitTime + audioTracks[i].clip.length) * audioTracks[i].targetSampleRate), endsample);

                        }
                    }
                }


                //make sure both starts are above -1
                if (startingsample < 0) startingsample = 0;
                if (endsample < 0) endsample = 0;

                //check if end is larger than the existing data
                if (endsample > mixedData[0].Length) optimized = false;
            }

            //Debug.Log(counter + " Tracks have been updated");

            //disable if no changes
            if (counter == 0 || !optimized)
            {
                startingsample = -1;
                endsample = -1;
                optimized = false;
            }

            bool filecheck = false;
            (filecheck, mixedData) = MixAudioBytesCombined(mixedData, startingsample, endsample);

            optimized = optimized && filecheck;

            byte[][] targetdata; //Extra Array to avoid normalizing the mixedData, needs to be further optimized later




            if (setting_doNormalizeOutput) //Normalize
            {
                targetdata = new byte[targetChannels][];
                for (int i = 0; i < targetChannels; i++)
                {
                    targetdata[i] = new byte[mixedData[0].Length];
                }


                int maxvalue = (int)Math.Pow(2, targetBitDepth) - 1;
                if (targetBitDepth > 8) maxvalue = maxvalue / 2;
                for (int i = 0; i < mixedData.Length; i++)
                    targetdata[i] = AudioMixUtils.Normalize(mixedData[i], targetBitDepth, targetBitDepth > 8, maxvalue, setting_normalizationFac_Output);
            }
            else
            {
                targetdata = mixedData;
            }



            int samples = getMixLength(0) / (targetBitDepth / 8);
            previewLength = (float)samples / targetSampleRate;

            //Check Texture2D and recreate if null
            if (buildTrackpreview == null)
            {
                buildTrackpreview = new Texture2D(2048, 255);
            }

            AudioTrack.DrawWaveform(AudioTrack.getFloatArrayFromSamples(targetBitDepth, targetdata, targetChannels, samples), buildTrackpreview.width, buildTrackpreview.height, buildTrackpreview, new Color(0.9f, 0.2f, 0.2f, 1));
            this.displayPreview = true;

            //Debug file exist
            //Debug.Log("File Exist: " + File.Exists(filePath + "/" + filename + ".wav"));



            if (counter <= 0 || !File.Exists(filePath + "/" + filename + ".wav") || !optimized)
            {
                CreateWaveFile(targetdata, filePath, filename + ".wav", targetSampleRate, targetChannels, targetBitDepth);
            }
            else
            {
                OverwriteArea(filePath, filename + ".wav", targetdata, targetSampleRate, targetChannels, targetBitDepth, startingsample, endsample);
            }



            //store inittimes for this mix
            for (int i = 0; i < audioTracks.Count; i++)
            {
                audioTracks[i].old_initTime = audioTracks[i].initTime;
                //Set marked_dirty_render to false
                audioTracks[i].marked_dirty_render = false;
            }

            // Reload the Folder
            AssetDatabase.Refresh();
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
                    maxLength = (int)Math.Round(((audiotrack.clip.length + (audiotrack.initTime)) * audiotrack.targetSampleRate * (targetBitDepth / 8)));
                }
            }
            return maxLength;
        }



        public (bool, byte[][]) MixAudioBytesCombined(byte[][] mixedData, int renderstartsample = -1, int renderendsample = -1)
        {
            //Prepare Inputs


            bool filesizeSufficient = true;

            int numChannels = targetChannels;
            int numTracks = audioTracks.Count;
            int numBytes = getMixLength(0);
            int bytesPerSample = targetBitDepth / 8;

            //Checking if Renderarea is not Valid
            if (
                mixedData == null ||
                numChannels > mixedData.Length ||
                mixedData[0] == null ||
                mixedData[0].Length < numBytes ||
                mixedData[0].Length > numBytes
            )
            {
                mixedData = new byte[numChannels][];
                filesizeSufficient = false;
            }

            if (renderstartsample == -1) { renderstartsample = 0; filesizeSufficient = false; }
            if (renderendsample == -1) { renderendsample = numBytes; filesizeSufficient = false; }

            //Debug.Log("Renderstartsample: " + renderstartsample + " Renderendsample: " + renderendsample);




            // Prepare Settings
            int maxvalue = (int)Math.Pow(2, targetBitDepth) - 1;
            int totalcounter = 0;

            //Vuffers
            int current_source_Index = 0;
            int current_dest_Index = 0;


            for (int ch = 0; ch < numChannels; ch++) //channels
            {
                if (!filesizeSufficient)
                    mixedData[ch] = new byte[numBytes];

                for (int i = 0; i < numBytes; i += bytesPerSample) //Samples
                {
                    if (filesizeSufficient && (i / bytesPerSample < renderstartsample || i / bytesPerSample > renderendsample)) continue;
                    long sum = 0;
                    int counter = 0;


                    for (int j = 0; j < numTracks; j++) //Tracks
                    {
                        //Find the Starting Index of a Track:
                        current_source_Index = i - (int)(audioTracks[j].initTime * targetSampleRate) * bytesPerSample;

                        //Limit Rendering to the Track Length
                        if (current_source_Index >= 0 && current_source_Index + bytesPerSample - 1 < audioTracks[j].audioDataNormalized[ch].Length)
                        {

                            totalcounter++;
                            //Copy the Bytechunk
                            byte[] sampleBytes = new byte[bytesPerSample];
                            Array.Copy(audioTracks[j].audioDataNormalized[ch], current_source_Index, sampleBytes, 0, bytesPerSample);
                            long sample = 0;
                            (counter, sample) = readTracksBytes(targetBitDepth, sampleBytes, counter);
                            // Add other bit depths as needed
                            sum += sample;

                            current_dest_Index = -1;
                            current_source_Index = -1;
                        }
                    }

                    //Adjust Value based on amounts of Tracks that Contributed
                    if (counter == 0) { counter = 1; }
                    long mixedSample = sum / numTracks / 2;
                    counter = 0;

                    byte[] mixedSampleBytes = new byte[bytesPerSample];
                    mixedSampleBytes = doubleToByte(targetBitDepth, mixedSample);

                    for (int b = 0; b < bytesPerSample; b++)
                    {
                        if (i + b < mixedData[ch].Length)
                        {
                            mixedData[ch][i + b] = mixedSampleBytes[b];
                        }
                    }
                }
            }
            //Debug.Log("Bytes Rendered:" + totalcounter);
            return (filesizeSufficient, mixedData);
        }


        public (int, long) readTracksBytes(int targetBitDepth, byte[] sampleBytes, int counter)
        {
            long sample = 0;

            if (targetBitDepth == 8)
            {
                counter++;
                sample = sampleBytes[0];
            }
            else if (targetBitDepth == 16)
            {
                counter++;
                sample = BitConverter.ToInt16(sampleBytes, 0);
            }
            else if (targetBitDepth == 24)
            {
                counter++;
                // Treat the 24-bit data as a 32-bit signed integer
                sample = sampleBytes[0] | (sampleBytes[1] << 8) | (sampleBytes[2] << 16);
                if ((sample & 0x800000) != 0)
                {
                    // If the sign bit is set, extend the sign bit
                    sample |= 0xFF000000;
                }
            }
            else if (targetBitDepth == 32)
            {
                counter++;
                sample = BitConverter.ToInt32(sampleBytes, 0);
            }
            else if (targetBitDepth == 64)
            {
                counter++;
                sample = BitConverter.ToInt64(sampleBytes, 0);
            }

            return (counter, sample);
        }

        public byte[] doubleToByte(int targetBitDepth, double mixedSample)
        {
            byte[] mixedSampleBytes = null;

            if (targetBitDepth == 8)
            {
                mixedSampleBytes = new byte[] { (byte)mixedSample };
            }
            else if (targetBitDepth == 16)
            {
                mixedSampleBytes = BitConverter.GetBytes((short)mixedSample);
            }
            else if (targetBitDepth == 32)
            {
                mixedSampleBytes = BitConverter.GetBytes((int)mixedSample);
            }
            else if (targetBitDepth == 64)
            {
                mixedSampleBytes = BitConverter.GetBytes(mixedSample);
            }

            return mixedSampleBytes;
        }


        public void OverwriteArea(
            string filePath,
            string filename,
            byte[][] audioData,
            int sampleRate,
            int numChannels,
            int bitDepth,
            int startingSample,
            int endingSample)
        {
            // Combine file path and filename
            filePath = Path.Combine(filePath, filename);

            // Open the file in read/write mode
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
            {
                // Create a new header
                byte[] header = new byte[44];
                int audioDataSize = audioData.Sum(ch => ch.Length);
                int fileSize = audioDataSize + 44;
                header = CreateWavFileHeader(header, sampleRate, numChannels, bitDepth, audioDataSize, fileSize);

                // Write the new header to the file
                fileStream.Write(header, 0, header.Length);

                // Calculate the size of a sample in bytes
                int sampleSize = (int)bitDepth / 8;

                // Calculate the starting and ending positions in the file
                int startPos = 44 + startingSample * sampleSize; // 44 for the header
                int endPos = 44 + endingSample * sampleSize;


                // Set the position in the file
                fileStream.Position = startPos;

                int counter = 0;


                for (int i = startingSample * sampleSize; i < endingSample * sampleSize; i += (int)bitDepth / 8)
                {
                    for (int ch = 0; ch < numChannels; ch++)
                    {
                        for (int b = 0; b < (int)bitDepth / 8; b++)
                        {
                            if (i + b < audioData[ch].Length)
                            {
                                fileStream.WriteByte(audioData[ch][i + b]);
                                counter++;
                            }
                        }
                    }
                }


                fileStream.Close();


                //Debug how many of the bytes were written in comparison to the total
                Debug.Log("Bytes Written: " + counter + " Total Bytes: " + audioDataSize);
            }
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


        //AudioData Class for storing Json Data
        [Serializable]
        public class AudioData
        {
            public string[] ClipPaths;
            public string[] GUIDs;
            public float[] InitTimes;
            public float[] targetgains;
            public bool[] muted;
            public string OutputFilePath;
            public string OutputFileName;

            //Store Noramlization settings
            public float normalizationFacInput;
            public float normalizationFacOutput;
            public bool doNormalizeInput;
            public bool doNormalizeOutput;

            //Store gain
            public float targetgain_In;
            public float targetgain_Out;

            public bool autobuild;
            public bool snapView;
            public bool autosave;
            public bool optimizedBuild;

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

                //Try get the clip using the GUID
                string assetPath = "";
                if (audioData.GUIDs !=null && audioData.GUIDs[i] != "" )
                {
                    assetPath = AssetDatabase.GUIDToAssetPath(audioData.GUIDs[i]);
                    //check if the asset exists
                    if (assetPath == "")
                    {
                        assetPath = audioData.ClipPaths[i];
                    }

                }
                else
                {
                    assetPath = audioData.ClipPaths[i];
                }

                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
                if (clip != null)
                {
                    AudioTrack audioTrack = new AudioTrack(clip);
                    if (audioData.InitTimes.Length > i)
                        audioTrack.initTime = audioData.InitTimes[i];
                    if (audioData.targetgains != null)
                        if (audioData.targetgains.Length > i)
                            audioTrack.targetgain = audioData.targetgains[i];
                    if (audioData.muted != null)
                        if (audioData.muted.Length > i)
                            audioTrack.muted = audioData.muted[i];

                    audioTracks.Add(audioTrack);
                }
            }

            //set the normalization settings
            this.setting_normalizationFac_Input = audioData.normalizationFacInput;
            this.setting_normalizationFac_Output = audioData.normalizationFacOutput;
            this.setting_doNormalizeInput = audioData.doNormalizeInput;
            this.setting_doNormalizeOutput = audioData.doNormalizeOutput;

            this.targetgain_In = audioData.targetgain_In;
            this.targetgain_Out = audioData.targetgain_Out;

            //autobuild, snapview, autosave
            this.autobuild = audioData.autosave;
            this.snapView = audioData.snapView;
            this.autosave = audioData.autosave;
            this.optimizedBuild = audioData.optimizedBuild;
            reloadAudioData();

        }

        public void SaveJson(string filePath)
        {

            AudioData audioData = new AudioData
            {
                ClipPaths = new string[audioTracks.Count],
                GUIDs = new string[audioTracks.Count],
                InitTimes = new float[audioTracks.Count],
                targetgains = new float[audioTracks.Count],
                muted = new bool[audioTracks.Count],
                OutputFilePath = filePath,
                OutputFileName = Path.GetFileNameWithoutExtension(filePath),
                //Store Noramlization settings

                normalizationFacInput = setting_normalizationFac_Input,
                normalizationFacOutput = setting_normalizationFac_Output,
                doNormalizeInput = setting_doNormalizeInput,
                doNormalizeOutput = setting_doNormalizeOutput,
                targetgain_In = targetgain_In,
                targetgain_Out = targetgain_Out,

                //settingsd
                autobuild = this.autobuild,
                snapView = this.snapView,
                autosave = this.autosave,
                optimizedBuild = this.optimizedBuild
            };

            for (int i = 0; i < audioTracks.Count; i++)
            {
                audioData.ClipPaths[i] = AssetDatabase.GetAssetPath(audioTracks[i].clip);
                audioData.GUIDs[i] = audioTracks[i].guid;
                audioData.InitTimes[i] = audioTracks[i].initTime;
                audioData.targetgains[i] = audioTracks[i].targetgain;
                audioData.muted[i] = audioTracks[i].muted;
                audioData.GUIDs[i] = audioTracks[i].guid;

            }

            // Store clips in json
            string json = JsonUtility.ToJson(audioData);

            // Write the Json File
            File.WriteAllText(filePath, json);


            // Write the Json File
            File.WriteAllText(filePath, json);

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


            //Debug.Log("Data Size: " + dataSize);
            //Debug.Log("File Size: " + fileSize);

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
            fileStream.Close();
        }



        //report average and max from audio data byte array
        public void ReportAudioData(byte[] bytedata, string name = "")
        {
            AudioTrack.ReportAudioData(bytedata, name);
        }

    }

}