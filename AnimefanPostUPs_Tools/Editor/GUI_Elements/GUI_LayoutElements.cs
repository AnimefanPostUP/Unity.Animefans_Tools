
namespace AnimefanPostUPs_Tools.GUI_LayoutElements
{

    using UnityEngine;
    using UnityEditor;
    using AnimefanPostUPs_Tools.SmartColorUtility;
    using static AnimefanPostUPs_Tools.SmartColorUtility.ColorRGBA;


    //Class for drawing Split GUI

    class GUI_LayoutElements
    {
        class Splitviewer
        {

            public float splitPosition = 200f;
            public bool isResizing;


            public bool drawSplit(Event current, Rect position)
            {

                float posXX = 70 + 15;
                //Split Field
                GUILayout.Space(9);
                EditorGUIUtility.AddCursorRect(new Rect(splitPosition, posXX, 6, position.height), MouseCursor.ResizeHorizontal);
                EditorGUI.DrawRect(new Rect(splitPosition, posXX, 6, position.height), GetRGBA(ColorRGBA.grayscale_064));
                GUILayout.Space(9);

                //Entry
                if (current.type == EventType.MouseDown && new Rect(splitPosition, posXX, 6, position.height).Contains(current.mousePosition))
                    isResizing = true;

                //Loop if Active
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

        }
        private static Color GetRGBA(ColorRGBA color)
        {
            return SmartColorUtility.GetRGBA(color);
        }

    }

}