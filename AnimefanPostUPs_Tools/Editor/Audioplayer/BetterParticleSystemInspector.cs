/*
using System.Threading;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
//Callback
using System;
using System.Collections.Generic;

//Import Odin
using Sirenix.OdinInspector.Editor;

[CustomEditor(typeof(ParticleSystem))]
public class BetterParticleSystemInspector : Editor
{

    bool showOriginalInspector = false;
    public override void OnInspectorGUI()
    {

        //Display the original inspector
        showOriginalInspector = EditorGUILayout.Toggle("Show Original Inspector", showOriginalInspector);
  if (showOriginalInspector)
{
    Editor defaultEditor = CreateEditor(target, Type.GetType("UnityEditor.ParticleSystemInspector, UnityEditor"));
    defaultEditor.OnInspectorGUI();
    return;
}
        //base.OnInspectorGUI();

        ParticleSystem particleSystem = (ParticleSystem)target;

        // Display Looping, Prewarm and Duration using DrawXnXField
        bool looping = particleSystem.main.loop;
        bool prewarm = particleSystem.main.prewarm;
        bool playOnAwake = particleSystem.main.playOnAwake;
        float duration = particleSystem.main.duration;
        float startLifetime = particleSystem.main.startLifetime.constant;


        //Vertical Layout

        float currentWindowWidth = EditorGUIUtility.currentViewWidth;

        //SLtyle for Toggle
        GUIStyle style_toggle = new GUIStyle(GUI.skin.button);
        style_toggle.fixedWidth = 120;
        style_toggle.fixedHeight = 20;
        style_toggle.margin = new RectOffset(0, 0, 0, 0);
        style_toggle.alignment = TextAnchor.MiddleCenter;

        //disabled
        GUIStyle disabledstyle = new GUIStyle(GUI.skin.button);
        disabledstyle.normal.background = MakeTex(2, 2, new Color(0.2f, 0.1f, 0.1f, 1f)); // Change the background color to a semi-transparent red
        disabledstyle.hover.background = MakeTex(2, 2, new Color(0.2f, 0.1f, 0.1f, 1f)); // Change the hover background color to the same color
        disabledstyle.active.background = MakeTex(2, 2, new Color(0.2f, 0.1f, 0.1f, 1f)); // Change the active background color to the same color
        disabledstyle.fixedWidth = 120;
        disabledstyle.fixedHeight = 20;
        disabledstyle.margin = new RectOffset(0, 0, 0, 0);
        disabledstyle.alignment = TextAnchor.MiddleCenter;
        //make text gray
        disabledstyle.normal.textColor = Color.gray;

        // Style for enabled state
        GUIStyle enabledstyle = new GUIStyle(GUI.skin.button);
        enabledstyle.normal.background = MakeTex(2, 2, new Color(0.4f, 0.5f, 0.4f, 1f)); // Change the background color to a semi-transparent green
        enabledstyle.hover.background = MakeTex(2, 2, new Color(0.4f, 0.5f, 0.4f, 1f)); // Change the hover background color to the same color
        enabledstyle.active.background = MakeTex(2, 2, new Color(0.4f, 0.5f, 0.4f, 1f)); // Change the active background color to the same color
        enabledstyle.fixedWidth = 120;
        enabledstyle.fixedHeight = 20;
        enabledstyle.margin = new RectOffset(0, 0, 0, 0);
        enabledstyle.alignment = TextAnchor.MiddleCenter;
        //Border
        enabledstyle.border = new RectOffset(4, 4, 4, 4);
        //make font bold
        enabledstyle.fontStyle = FontStyle.Bold;

        //Create fields without the RowLayout
        EditorGUILayout.BeginVertical();
        //horizontal Layout
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Loop", looping ? enabledstyle : disabledstyle)) looping = !looping;
        if (GUILayout.Button("Prewarm", prewarm ? enabledstyle : disabledstyle)) prewarm = !prewarm;
        if (GUILayout.Button("PlayOnAwake", playOnAwake ? enabledstyle : disabledstyle)) playOnAwake = !playOnAwake;
        EditorGUILayout.EndHorizontal();



        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Duration", GUILayout.Width(60));

        duration = EditorGUILayout.FloatField("", duration, GUILayout.Width(60));
        if (GUILayout.Button("-", GUILayout.Width(20)))
        {
            duration -= 1;
        }
        if (GUILayout.Button("+", GUILayout.Width(20)))
        {
            duration += 1;
        }
        EditorGUILayout.EndHorizontal();





        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Lifetime", GUILayout.Width(60));

        startLifetime = EditorGUILayout.FloatField("", startLifetime, GUILayout.Width(60));
        if (GUILayout.Button("-", GUILayout.Width(20)))
        {
            startLifetime = Mathf.Max(0, startLifetime - 1);
        }
        if (GUILayout.Button("+", GUILayout.Width(20)))
        {
            startLifetime += 1;
        }
        startLifetime = startLifetime;

        EditorGUILayout.EndHorizontal();











        //Flexible Space
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndVertical();


        // Get a reference to the ParticleSystem component
        ParticleSystem particleSystems = (ParticleSystem)target;

        // Apply any changes made in the inspector
        if (GUI.changed)
        {
            // Get a local copy of the MainModule
            ParticleSystem.MainModule main = particleSystems.main;

            // Set the values
            main.loop = looping;
            main.prewarm = prewarm;
            main.playOnAwake = playOnAwake;

            main.duration = duration;
            main.startLifetime = startLifetime;


            // Undo
            Undo.RecordObject(particleSystems, "Changed Particle System");
            EditorUtility.SetDirty(particleSystems);
        }
    }
    Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
            pix[i] = col;
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }

    //Class for anonyme GUI Layouts
    class RowLayout
    {

        public GUIStyle text_style = new GUIStyle();
        public GUIStyle button_style = new GUIStyle();
        public GUIStyle background_style = new GUIStyle();
        public GUIStyle slider_style = new GUIStyle();

        public GUIStyle field_style = new GUIStyle();


        public void InitializeStyles()
        {
            text_style = new GUIStyle(GUI.skin.label);
            text_style.alignment = TextAnchor.MiddleLeft;

            button_style = new GUIStyle(GUI.skin.button);
            button_style.alignment = TextAnchor.MiddleCenter;

            background_style = new GUIStyle(GUI.skin.box);
            background_style.alignment = TextAnchor.MiddleCenter;

            slider_style = new GUIStyle(GUI.skin.horizontalSlider);
            slider_style.alignment = TextAnchor.MiddleCenter;

            field_style = new GUIStyle(GUI.skin.textField);
            field_style.alignment = TextAnchor.MiddleCenter;
        }


        public float width;
        //row Elements
        public List<RowElement> elements = new List<RowElement>();

        public RowLayout(float width)
        {
            this.width = width;
        }

        public void DrawRow()
        {
            InitializeStyles();
            EditorGUILayout.BeginHorizontal(GUILayout.Width(width));

            //set the style widths
            text_style.fixedWidth = width / elements.Count;
            button_style.fixedWidth = width / elements.Count;
            background_style.fixedWidth = width / elements.Count;
            slider_style.fixedWidth = width / elements.Count;
            field_style.fixedWidth = width / elements.Count;


            foreach (RowElement element in elements)
            {
                //set all styles:
                element.text_style = text_style;
                element.button_style = button_style;
                element.background_style = background_style;
                element.slider_style = slider_style;
                element.field_style = field_style;




                element.width = width / elements.Count;
                element.Draw();
            }

            EditorGUILayout.EndHorizontal();
        }

        //Method to add a new Element to store a Row

    }

    //Class of a Element in a Row
    class RowElement
    {


        //Enums of Types of Elements
        public enum ElementTypes
        {
            typeBool,
            typeFloat,
            typeInt,
            typeVector3
        }

        public enum SubtypeBool
        {
            Button,
            Checkbox,
            Slider
        }

        //Subtype Float
        public enum SubtypeFloat
        {
            Slider,
            Field
        }

        //Subtype Int
        public enum SubtypeInt
        {
            Slider,
            Field
        }

        //Subtype Vector3
        public enum SubtypeVector3
        {
            Field
        }



        public ElementTypes type;
        public SubtypeBool subtypeBool;
        public SubtypeFloat subtypeFloat;
        public SubtypeInt subtypeInt;
        public SubtypeVector3 subtypeVector3;



        public string label;
        public float width;
        public bool boolvalue;
        public float floatvalue;
        public int intvalue;
        public Vector3 vector3value;
        public object value;

        public GUIStyle text_style;
        public GUIStyle button_style;
        public GUIStyle background_style;
        public GUIStyle slider_style;

        public GUIStyle field_style;

        public RowElement(SubtypeBool type, string label, ref bool boolvalue)
        {
            this.type = ElementTypes.typeBool;
            this.subtypeBool = type;
            this.label = label;
            this.boolvalue = boolvalue;
        }

        public RowElement(SubtypeFloat type, string label, ref float floatvalue)
        {

            this.type = ElementTypes.typeFloat;
            this.subtypeFloat = type;
            this.label = label;
            this.value = floatvalue;
        }

        public RowElement(SubtypeInt type, string label, ref int intvalue)
        {

            this.type = ElementTypes.typeInt;
            this.subtypeInt = type;
            this.label = label;
            this.intvalue = intvalue;
        }

        public RowElement(SubtypeVector3 type, string label, ref Vector3 vector3value)
        {

            this.type = ElementTypes.typeVector3;
            this.subtypeVector3 = type;
            this.label = label;
            this.vector3value = vector3value;
        }





        public void Draw()
        {
            switch (type)
            {
                case ElementTypes.typeBool:
                    DrawBoolToggle(label, ref boolvalue, width);
                    break;
                case ElementTypes.typeFloat:
                    DrawFloatField(label, ref floatvalue, width);
                    break;
                case ElementTypes.typeInt:
                    DrawIntField(label, ref intvalue, width);
                    break;
                case ElementTypes.typeVector3:
                    DrawVector3Field(label, ref vector3value, width);
                    break;
            }
        }

        private void DrawBoolToggle(string label, ref bool value, float width)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(width));
            value = EditorGUILayout.Toggle(label, value, button_style);
            EditorGUILayout.EndVertical();
        }

        private void DrawFloatField(string label, ref float value, float width)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(width));
            EditorGUILayout.LabelField(label, text_style, GUILayout.Width(width / 2));
            value = EditorGUILayout.FloatField(value, field_style, GUILayout.Width(width / 2));
            EditorGUILayout.EndVertical();
        }

        private void DrawIntField(string label, ref int value, float width)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(width));
            EditorGUILayout.LabelField(label, text_style, GUILayout.Width(width / 2));
            value = EditorGUILayout.IntField(value, field_style, GUILayout.Width(width / 2));
            EditorGUILayout.EndVertical();
        }

        private void DrawVector3Field(string label, ref Vector3 value, float width)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(width));
            EditorGUILayout.LabelField(label, text_style, GUILayout.Width(width / 2));
            value = EditorGUILayout.Vector3Field(label, value, GUILayout.Width(width / 2));
            EditorGUILayout.EndVertical();
        }
    }
}
*/