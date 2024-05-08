//Define NameSpace
namespace AnimefanPostUPs_Tools.SmartColorUtility
{
    using System;
    using UnityEngine;

    class SmartColorUtility
    {
        public static Color GetRGBA(ColorRGBA color)
        {
            return new Color32((byte)((int)color >> 24), (byte)((int)color >> 16), (byte)((int)color >> 8), (byte)color);
        }
    }

    //ENUMS that hold Defined Colors
    [Flags]
    public enum ColorRGBA : uint
    {

        //Main Palette
        none = (0u << 24) | (0u << 16) | (0u << 8) | 0u,
        red = (255u << 24) | (0u << 16) | (0u << 8) | 255u,
        green = (0u << 24) | (255u << 16) | (0u << 8) | 255u,
        yellow = (255u << 24) | (255u << 16) | (0u << 8) | 255u,
        orange = (255u << 24) | (128u << 16) | (0u << 8) | 255u,
        purple = (128u << 24) | (0u << 16) | (128u << 8) | 255u,

        //Non Colors
        white = (255u << 24) | (255u << 16) | (255u << 8) | 255u,
        black = (0u << 24) | (0u << 16) | (0u << 8) | 255u,
        grey = (128u << 24) | (128u << 16) | (128u << 8) | 255u,
        darkgrey = (32u << 24) | (32u << 16) | (32u << 8) | 255u,
        lightgrey = (192u << 24) | (192u << 16) | (192u << 8) | 255u,
        deepgray = (12u << 24) | (12u << 16) | (12u << 8) | 255u,

        //Gray Colors in Stept of 16
        grayscale_000 = (0u << 24) | (0u << 16) | (0u << 8) | 255u,
        grayscale_016 = (16u << 24) | (16u << 16) | (16u << 8) | 255u,
        grayscale_032 = (32u << 24) | (32u << 16) | (32u << 8) | 255u,
        grayscale_048 = (48u << 24) | (48u << 16) | (48u << 8) | 255u,
        grayscale_064 = (64u << 24) | (64u << 16) | (64u << 8) | 255u,
        grayscale_080 = (80u << 24) | (80u << 16) | (80u << 8) | 255u,
        grayscale_096 = (96u << 24) | (96u << 16) | (96u << 8) | 255u,
        grayscale_112 = (112u << 24) | (112u << 16) | (112u << 8) | 255u,
        grayscale_128 = (128u << 24) | (128u << 16) | (128u << 8) | 255u,
        grayscale_144 = (144u << 24) | (144u << 16) | (144u << 8) | 255u,
        grayscale_160 = (160u << 24) | (160u << 16) | (160u << 8) | 255u,
        grayscale_176 = (176u << 24) | (176u << 16) | (176u << 8) | 255u,
        grayscale_192 = (192u << 24) | (192u << 16) | (192u << 8) | 255u,
        grayscale_208 = (208u << 24) | (208u << 16) | (208u << 8) | 255u,
        grayscale_224 = (224u << 24) | (224u << 16) | (224u << 8) | 255u,
        grayscale_240 = (240u << 24) | (240u << 16) | (240u << 8) | 255u,
        grayscale_255 = (255u << 24) | (255u << 16) | (255u << 8) | 255u


    }
}