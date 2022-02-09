using System;
using Environment.Data;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
// ReSharper disable InconsistentNaming

namespace Environment
{
    [CustomEditor(typeof(BlockPrefab))]
    public class BlockPrefabViewer : Editor
    {
        private BlockPrefab m_Prefab;

        private ReorderableList m_AtlasUVArray;

        private Material m_PreviewMaterial;
        private Mesh m_CubeMesh;

        private readonly BlockType[] m_Caches = new BlockType[2];
        private readonly string[] m_Direction = { "Right", "Left", "Up", "Down", "Front", "Back" };
        
        private void OnEnable()
        {
            m_Prefab = target as BlockPrefab;

            m_Caches[1] = m_Prefab!.block;

            m_PreviewMaterial = new Material(Shader.Find("Hidden/BlockPrefabPreview"));
            m_CubeMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");

            m_AtlasUVArray = new ReorderableList(serializedObject, serializedObject.FindProperty("atlasPositions"),
                false, true, false, false)
            {
                drawHeaderCallback = rect => GUI.Label(rect, "Atlas Positions"), 
                elementHeight = EditorGUIUtility.singleLineHeight * 1.2f,
                drawElementCallback = (rect, index, active, focused) =>
                {
                    var item = m_AtlasUVArray.serializedProperty.GetArrayElementAtIndex(index);
                    rect.y += 4; // keep a little bit lower
                    EditorGUI.PropertyField(rect, item, new GUIContent(m_Direction[index]));
                },
                elementHeightCallback = index =>
                {
                    var item = m_AtlasUVArray.serializedProperty.GetArrayElementAtIndex(index);
                    return EditorGUI.GetPropertyHeight(item, true) * 1.2f;
                }
            };
        }

        private void OnDisable()
        {
            AssetDatabase.SaveAssets();
            DestroyPreview();
        }

        #region BlockSettings

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            m_Caches[0] = (BlockType) EditorGUILayout.EnumPopup("Block Type", m_Prefab.block);
            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("shape"), new GUIContent("Block Shape"));
            EditorGUILayout.Space(10);
            m_AtlasUVArray.DoLayoutList();
            if (m_Caches[0] != m_Prefab.block)
            {
                var fallback = string.IsNullOrWhiteSpace(AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(m_Prefab.GetInstanceID()), m_Caches[0].ToString()));
                m_Prefab.block = fallback ? m_Caches[0] : m_Caches[1];
                m_Prefab.name = (fallback ? m_Caches[0] : m_Caches[1]).ToString();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                m_Caches[1] = m_Prefab.block;
            }
            serializedObject.ApplyModifiedProperties();
        }

        #endregion

        #region BlockPreview

        private PreviewRenderUtility m_PreviewRenderUtility;
        
        private Vector2 m_Drag;
        
        private readonly Vector4[] m_Uvs = new Vector4[3];
        private static readonly int Uvs = Shader.PropertyToID("uvs");
        private static readonly int Scale = Shader.PropertyToID("scale");
        
        public override bool HasPreviewGUI()
        {
            if (m_Prefab == null) throw new Exception("No prefab Data");

            for (int i = 0; i < 3; i++) 
                m_Uvs[i] = new Vector4(m_Prefab.atlasPositions[i * 2].x, m_Prefab.atlasPositions[i * 2].y, m_Prefab.atlasPositions[i * 2 + 1].x, m_Prefab.atlasPositions[i * 2 + 1].y);

            DestroyPreview();
            
            m_PreviewRenderUtility = new PreviewRenderUtility();
            GC.SuppressFinalize(m_PreviewRenderUtility);
            m_PreviewRenderUtility.camera.fieldOfView = 30f;

            return true;
        }

        public override void OnPreviewGUI(Rect rect, GUIStyle background)
        {
            m_Drag = Drag2D(m_Drag, rect);

            if (Event.current.type != EventType.Repaint) return;
            
            if (m_PreviewRenderUtility == null)
            {
                EditorGUI.DropShadowLabel(rect, "Error");
            }
            else
            {
                m_PreviewMaterial.SetVectorArray(Uvs, m_Uvs);
                m_PreviewMaterial.SetFloat(Scale, 1f / Shared.AtlasSize.x);
                
                m_PreviewRenderUtility.BeginPreview(rect, background);
                
                m_PreviewRenderUtility.DrawMesh(m_CubeMesh, Matrix4x4.identity, m_PreviewMaterial, 0);

                m_PreviewRenderUtility.camera.transform.position = Vector2.zero;
                m_PreviewRenderUtility.camera.transform.rotation = Quaternion.Euler(new Vector3(-m_Drag.y, -m_Drag.x, 0));
                m_PreviewRenderUtility.camera.transform.position = m_PreviewRenderUtility.camera.transform.forward * -6f;
                
                m_PreviewRenderUtility.Render(true);
                
                m_PreviewRenderUtility.EndAndDrawPreview(rect);
            }
        }

        private void DestroyPreview() => m_PreviewRenderUtility?.Cleanup();

        private static Vector2 Drag2D(Vector2 scrollPos, Rect position)
        {
            var controlID = GUIUtility.GetControlID("Slider".GetHashCode(), FocusType.Passive);
            var current = Event.current;
            switch (current.GetTypeForControl(controlID)) {
                case EventType.MouseDown:
                    if (position.Contains(current.mousePosition) && position.width > 50f)
                    {
                        GUIUtility.hotControl = controlID;
                        current.Use();
                        EditorGUIUtility.SetWantsMouseJumping(1);
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlID)
                    {
                        GUIUtility.hotControl = 0;
                    }
                    EditorGUIUtility.SetWantsMouseJumping(0);
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlID)
                    {
                        scrollPos -= current.delta * (!current.shift ? 1 : 3) / Mathf.Min(position.width, position.height) * 140f;
                        scrollPos.y = Mathf.Clamp(scrollPos.y, -90f, 90f);
                        current.Use();
                        GUI.changed = true;
                    }
                    break;
            }
            return scrollPos;
        }
        
        #endregion
    }
}