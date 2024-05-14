namespace AnimefanPostUPs_Tools.AudioTrack
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
    using AnimefanPostUPs_Tools.AudioMixUtils;
    //get Texturetypes
    //using AnimefanPostUPs_Tools.ColorTextureItem.TexItemType;
    using AnimefanPostUPs_Tools.MP3Reader;
    using AnimefanPostUPs_Tools.WavReader;


    //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    //Audiotrack
    public class AudioTrack
    {
        //Main Variables

        public bool attemptUpdate = false;
        public AudioClip clip;
        public string absolutePath;
        public byte[][] audioData; //Final

        public byte[][] audioDataNormalized;


        public byte[][] sampledChannels;

        //Databuffer of sourcefile
        private int _oldSampleRate = 0;
        private int _oldChannels = 0;
        private int _oldBitDepth = 0;
        private float _length = 0;
        public bool muted=false;
        Dictionary<string, int> header;
        public float oldtargetgain = 0;
        public float[][] audioCurve;
        public Texture2D previewImage;
        public int curveResolution = 250;

        //Targetsettings
        private int _targetSampleRate = 44100;
        public int targetSampleRate
        {
            get { return _targetSampleRate; }
            set
            {
                if (value != _targetSampleRate)
                {
                    //Debug.Log("Setting SampleRate to: " + value);
                    marked_dirty_settings = true;
                    _targetSampleRate = value;
                }

            }
        }

        private int _targetBitDepth = 16;
        public int targetBitDepth
        {
            get { return _targetBitDepth; }
            set
            {

                if (value != _targetBitDepth)
                {
                    //Debug.Log("Setting BitDepth to: " + value);
                    marked_dirty_settings = true;
                    _targetBitDepth = value;
                }
            }
        }

        private int _targetChannels = 2;
        public int targetChannels
        {
            get { return _targetChannels; }
            set
            {
                if (value != _targetChannels)
                {
                    //Debug.Log("Setting Channels to: " + value);
                    marked_dirty_settings = true;
                    _targetChannels = value;
                }
            }
        }

        private float _targetgain = 0.7f;
        public float targetgain
        {
            get { return _targetgain; }
            set
            {
                if (value != _targetgain)
                {
                    //Debug.Log("Setting Gain to: " + value);
                    marked_dirty_normalization = true;
                    _targetgain = value;
                }
            }
        }

        private float _targetgain_scaling = 0.7f;
        public float targetgain_scaling
        {
            get { return _targetgain_scaling; }
            set
            {
                if (value != _targetgain_scaling)
                {
                    //Debug.Log("Setting Gain to: " + value);
                    marked_dirty_normalization = true;
                    _targetgain_scaling = value;
                }
            }
        }


        public float old_initTime = 0;

        private float _initTime = 0;
        public float initTime
        {
            get { return _initTime; }
            set
            {
                if (value != _initTime)
                {
                    //Debug.Log("Setting Time to: " + value);
                    marked_dirty_time = true;
                    marked_dirty_render = true;
                    _initTime = value;
                }
            }
        }

        private int _startsample = 0;
        public int startsample
        {
            get { return (int)(_initTime * _targetSampleRate); }
            set
            {
                if (value != _startsample)
                {
                    //Debug.Log("Setting Sample to: " + value);
                    marked_dirty_time = true;
                    _startsample = value;
                }
            }
        }

        private int _endsample = 0;
        public int endsample
        {
            get { return ((int)((_startsample) + ((clip.length) * _targetSampleRate))); }
            set
            {
                if (_endsample != value)
                {
                    //Debug.Log("Setting Sample to: " + value);
                    marked_dirty_time = true;
                    _endsample = value;
                }
            }
        }

        //Getter and Setter for doNormalizeInput
        private bool _setting_doNormalizeInput = true;
        public bool setting_doNormalizeInput
        {
            get { return _setting_doNormalizeInput; }
            set
            {

                if (_setting_doNormalizeInput != value)
                {
                    //Debug.Log("Setting Normalize to: " + value);
                    marked_dirty_normalization = true;
                    _setting_doNormalizeInput = value;
                }

            }
        }

        //Threshold for normalization
        private float _setting_normalizationFac_Input = 0.3f;
        public float setting_normalizationFac_Input
        {
            get { return _setting_normalizationFac_Input; }
            set
            {
                if (_setting_normalizationFac_Input != value)
                {
                    marked_dirty_normalization |= true;
                    _setting_normalizationFac_Input = value;
                }
            }

        }

        public bool marked_dirty_time = true;


        public bool marked_dirty_settings = true;
        public bool marked_dirty_normalization = true;
        public bool marked_dirty_preview = true;
        public bool marked_dirty_render= true;



        public AudioTrack(AudioClip clip)
        {
            this.clip = clip;
            this.absolutePath = AssetDatabase.GetAssetPath(clip);
            previewImage = new Texture2D(512, 255);
            checkUpdate();
        }

        public void checkUpdate()
        {

            //Debug the marked dirty
            //Debug.Log("Marked Dirty: " + marked_dirty_settings + " " + marked_dirty_normalization + " " + marked_dirty_preview + " " + marked_dirty_time);

            if (marked_dirty_settings || audioData == null)
            {
                marked_dirty_render = true;
                update_AudioData();
                updated_PreviewImage();
                marked_dirty_settings = false;
            }

            if (marked_dirty_normalization || audioDataNormalized == null)
            {
                marked_dirty_render = true;
                update_NormalizedAudioData();
                marked_dirty_normalization = false;
            }

            if (marked_dirty_preview || previewImage == null)
            {
                marked_dirty_render = true;
                DrawWaveform(getFloatArrayFromSamples(_targetBitDepth, audioData, _targetChannels, audioData[0].Length/2), 512, 255, previewImage);
                marked_dirty_preview = false;
            }

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
            //Debug.Log("[" + name + "] Bytes: " + bytedata.Length + "// Max: " + max + "// Median: " + average / bytedata.Length);
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

        public static void DrawWaveform(float[] samples, int width, int height, Texture2D targetTexture)
        {
            // Create a new texture for the waveform using transparent defaultcolor

            //Definining Curve Color
            Color curveColor = new Color(0.9f, 0.15f, 0.1f, 0.1f);

            //Defining background gracient color
            Color backgroundColor_a = new Color(0.1f, 0.1f, 0.1f, 0.1f);
            Color backgroundColor_b = new Color(0.09f, 0.09f, 0.09f, 0.1f);

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
                //Debug.LogError("Audio Data is empty");
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
                //ReportAudioData(audioBytes, "Read WAV Audio Bytes");
            }
            else if (Path.GetExtension(absolutePath) == ".mp3")
            {
                header = MP3Reader.ReadMp3Header(absolutePath);
                byte[] newWavData = MP3Reader.GetAudioDataFromMp3(absolutePath);
                header = WavReader.ReadWavHeader(newWavData);
                audioBytes = WavReader.GetAudioDataFromWav(newWavData, clip.length, header["BitsPerSample"] / 8, header["SampleRate"], header["Channels"]);
                //ReportAudioData(audioBytes, "Read MP3 Audio Bytes");
            }

            else return null;

            //Initialize Variables 

            int totalBytes = audioBytes.Length; // Subtract 44 to exclude the .wav header
            float durationInSeconds = clip.length;

            //Debug.Log("durationInSeconds: " + durationInSeconds);

            int channels = header["Channels"];
            int sampleRate = header["SampleRate"];

            int blockSize = header["BlockAlign"];
            int bitDepth = header["BitsPerSample"];
            int bytesPerSample = bitDepth / 8;

            //Debug all Variables
            //Debug.Log("Total Bytes: " + totalBytes);
            //Debug.Log("Duration: " + durationInSeconds);
            //Debug.Log("Channels: " + channels);
            //Debug.Log("Sample Rate: " + sampleRate);
            //Debug.Log("Block Size: " + blockSize);
            //Debug.Log("Bit Depth: " + bitDepth);
            //Debug.Log("Bytes Per Sample: " + bytesPerSample);


            //Abort if 0
            if (sampleRate == 0 || bitDepth == 0 || channels == 0)
            {
                //Debug.LogError("Invalid audio data");
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
                //Debug.LogWarning("Invalid Sample Count");
                //Debug.LogWarning("Sample Count: " + validateSamplecount);
                //Debug.LogWarning("Expected Sample Count: " + sampleRate * durationInSeconds * channels);
                //Try if Channelcount was wrong
                channels = 1;
                validateSamplecount = (int)Math.Round((double)audioBytes.Length / bytesPerSample / channels);
                if (validateSamplecount != (int)Math.Round(sampleRate * durationInSeconds))
                {
                    //Debug.LogWarning("Invalid Sample Count...recreating placeholderdata");
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
                    //Debug.LogWarning("Copied Bytes: " + copiedBytes);

                    audioBytes = newAudioBytes;
                    //Debug new audiobytes
                    //Debug.Log(audioBytes.Length.ToString() + " - Recreated Audio Bytes");
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

            /*
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
                     */



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

            //ReportAudioData(resampledBytes, "Resampled Data");

            return resampledBytes;
        }

        //Updater
        public void updated_PreviewImage()
        {
            if (marked_dirty_preview || previewImage == null)
            {
                marked_dirty_preview = false;
                DrawWaveform(getFloatArrayFromSamples(_targetBitDepth, audioData, _targetChannels, audioData[0].Length/2), 512, 255, previewImage);
            }
        }

        public void update_NormalizedAudioData()
        {
            marked_dirty_render=true;
            marked_dirty_normalization = false;
            int maxvalue = (int)Math.Pow(2, _targetBitDepth) - 1;
            float targetMax = maxvalue * _targetgain * targetgain_scaling;
            if (targetBitDepth > 8) targetMax = targetMax / 2;

            startsample = (int)(this.initTime * _targetSampleRate);
            endsample = ((int)((this.startsample) + ((clip.length) * _targetSampleRate)));


            //Init normalized Datablock
            for (int ch = 0; ch < _targetChannels; ch++)
            {

                audioDataNormalized = new byte[_targetChannels][];
                if (audioData[ch] != null && audioData[ch].Length > 0 && audioData[ch] != null)
                {
                    audioDataNormalized[ch] = new byte[this.audioData[ch].Length];

                }

            }

            if (this._setting_doNormalizeInput)
            {
                for (int ch = 0; ch < _targetChannels; ch++)
                    audioDataNormalized[ch] = AudioMixUtils.Normalize(this.audioData[ch], _targetBitDepth, _targetBitDepth > 8, targetMax, _setting_normalizationFac_Input);
            } else {
                    //set Normalized Data to original by copying all bytes
                    for (int ch = 0; ch < _targetChannels; ch++)
                    {
                        audioDataNormalized[ch] = new byte[this.audioData[ch].Length];
                        for (int i = 0; i < this.audioData[ch].Length; i++)
                        {
                            audioDataNormalized[ch][i] = this.audioData[ch][i];
                        }
                    }
                    

            }
        }


        public void update_AudioData()
        {
            marked_dirty_render=true;
            marked_dirty_settings = false;
            audioData = GetChannels();
        }




    }
}
