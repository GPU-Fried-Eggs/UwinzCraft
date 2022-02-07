using System;
using System.Linq;
using Environment.Data;
using UnityEditor;
using UnityEngine;

namespace Environment
{
    [CustomEditor(typeof(SkyboxPrefab))]
    public class SkyboxPrefabViewer : Editor
    {
        private SkyboxPrefab m_Prefab;

        private Vector3 m_starFieldColor = Vector3.one; 
        private Vector3 m_starFieldPosition = Vector3.zero;

        private void OnEnable() => m_Prefab = target as SkyboxPrefab;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            
            // Time of day
            EditorGUILayout.LabelField("Time Of Day", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            EditorGUILayout.BeginVertical("Box");
            GUILayout.Space(-5);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space();
            GUILayout.Label("Day and Night Length", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Evaluate Time of Day by Curve?");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("setTimeByCurve"), GUIContent.none, GUILayout.Width(15));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.CurveField(serializedObject.FindProperty("dayLengthCurve"), Color.green, new Rect(0, 0, 24, 24), GUIContent.none, GUILayout.Height(30));
            
            EditorGUILayout.EndVertical();
            
            // Texture and materials 
            EditorGUILayout.LabelField("Resources", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sunTexture"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("moonTexture"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cloudTexture"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("starfieldTexture"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("skyMaterial"));
            
            // Scattering
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Scattering", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);
            
            EditorGUILayout.CurveField(serializedObject.FindProperty("rayleighCurve"), Color.green, new Rect(0.0f, 0.0f, 24.0f, 5.0f), new GUIContent("Rayleigh"));
            EditorGUILayout.CurveField(serializedObject.FindProperty("mieCurve"), Color.green, new Rect(0.0f, 0.0f, 24.0f, 30.0f), new GUIContent("Mie"));
            EditorGUILayout.CurveField(serializedObject.FindProperty("krCurve"), Color.green, new Rect(0.0f, 1.0f, 24.0f, 29.0f), new GUIContent("Kr"));
            EditorGUILayout.CurveField(serializedObject.FindProperty("kmCurve"), Color.green, new Rect(0.0f, 1.0f, 24.0f, 29.0f), new GUIContent("Km"));
            EditorGUILayout.CurveField(serializedObject.FindProperty("scatteringCurve"), Color.green, new Rect(0.0f, 0.0f, 24.0f, 75.0f), new GUIContent("Scattering"));
            EditorGUILayout.CurveField(serializedObject.FindProperty("sunIntensityCurve"), Color.green, new Rect(0.0f, 0.0f, 24.0f, 5.0f), new GUIContent("Sun Intensity"));
            EditorGUILayout.CurveField(serializedObject.FindProperty("nightIntensityCurve"), Color.green, new Rect(0.0f, 0.0f, 24.0f, 5.0f), new GUIContent("Night Intensity"));
            EditorGUILayout.CurveField(serializedObject.FindProperty("exposureCurve"), Color.green, new Rect(0.0f, 0.0f, 24.0f, 8.0f), new GUIContent("Exposure"));
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("rayleighGradientColor"), new GUIContent("Rayleigh Color"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("mieGradientColor"), new GUIContent("Mie Color"));
            
            // Night Sky
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Night Sky", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("moonDiskGradientColor"), new GUIContent("Moon Disk Color"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("moonBrightGradientColor"), new GUIContent("Moon Bright Color"));
            
            EditorGUILayout.CurveField(serializedObject.FindProperty("moonBrightRangeCurve"), Color.green, new Rect(0.0f, 0.0f, 24.0f, 1.0f), new GUIContent("Moon Bright Range"));
            EditorGUILayout.CurveField(serializedObject.FindProperty("starfieldIntensityCurve"), Color.green, new Rect(0.0f, 0.0f, 24.0f, 10.0f), new GUIContent("Starfield Intensity"));
            EditorGUILayout.CurveField(serializedObject.FindProperty("milkyWayIntensityCurve"), Color.green, new Rect(0.0f, 0.0f, 24.0f, 1.0f), new GUIContent("Milky Way Intensity"));

            m_starFieldColor.x = EditorGUILayout.Slider("Starfield Color R", m_Prefab.starfieldColorBalance.x, 1.0f, 2.0f);
            m_starFieldColor.y = EditorGUILayout.Slider("Starfield Color G", m_Prefab.starfieldColorBalance.y, 1.0f, 2.0f);
            m_starFieldColor.z = EditorGUILayout.Slider("Starfield Color B", m_Prefab.starfieldColorBalance.z, 1.0f, 2.0f);

            m_starFieldPosition.x = EditorGUILayout.Slider("Starfield Position X", m_Prefab.starfieldPosition.x, 0.0f, 360.0f);
            m_starFieldPosition.y = EditorGUILayout.Slider("Starfield Position Y", m_Prefab.starfieldPosition.y, 0.0f, 360.0f);
            m_starFieldPosition.z = EditorGUILayout.Slider("Starfield Position Z", m_Prefab.starfieldPosition.z, 0.0f, 360.0f);
            
            // Clouds
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Clouds", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cloudGradientColor"), new GUIContent("Cloud Color"));
            EditorGUILayout.CurveField(serializedObject.FindProperty("cloudScatteringCurve"), Color.green, new Rect(0.0f, 0.0f, 24.0f, 1.5f), new GUIContent("Cloud Scattering"));
            EditorGUILayout.CurveField(serializedObject.FindProperty("cloudExtinctionCurve"), Color.green, new Rect(0.0f, 0.0f, 24.0f, 1.0f), new GUIContent("Cloud Extinction"));
            EditorGUILayout.CurveField(serializedObject.FindProperty("cloudPowerCurve"), Color.green, new Rect(0.0f, 1.8f, 24.0f, 2.4f), new GUIContent("Cloud Power"));
            EditorGUILayout.CurveField(serializedObject.FindProperty("cloudIntensityCurve"), Color.green, new Rect(0.0f, 0.0f, 24.0f, 2.0f), new GUIContent("Cloud Intensity"));
            
            EditorGUILayout.Slider(serializedObject.FindProperty("cloudRotationSpeed"), -0.01f, 0.01f, "Rotation Speed");
            
            // Fog scattering
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Fog scattering", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);
            
            EditorGUILayout.CurveField(serializedObject.FindProperty("fogDistanceCurve"), Color.green, new Rect(0.0f, 0.0f, 24.0f, 20000.0f), new GUIContent("Distance"));
            EditorGUILayout.Slider(serializedObject.FindProperty("fogBlend"), 0.0f, 1.0f, "Blend");
            EditorGUILayout.Slider(serializedObject.FindProperty("mieDistance"), 0.0f, 1.0f, "Mie Distance");
            
            // Lighting
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Lighting", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);
            
            EditorGUILayout.CurveField(serializedObject.FindProperty("lightIntensityCurve"), Color.green, new Rect(0.0f, 0.0f, 24.0f, 8.0f), new GUIContent("Directional Light Intensity"));
            EditorGUILayout.CurveField(serializedObject.FindProperty("flareIntensityCurve"), Color.green, new Rect(0.0f, 0.0f, 24.0f, 8.0f), new GUIContent("Directional Light Flare Intensity"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("lightGradientColor"), new GUIContent("Directional Light Color"));
            EditorGUILayout.CurveField(serializedObject.FindProperty("ambientIntensityCurve"), Color.green, new Rect(0.0f, 0.0f, 24.0f, 8.0f), new GUIContent("Environment Intensity"));
            
            //EditorGUILayout.PropertyField(serializedObject.FindProperty("ambientSkyGradientColor"), new GUIContent("Environment Ambient Color"));
            //EditorGUILayout.PropertyField(serializedObject.FindProperty("equatorSkyGradientColor"), new GUIContent("Environment Equator Color"));
            //EditorGUILayout.PropertyField(serializedObject.FindProperty("groundSkyGradientColor"), new GUIContent("Environment Ground Color"));
            
            // Options
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Options", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);
            
            EditorGUILayout.Slider(serializedObject.FindProperty("sunDiskSize"), 0.0f, 1.0f, "Sun Disk Size");
            EditorGUILayout.Slider(serializedObject.FindProperty("moonDiskSize"), 0.0f, 1.0f, "Moon Disk Size");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cloudMode"), new GUIContent("Cloud Mode"));

            // Events
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Events", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("onSunRise"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("onSunSet"));

            // End custom Inspector
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(m_Prefab, "Undo Skybox Setup");
                serializedObject.ApplyModifiedProperties();
                m_Prefab.starfieldColorBalance = m_starFieldColor;
                m_Prefab.starfieldPosition = m_starFieldPosition;
            }
            
        }
    }
}