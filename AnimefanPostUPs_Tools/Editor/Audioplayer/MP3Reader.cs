 namespace AnimefanPostUPs_Tools.MP3Reader
    {
        using System;
        using System.Collections.Generic;
        using System.IO;
        using NAudio.Wave;
        using UnityEngine;
        public static class MP3Reader
        {

            public static byte[] GetAudioDataFromMp3(string filePath)
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


            public static void ConvertMp3ToWav(string mp3FilePath, string wavFilePath)
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
            public static Dictionary<string, int> ReadMp3Header(string filePath)
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
            public static void WriteWavFile(string filePath, int sampleRate, int channels, int bitsPerSample, int durationInSeconds)
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



    }