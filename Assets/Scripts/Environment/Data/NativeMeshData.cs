using System.Collections;
using Environment.System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Utilities;

namespace Environment.Data
{
    public class NativeMeshData
    {
        private NativeArray<Block> nativeBlocks;
        public NativeArray<float3> nativeVertices;
        public NativeArray<float3> nativeNormals;
        public NativeList<int> nativeBlockIndices;
        public NativeList<int> nativeLiquidIndices;
        public NativeList<int> nativeFoliageIndices;
        public NativeList<int> nativeTransparentIndices;
        public NativeArray<float4> nativeUVs;
        public NativeArray<Color> nativeColors;
        public JobHandle jobHandle;
        private NativeCounter counter;

        public NativeMeshData(int3 chunkSize)
        {
            var numBlocks = chunkSize.x * chunkSize.y * chunkSize.z;
            var numLayer = chunkSize.x * chunkSize.z;

            nativeBlocks = new NativeArray<Block>(numBlocks, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            nativeVertices = new NativeArray<float3>(12 * numBlocks, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            nativeNormals = new NativeArray<float3>(12 * numBlocks, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            nativeUVs = new NativeArray<float4>(12 * numBlocks, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            nativeColors = new NativeArray<Color>(12 * numBlocks, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            nativeBlockIndices = new NativeList<int>(18 * numBlocks, Allocator.TempJob);
            nativeLiquidIndices = new NativeList<int>(numLayer, Allocator.TempJob);
            nativeFoliageIndices = new NativeList<int>(numLayer, Allocator.TempJob);
            nativeTransparentIndices = new NativeList<int>(numLayer, Allocator.TempJob);
            counter = new NativeCounter(Allocator.TempJob);
        }

        ~NativeMeshData()
        {
            jobHandle.Complete(); Dispose();
        }

        public void Dispose()
        {
            if (nativeBlocks.IsCreated) nativeBlocks.Dispose();
            if (nativeVertices.IsCreated) nativeVertices.Dispose();
            if (nativeNormals.IsCreated) nativeNormals.Dispose();
            if (nativeUVs.IsCreated) nativeUVs.Dispose();
            if (nativeColors.IsCreated) nativeColors.Dispose();
            if (nativeBlockIndices.IsCreated) nativeBlockIndices.Dispose();
            if (nativeLiquidIndices.IsCreated) nativeLiquidIndices.Dispose();
            if (nativeFoliageIndices.IsCreated) nativeFoliageIndices.Dispose();
            if (nativeTransparentIndices.IsCreated) nativeTransparentIndices.Dispose();
            if (counter.IsCreated) counter.Dispose();
        }

        public IEnumerator Generate(Block[] blocks, int3 chunkSize, bool argent = false)
        {
            nativeBlocks.CopyFrom(blocks);
            
            jobHandle = new BuildMeshSystem
            {
                blocks = nativeBlocks,
                chunkSize = chunkSize,
                vertices = nativeVertices,
                normals = nativeNormals,
                uvs = nativeUVs,
                colors = nativeColors,
                blockIndices = nativeBlockIndices,
                liquidIndices = nativeLiquidIndices,
                foliageIndices = nativeFoliageIndices,
                transparentIndices = nativeTransparentIndices,
                counter = counter
            }.Schedule();

            int frameCount = 1;
            yield return new WaitUntil(() =>
            {
                frameCount++;
                return jobHandle.IsCompleted || frameCount >= 4 || argent;
            });

            jobHandle.Complete();
        }

        public void GetMeshInformation(out int verticesSize) => verticesSize = counter.Count * 4;
    }
}