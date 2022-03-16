using System.Collections;
using Environment.System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
// ReSharper disable InconsistentNaming

namespace Environment.Data
{
    public class NativeMapData
    {
        private NativeArray<Block> m_NativeBlocks;
        public JobHandle jobHandle;

        public NativeMapData(int3 chunkSize) => m_NativeBlocks = new NativeArray<Block>(chunkSize.x * chunkSize.y * chunkSize.z, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        ~NativeMapData()
        {
            jobHandle.Complete(); Dispose();
        }

        public void Dispose()
        {
            if (m_NativeBlocks.IsCreated) m_NativeBlocks.Dispose();
        }

        public IEnumerator Generate(Block[] blocks, int3 chunkPosition, int3 chunkSize)
        {
            var maxHeight = 0;
            
            var baseJobHandle = new BuildBaseSystem
            {
                chunkPosition = chunkPosition,
                chunkSize = chunkSize,
                maxHeight = maxHeight,
                blocks = m_NativeBlocks,
            }.Schedule(m_NativeBlocks.Length, 32);

            var mineJobHandle = new BuildMineSystem
            {
                chunkPosition = chunkPosition,
                chunkSize = chunkSize,
                blocks = m_NativeBlocks,
            }.Schedule(m_NativeBlocks.Length, 32, baseJobHandle);

            jobHandle = new BuildFoliageSystem
            {
                chunkPosition = chunkPosition,
                chunkSize = chunkSize,
                blocks = m_NativeBlocks,
            }.Schedule(m_NativeBlocks.Length, 32, mineJobHandle);
            
            //jobHandle = new BuildBiomesSystem
            //{
            //    chunkPosition = chunkPosition,
            //    chunkSize = chunkSize,
            //    blocks = m_NativeBlocks,
            //}.Schedule(m_NativeBlocks.Length, 32);
            
            int frameCount = 1;
            yield return new WaitUntil(() =>
            {
                frameCount++;
                return jobHandle.IsCompleted || frameCount >= 4;
            });
            
            jobHandle.Complete();
            m_NativeBlocks.CopyTo(blocks);
            m_NativeBlocks.Dispose();
        }
    }
}