
namespace AnimefanPostUPs_Tools.GUI_LayoutElements
{

    using UnityEngine;
    using UnityEditor;
    //UI Layout Missing and GetRGBA

    using AnimefanPostUPs_Tools.SmartColorUtility;
    using static AnimefanPostUPs_Tools.SmartColorUtility.ColorRGBA;
    using AnimefanPostUPs_Tools.ColorTextureManager;
    using AnimefanPostUPs_Tools.ColorTextureItem;

    using AnimefanPostUPs_Tools.KeyMonitorGroup;
    using AnimefanPostUPs_Tools.KeyActionGroup;

    using System;
    using System.Collections.Generic;



    //Class GUIManager for handling GUI Elements
    public class GUILayoutHandler
    {

        public class GUIStyleholder
        {

            //Store Alignment

            public Vector2 contentAlignment;
            public RectOffset padding;
            public RectOffset margin;
            public RectOffset border;

            //Store Background
            public ColorTextureManager.ColorTextureDummy activeBackground;
            public ColorTextureManager.ColorTextureDummy normalBackground;
            public ColorTextureManager.ColorTextureDummy hoverBackground;

            //texture for background


            //Store Text
            public TextAnchor textalignment;
            public ColorRGBA textColor;
            public Font font;
            public int fontSize;
            public FontStyle fontStyle;
            public bool wordWrap;
            public bool richText;

            //Enum Class Preset Types
            public enum Presettype
            {
                Default,
                Button,
                Label,
                TextField,
                TextArea,
                Toggle,
                Slider,
                Scrollbar,
                ScrollView,
                Window,
                Box,
                HorizontalSlider,
                VerticalSlider,
                HorizontalScrollbar,
                VerticalScrollbar,
                Custom
            }

            public GUIStyleholder(Presettype presettype)
            {
            }


            //Initialize a given Style
            public GUIStyle GetStyle(GUIStyle style, ColorTextureManager ColorManager)
            {
                style = new GUIStyle(style);

                //Alignment
                style.contentOffset = contentAlignment;
                style.padding = padding;
                style.margin = margin;
                style.border = border;

                //Text
                style.alignment = textalignment;
                style.normal.textColor = GetRGBA(textColor);
                style.font = font;
                style.fontSize = fontSize;
                style.fontStyle = fontStyle;
                style.wordWrap = wordWrap;
                style.richText = richText;

                //Background
                style.normal.background = activeBackground.LoadTexture(ColorManager);
                style.active.background = activeBackground.LoadTexture(ColorManager);
                style.hover.background = hoverBackground.LoadTexture(ColorManager);
                return style;
            }

            private static Color GetRGBA(ColorRGBA color)
            {
                return SmartColorUtility.GetRGBA(color);
            }


        }

        public interface IGUIElement
        {
            //Pre Funtion and Post Function
            //colorTextureManager
            ColorTextureManager ColorManager { get; set; }
            void DrawGUI(Event current);
        }


        public class Button : IGUIElement
        {
            public GUIStyleholder style;
            public ColorTextureManager ColorManager { get; set; }

            public void DrawGUI(Event current)
            {
                //Draw GUI
            }

            private static Color GetRGBA(ColorRGBA color)
            {
                return SmartColorUtility.GetRGBA(color);
            }
        }



    }

    //Class for Dropdown Menu that contains the Name of the Option, a Drawing Function and manages "Dropdown Items" that contain a boolean value or a Function
    public class DropdownMenu
    {
        //Enum for the Dropdown Items if they are a boolean or a function
        public enum DropdownItemType
        {
            Option_Bool,
            Option_Func,
            Option_Func_Bool
        }

        public string name;
        public DropdownItemType type;
        public List<DropdownItem> items = new List<DropdownItem>();
        public bool isOpen;
        public Rect displayArea;
        public ColorTextureManager colormgr;
        public Vector2 mouseposition;
        public Vector2 position;
        EventType oldType;

        //textures for Mainbutton as TextureItemDummy
        public ColorTextureManager.ColorTextureDummy mainButtonActive;
        public ColorTextureManager.ColorTextureDummy mainButtonNormal;
        public ColorTextureManager.ColorTextureDummy mainButtonHover;



        //textures for Itembuttons
        public ColorTextureManager.ColorTextureDummy itemButtonActive;
        public ColorTextureManager.ColorTextureDummy itemButtonNormal;
        public ColorTextureManager.ColorTextureDummy itemButtonHover;

        //Background:
        public ColorTextureManager.ColorTextureDummy backgroundTexture;


        public Rect itemPadding = new Rect(5, 2, 5, 2);
        public DropdownMenu(string name, ColorTextureManager colormgr, Vector2 position)
        { 

            this.name = name;
            this.colormgr = colormgr;
            this.position = position;

            //initialize the Textures
            mainButtonActive = new ColorTextureManager.ColorTextureDummy(TexItemType.Solid, ColorRGBA.grayscale_032, ColorRGBA.grayscale_032, 2);
            mainButtonNormal = new ColorTextureManager.ColorTextureDummy(TexItemType.Solid, ColorRGBA.grayscale_032, ColorRGBA.grayscale_032, 2);
            mainButtonHover = new ColorTextureManager.ColorTextureDummy(TexItemType.Solid, ColorRGBA.grayscale_032, ColorRGBA.grayscale_032, 2);

            itemButtonActive = new ColorTextureManager.ColorTextureDummy(TexItemType.Solid, ColorRGBA.grayscale_032, ColorRGBA.grayscale_032, 2);
            itemButtonNormal = new ColorTextureManager.ColorTextureDummy(TexItemType.Solid, ColorRGBA.grayscale_032, ColorRGBA.grayscale_032, 2);
            itemButtonHover = new ColorTextureManager.ColorTextureDummy(TexItemType.Solid, ColorRGBA.grayscale_032, ColorRGBA.grayscale_032, 2);

            backgroundTexture = new ColorTextureManager.ColorTextureDummy(TexItemType.Solid, ColorRGBA.grayscale_032, ColorRGBA.grayscale_032, 2);
        }

        public void AddItem(string name, bool value)
        {
            items.Add(new DropdownItem(name, value));
        }

        public void AddItem(string name, Action function)
        {
            items.Add(new DropdownItem(name, function));
        }

        public void AddItem(string name, bool value, Action function)
        {
            items.Add(new DropdownItem(name, value, function));
        }

        public bool getValue(string name)
        {
            foreach (DropdownItem item in items)
            {
                if (item.name == name)
                {
                    return item.value;
                }
            }
            return false;
        }

        //Set
        public void setValue(string name, bool value)
        {
            foreach (DropdownItem item in items)
            {
                if (item.name == name)
                {
                    item.value = value;
                }
            }
        }

        public void checkMouseEvents(Event current)
        {



            oldType = current.type;
            //If Dropdown is open use the Mouseevent
            if (this.isOpen)
            {
                if (current.type == EventType.MouseDown || current.type == EventType.MouseDrag)
                {
                    if (current.type == EventType.MouseDown)
                    {
                        this.mouseposition = current.mousePosition;
                    }
                    current.type = EventType.Used;
                }
            }

        }



        public void Draw(Event current, Vector2 size)
        {

            GUIStyle style = new GUIStyle(GUI.skin.button);
            style.fixedHeight = size.y;
            style.fixedWidth = size.x;
            //Dark Background
            style.normal.background = mainButtonNormal.LoadTexture(colormgr);
            style.active.background = mainButtonActive.LoadTexture(colormgr);
            style.hover.background = mainButtonHover.LoadTexture(colormgr);

            //relative to main Button
            float item_xOffset = 0;
            float item_yOffset = size.y + 5;

            //Rect for main Button
            Rect mainButtonRect = new Rect(
                position.x,
                position.y,
                size.x,
                size.y
                );



            if (GUI.Button(mainButtonRect, name, style))
            {
                isOpen = !isOpen;
            }

            if (isOpen)
            {
                int itemHeight = (int)size.y; // Replace with the actual height of each item
                int numItems = items.Count;
                //Draw largeer Box behind using gradient


                displayDropdownOverlay(mainButtonRect, oldType, size);
            }
        }

        public void displayDropdownOverlay(Rect buttonRect, EventType oldType, Vector2 size)
        {
            float boxYOffset = 6;
            float item_xOffset = 0;
            float item_yOffset = size.y + boxYOffset;




            //Rect for Content
            Rect contentRect = new Rect(
                position.x + item_xOffset,
                position.y + item_yOffset - boxYOffset,
                size.x,
                (size.y * items.Count) + boxYOffset
                );

            //Background style
            GUIStyle backgroundstyle = new GUIStyle(GUI.skin.box);
            backgroundstyle.normal.background = itemButtonNormal.LoadTexture(colormgr);


            GUI.Box(contentRect, "", backgroundstyle);

            //create Style for the Dropdown Items
            GUIStyle style = new GUIStyle(GUI.skin.button);
            style.fixedHeight = size.y - itemPadding.height - itemPadding.y - itemPadding.width;
            style.fixedWidth = size.x - itemPadding.width - itemPadding.x - itemPadding.height;
            style.normal.background = itemButtonNormal.LoadTexture(colormgr);
            style.active.background = itemButtonActive.LoadTexture(colormgr);
            style.hover.background = itemButtonHover.LoadTexture(colormgr);
            style.alignment = TextAnchor.MiddleLeft;



            bool isMouseDownEvent = oldType == EventType.MouseDown;
            bool trigger = false;

            for (int i = 0; i < items.Count; i++)
            {
                DropdownItem item = items[i];

                // Position the area for this item within the container
                Rect itemsRect = new Rect(
                position.x + item_xOffset + itemPadding.x,
                position.y + ((size.y) * i) + item_yOffset + itemPadding.y,
                size.x - itemPadding.width - itemPadding.x,
                size.y - itemPadding.height - itemPadding.y
                );

                if (item.type == DropdownItemType.Option_Bool)
                {
                    string icon = item.value ? "P4_CheckOutRemote" : "P4_DeletedRemote";
                    GUI.Label(itemsRect, new GUIContent(item.name, EditorGUIUtility.IconContent(icon).image), style);
                    //Add icon

                    if (isMouseDownEvent)
                        if (itemsRect.Contains(mouseposition))
                        {
                            item.value = !item.value;
                            trigger = true;
                        }
                }
                else if (item.type == DropdownItemType.Option_Func)
                {
                    GUI.Label(itemsRect, item.name, style);
                    if (isMouseDownEvent)
                        if (itemsRect.Contains(mouseposition))
                        {
                            item.function();
                            trigger = true;
                        }
                }
                else if (item.type == DropdownItemType.Option_Func_Bool)
                {
                    string icon = item.value ? "P4_CheckOutRemote" : "P4_DeletedLocal";
                    GUI.Label(itemsRect, new GUIContent(item.name, EditorGUIUtility.IconContent(icon).image), style);
                    if (isMouseDownEvent)
                        if (itemsRect.Contains(mouseposition))
                        {
                            item.function();
                            trigger = true;
                        }
                }
            }

            if (isMouseDownEvent && !trigger) 
            {
                isOpen = false;
            }
        }

        public void displayDropdown()
        {

            //set the Display Area        
            foreach (DropdownItem item in items)
            {
                if (item.type == DropdownItemType.Option_Bool)
                {
                    if (GUILayout.Button(item.name)) item.value = !item.value;
                }
                else if (item.type == DropdownItemType.Option_Func)
                {
                    if (GUILayout.Button(item.name))
                    {
                        item.function();
                    }
                }
            }

        }

        public class DropdownItem
        {
            public DropdownItemType type;
            public string name;
            public bool value;
            public Action function;

            public DropdownItem(string name, bool value)
            {

                this.name = name;
                this.value = value;
                this.type = DropdownItemType.Option_Bool;
            }

            public DropdownItem(string name, Action function)
            {
                this.name = name;
                this.function = function;
                this.type = DropdownItemType.Option_Func;
            }

            //funcbool
            public DropdownItem(string name, bool value, Action function)
            {
                this.name = name;
                this.function = function;
                this.value = value;
                this.type = DropdownItemType.Option_Func_Bool;
            }
        }
    }

    public class Splitviewer
    {

        public float splitPosition = 200f;
        public bool isResizing;


        public bool drawSplit(Event current, Rect position)
        {

            float posXX = 70 + 15;
            //Split Line to Grab on
            GUILayout.Space(9);
            EditorGUIUtility.AddCursorRect(new Rect(splitPosition, posXX, 6, position.height), MouseCursor.ResizeHorizontal);
            EditorGUI.DrawRect(new Rect(splitPosition, posXX, 6, position.height), GetRGBA(ColorRGBA.grayscale_064));
            GUILayout.Space(9);

            //Activates the Resizing
            if (current.type == EventType.MouseDown && new Rect(splitPosition, posXX, 6, position.height).Contains(current.mousePosition))
                isResizing = true;

            //Disabled Resizing
            if (isResizing)
            {
                splitPosition = Mathf.Clamp(current.mousePosition.x, 120, position.width - posXX);
                isResizing = checkExit(current, position);
            }
            return isResizing;
        }

        private bool checkExit(Event current, Rect position)
        {

            if (current.type == EventType.MouseUp) return false;

            if (current.mousePosition.x < 15 || current.mousePosition.x > position.width - 15 ||
            current.mousePosition.y < 15 || current.mousePosition.y > position.height - 15) return false;

            return true;
        }
        private static Color GetRGBA(ColorRGBA color)
        {
            return SmartColorUtility.GetRGBA(color);
        }

    }

}