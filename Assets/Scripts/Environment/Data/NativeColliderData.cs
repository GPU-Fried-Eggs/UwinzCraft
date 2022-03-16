using System.Collections;
using System.Collections.Generic;
using Environment.System;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Utilities;
// ReSharper disable InconsistentNaming

namespace Environment.Data
{
    public class NativeColliderData : MonoSingleton<NativeColliderData>
    {
        private struct MeshNode { public Chunk chunk; public Mesh mesh; }
        
        private List<MeshNode> m_Meshes = new List<MeshNode>();
        
        private NativeArray<int> m_MeshIds;
        private JobHandle m_JobHandle;
        
        public void Enqueue(Chunk chunk, Mesh mesh) => m_Meshes.Add(new MeshNode { chunk = chunk, mesh = mesh });

        private void Start() => StartCoroutine(BakeUpdater());

        private void OnDestroy()
        {
            m_JobHandle.Complete();
            if (m_MeshIds.IsCreated) m_MeshIds.Dispose();
        }
        
        private IEnumerator BakeUpdater()
        {
            int counter = 0;
            while (true)
            {
                if (m_Meshes.Count == 0) { counter = 0; yield return null; continue; }
                if (counter < 4 && m_Meshes.Count < 5) { counter++; yield return null; continue; } // 批处理

                m_MeshIds = new NativeArray<int>(m_Meshes.Count, Allocator.TempJob);

                for (int i = 0, iMax = m_Meshes.Count; i < iMax; ++i) 
                    m_MeshIds[i] = m_Meshes[i].mesh.GetInstanceID();
                
                m_JobHandle = new BuildColliderSystem { meshIds = m_MeshIds }.Schedule(m_MeshIds.Length, 32);
                JobHandle.ScheduleBatchedJobs();
                
                int frameCount = 1;
                yield return new WaitUntil(() =>
                {
                    frameCount++;
                    return m_JobHandle.IsCompleted || frameCount >= 4;
                });
                
                m_JobHandle.Complete();
                m_MeshIds.Dispose();

                for (int i = 0, iMax = m_Meshes.Count; i < iMax; i++)
                    m_Meshes[i].chunk.SetSharedMesh(m_Meshes[i].mesh);

                m_Meshes.Clear();
                counter = 0;
                yield return null;
            }
        }
    }
}