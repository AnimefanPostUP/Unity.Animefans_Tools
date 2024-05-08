namespace AnifansAssetManager.ColorTextureManager
{

    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;
    using System.Linq;
    using System.IO;
    using AnifansAssetManager.ColorTextureItem;
    using AnifansAssetManager.SmartColorUtility;
    using static AnifansAssetManager.SmartColorUtility.ColorRGBA;
    using System;


    //Colormanager Class
    public class ColorTextureManager
    {

        public string CacheFolder = "Assets/AnimefansAssetManager/Editor/Textures_Internal/";

        public List<ColorTextureItem> textureElements = new List<ColorTextureItem>();

        //Get/Create
        private ColorTextureItem FindTexture(TexItemType type, ColorRGBA color_a, ColorRGBA color_b, int tiling)
        {
            //iterate Textures, then Filter by the Type, then by the First Color, then Second and then at last by the Tiling
            ColorTextureItem item = textureElements.Find(item => item.type == type && item.color_a == color_a && item.color_b == color_b && item.tiling == tiling);
            return item;
        }

        //Load/Create
        public Texture2D LoadTexture(TexItemType type, ColorRGBA color_a, ColorRGBA color_b = ColorRGBA.none, int tiling = 2)
        {
            ColorTextureItem item = FindTexture(type, color_a, color_b, tiling) ?? new ColorTextureItem(this, color_a, type, color_b, tiling);
            if (item.texture == null)
            {
                item.generateTexture();
                Debug.Log("Texture Was recreated");
            }
            return item.texture;
        }


        public Texture2D GetTextureItemAt(int index, TexItemType? type = null, ColorRGBA? color_a = null, ColorRGBA? color_b = null, int? tiling = -1)
        {
            var filteredItems = textureElements.Where(item =>
                (!type.HasValue || item.type == type.Value) &&
                (!color_a.HasValue || item.color_a == color_a) &&
                (!color_b.HasValue || item.color_b == color_b) &&
                (tiling == -1 || item.tiling == tiling.Value));

            if (filteredItems.Count() == 0) return Texture2D.whiteTexture;

            return filteredItems.ElementAtOrDefault(index).texture;
        }


        //Get Texture count by Type, or by Color
        public int GetTextureCount(TexItemType? type, ColorRGBA? color_a, ColorRGBA? color_b, int tiling)
        {
            return textureElements.Count(item =>
                (!type.HasValue || item.type == type.Value) &&
                (!color_a.HasValue || item.color_a == color_a) &&
                (!color_b.HasValue || item.color_b == color_b) &&
                (tiling == -1 || item.tiling == tiling));
        }

        public void Unload() { foreach (ColorTextureItem item in textureElements) DestroyTextureItem(item); }
        private void DestroyTextureItem(ColorTextureItem item) { if (item.texture != null) UnityEngine.Object.DestroyImmediate(item.texture); }
        public void Register(ColorTextureItem updater)
        {
            textureElements.Add(updater);
        }

        public void RegenerateCache()
        {
            foreach (ColorTextureItem item in textureElements)
            {
                DestroyTextureItem(item);
                item.generateTexture();
            }
        }

        public void ClearCache()
        {
            foreach (ColorTextureItem item in textureElements)
            {
                DestroyTextureItem(item);
            }
            textureElements.Clear();

            FindAndDeleteFiles();

            //Delete all Textures inside the Paths Folder that Follow the Naming Sceme


        }

        public void FindAndDeleteFiles()
        {

            //
            // Get the .png files in CacheFolder
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new string[] { CacheFolder });
            string[] files = new string[guids.Length];

            for (int i = 0; i < guids.Length; i++)
            {
                files[i] = AssetDatabase.GUIDToAssetPath(guids[i]);
            }

            foreach (string file in files)
            {
                // separate the filename from the path
                string filename = Path.GetFileName(file);

                bool deletable = false;

                string[] enumNames = Enum.GetNames(typeof(TexItemType));

                // check if it start with any of the enum names
                foreach (string name in enumNames)
                {
                    if (filename.StartsWith(name))
                    {
                        deletable = true;
                        break;
                    }
                }

                // check if the file Contains any existing Color
                if (deletable)
                {
                    foreach (ColorRGBA color in Enum.GetValues(typeof(ColorRGBA)))
                    {
                        if (file.Contains(color.ToString()))
                        {
                            deletable = false;
                            break;
                        }
                    }
                }

                if (deletable)
                {
                    AssetDatabase.DeleteAsset(file);
                }

            }
        }

    }
}