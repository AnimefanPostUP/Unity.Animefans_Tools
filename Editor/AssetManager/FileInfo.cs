namespace AnifansAssetManager.FileInfo
{

    using System.IO;
    using System.Threading.Tasks;
    using UnityEditor;
    using UnityEngine;

    public class ew_FileInfo
    {
        public Texture2D preview;
        public FileTypes type = FileTypes.Not_Identified;

        public string path => string.IsNullOrEmpty(guid) ? null : AssetDatabase.GUIDToAssetPath(guid);
        public string name => asset == null ? null : asset.name;
        public string absolutePath => string.IsNullOrEmpty(path) ? null : Path.GetFullPath(path);

        public string extension => string.IsNullOrEmpty(path) ? null : Path.GetExtension(path);

        public string guid = ""; //Unitys GUUID

        public string fildeID = ""; //ID for the File

        public UnityEngine.Object asset => string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);

        public string nameproxy = "";

        public int previewResolution = 64;

        //init
        public ew_FileInfo(string path, string guid)
        {
            //Set Path
            this.guid = guid;
        }

        public async void tryGetPeview()
        {

            if (asset != null)
            {
                //make Preview Persistent Texture2D
                Texture2D preview = await GetPreviewImage();

                //Initialize Preview using size of preview
                if (preview != null)
                {

                    if (this.preview == null)
                    {
                        if (previewResolution != -1)
                            this.preview = new Texture2D(previewResolution, previewResolution, preview.format, false);

                        else this.preview = new Texture2D(preview.width, preview.height, preview.format, false);
                    }

                    CopyTexture(preview, this.preview);
                }
            }
        }

public void CopyTexture(Texture2D source, Texture2D dest)
{
    //get sizes
    int width = source.width;
    int height = source.height;

    //get sizes of the dest
    int destWidth = dest.width;
    int destHeight = dest.height;

    //copy Pixels even if images have different sizes
    for (int y = 0; y < destHeight; y++)
    {
        for (int x = 0; x < destWidth; x++)
        {
            // Calculate the corresponding source coordinates
            int sourceX = x * width / destWidth;
            int sourceY = y * height / destHeight;

            // Copy the pixel from the source to the destination
            dest.SetPixel(x, y, source.GetPixel(sourceX, sourceY));
        }
    }

    dest.Apply();
}

        public async Task<Texture2D> GetPreviewImage() //Copy of Original!
        {
            if (this.asset == null) return null;

            return AssetPreview.GetAssetPreview(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(this.path));
        }

    }


    //Enum for Filetypes
    [System.Flags]
    public enum FileTypes
    {
        None = 0,
        Prefab = 1 << 0,
        Scene = 1 << 1,
        Material = 1 << 2,
        Texture = 1 << 3,
        Model = 1 << 4,
        Animation = 1 << 5,
        Audio = 1 << 6,
        Script = 1 << 7,
        Shader = 1 << 8,
        Other = 1 << 9,
        Not_Identified = 1 << 10
    }


}