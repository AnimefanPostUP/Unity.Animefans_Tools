namespace AnifansAssetManager.FolderInfo
{


    using System.IO;
    using System.Threading.Tasks;
    using UnityEditor;
    using UnityEngine;
    using System.Collections.Generic;
    using AnifansAssetManager.FileInfo;
    using System;
    using System.Linq;
    //import AssetPostprocessor
    using UnityEditor.Experimental.AssetImporters;

    //Use RegEx
    using System.Text.RegularExpressions;



    //Enum for Filetypes
    [System.Flags]
    public enum FileType
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

        //Define icons per Filetype using an array
    }

    public static class FileTypeIcons
    {
        private static readonly Dictionary<FileTypes, string> Icons = new Dictionary<FileTypes, string>
    {
        { FileTypes.Prefab, "Prefab Icon" },
        { FileTypes.Scene, "SceneAsset Icon" },
        { FileTypes.Material, "Material Icon" },
        { FileTypes.Texture, "Texture Icon" },
        { FileTypes.Model, "d_MeshFilter Icon" },
        { FileTypes.Animation, "Animation Icon" },
        { FileTypes.Audio, "AudioSource Icon" },
        { FileTypes.Script, "cs Script Icon" },
        { FileTypes.Shader, "Shader Icon" },
        // Add other file types and their icons here
    };

        public static string GetIcon(FileTypes fileType)
        {
            return Icons.TryGetValue(fileType, out var icon) ? icon : "Default Icon";
        }
    }

    class FolderInfo
    {
        //Asset Item Array 
        public List<ew_FileInfo> files = new List<ew_FileInfo>();
        public string path;
        public string name;
        public bool displayNames = false;
        public int backgroundtexture = 0;

        public FileTypes shownTypes = FileTypes.Prefab | FileTypes.Scene | FileTypes.Material | FileTypes.Texture | FileTypes.Model | FileTypes.Animation | FileTypes.Audio | FileTypes.Script | FileTypes.Shader | FileTypes.Other;
        public bool showFolders = true;
        public int previewResolution = 64;

        public FolderInfo(string path, string name)
        {
            //AssetChangeDetector.AssetChanged += OnAssetChanged;

            this.path = path;
            this.name = name;
            files = new List<ew_FileInfo>();

            //Get all Files in the Folder
            reloadFiles();
        }

        public void unload()
        {
            //AssetChangeDetector.AssetChanged -= OnAssetChanged;
            foreach (ew_FileInfo file in files)
            {
                if (file.preview != null)
                {
                    UnityEngine.Object.DestroyImmediate(file.preview);
                }
            }
        }

        //Functon to return Filetype Enum
        public FileTypes getFileType(string path)
        {
            //Check if Path Exists
            if (File.Exists(path))
            {
                //Get Extension
                string extension = Path.GetExtension(path);

                //Check for Filetype
                if (extension == ".prefab") return FileTypes.Prefab;
                if (extension == ".unity") return FileTypes.Scene;
                if (extension == ".mat") return FileTypes.Material;
                if (extension == ".png") return FileTypes.Texture;
                if (extension == ".fbx") return FileTypes.Model;
                if (extension == ".anim") return FileTypes.Animation;
                if (extension == ".wav") return FileTypes.Audio;
                if (extension == ".cs") return FileTypes.Script;
                if (extension == ".shader") return FileTypes.Shader;
                else return FileTypes.Other;
            }
            else return FileTypes.Not_Identified;
        }

        //Function to reload all Files of the Folders

        private void OnAssetChanged(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            string feedback = "[AssetManager] Files changed";
            feedback = feedback + findNewFiles();
            feedback = feedback + removeMovedFiles();
            feedback = feedback + removeDeletedFiles(deletedAssets);
            Debug.Log(feedback);


        }

        public string removeDeletedFiles(string[] deletedAssets)
        {
            string feedback = "";
            int counter_files = 0;
            int counter_previews = 0;
            //check if the file is inside files array

            foreach (string path in deletedAssets)
            {

                //Check for Extension and if none then Skip
                string absolutePath = Path.GetFullPath(path);
                string extension = Path.GetExtension(absolutePath);
                if (string.IsNullOrEmpty(extension)) continue;


                ew_FileInfo file = files.Find(x => x.path == path);
                if (file != null)
                {
                    //Delete the preview
                    if (file.preview != null)
                    {
                        UnityEngine.Object.DestroyImmediate(file.preview);
                        counter_previews++;
                    }
                    files.Remove(file);
                    counter_files++;
                }
            }
            return "\n removed " + counter_files + " deleted files /" + "with " + counter_previews + "previews";
        }

        public string removeMovedFiles()
        {
            string feedback = "";
            int counter_files = 0;
            int counter_previews = 0;
            // Iterate over all files
            foreach (ew_FileInfo file in files)
            {
                // Convert Unity relative path to system absolute path
                string absolutePath = Path.GetFullPath(file.path);

                //Check for Extension and if none then Skip
                string extension = Path.GetExtension(absolutePath);
                if (string.IsNullOrEmpty(extension)) continue;

                // Check if the file still exists at the path
                if (!System.IO.File.Exists(absolutePath))
                {
                    // Delete the preview
                    if (file.preview != null)
                    {
                        UnityEngine.Object.DestroyImmediate(file.preview);
                        counter_previews++;
                    }

                    // Remove the file from the list
                    files.Remove(file);
                    counter_files++;
                }
            }
            return "\n  removed " + counter_files + " deleted files /" + "with " + counter_previews + "previews";

        }

        public string findNewFiles()
        {
            string feedback = "";
            int counter = 0;

            //Get all Files in the Folder
            string[] assetGuids = AssetDatabase.FindAssets("", new[] { path });

            //Loop through all Files

            int countervar = 0;
            foreach (string guid in assetGuids)
            {


                //Break if the file is already in the list
                if (files.Exists(x => x.guid == guid))
                {
                    continue;
                }

                //Debug.Log("Loading: " + guid);
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                //Check for Extension and if none then Skip
                string absolutePath = Path.GetFullPath(assetPath);
                string extension = Path.GetExtension(absolutePath);
                if (string.IsNullOrEmpty(extension)) continue;

                addFile(assetPath, guid);
                countervar++;
                if (countervar > 2000)
                {
                    Debug.Log("To many Files in Folder: " + path);
                    break;
                }
            }
            setResolutions();
            generateProxyNames();
            return "\n added " + countervar + "new files ";
        }



        public async void reloadFiles()
        {

            //DestroyInstant all Preview Textures if they exist
            foreach (ew_FileInfo file in files)
            {

                if (file.preview != null)
                {
                    UnityEngine.Object.DestroyImmediate(file.preview);
                }
            }

            //Clear Files
            files.Clear();

            //Get all Files in the Folder
            string[] assetGuids = AssetDatabase.FindAssets("", new[] { path });

            //Loop through all Files

            int countervar = 0;
            foreach (string guid in assetGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                //Check for Extension and if none then Skip
                string absolutePath = Path.GetFullPath(assetPath);
                string extension = Path.GetExtension(absolutePath);
                if (string.IsNullOrEmpty(extension)) continue;

                await addFile(assetPath, guid);
                countervar++;
                if (countervar > 2000)
                {
                    Debug.Log("To many Files in Folder: " + path);
                    break;
                }
            }
            setResolutions();
            generateProxyNames();
        }

        public void setResolutions()
        {
            foreach (ew_FileInfo file in files)
            {
                file.previewResolution = previewResolution;
            }
        }

        public void generateProxyNames()
        {
            //Find Parts of the Name that are Shared across other Files and remove these parts
            //String array
            string[] names = new string[files.Count];

            foreach (ew_FileInfo file in files)
            {
                names[files.IndexOf(file)] = file.asset.name;
            }

            string[] newNames = RemoveCommonParts(names);

            for (int i = 0; i < files.Count; i++)
            {
                files[i].nameproxy = newNames[i];
            }
        }


        public string[] RemoveCommonParts(string[] strings)
        {
            var splitStrings = strings.Select(str =>
            {
                var lastUnderscoreIndex = str.LastIndexOf('_');
                var prefix = lastUnderscoreIndex >= 0 ? str.Substring(0, lastUnderscoreIndex) : str;
                var suffix = lastUnderscoreIndex >= 0 ? str.Substring(lastUnderscoreIndex) : "";
                var numberParts = Regex.Split(prefix, @"\D+").Where(part => !string.IsNullOrEmpty(part)).ToList();
                var nonNumberParts = Regex.Split(prefix, @"\d+").Where(part => !string.IsNullOrEmpty(part)).ToList();
                return new { nonNumberParts, numberParts, suffix };
            }).ToList();

            var commonParts = new HashSet<string>();

            foreach (var strParts in splitStrings)
            {
                foreach (var part in strParts.nonNumberParts)
                {
                    commonParts.Add(part);
                }
            }

            for (int i = 0; i < splitStrings.Count; i++)
            {
                var filteredParts = splitStrings[i].nonNumberParts.Where(part => !commonParts.Contains(part)).ToList();
                splitStrings[i] = new
                {
                    nonNumberParts = filteredParts.Count > 0 ? filteredParts : new List<string> { "..." },
                    numberParts = splitStrings[i].numberParts,
                    suffix = splitStrings[i].suffix
                };
            }

            for (int i = 0; i < strings.Length; i++)
            {
                var nonNumberParts = splitStrings[i].nonNumberParts;
                var numberParts = splitStrings[i].numberParts;
                var suffix = splitStrings[i].suffix;
                var newStr = "";
                for (int j = 0; j < nonNumberParts.Count; j++)
                {
                    newStr += nonNumberParts[j];
                    if (j < numberParts.Count)
                    {
                        newStr += numberParts[j];
                    }
                }
                newStr += suffix;
                strings[i] = newStr;
            }

            return strings;
        }




        public void reloadPreviews()
        {
            foreach (ew_FileInfo file in files)
            {
                file.previewResolution = previewResolution;
                if (file.preview != null)
                {
                    UnityEngine.Object.DestroyImmediate(file.preview);
                }
                file.preview=null;
                file.tryGetPeview();
            }
        }

        public async Task<ew_FileInfo> addFile(string path, string guid)
        {

            //check if Path Exists
            if (File.Exists(path))
            {

                ew_FileInfo newFile = new ew_FileInfo(path, guid);
                newFile.type = getFileType(path);
                newFile.previewResolution = previewResolution;
                newFile.tryGetPeview();

                files.Add(newFile);
                return newFile;
            }
            else
            {
                Debug.Log("File not found: " + path);
                return null;
            }
        }



    }
    public class AssetChangeDetector //: AssetPostprocessor
    {
        public static event Action<string[], string[], string[], string[]> AssetChanged;

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {

            AssetChanged?.Invoke(importedAssets, deletedAssets, movedAssets, movedFromAssetPaths);
        }
    }

}

