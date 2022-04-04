using System;
using System.Collections;
using System.Collections.Generic;
using Environment.System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Utilities;
// ReSharper disable InconsistentNaming

namespace Environment.Data
{
    public class NativeMeshData
    {
        private NativeArray<Block> m_NativeBlocks;
        private NativeCounter m_NativeCounter;
        
        public NativeArray<float3> nativeVertices;
        public NativeArray<float3> nativeNormals;
        public NativeArray<float4> nativeUVs;
        public NativeArray<Color> nativeColors;
        public NativeList<int> nativeBlockIndices;
        public NativeList<int> nativeLiquidIndices;
        public NativeList<int> nativeFoliageIndices;
        public NativeList<int> nativeTransparentIndices;
        public JobHandle jobHandle;

        public NativeMeshData(int3 chunkSize)
        {
            var numBlocks = chunkSize.x * chunkSize.y * chunkSize.z;
            var numLayer = chunkSize.x * chunkSize.z;

            m_NativeBlocks = new NativeArray<Block>(numBlocks, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            nativeVertices = new NativeArray<float3>(12 * numBlocks, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            nativeNormals = new NativeArray<float3>(12 * numBlocks, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            nativeUVs = new NativeArray<float4>(12 * numBlocks, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            nativeColors = new NativeArray<Color>(12 * numBlocks, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            nativeBlockIndices = new NativeList<int>(18 * numBlocks, Allocator.TempJob);
            nativeLiquidIndices = new NativeList<int>(numLayer, Allocator.TempJob);
            nativeFoliageIndices = new NativeList<int>(numLayer, Allocator.TempJob);
            nativeTransparentIndices = new NativeList<int>(numLayer, Allocator.TempJob);
            m_NativeCounter = new NativeCounter(Allocator.TempJob);
        }

        ~NativeMeshData()
        {
            jobHandle.Complete(); Dispose();
        }

        public void Dispose()
        {
            if (m_NativeBlocks.IsCreated) m_NativeBlocks.Dispose();
            if (nativeVertices.IsCreated) nativeVertices.Dispose();
            if (nativeNormals.IsCreated) nativeNormals.Dispose();
            if (nativeUVs.IsCreated) nativeUVs.Dispose();
            if (nativeColors.IsCreated) nativeColors.Dispose();
            if (nativeBlockIndices.IsCreated) nativeBlockIndices.Dispose();
            if (nativeLiquidIndices.IsCreated) nativeLiquidIndices.Dispose();
            if (nativeFoliageIndices.IsCreated) nativeFoliageIndices.Dispose();
            if (nativeTransparentIndices.IsCreated) nativeTransparentIndices.Dispose();
            if (m_NativeCounter.IsCreated) m_NativeCounter.Dispose();
        }

        public IEnumerator Generate(Block[] blocks, int3 chunkSize, bool argent = false)
        {
            m_NativeBlocks.CopyFrom(blocks);
            
            jobHandle = new BuildMeshSystem
            {
                blocks = m_NativeBlocks,
                chunkSize = chunkSize,
                vertices = nativeVertices,
                normals = nativeNormals,
                uvs = nativeUVs,
                colors = nativeColors,
                blockIndices = nativeBlockIndices,
                liquidIndices = nativeLiquidIndices,
                foliageIndices = nativeFoliageIndices,
                transparentIndices = nativeTransparentIndices,
                counter = m_NativeCounter
            }.Schedule();

            int frameCount = 1;
            yield return new WaitUntil(() =>
            {
                frameCount++;
                return jobHandle.IsCompleted || frameCount >= 4 || argent;
            });

            jobHandle.Complete();
        }

        public void GetMeshInformation(out int verticesSize) => verticesSize = m_NativeCounter.Count * 4;
    }
}