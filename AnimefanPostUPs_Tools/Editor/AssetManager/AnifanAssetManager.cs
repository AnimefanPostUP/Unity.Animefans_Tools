using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Data;

using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

using AnimefanPostUPs_Tools.KeyMonitorGroup;
using AnimefanPostUPs_Tools.KeyActionGroup;

using AnifansAssetManager.FolderInfo;
using AnifansAssetManager.FileInfo;

using AnimefanPostUPs_Tools.ColorTextureManager;
using AnimefanPostUPs_Tools.ColorTextureItem;

using AnimefanPostUPs_Tools.SmartColorUtility;

using AnimefanPostUPs_Tools.GUI_LayoutElements;





public class AnifanAssetManager : EditorWindow
{
    const string CacheFolder = "Assets/AnimefanPostUPs-Tools/AnimefanPostUPs_Tools/Editor/Textures_Internal/";

    //INTERNAL
    private string filterString = "";
    public int selectedFolderIndex = -1;
    List<FolderInfo> folders = new List<FolderInfo>(); //List of Current Folders

    public string searchFilter = ""; //Search Filter

    //GUI
    Splitviewer splitviewer = new Splitviewer();
    public DropdownMenu dropdownMenu;
    public DropdownMenu menu_Text;
    public DropdownMenu menu_Img;

    private static readonly string guiID = "EasyWindow";    //GUID to Identify this Windows Drag_Exits
    private Vector2 display_Content_scroll;
    private Vector2 display_Assets_scroll;
    private Vector2 display_Debug_scroll;

    private const float HEADER_HEIGHT = 80;
    private const int ICONBUTTON_SIZE = 30;
    private const float UI_ELEMENT_HEIGHT = 25;
    private const float DISPLAYSIZE_MIN = 20;
    private const float DISPLAYSIZE_MAX = 140;
    private int fontsize = 12;

    //Settings
    private bool displayHelp = false;
    private float displayFoldersWidth = 160f;
    public float displaysize = 50f;

    //Managers
    public ColorTextureManager colmgr;
    public KeyMonitorGroup keys;
    public KeyActionGroup keyActions;

    int selectedButton = -1;
    //string[] buttonLabels = { "Filter", "Filenames", "Extras" };

    //Standard Functions
    [MenuItem("Animtools/EasyWindow")]
    public static void ShowWindow()
    {
        GetWindow<AnifanAssetManager>("EasyWindow");
    }


    private void OnEnable()
    {


        init_keys();
        init_keyactions();

        init_colmgr();
        init_colmgr_colors();

        init_dropdownmenus();
        init_menuText();
        init_menu_Previews();

        load_Paths();
    }

    private void OnDisable()
    {
        save_Paths();
        EditorApplication.update -= keys.updateMonitors;
        EditorApplication.update -= keyActions.updateKeyActions;

        colmgr.Unload();

        //iterate all FolderInfos and unload the files
        foreach (FolderInfo folder in folders)
        {
            folder.unload();
        }

    }

    void init_menu_Previews()
    {
        menu_Img = new DropdownMenu("Previews", colmgr, new Vector2(210, 0));

        menu_Img.AddItem("Reload",reloadPreviews);
        menu_Img.AddItem("1:1",setResolution_1_1);   
        menu_Img.AddItem("32x32",setResolution_32);
        menu_Img.AddItem("64x64",setResolution_64);
        menu_Img.AddItem("128x128",setResolution_128);
        menu_Img.AddItem("256x256",setResolution_256);
        setDropdownTextures(menu_Img);
    }

    //set Resulution of the Image
    public void setResolution(int res)
    {
        if (selectedFolderIndex >= 0 && selectedFolderIndex < folders.Count)
        folders[selectedFolderIndex].previewResolution = res;
    }

    //set resolution to 64
    public void setResolution_1_1() { setResolution(-1); reloadPreviews();}
    public void setResolution_32() { setResolution(32);reloadPreviews();}
    public void setResolution_64() { setResolution(64);reloadPreviews();}
    public void setResolution_128() { setResolution(128);reloadPreviews();}
    public void setResolution_256() { setResolution(256);reloadPreviews();}

    public void reloadPreviews()
    {
        if (selectedFolderIndex >= 0 && selectedFolderIndex < folders.Count)
        {
            folders[selectedFolderIndex].reloadFiles();
        }
    }

    void init_dropdownmenus()
    {
        dropdownMenu = new DropdownMenu("Visibility", colmgr,new Vector2(0, 0));
        //set x and y position
        dropdownMenu.AddItem("Filters", true);
        dropdownMenu.AddItem("Icons", true);
        setDropdownTextures(dropdownMenu);
    }

    void init_menuText()
    {
        menu_Text = new DropdownMenu("Text", colmgr,new Vector2(105, 0));
        //set x and y position
        menu_Text.AddItem("Proxys", true);
        menu_Text.AddItem("Forceproxy", false);
        menu_Text.AddItem("Font 8", false, setFontSize_8);
        menu_Text.AddItem("Font 12", true, setFontSize_12);
        menu_Text.AddItem("Font 16", false, setFontSize_16);
        setDropdownTextures(menu_Text);
    }

    public void setFontsize(int size)
    {
        fontsize = size;
    }

    public void setFontSize_8() { setFontsize(10); menu_Text.setValue("Font 12", false); menu_Text.setValue("Font 16", false); menu_Text.setValue("Font 8", true); }
    public void setFontSize_12() { setFontsize(12); menu_Text.setValue("Font 8", false); menu_Text.setValue("Font 16", false); menu_Text.setValue("Font 12", true); }
    public void setFontSize_16() { setFontsize(14); menu_Text.setValue("Font 8", false); menu_Text.setValue("Font 12", false); menu_Text.setValue("Font 16", true); }

    //Create ColorTextureDummys and set it for a given dropdown
    void setDropdownTextures(DropdownMenu menu)
    {
        menu.itemButtonActive = new ColorTextureManager.ColorTextureDummy(TexItemType.Gradient_Radial, ColorRGBA.lightred, ColorRGBA.grayscale_025, 8);
        menu.itemButtonNormal =  new ColorTextureManager.ColorTextureDummy(TexItemType.Solid, ColorRGBA.grayscale_025, ColorRGBA.none, 8);
        menu.itemButtonHover =  new ColorTextureManager.ColorTextureDummy(TexItemType.Gradient_Radial, ColorRGBA.lightgreyred, ColorRGBA.grayscale_025, 8);

        menu.mainButtonNormal =  new ColorTextureManager.ColorTextureDummy(TexItemType.Solid, ColorRGBA.grayscale_025, ColorRGBA.none, 8);
        menu.mainButtonHover =  new ColorTextureManager.ColorTextureDummy(TexItemType.Solid, ColorRGBA.grayscale_032, ColorRGBA.none, 8);
        menu.mainButtonActive =  new ColorTextureManager.ColorTextureDummy(TexItemType.Gradient_Radial, ColorRGBA.grayscale_032, ColorRGBA.grayscale_064, 8);

        menu.backgroundTexture =  new ColorTextureManager.ColorTextureDummy(TexItemType.Bordered, ColorRGBA.grayscale_032, ColorRGBA.grayscale_016, 8);
    }

    void init_keys()
    {
        keys = new KeyMonitorGroup();
        keys.init();
    }

    void init_keyactions()
    {
        keyActions = new KeyActionGroup();
        keyActions.init(keys);
    }

    void init_colmgr()
    {
        colmgr = new ColorTextureManager();
        colmgr.CacheFolder = CacheFolder;
        colmgr.ClearCache();
    }

    void init_colmgr_colors()
    {

        colmgr.LoadTexture(TexItemType.Solid, ColorRGBA.red, ColorRGBA.none, 8);

        //Register some Grids with 2 equal colors
        colmgr.LoadTexture(TexItemType.Checker, ColorRGBA.grey, ColorRGBA.grey, 8);
        colmgr.LoadTexture(TexItemType.Checker, ColorRGBA.darkgrey, ColorRGBA.darkgrey, 8);
        colmgr.LoadTexture(TexItemType.Checker, ColorRGBA.lightgrey, ColorRGBA.lightgrey, 8);

        //generate Grids using grayscale in steps of 32
        colmgr.LoadTexture(TexItemType.Checker, ColorRGBA.grayscale_000, ColorRGBA.grayscale_048, 8);
        colmgr.LoadTexture(TexItemType.Checker, ColorRGBA.grayscale_032, ColorRGBA.grayscale_096, 8);
        colmgr.LoadTexture(TexItemType.Checker, ColorRGBA.grayscale_064, ColorRGBA.grayscale_128, 8);
        colmgr.LoadTexture(TexItemType.Checker, ColorRGBA.grayscale_096, ColorRGBA.grayscale_160, 8);
        colmgr.LoadTexture(TexItemType.Checker, ColorRGBA.grayscale_128, ColorRGBA.grayscale_192, 8);
        colmgr.LoadTexture(TexItemType.Checker, ColorRGBA.grayscale_160, ColorRGBA.grayscale_160, 8);
        colmgr.LoadTexture(TexItemType.Checker, ColorRGBA.grayscale_192, ColorRGBA.grayscale_255, 8);
    }



    private void OnGUI()
    {

        if (folders.Count > 0)
        {
            selectedFolderIndex = Mathf.Clamp(selectedFolderIndex, 0, folders.Count - 1);
        }


        dropdownMenu.checkMouseEvents(Event.current);
        menu_Text.checkMouseEvents(Event.current);
        menu_Img.checkMouseEvents(Event.current);

        //Scroll Zoom Event
        displaysize = displayContentScrollEvent(displaysize, DISPLAYSIZE_MIN, DISPLAYSIZE_MAX, Event.current);

        //Keys
        keys.updateMonitors();
        keyActions.updateKeyActions();

        //Header Bar
        displayHeader();

        GUILayout.BeginHorizontal();
        {

            GUILayout.BeginVertical("box", GUILayout.Width(splitviewer.splitPosition));
            {
                displayFolders(splitviewer.splitPosition);
                displayDebugHeader();
            }
            GUILayout.EndVertical();

            if (splitviewer.drawSplit(Event.current, position)) Repaint();



            GUILayout.BeginVertical();
            {
                displayHelpHeader();
                displayContent(position.width - splitviewer.splitPosition);
            }
            GUILayout.EndVertical();

            //use Vertical layout with brackeds like vertical{}


        }
        GUILayout.EndHorizontal();
        dropdownMenu.Draw(Event.current, new Vector2(100, 30));
        menu_Text.Draw(Event.current, new Vector2(100, 30));
        menu_Img.Draw(Event.current, new Vector2(100, 30));

    }

    public float displayContentScrollEvent(float target, float min, float max, Event current)
    {
        //check if mouse event scroll
        if (current.type == EventType.ScrollWheel && current.control)
        {
            target = Mathf.Clamp(target - (2 * current.delta.y), min, max);
            Repaint();
            current.type = EventType.Used;
        }
        return target;
    }



    private void displayHelpHeader()
    {

        if (displayHelp) //Also used for Debugging
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.BeginVertical();
                {
                    boldParser("<Help:>");
                    boldParser("<Control + Left:> Focus");
                    boldParser("<Alt + Left:> in Explorer");
                    boldParser("<Shift + Left:> Drag");
                }
                GUILayout.EndVertical();


                GUILayout.BeginVertical();
                {
                    boldParser("<Control + Scroll:> Zoom");
                    boldParser("<Shift + D + Hover:> Duplicate");
                    boldParser("<Control + Alt + Shift + Click:> Debug");
                }
                GUILayout.EndVertical();


            }
            GUILayout.EndHorizontal();
            GUILayout.Space(40);
        }

    }

    //Method to create buttonStyle_Content
    private GUIStyle create_Button_Sized(int width, int height)
    {
        GUIStyle style = new GUIStyle(GUI.skin.button);
        style.fixedHeight = width;
        style.fixedWidth = height;
        return style;
    }

    private GUIStyle create_Label_Sized(int width, int height)
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fixedHeight = width;
        style.fixedWidth = height;
        style.alignment = TextAnchor.MiddleCenter;
        return style;
    }


    private void DrawStatusIcon(GUIStyle iconstyle)
    {

        void makeLabel(string icon, GUIStyle style)
        {
            GUILayout.Label(EditorGUIUtility.IconContent(icon).image, iconstyle);
        }

        if (keyActions.mode_Hidden.call_NoExecution())
        {
            makeLabel("d_CacheServerDisconnected", iconstyle);
            Repaint();
        }
        else if (keyActions.mode_Focus.call_NoExecution())
        {
            makeLabel("d_Search Icon", iconstyle);
            Repaint();
        }
        else if (keyActions.mode_Explorer.call_NoExecution())
        {
            makeLabel("d_Search Icon", iconstyle);
            Repaint();
        }
        else if (keyActions.action_Update.call_NoExecution())
        {
            makeLabel("d_Refresh", iconstyle);
            Repaint();
        }
        else
        {
            makeLabel("ViewToolOrbit", iconstyle);

        }
    }

    private void displayHeader()
    {



        //Button Style for Filter and Settings
        GUIStyle style_Button_Header = create_Button_Sized(ICONBUTTON_SIZE, ICONBUTTON_SIZE);

        //Lable Style for the Icons
        GUIStyle style_Icons_Header = create_Label_Sized(ICONBUTTON_SIZE, ICONBUTTON_SIZE);

        style_Icons_Header.normal.background = colmgr.LoadTexture(TexItemType.Solid, ColorRGBA.darkgrey);

        //Draw Background Texture
        GUI.DrawTexture(new Rect(0, 0, position.width, HEADER_HEIGHT), colmgr.LoadTexture(TexItemType.Gradient_Vertical, ColorRGBA.grayscale_032, ColorRGBA.grayscale_048, 32), ScaleMode.StretchToFill, true);


        using (new GUILayout.HorizontalScope(GUILayout.Height(HEADER_HEIGHT), GUILayout.Width(position.width)))
        {

            //Begin Vertical
            GUILayout.BeginVertical();
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();

                    /*
                    for (int i = 0; i < buttonLabels.Length; i++)
                    {
                        bool isActive = (selectedButton == i);
                        bool isToggled = GUILayout.Toggle(isActive, buttonLabels[i], "Button");

                        if (isToggled != isActive)
                        {
                            selectedButton = isToggled ? i : -1;
                        }
                    }
                    */

                    //BACKGROUND SWITCHER
                    if (folders.Count > 0)
                    {
                        folders[selectedFolderIndex].displayNames = GUILayout.Toggle(folders[selectedFolderIndex].displayNames, EditorGUIUtility.IconContent("TrueTypeFontImporter Icon").image, style_Button_Header);
                        if (GUILayout.Button(EditorGUIUtility.IconContent("d_PreTexRGB").image, style_Button_Header))
                            folders[selectedFolderIndex].backgroundtexture = (folders[selectedFolderIndex].backgroundtexture + 1) % colmgr.GetTextureCount(TexItemType.Checker, null, null, -1);
                    }

                    //ADD FOLDER FROM FOLDER DIALOG
                    if (GUILayout.Button(EditorGUIUtility.IconContent("d_Folder Icon").image, style_Button_Header)) add_From_Folderdialog();

                    //HIDDEN MODE
                    else if (keyActions.mode_Hidden.call_NoExecution())
                        if (GUILayout.Button("!!!Clear All Folders!!!")) { folders.Clear(); }


                        //REMOVE CURRENT FOLDER
                        else if (keyActions.mode_Hidden.call_NoExecution())
                            if (GUILayout.Button("Remove Current Folder"))
                            {
                                //save the selected folder index
                                int oldindex = selectedFolderIndex;
                                if (folders.Count > 0)
                                {

                                    //Save Editor PRef
                                    EditorPrefs.SetString("anifanAMGR_AssetPath" + selectedFolderIndex, "");
                                    selectedFolderIndex = (folders.Count == 1) ? -1 : Math.Max(0, selectedFolderIndex - 1); //Clamp / Lower Index / Reset to -1
                                    folders.RemoveAt(oldindex);
                                    //remove from Editor Pref by setting the path to null
                                }
                            }

                    //REFRESH
                    if (GUILayout.Button(EditorGUIUtility.IconContent("d_Refresh").image, style_Button_Header))
                    {
                        folders[selectedFolderIndex].reloadFiles(); //GetPreviewImage
                        folders[selectedFolderIndex].reloadPreviews();
                        colmgr.ClearCache();
                        init_colmgr_colors();
                        colmgr.RegenerateCache();
                    }

                    //HELP BUTTON
                    if (GUILayout.Button("?", GUILayout.Width(ICONBUTTON_SIZE), GUILayout.Height(ICONBUTTON_SIZE))) displayHelp = !displayHelp;

                    //Horizontal
                }
                GUILayout.EndHorizontal();

                //10 unit space
                GUILayout.Space(10);



                GUILayout.BeginHorizontal();
                {
                    //Add Spacing based on split
                    DrawStatusIcon(style_Icons_Header);

                    GUILayout.Space(splitviewer.splitPosition - ICONBUTTON_SIZE);

                    //FILTER BUTTONS
                    if (folders.Count > 0 && dropdownMenu.getValue("Filters"))
                    {
                        FolderInfo folder = folders[selectedFolderIndex];

                        bool IsTypeShown(FileTypes type) => (folder.shownTypes & type) != 0;
                        void FileTypeToggleGUI(FileTypes type, string iconName)
                        {
                            //Draw Filter Toggle and set the shownTypes
                            folder.shownTypes = GUILayout.Toggle(IsTypeShown(type), EditorGUIUtility.IconContent(iconName).image, style_Button_Header)
                            ? folder.shownTypes |= type : folder.shownTypes &= ~type;
                        }

                        //Draw Filter Buttons
                        GUILayout.BeginVertical();
                        GUILayout.BeginHorizontal();
                        for (int i = 1; i < Enum.GetValues(typeof(FileTypes)).Length - 3; i++)
                        {
                            FileTypeToggleGUI((FileTypes)(1 << i), FileTypeIcons.GetIcon((FileTypes)(1 << i)));
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndVertical();
                    }

                    //Flex
                    GUILayout.FlexibleSpace();


                    displaysize = GUILayout.HorizontalSlider(displaysize, DISPLAYSIZE_MIN, DISPLAYSIZE_MAX, GUILayout.Width(100));

                    GUILayout.Space(10);
                }
                GUILayout.EndHorizontal();

                //Flex
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndVertical();
            GUILayout.Space(15);
        }
        GUILayout.Space(20);
    }



    private Texture2D getContentBackgroundTexture()
    {
        int texturecount = colmgr.GetTextureCount(TexItemType.Checker, null, null, -1);
        //boxStyle_Previewbackground.normal.background = textures_previewbackground[folders[selectedFolderIndex].backgroundtexture % colmgr.grids.Count];
        if (texturecount > 0)
        {
            Texture2D tex = colmgr.GetTextureItemAt(
            folders[selectedFolderIndex].backgroundtexture % texturecount, TexItemType.Checker, null, null, -1);
            if (tex != null)
                return tex;
            return null;
        }
        else
        {
            return colmgr.LoadTexture(TexItemType.Checker, ColorRGBA.grey, ColorRGBA.darkgrey, 9);
        }
    }


    private async void displayContent(float displaywidth)
    {



        DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
        GUIStyle boxStyle_Previewbackground = new GUIStyle(GUI.skin.box);
        Event currentEvent = Event.current;

        //Draw Background Texture based on Droparea with Gradient

        GUILayout.BeginVertical();
        GUI.DrawTexture(new Rect(splitviewer.splitPosition, HEADER_HEIGHT, displaywidth, position.height), colmgr.LoadTexture(TexItemType.Gradient_Vertical, ColorRGBA.grayscale_048, ColorRGBA.grayscale_064, 32), ScaleMode.StretchToFill, true);


        display_Content_scroll = GUILayout.BeginScrollView(display_Content_scroll);
        Rect dropArea = new Rect(0, 0, displaywidth, position.height);


        //check if the count of the assetpaths is greater than 0
        if (folders.Count > 0)
        {
            //if the selected folder index is greater than 0 and less than the count of the assetpaths
            if (selectedFolderIndex >= 0 && selectedFolderIndex < folders.Count)
            {
                Texture2D backgroundTexture = getContentBackgroundTexture();
                if (backgroundTexture != null)
                {
                    boxStyle_Previewbackground.normal.background = backgroundTexture;
                }

            }
        }


        //If Folder is Selected
        if (selectedFolderIndex >= 0 && selectedFolderIndex < folders.Count)
        {
            //Setup Path
            FolderInfo currentFolder = folders[selectedFolderIndex];
            string assetPath = currentFolder.path;

            //IF DROP IS HAPPENING
            if (currentEvent.type == EventType.DragUpdated || currentEvent.type == EventType.DragPerform)
            {
                bool notThisWindow = DragAndDrop.GetGenericData("SourceGUI") as string != guiID;

                if (notThisWindow) //If not this Window display Dropping UI Text
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    //LAbel
                    GUI.Label(dropArea, "Drop Here");
                    GUI.Box(dropArea, "");
                }

                //When File is Dropped into the Asset View
                if (currentEvent.type == EventType.DragPerform && notThisWindow)
                    foreach (var draggedObject in DragAndDrop.objectReferences)
                    {
                        //If -> Create Prefab
                        if (draggedObject is GameObject)
                        {
                            //Check Filetype and Perform the Drop (OUTSOURCE THIS LATER!)
                            if (draggedObject.name.Contains("(Clone)"))
                            {
                                //Create a Prefab
                                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(draggedObject as GameObject, assetPath + "/" + draggedObject.name + ".prefab");
                                Debug.Log("Prefab Created at: " + assetPath + "/" + draggedObject.name + ".prefab");
                            }
                            else
                            {
                                Debug.Log("Not a Clone");
                            }
                        }
                        else
                        //If -> Move Folder
                        if (Directory.Exists(AssetDatabase.GetAssetPath(draggedObject)))
                        {
                            //try to move the folder
                            string sourcePath = AssetDatabase.GetAssetPath(draggedObject);
                            string destinationPath = assetPath + "/" + Path.GetFileName(sourcePath);
                            string result = AssetDatabase.MoveAsset(sourcePath, destinationPath);

                            if (string.IsNullOrEmpty(result))
                            {
                                Debug.Log("Moved Folder to: " + destinationPath);
                            }
                            else
                            {
                                Debug.LogError("Asset move failed: " + result);
                            }
                        }
                        else
                        {

                            //If not a Directory or Gameobject -> Move the File
                            string path = AssetDatabase.GetAssetPath(draggedObject);
                            if (path != "")
                            {
                                //Move the File with unknown Type to the Selected Folder
                                string destinationPath = assetPath + "/" + Path.GetFileName(path);
                                string result = AssetDatabase.MoveAsset(path, destinationPath);

                                if (string.IsNullOrEmpty(result))
                                {
                                    Debug.Log("Moved File to: " + destinationPath);
                                }
                                else
                                {
                                    Debug.LogError("Asset move failed: " + result);
                                }


                            }
                        }
                    }


            }


            // Get all the assets inside the selected folder
            //string[] assetGuids = AssetDatabase.FindAssets("", new[] { assetPath });



            int columnCount = 0;
            int maxColumns = Mathf.FloorToInt((displaywidth - 20) / (displaysize + 6)); // 70 is the width of each column (50 for the image and 20 for padding)


            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontsize,
                margin = new RectOffset(0, 0, 2, 2),
                padding = new RectOffset(2, 6, 0, 0)
            };

            GUILayout.BeginHorizontal();
            foreach (ew_FileInfo file in currentFolder.files)
            {
                FolderInfo current = folders[selectedFolderIndex];

                // Continue if the asset is not of the selected type
                FileTypes assetPathType = file.type;
                if ((current.shownTypes & assetPathType) == 0)
                {
                    continue;
                }


                //Breaking Over to New Line
                if (maxColumns>0)
                if (columnCount % maxColumns == 0 && columnCount != 0)
                {
                    GUILayout.FlexibleSpace(); // Add flexible space to align the content to the left
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                }
                GUILayout.BeginVertical();



                //Asset Image Rect
                Rect assetRect = GUILayoutUtility.GetRect(displaysize, displaysize);
                bool isHovering = assetRect.Contains(currentEvent.mousePosition);

                bool redraw = false;
                if (isHovering)
                {
                    if (keyActions.action_Update.call())
                    {
                        redraw = true;
                    }
                }


                string assetPathTarget = AssetDatabase.GUIDToAssetPath(file.guid);

                //Draw the Assets Image
                if (file.preview != null)
                {

                    //use BoxStyle_Previewbackground instead of gridTexture
                    GUI.DrawTexture(new Rect(assetRect.x, assetRect.y, assetRect.width, assetRect.height), boxStyle_Previewbackground.normal.background, ScaleMode.StretchToFill, true);
                    GUI.DrawTexture(assetRect, file.preview, ScaleMode.ScaleToFit, true);
                    //draw a frame around the image


                }
                else //If No Image set yet use Placeholder
                {
                    file.tryGetPeview();

                    //find type based on the assets type
                    string type = getIconForFiletype(assetPathTarget);
                    //Draw default file icon from unity
                    GUI.DrawTexture(assetRect, EditorGUIUtility.IconContent(type).image, ScaleMode.ScaleToFit, true);
                }

                if (currentFolder.displayNames)
                    if ((((menu_Text.getValue("Proxys") && file.name.Length > (displaysize / fontsize))) || (menu_Text.getValue("Forceproxy"))&&menu_Text.getValue("Proxys") )&& file.nameproxy != "")
                    {
                        GUILayout.Label(file.nameproxy, labelStyle, GUILayout.Width(displaysize));
                    }
                    else
                        GUILayout.Label(file.name, labelStyle, GUILayout.Width(displaysize));

                //int maxCharacters = 8;

                //float extramodifier = -4 + (((displaysize * displaysize) / (140f * 140f)) * 3f);
                //if (extramodifier < 1) extramodifier = 1;

                ////Set Max Characters depending on the display size
                //maxCharacters = Mathf.FloorToInt((displaysize / 8) * extramodifier);

                //// Draw the asset's name
                //if (currentFolder.displayNames)
                //{
                //    //Horizontal Layout for the Name
                //    GUILayout.BeginVertical();
                //    GUILayout.Label(file.name.Substring(0, Mathf.Min(file.name.Length, maxCharacters)), labelStyle);

                //    //Drawing the rest of the name in a second line if it is too long
                //    if (file.name.Length > maxCharacters)
                //    {
                //        int remainingLength = file.name.Length - maxCharacters;
                //        GUILayout.Label(file.name.Substring(maxCharacters, Mathf.Min(remainingLength, maxCharacters)), labelStyle);
                //    }
                //    GUILayout.EndVertical();
                //}

                // Handle mouse events for Dragging

                if (!Event.current.shift || keyActions.action_Duplicate.call_NoExecution())
                    if (assetRect.Contains(currentEvent.mousePosition))
                    {

                        if (keyActions.action_Duplicate.call())
                        {
                            //Duplicate the asset
                            duplicateAsset(assetPathTarget);

                        }

                        if (currentEvent.type == EventType.MouseDown || currentEvent.type == EventType.MouseDrag)
                        {


                            if (keyActions.mode_Focus.call())
                            { //Pinging the Asset
                                EditorGUIUtility.PingObject(file.asset);
                                Selection.activeObject = file.asset;
                            }
                            else if (keyActions.mode_Explorer.call())
                            { //Open the Folder in Explorer
                                string absolutePath = Path.GetFullPath(assetPathTarget);
                                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{absolutePath}\"");
                            }
                            else
                            { //Initiate Drag and Drop
                                DragAndDrop.PrepareStartDrag();
                                DragAndDrop.objectReferences = new UnityEngine.Object[] { file.asset };
                                DragAndDrop.SetGenericData("SourceGUI", guiID);
                                DragAndDrop.StartDrag(file.name);
                                currentEvent.Use();
                            }
                        }
                    }

                GUILayout.Space(3);
                GUILayout.EndVertical();
                GUILayout.Space(3);

                columnCount++;
            }
            GUILayout.FlexibleSpace(); // Add flexible space to align the content to the left
            GUILayout.EndHorizontal();


        }

        GUILayout.EndScrollView();
        GUILayout.EndVertical();
    }

    public async Task<Texture2D> GetPreviewImage(string assetPath, UnityEngine.Object asset)
    {
        Texture2D assetPreview = AssetPreview.GetAssetPreview(asset);
        if (assetPreview != null)
        {
            // Create a new texture without mipmaps and copy the preview into it
            Texture2D persistentPreview = new Texture2D(assetPreview.width, assetPreview.height, assetPreview.format, false);
            Graphics.CopyTexture(assetPreview, persistentPreview);

            // Store the persistent preview instead of the temporary one
            return persistentPreview;
        }

        return null;
    }


    //Scroll Variable

    private void displayFolders(float displaywidth)
    {


        display_Assets_scroll = GUILayout.BeginScrollView(display_Assets_scroll);


        GUILayout.BeginHorizontal();
        //make it scrollable
        GUILayout.BeginVertical(GUILayout.Width(displaywidth));



        //check for File Drops

        Event currentEvent = Event.current;

        //If Drop is happening
        if (currentEvent.type == EventType.DragUpdated || currentEvent.type == EventType.DragPerform)
        {
            bool notThisWindow = DragAndDrop.GetGenericData("SourceGUI") as string != guiID;

            if (notThisWindow)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            }

            if (currentEvent.type == EventType.DragPerform && notThisWindow)
            {

                foreach (var draggedObject in DragAndDrop.objectReferences)
                {
                    //check if it's a directory
                    if (Directory.Exists(AssetDatabase.GetAssetPath(draggedObject)))
                    {
                        //Move the Folder with unknown Type to the Selected Folder
                        string path = AssetDatabase.GetAssetPath(draggedObject);
                        //add the folder to the AssetPathObject list
                        FolderInfo newAssetpath = new FolderInfo(path, Path.GetFileName(path));
                        //add the new assetpath to the list
                        folders.Add(newAssetpath);

                    }
                }

            }
        }

        // Display the list of asset paths
        for (int i = 0; i < folders.Count; i++)
        {
            GUILayout.BeginHorizontal();
            FolderInfo assetPathObject = folders[i];

            // Use a different style for the selected button
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            //limit height to 25 and width to 100
            buttonStyle.fixedHeight = UI_ELEMENT_HEIGHT;
            buttonStyle.fixedWidth = displaywidth - 20;
            if (keyActions.mode_Hidden.call_NoExecution())
                buttonStyle.fixedWidth = displaywidth - 40;

            buttonStyle.fontStyle = FontStyle.Bold;
            buttonStyle.alignment = TextAnchor.MiddleLeft;
            buttonStyle.fontSize = fontsize;

            //Use a Gradient for the Background using the colormanager
            Texture2D backgroundgradient = colmgr.LoadTexture(TexItemType.Gradient_Horizontal, ColorRGBA.grayscale_032, ColorRGBA.grayscale_048, 64);
            buttonStyle.normal.background = backgroundgradient;


            // Use a different style for the selected button
            GUIStyle buttonXStyle = new GUIStyle(GUI.skin.button);
            //limit height to 25 and width to 100
            buttonXStyle.fixedHeight = 1;
            buttonXStyle.fixedWidth = 1;

            buttonXStyle.fontStyle = FontStyle.Bold;
            buttonXStyle.alignment = TextAnchor.MiddleLeft;
            buttonXStyle.fontSize = fontsize;



            if (i == selectedFolderIndex)
            {
                Texture2D background = new Texture2D(1, 1);
                Color color = new Color(0.4f, 0.4f, 0.6f, 1.0f);
                background.SetPixel(0, 0, color);
                background.Apply();

                buttonStyle.normal.background = background;
            }





            if (keyActions.mode_Focus.call_NoExecution())
            {
                if (GUILayout.Button(new GUIContent(assetPathObject.name, EditorGUIUtility.IconContent("ViewToolOrbit").image), buttonStyle))

                    if (Directory.Exists(assetPathObject.path))
                    {
                        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPathObject.path));
                        Selection.activeObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPathObject.path);
                    }


            }
            else if (keyActions.mode_Explorer.call_NoExecution())
            {
                if (GUILayout.Button(new GUIContent(assetPathObject.name, EditorGUIUtility.IconContent("BuildSettings.Metro.Small").image), buttonStyle))
                    System.Diagnostics.Process.Start(Path.GetFullPath(assetPathObject.path));

            }
            else
            {
                if (dropdownMenu.getValue("Icons"))
                {
                    if (GUILayout.Button(new GUIContent(assetPathObject.name, EditorGUIUtility.IconContent("Folder Icon").image), buttonStyle))
                    {
                        selectedFolderIndex = i != selectedFolderIndex ? i : selectedFolderIndex;
                    }
                }
                else
                {
                    if (GUILayout.Button(new GUIContent(assetPathObject.name), buttonStyle))
                    {
                        selectedFolderIndex = i != selectedFolderIndex ? i : selectedFolderIndex;
                    }
                }
            }



            if (keyActions.mode_Hidden.call_NoExecution())
            {
                buttonXStyle.fixedWidth = 25;
                buttonXStyle.fixedHeight = UI_ELEMENT_HEIGHT;

                //Folder Remove Button
                if (GUILayout.Button("X", buttonXStyle))
                {
                    EditorPrefs.SetString("anifanAMGR_AssetPath" + i, "");
                    selectedFolderIndex = selectedFolderIndex > 0 ? selectedFolderIndex - 1 : 0;
                    folders.RemoveAt(i);
                }
            }

            GUILayout.EndHorizontal();
        }

        GUILayout.EndVertical();
        //Spacer 
        GUILayout.Space(10);
        GUILayout.EndHorizontal();
        GUILayout.EndScrollView();
    }

    //================================================================================================
    //Folder Management


    private void AddFolder(string path)
    {
        string assetName = System.IO.Path.GetFileName(path);
        folders.Add(new FolderInfo(path, assetName));
    }


    private void add_From_Folderdialog()
    {
        //call popup window
        string path = EditorUtility.OpenFolderPanel("Select Folder", "", "");
        if (!string.IsNullOrEmpty(path))
        {
            filterString = path;
            EditorPrefs.SetString("filterString", filterString);

            // Automatically add the selected folder
            AddFolder(filterString);
        }
    }


    //================================================================================================
    //UTILS


    private void duplicateAsset(string assetPath)
    {
        Debug.Log("Duplicating asset at path: " + assetPath);
        // Get the asset at the path
        UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);

        // Duplicate the asset
        string assetName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        string assetExtension = System.IO.Path.GetExtension(assetPath);
        string baseAssetName = assetName;
        int count = 0;

        // Check if the asset name already contains an increment
        int underscoreIndex = assetName.LastIndexOf('_');
        if (underscoreIndex != -1 && int.TryParse(assetName.Substring(underscoreIndex + 1), out int existingIncrement))
        {
            baseAssetName = assetName.Substring(0, underscoreIndex);
            count = existingIncrement;
        }

        string newAssetName = baseAssetName;
        string newPath = "";

        do
        {
            count++;
            newAssetName = $"{baseAssetName}_{count.ToString("D3")}{assetExtension}";
            newPath = AssetDatabase.GenerateUniqueAssetPath(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(assetPath), newAssetName));
        } while (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(newPath) != null);

        AssetDatabase.CopyAsset(assetPath, newPath);

        // Refresh the asset database
        AssetDatabase.Refresh();

        // Focus the new asset in the Project window
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(newPath));
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(newPath);
    }

    private static Color GetRGBA(ColorRGBA color)
    {
        return SmartColorUtility.GetRGBA(color);
    }

    //================================================================================================
    //Ivon Management

    public string getIconForFiletype(string assetPathType)
    {
        Dictionary<string, string> fileTypeIcons = new Dictionary<string, string>()
        {
            { ".prefab", "d_Prefab Icon" },
            { ".unity", "_SceneAsset Icon" },
            { ".mat", "d_Material Icon" },
            { ".png", "d_RawImage Icon" },
            { ".fbx", "d_PrefabModel Icon" },
            { ".anim", "AnimationClip Icon" },
            { ".wav", "d_AudioImporter Icon" },
            { ".cs", "cs Script Icon" },
            { ".shader", "d_Shader Icon" }
        };

        string type = "Unknown";

        foreach (var fileTypeIcon in fileTypeIcons)
        {
            if (assetPathType.EndsWith(fileTypeIcon.Key))
            {
                type = fileTypeIcon.Value;
                break;
            }
        }

        return type;
    }



    //================================================================================================
    //LOAD AND SAVE PREFERENCES

    private void save_Paths()
    {
        // Save the asset paths to EditorPrefs
        for (int i = 0; i < folders.Count; i++)
        {
            FolderInfo assetPathObject = folders[i];
            EditorPrefs.SetString("anifanAMGR_AssetPath" + i, assetPathObject.path);
            EditorPrefs.SetString("anifanAMGR_AssetName" + i, assetPathObject.name);
            EditorPrefs.SetBool("anifanAMGR_DisplayNames" + i, assetPathObject.displayNames);
            EditorPrefs.SetInt("anifanAMGR_BackgroundTexture" + i, assetPathObject.backgroundtexture);
            EditorPrefs.SetInt("anifanAMGR_shownTypes" + i, (int)assetPathObject.shownTypes);
            EditorPrefs.SetBool("anifanAMGR_showFolders" + i, assetPathObject.showFolders);
            EditorPrefs.SetInt("anifanAMGR_previewResolution"+i, assetPathObject.previewResolution);
        }

        EditorPrefs.SetInt("anifanAMGR_selectedFolderIndex", selectedFolderIndex);

        //Store Menu options for the Dropdown Menus
        //Filter
        EditorPrefs.SetBool("anifanAMGR_Filters", dropdownMenu.getValue("Filters"));
        EditorPrefs.SetBool("anifanAMGR_Icons", dropdownMenu.getValue("Icons"));
        EditorPrefs.SetBool("anifanAMGR_Proxys", menu_Text.getValue("Proxys"));
        EditorPrefs.SetBool("anifanAMGR_Forceproxy", menu_Text.getValue("Forceproxy"));
        //Font size
        EditorPrefs.SetBool("anifanAMGR_Font8", menu_Text.getValue("Font 8"));
        EditorPrefs.SetBool("anifanAMGR_Font12", menu_Text.getValue("Font 12"));
        EditorPrefs.SetBool("anifanAMGR_Font16", menu_Text.getValue("Font 16"));

    }

    private void load_Paths()
    {
        // Load the asset paths from EditorPrefs
        folders.Clear();
        int i = 0;
        while (true)
        {
            string assetPath = EditorPrefs.GetString("anifanAMGR_AssetPath" + i);
            if (string.IsNullOrEmpty(assetPath))
            {
                break;
            }

            FolderInfo newasset = new FolderInfo(assetPath, EditorPrefs.GetString("anifanAMGR_AssetName" + i));
            newasset.displayNames = EditorPrefs.GetBool("anifanAMGR_DisplayNames" + i);
            newasset.backgroundtexture = EditorPrefs.GetInt("anifanAMGR_BackgroundTexture" + i);
            newasset.shownTypes = (FileTypes)EditorPrefs.GetInt("anifanAMGR_shownTypes" + i);
            newasset.showFolders = EditorPrefs.GetBool("anifanAMGR_shownFolders" + i);
            newasset.previewResolution = EditorPrefs.GetInt("anifanAMGR_previewResolution"+i);
            folders.Add(newasset);
            i++;
        }

        //Make Sure one Asset is Selected
        selectedFolderIndex = EditorPrefs.GetInt("anifanAMGR_selectedFolderIndex", -1);

        //Load Menu Options
        //Filter
        dropdownMenu.setValue("Filters", EditorPrefs.GetBool("anifanAMGR_Filters"));
        dropdownMenu.setValue("Icons", EditorPrefs.GetBool("anifanAMGR_Icons"));
        menu_Text.setValue("Proxys", EditorPrefs.GetBool("anifanAMGR_Proxys"));
        menu_Text.setValue("Forceproxy", EditorPrefs.GetBool("anifanAMGR_Forceproxy"));
        //Font size
        menu_Text.setValue("Font 8", EditorPrefs.GetBool("anifanAMGR_Font8"));
        menu_Text.setValue("Font 12", EditorPrefs.GetBool("anifanAMGR_Font12"));
        menu_Text.setValue("Font 16", EditorPrefs.GetBool("anifanAMGR_Font16"));



    }



    //Debugger:


    private void displayDebugHeader()
    {

        if (displayHelp) //Also used for Debugging
        {
            //Scrollview here
            display_Debug_scroll = GUILayout.BeginScrollView(display_Debug_scroll);
            {
                //GUI to Debug the colmgr

                GUILayout.BeginVertical(GUILayout.Height(100), GUILayout.Width(200));
                {

                    //Draw the Labels
                    void Debug_DrawLabels(Dictionary<string, string> debugdict)
                    {
                        foreach (KeyValuePair<string, string> entry in debugdict)
                        {
                            GUILayout.Label(entry.Key + ": " + entry.Value);
                        }
                    }


                    void Debug_DrawObject(List<object> value)
                    {
                        //Layout Vertical
                        GUILayout.BeginVertical();

                        foreach (object obj in value)
                        {
                            string type = obj.ToString();
                            if (obj == null)
                            {
                                GUILayout.Label(type + " is null");
                            }
                            else
                            {
                                GUILayout.Label(type + " is not null");
                            }
                        }
                        GUILayout.EndVertical();
                    }


                    void Debug_DrawColors(List<ColorTextureItem> value)
                    {
                        //Layout Vertical
                        GUILayout.BeginVertical();

                        //Draw each Color with thier color_a and color_b and type aswell as the tiling
                        foreach (ColorTextureItem col in value)
                        {
                            GUILayout.Label(col.type.ToString() + " [" + col.color_a.ToString() + "] [" + col.color_b.ToString() + "] T:" + col.tiling.ToString());
                        }

                        GUILayout.EndVertical();
                    }





                    //Color
                    GUILayout.Label("Color Debug", EditorStyles.boldLabel);
                    Dictionary<string, string> debug_colmgr = new Dictionary<string, string>
                    {
                    { "Grids", colmgr.GetTextureCount(TexItemType.Checker, null, null, -1).ToString() },
                    { "Solids", colmgr.GetTextureCount(TexItemType.Solid, null, null, -1).ToString() },
                    { "GradientsH", colmgr.GetTextureCount(TexItemType.Gradient_Horizontal, null, null, -1).ToString() },
                    { "GradientsV", colmgr.GetTextureCount(TexItemType.Gradient_Vertical, null, null, -1).ToString() },
                    { "Total Textures", colmgr.textureElements.Count.ToString() }
                    };

                    Debug_DrawLabels(debug_colmgr);
                    Debug_DrawColors(colmgr.textureElements);
                    if (folders.Count > 0) GUILayout.Label("Current Texture: " + folders[selectedFolderIndex].backgroundtexture);




                    //Folder
                    GUILayout.Space(10);
                    Dictionary<string, string> debug_folder = new Dictionary<string, string> {
                    { "Foldercount", folders.Count.ToString() },
                    { "Selected Folder", selectedFolderIndex.ToString() }
                    };

                    GUILayout.Label("Folder Debug", EditorStyles.boldLabel);
                    Debug_DrawLabels(debug_folder);
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndScrollView();
        }

    }

    //Boldparser to split the string into multiple parts and draw them in a bold style label
    private void boldParser(string input)
    {
        //Horizontal Layout
        GUILayout.BeginVertical();
        GUILayout.BeginHorizontal(GUILayout.Width(200));


        //Draw Label and draw new label with Bold Style if the string contains <b> </b>
        if (input.Contains("<") && input.Contains(">"))
        {
            string[] parts = input.Split(new string[] { "<", ">" }, StringSplitOptions.None);
            for (int i = 0; i < parts.Length; i++)
            {
                if (i % 2 == 0)
                {
                    GUILayout.Label(parts[i]);
                }
                else
                {
                    //Make 200 width and bold label
                    GUILayout.Label(parts[i], EditorStyles.boldLabel);
                }
            }
        }
        else
        {
            GUILayout.Label(input);
        }


        GUILayout.EndHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.EndVertical();
    }


}
