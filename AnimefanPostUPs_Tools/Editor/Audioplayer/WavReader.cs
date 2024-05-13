

namespace AnimefanPostUPs_Tools.WavReader
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using NAudio.Wave;
    using UnityEngine;



    public static class WavReader
    {

        public static Dictionary<string, int> ReadWavHeader(byte[] fileData)
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

                    if (reader.BaseStream.Position == reader.BaseStream.Length)
                    {
                        break; // Break the loop if end of file is reached
                    }
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

        public static byte[] GetAudioDataFromWav(byte[] wavFile, double audioLength, int byteDepth, int sampleRate, int channels)
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
    }


}
