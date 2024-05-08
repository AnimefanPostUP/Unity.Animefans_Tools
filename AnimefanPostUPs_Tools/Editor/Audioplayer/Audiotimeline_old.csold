using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using System;
using System.Collections;
using System.Reflection;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.IO;
using System.Threading.Tasks;


public class Audiotimeline : EditorWindow
{

    //clip slot

    public AudioClip audioClip;

    public string filename = "Mix";
    public string targetfolder = "Assets/AnimTools/";

    //Audio Manager
    private AudiotrackManager audioManager;

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
    }

    void init_audiomanager()
    {
        audioManager = new AudiotrackManager();
    }

    //on disable
    private void OnDisable()
    {
    }





    private void OnGUI()
    {
        DrawTimeline();
    }

    private void DrawTimeline()
    {
        audioClip = EditorGUILayout.ObjectField("Audio Clip", audioClip, typeof(AudioClip), false) as AudioClip;
        //Call//Unity field for folder
        targetfolder = EditorGUILayout.TextField("Target Folder", targetfolder);
        //Button to get Folder Popup
        if (GUILayout.Button("Select Folder"))
        {
            targetfolder = EditorUtility.OpenFolderPanel("Select Folder", targetfolder, "");
        }

        //Focus the folder
        if (GUILayout.Button("Focus Folder"))
        {
            //Focus in project window
            //Get relative path
            string relativepath = targetfolder.Replace(Application.dataPath, "Assets");
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(relativepath));
        }


        if (audioClip != null)
        {
            AudioTrack newAudioTrack = new AudioTrack(audioClip);
            audioManager.audioTracks.Add(newAudioTrack);
            audioClip = null;
        }

        //loop through all audio tracks
        foreach (var track in audioManager.audioTracks)
        {
            //Horizontal
            GUILayout.BeginHorizontal();

            //Write 100width label with name
            GUILayout.Label(track.clip.name, GUILayout.Width(25));

            //Inputfield for init time
            track.initTime = EditorGUILayout.FloatField("Start Time", track.initTime);

            //button to remove track
            if (GUILayout.Button("Remove"))
            {
                audioManager.audioTracks.Remove(track);
            }

            //button to load audio data
            if (GUILayout.Button("Load Audio Data"))
            {
                track.LoadAudioData();
            }

            GUILayout.EndHorizontal();
        }
        //Create File
        if (GUILayout.Button("Create Mix"))
        {
            audioManager.createMix(targetfolder, filename + ".wav");
        }
    }
}



public class AudiotrackManager
{

    public List<AudioTrack> audioTracks;

    //init  
    public AudiotrackManager()
    {
        audioTracks = new List<AudioTrack>();
    }

    public void createMix(string filePath, string filename)
    {
        byte[][] mixedData = MixAudioData();
        WriteWavFile(mixedData, filePath, filename);
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
                    if (b >= (int)audioTracks[i].initTime && b < (int)audioTracks[i].initTime + audioTracks[i].audioData[ch].Length)
                    {
                        mixedData[ch][i][b] = audioTracks[i].audioData[ch][b - (int)audioTracks[i].initTime];
                    }
                    else
                    {
                        mixedData[ch][i][b] = 0;
                    }
                }
            }
        }

        //Debug
        Debug.Log("Ordered Trackes CH1: " + mixedData[0].Length);
        Debug.Log("Ordered Trackes CH2: " + mixedData[1].Length);

        Debug.Log("After Collecting Bytes");
        ReportAudioData(mixedData[0][0]);
        ReportAudioData(mixedData[1][0]);

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
                for (int j = 0; j < numTracks; j++)
                {
                    mixedData[ch][i] += audiolines[ch][j][i];
                }
            }
        }
        //Debug
        Debug.Log("Mixed Data CH1: " + mixedData[0].Length);
        Debug.Log("Mixed Data CH2: " + mixedData[1].Length);

        Debug.Log("After Mixing Bytes");
        ReportAudioData(mixedData[0]);
        ReportAudioData(mixedData[1]);

        return mixedData;
    }


    public void WriteWavFile(byte[][] audioData, string filePath, string filename)
    {
        //Create Header for wav file 
        //16bit, 44100hz, 2 channels

        byte[] header = new byte[44];
        int sampleRate = 44100;
        int numChannels = 2;
        int bitDepth = 32;

        //RIFF chunk descriptor
        header[0] = 82; //R
        header[1] = 73; //I
        header[2] = 70; //F
        header[3] = 70; //F

        //file size
        int fileSize = audioData[0].Length + 36;
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
        int dataSize = audioData[0].Length;
        header[40] = (byte)(dataSize & 0xff);
        header[41] = (byte)((dataSize >> 8) & 0xff);
        header[42] = (byte)((dataSize >> 16) & 0xff);
        header[43] = (byte)((dataSize >> 24) & 0xff);

        //combine file path and filename
        filePath = Path.Combine(filePath, filename);

        //Write the bytes to a file
        FileStream fileStream = new FileStream(filePath, FileMode.Create);
        fileStream.Write(header, 0, header.Length);

        for (int i = 0; i < dataSize; i++)
        {
            for (int ch = 0; ch < numChannels; ch++)
            {
                fileStream.WriteByte(audioData[ch][i]);
            }
        }

        //Debug
        Debug.Log("File Written: " + filePath);
        Debug.Log("Data Size: " + dataSize);
        Debug.Log("File Size: " + fileSize);

        Debug.Log("END: " + fileSize);
        ReportAudioData(audioData[0]);


        fileStream.Close();

    }

    //report average and max from audio data byte array
    public void ReportAudioData(byte[] bytedata)
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

        Debug.Log("Max: " + max);

        Debug.Log("Average: " + average / bytedata.Length);
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

    public void ReportAudioData(byte[] bytedata)
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

        Debug.Log("Max: " + max);

        Debug.Log("Average: " + average / bytedata.Length);
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

    public byte[][] GetChannels()
    {


        //Get clip absolute path
        string path = AssetDatabase.GetAssetPath(clip);
        //absolute path
        string absolutePath = Path.GetFullPath(path);


        // Now you can read the uncompressed audio data from "temp.wav"
        byte[] audioBytes = uncompressData(System.IO.File.ReadAllBytes(absolutePath));

        audioBytes = audioBytes.Skip(44).ToArray();


        Debug.Log("Read Source Bytes");
        ReportAudioData(audioBytes);





        //Debug Total Bytes
        Debug.Log("Total Bytes: " + audioBytes.Length);

        // Parse the .wav file header
        //int channels = BitConverter.ToInt16(audioBytes, 22);
        //int bitDepth = BitConverter.ToInt16(audioBytes, 34);
        //Use unity to get the data

        int totalBytes = audioBytes.Length - 44; // Subtract 44 to exclude the .wav header
        int sampleRate = clip.frequency;
        int channels = clip.channels;
        float durationInSeconds = clip.length;

        Debug.Log("channels " + channels);

        Debug.Log("sampleRate: " + sampleRate);

        Debug.Log("durationInSeconds: " + durationInSeconds);


        //correct counts if 0
        if (sampleRate == 0)
        {
            sampleRate = 44100;
        }

        if (channels == 0)
        {
            channels = 2;
        }

        int bitDepth = ReadWavHeader(absolutePath);
        Debug.Log("Bits Per Sample: " + bitDepth);

        int bytesPerSample = (int)bitDepth / 8;
        Debug.Log("Bytes Depth: " + bytesPerSample);

        //Auto adjust the the next closes by using floats and rounding

        //Store
        _oldSampleRate = sampleRate;
        _oldChannels = channels;
        _oldBitDepth = bitDepth;
        _length = clip.length;

        // Calculate the total number of samples
        int totalSamples = ((audioBytes.Length) / (bytesPerSample));
        Debug.Log("Total Samples: " + totalSamples);

        // Create a jagged array to hold the audio data for each channel
        byte[][] channelData = new byte[channels][];

        for (int i = 0; i < channels; i++)
        {
            channelData[i] = new byte[totalSamples * bytesPerSample];
        }

        // Separate the audio data into channels
        for (int i = 0, j = 0; i < audioBytes.Length && j < totalSamples; i += bytesPerSample * channels, j++)
        {
            for (int channel = 0; channel < channels; channel++)
            {
                int sourceIndex = i + bytesPerSample * channel;
                if (sourceIndex + bytesPerSample <= audioBytes.Length)
                {
                    Array.Copy(audioBytes, sourceIndex, channelData[channel], j * bytesPerSample, bytesPerSample);
                }
            }
        }

        // If the audio is mono, duplicate the single channel to create a stereo effect
        if (channels == 1)
        {
            if (channelData[0].Length <= channelData[1].Length)
            {
                Array.Copy(channelData[0], channelData[1], channelData[0].Length);
            }
        }
        Debug.Log("Separated");
        ReportAudioData(channelData[0]);
        ReportAudioData(channelData[1]);

        return channelData;
    }
    private byte[] GetSampledAudioData(byte[] audioBytes, int oldSampleRate, int oldBitDepth, int newSampleRate, int newBitDepth)
    {
        // Calculate the number of samples in the original and resampled audio data
        int oldNumSamples = audioBytes.Length / (oldBitDepth / 8);

        //Debug newBit


        int newNumSamples = (int)(_length * newSampleRate * 4);

        int oldSamples = audioBytes.Length / (oldBitDepth / 8);


        Debug.Log("New Bit Depth: " + newBitDepth);
        Debug.Log("Old Num Samples: " + oldBitDepth);
        Debug.Log("Old Num Samples: " + oldNumSamples);
        Debug.Log("New Num Samples: " + newNumSamples);

        int oldDepthBytes = oldBitDepth / 8;
        int newDepthBytes = newBitDepth / 8;

        int newbytecount = (int)newNumSamples * newDepthBytes;
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
                    value |= (uint)(audioBytes[oldIndex + j] << (8 * (oldDepthBytes - 1 - j)));
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
                resampledBytes[i + j] = (byte)(value >> (8 * (newDepthBytes - 1 - j)));
            }
        }


        //Debug
        Debug.Log("Resampled Data");
        ReportAudioData(resampledBytes);

        return resampledBytes;
    }

    public int ReadWavHeader(string filePath)
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

            // Skip byte rate and block align
            reader.ReadInt32();
            reader.ReadInt16();

            // Read bits per sample
            short bitsPerSample = reader.ReadInt16();

            Debug.Log($"Channels: {numChannels}, Sample Rate: {sampleRate}, Bits Per Sample: {bitsPerSample}");

            return bitsPerSample;
        }
    }

}
