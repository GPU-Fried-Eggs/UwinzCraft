using System;
using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using Utilities;

public class VoronoiTextureViewer : EditorWindow
{
    private Texture2D m_Texture;

    private NativeArray<byte> m_Buffer;

    private int2 m_Resolution;

    private float m_Record;
    
    [MenuItem("Window/Voronoi Viewer")]
    private static void ShowWindow() => GetWindow(typeof(VoronoiTextureViewer));
    
    private void OnGUI()
    {
        if (m_Resolution.Equals(null) || m_Resolution.x != (int)position.width || m_Resolution.y != (int) position.height)
        {
            m_Resolution = new int2((int) position.width, (int) position.height);
            m_Buffer = new NativeArray<byte>((int)position.width * (int)position.height * 3, Allocator.Persistent);
            m_Record = 0f;
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            Paint((int)position.width, (int)position.height);
            stopWatch.Stop();
            m_Record = stopWatch.ElapsedMilliseconds;
        }
        EditorGUI.DrawPreviewTexture(GUILayoutUtility.GetRect(position.width, position.height), m_Texture);
        EditorGUI.LabelField(new Rect(5, 5, 100, 20),  m_Record + " ms", new GUIStyle { normal = { textColor = Color.green }});
    }

    private void OnDisable()
    {
        if (m_Buffer.IsCreated) m_Buffer.Dispose();
    }

    private void Paint(int width, int height)
    {
        m_Texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        var jobHandle = new GenerateNoiseTexture
        {
            resolution = new int2(width, height),
            buffer = m_Buffer,
        }.Schedule(width * height, 64);
        jobHandle.Complete();
        m_Texture.SetPixelData(m_Buffer, 0);
        m_Texture.Apply();
    }
    
    [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
    private struct GenerateNoiseTexture : IJobParallelFor
    {
        [ReadOnly] public int2 resolution;
        
        [NativeDisableParallelForRestriction] public NativeArray<byte> buffer;

        public void Execute(int index)
        {
            var position = index.To2DIndex(resolution.x);
            var output = math.floor(Noise.Voronoi(position, 0.01f));
            buffer[index * 3] = (byte) output.y;
            buffer[index * 3 + 1] = (byte) output.z;
            buffer[index * 3 + 2] = (byte) output.w;
        }
    }
}