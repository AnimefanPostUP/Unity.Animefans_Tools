namespace AnimefanPostUPs_Tools.AudioMixUtils
{

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    using Math = UnityEngine.Mathf;
    public static class AudioMixUtils
    {

        public static byte[] Normalize(byte[] audioData, int bitDepth, bool considerSignBit, float targetMax, float strength)
        {

            //Debug all inputs
            //Debug.Log("AudioData: " + audioData+" bitDepth: "+bitDepth+" considerSignBit: "+considerSignBit+" targetMax: "+targetMax+" strength: "+strength);
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
            float normalizationFactor = ((targetMax) / (currentMax)+0.000001f);
            //Debug.Log("CurrentMax: " + currentMax + " NormalizationFactor: " + normalizationFactor);
            for (int i = 0; i < audioDataFloat.Length; i++)
            {
                audioDataFloat[i] *= Math.Lerp(1, normalizationFactor, strength);
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


    }

    

}