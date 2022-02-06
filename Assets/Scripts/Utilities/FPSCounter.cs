using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.UI;

namespace Utilities
{
    public class FPSCounter : MonoBehaviour
    {
        private const float MemoryDivider = 1048576;
        private Canvas m_Canvas;
        private Text m_Text;

        private ProfilerRecorder m_MainThreadTimeRecorder;
        private ProfilerRecorder m_SystemMemoryRecorder, m_GCMemoryRecorder;
        private ProfilerRecorder m_DrawCallsRecorder, m_VerticesRecorder, m_TrianglesRecorder;
        
        private static float Fps => 1 / Time.unscaledDeltaTime;
        private static double GetRecorderFrameAverage(ProfilerRecorder recorder)
        {
            var samplesCount = recorder.Capacity;
            if (samplesCount == 0)
                return 0;

            double r = 0;
            unsafe
            {
                var samples = stackalloc ProfilerRecorderSample[samplesCount];
                recorder.CopyTo(samples, samplesCount);
                for (var i = 0; i < samplesCount; ++i)
                    r += samples[i].Value;
                r /= samplesCount;
            }

            return r;
        }
        
        private void OnEnable()
        {
            m_MainThreadTimeRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 15);
            m_SystemMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "System Used Memory");
            m_GCMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Reserved Memory");
            m_DrawCallsRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            m_VerticesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Vertices Count");
            m_TrianglesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
        }

        private void OnDisable()
        {
            m_MainThreadTimeRecorder.Dispose();
            m_SystemMemoryRecorder.Dispose();
            m_GCMemoryRecorder.Dispose();
            m_DrawCallsRecorder.Dispose();
            m_VerticesRecorder.Dispose();
            m_TrianglesRecorder.Dispose();
        }
        
        private void Awake()
        {
            #region GUIBuilder

            m_Canvas = gameObject.AddComponent<Canvas>();
            m_Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            m_Text = Instantiate(new GameObject("Text Field") { hideFlags = HideFlags.HideInHierarchy }, m_Canvas.transform, true).AddComponent<Text>();
            m_Text.rectTransform.localPosition = new Vector3(10, -10);
            m_Text.rectTransform.sizeDelta = new Vector2(Screen.width / 4f, Screen.height / 3f);
            m_Text.rectTransform.anchorMax = Vector2.up;
            m_Text.rectTransform.anchorMin = Vector2.up;
            m_Text.rectTransform.pivot = Vector2.up;
            m_Text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            m_Text.lineSpacing = 1.2f;
            m_Text.fontSize = math.clamp(Screen.height / 35, 14, 30);
            m_Text.horizontalOverflow = HorizontalWrapMode.Overflow;

            #endregion
            
            InvokeRepeating(nameof(SysInfoUpdater), 0, 0.6f);
        }

        private void SysInfoUpdater()
        {
            m_Text.text = $"Fps: {Fps:F1}  [{GetRecorderFrameAverage(m_MainThreadTimeRecorder) * (1e-6f):F1} ms]\n" +
                          $"Sys Memory: {m_SystemMemoryRecorder.LastValue / MemoryDivider:F0}MB\n" +
                          $"GC Memory: {m_GCMemoryRecorder.LastValue / MemoryDivider:F0}MB\n" +
                          $"Draw Calls: {m_DrawCallsRecorder.LastValue}\n" +
                          $"Verts: {m_VerticesRecorder.LastValue * (1e-3f):F1}k  Tris: {m_TrianglesRecorder.LastValue * (1e-3f):F1}k";
        }
    }
}
