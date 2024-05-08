namespace AnimefanPostUPs_Tools.ColorTextureItem
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;
    using System.Linq;
    using System.IO;
    using AnimefanPostUPs_Tools.ColorTextureManager;
    using AnimefanPostUPs_Tools.SmartColorUtility;
    using static AnimefanPostUPs_Tools.SmartColorUtility.ColorRGBA;

    public enum TexItemType { Solid, Gradient_Horizontal, Gradient_Vertical, Grid }
    public class ColorTextureItem
    {


        public ColorRGBA color_a;
        public ColorRGBA color_b;
        public int tiling;
        public Texture2D texture;
        public TexItemType type;

        private ColorTextureManager source;


        public ColorTextureItem(ColorTextureManager source, ColorRGBA color_a, TexItemType type = TexItemType.Solid, ColorRGBA color_b = ColorRGBA.white, int tiling = 2)
        {

            this.source = source;
            this.type = type;
            this.tiling = tiling;
            this.color_a = color_a;
            this.color_b = color_b;

            generateTexture();

            source.Register(this);


        }

        private static Color GetRGBA(ColorRGBA color)
        {
            return SmartColorUtility.GetRGBA(color);
        }

        //Lerp function for 255 based colors
        static Color Lerp(Color a, Color b, float t)
        {
            return new Color(
                Mathf.Lerp(a.r, b.r, t),
                Mathf.Lerp(a.g, b.g, t),
                Mathf.Lerp(a.b, b.b, t),
                Mathf.Lerp(a.a, b.a, t)
            );
        }

        public void generateTexture()
        {
            //if (texture != null) return;

            if (type == TexItemType.Solid)
            {
                texture = new Texture2D(1, 1);
                texture.SetPixel(0, 0, GetRGBA(color_a));
                texture.Apply();
            }
            else if (type == TexItemType.Gradient_Horizontal)
            {
                texture = new Texture2D(tiling, 1);
                //draw the Pixels Color from Left to Right from color a to color b
                for (int x = 0; x < texture.width; x++)
                {
                    float lerp = (float)x / (float)(texture.width - 1);
                    texture.SetPixel(x, 0, Lerp(GetRGBA(color_a), GetRGBA(color_b), lerp));
                }
                texture.Apply();
            }
            else if (type == TexItemType.Gradient_Vertical)
            {
                texture = new Texture2D(1, tiling);
                //draw the Pixels Color from Top to Bottom from color a to color b
                for (int y = 0; y < texture.height; y++)
                {
                    float lerp = (float)y / (float)(texture.height - 1);
                    texture.SetPixel(0, y, Lerp(GetRGBA(color_a), GetRGBA(color_b), lerp));
                }
                texture.Apply();

            }
            else if (type == TexItemType.Grid)
            {
                //Draw Grid
                texture = new Texture2D(tiling, tiling);


                for (int y = 0; y < texture.height; y++)
                    for (int x = 0; x < texture.width; x++)
                    {
                        texture.SetPixel(x, y, x % 2 == 0 ^ y % 2 == 0 ? GetRGBA(color_a) : GetRGBA(color_b));
                    }
                texture.filterMode = FilterMode.Point;
                texture.Apply();
            }



            string filename = CreateAsset();

            if (texture != null)
            {
                if (type == TexItemType.Grid)
                {
                    texture.filterMode = FilterMode.Point;
                    texture.Apply();
                }
            }
            else
            {
                //Set to unitys  Utility Default White texture
                texture = Texture2D.whiteTexture;
            }

        }

        public string CreateAsset()
        {

            string filename = generateFilename();

            //Check if it already exists and return it if
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(getCacheFolder() + filename + ".png") != null)
            {
                return getCacheFolder() + filename + ".png";
            }

            SaveTextureAsReadableAsset(texture, getCacheFolder() + filename + ".png");


            //set the Filter mode and apply
            texture.filterMode = FilterMode.Point;
            Debug.Log("FiltermodeSet");
            texture.Apply();

            return filename;
        }

        public void SaveTextureAsReadableAsset(Texture2D texture, string path)
        {
            // Save the texture as a PNG file
            byte[] pngData = texture.EncodeToPNG();
            System.IO.File.WriteAllBytes(path, pngData);

            // Import the PNG file as a texture
            AssetDatabase.ImportAsset(path);

            // Refresh the asset database
            AssetDatabase.Refresh();

            // Get the TextureImporter for the texture
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);

            // Set isReadable to true
            importer.isReadable = true;

            // Save and reimport the texture
            importer.SaveAndReimport();

            //load asset
            texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        public string generateFilename()
        {
            string filename = "";

            //Create a filename that constists of the Type, used Colors and Tiling
            if (type == TexItemType.Solid)
            {
                filename = "Solid_" + color_a.ToString();
            }
            else if (type == TexItemType.Gradient_Horizontal)
            {
                filename = "Gradient_H_" + color_a.ToString() + "_" + color_b.ToString() + "_" + tiling.ToString();
            }
            else if (type == TexItemType.Gradient_Vertical)
            {
                filename = "Gradient_V_" + color_a.ToString() + "_" + color_b.ToString() + "_" + tiling.ToString();
            }
            else if (type == TexItemType.Grid)
            {
                filename = "Grid_" + color_a.ToString() + "_" + color_b.ToString() + "_" + tiling.ToString();
            }

            return filename;
        }

        public string getPath()
        {
            return getCacheFolder() + generateFilename() + ".png";
        }

        private string getCacheFolder()
        {
            return source.CacheFolder;
        }
    }
}