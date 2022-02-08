using System.Runtime.CompilerServices;
using Environment.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Utilities;

namespace Environment.System
{
    [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
    public struct BuildMeshSystem : IJob
    {
        [ReadOnly] public NativeArray<Block> blocks;
        [ReadOnly] public int3 chunkSize;

        [WriteOnly] public NativeArray<float3> vertices;
        [WriteOnly] public NativeArray<float3> normals;
        [WriteOnly] public NativeArray<float4> uvs;
        [WriteOnly] public NativeArray<Color> colors;
        
        [WriteOnly] public NativeList<int> blockIndices;
        [WriteOnly] public NativeList<int> liquidIndices;
        [WriteOnly] public NativeList<int> foliageIndices;
        [WriteOnly] public NativeList<int> transparentIndices;

        [WriteOnly] public NativeCounter counter;
    
        private struct Empty { }
    
        /* ---------------------------------------------------------------------------------------- *
         * 0/1. daX = 2 -> z   , daY = 1 -> y   , daZ = 0 -> x                                      *
         *   => x = chunkSize.z, y = chunkSize.y, d = chunkSize.x => gridPosition = (depth, y, x)   *
         *                                                                                          *
         * 2/3. daX = 0        , daY = 2        , daZ = 1                                           *
         *   => x = chunkSize.x, y = chunkSize.z, d = chunkSize.y => gridPosition = (x, depth, y)   *
         *                                                                                          *
         * 4/5. daX = 0        , daY = 1        , daZ = 2                                           *
         *   => x = chunkSize.x, y = chunkSize.y, d = chunkSize.z => gridPosition = (x, y, depth)   *
         * ---------------------------------------------------------------------------------------- */
        public void Execute()
        {
            for (int direction = 0; direction < 6; direction++)
            {
                var hashMap = new NativeHashMap<int3, Empty>(chunkSize[SharedData.DirectionAlignedX.Data[direction]] * chunkSize[SharedData.DirectionAlignedY.Data[direction]], Allocator.Temp);
                for (int depth = 0; depth < chunkSize[SharedData.DirectionAlignedZ.Data[direction]]; depth++)
                {
                    for (int x = 0; x < chunkSize[SharedData.DirectionAlignedX.Data[direction]]; x++)
                    {
                        for (int y = 0; y < chunkSize[SharedData.DirectionAlignedY.Data[direction]];)
                        {
                            int3 gridPosition = new int3 { [SharedData.DirectionAlignedX.Data[direction]] = x, [SharedData.DirectionAlignedY.Data[direction]] = y, [SharedData.DirectionAlignedZ.Data[direction]] = depth };
                            
                            var block = blocks[gridPosition.To1DIndex(chunkSize)];
                            var shape = SharedData.BlockData.Data[UnsafeUtility.EnumToInt(block.type)].GetBlockShape();

                            #region Step1: Ignore Air block, calculated blocks and transparency blocks
                            // if no block at the position, skip it.
                            if (shape == (long) BlockShape.Empty) { y++; continue; }
                            // if already registered, skip it.
                            if (hashMap.ContainsKey(gridPosition)) { y++; continue; }
    
                            int3 neighborPosition = gridPosition + SharedData.CubeDirectionOffsets.Data[direction];
       
                            if (TransparencyCheck(in blocks, out BlockType neighborBlock, neighborPosition, chunkSize)) { y++; continue; }

                            if (shape == (long) BlockShape.Liquid)
                            {
                                if (direction != 2) { y++; continue; }
                                if (neighborBlock == BlockType.Water) { y++; continue; }
                            }
    
                            if (shape == (long) BlockShape.Foliage)
                            {
                                if (direction != 2) { y++; continue; }
                            }

                            #endregion
    
                            #region Step2: Get the calculated light data
                            
                            // Get the light data results
                            //var light = lightData[gridPosition.To1DIndex(chunkSize)];
                            
                            #endregion
    
                            hashMap.TryAdd(gridPosition, new Empty());
    
                            #region Step3: Mesh combine in xy direction
    
                            int height = 1, width = 1;
                            if (shape != (long) BlockShape.Transparent && shape != (long) BlockShape.Liquid && shape != (long) BlockShape.Foliage)
                            {
                                for (height = 1; height + y < chunkSize[SharedData.DirectionAlignedY.Data[direction]]; height++)
                                {
                                    int3 nextPosition = gridPosition;
                                    nextPosition[SharedData.DirectionAlignedY.Data[direction]] += height;

                                    var nextBlock = blocks[nextPosition.To1DIndex(chunkSize)];
                                    
                                    if (nextBlock.type != block.type) break;
                                    if (hashMap.ContainsKey(nextPosition)) break;

                                    hashMap.TryAdd(nextPosition, default);
                                }

                                bool isDone = false;

                                for (width = 1; width + x < chunkSize[SharedData.DirectionAlignedX.Data[direction]]; width++)
                                {
                                    for (int dy = 0; dy < height; dy++)
                                    {
                                        int3 nextPosition = gridPosition;
                                        nextPosition[SharedData.DirectionAlignedX.Data[direction]] += width;
                                        nextPosition[SharedData.DirectionAlignedY.Data[direction]] += dy;

                                        var nextBlock = blocks[nextPosition.To1DIndex(chunkSize)];
                            
                                        if (nextBlock.type != block.type || hashMap.ContainsKey(nextPosition))
                                        {
                                            isDone = true;
                                            break;
                                        }
                                    }

                                    if (isDone) break;

                                    for (int dy = 0; dy < height; dy++)
                                    {
                                        int3 nextPosition = gridPosition;
                                        nextPosition[SharedData.DirectionAlignedX.Data[direction]] += width;
                                        nextPosition[SharedData.DirectionAlignedY.Data[direction]] += dy;
                                        hashMap.TryAdd(nextPosition, default);
                                    }
                                }
                            }

                            #endregion

                            switch (shape)
                            {
                                case (long) BlockShape.Liquid:
                                {
                                    var numVertices = counter.Increment() * 4;
                                    for (int i = 0; i < 4; i++)
                                    {
                                        // 6(0,1,1) --> 7(1,1,1)
                                        //    |             |
                                        // 4(0,1,0) --> 5(1,1,0)
                                        float3 vertex = SharedData.CubeVertices.Data[SharedData.CubeFaces.Data[i + 2 * 4]];

                                        colors[numVertices + i] = new Color(0, 0, 0, 1);
                                        vertices[numVertices + i] = vertex + gridPosition + new float3(0, -0.13f, 0);
                                        normals[numVertices + i] = new float3(0, 1, 0);
                                        uvs[numVertices + i] = new float4(1, 1, 0, 0);
                                    }

                                    for (int i = 0; i < 6; i++)
                                        liquidIndices.Add(SharedData.CubeFlippedIndices.Data[12 + i] + numVertices);
                                    
                                    break;
                                }
                                case (long) BlockShape.Foliage:
                                {
                                    var numVertices = counter.Increment(2) * 4;
                                    var rand = 1;
                                    for (int i = 0; i < 8; i++)
                                    {
                                        /*      7 ------- 6
                                         *     /|        /|
                                         *    / |       / |
                                         *   4 ------- 5  |
                                         *   |  3 -----|- 2
                                         *   | /       | /
                                         *   |/        |/
                                         *   0 ------- 1
                                         *            1 -> 2 -> 5 -> 6 -> 0 -> 3 -> 4 -> 7
                                         *   We want  0 -> 6 -> 2 -> 0 -> 4 -> 6 -> 1 -> 7 -> 3 -> 1 -> 5 -> 7
                                         *   so       4 -> 3 -> 1 -> 4 -> 6 -> 3 -> 0 -> 7 -> 5 -> 0 -> 2 -> 7
                                         */

                                        float3 vertex = SharedData.CubeVertices.Data[SharedData.CubeFaces.Data[i]];

                                        colors[numVertices + i] = new Color(0, 0, 0, 1);
                                        vertices[numVertices + i] = vertex + gridPosition + new float3(0, -0.04f, 0);
                                        normals[numVertices + i] = (i < 4) ? new float3(0.7f, 0, 0.7f) : new float3(-0.7f, 0, 0.7f);
                                        uvs[numVertices + i] = new float4
                                        {
                                            x = SharedData.CubeUVs.Data[i % 4].x * 0.25f * rand,
                                            y = 1 - (SharedData.CubeUVs.Data[i % 4].y * 0.25f * (rand % 4)),
                                            z = 0.25f * (rand - 1),
                                            w = 0.25f * ((rand % 4) - 1),
                                        };
                                    }

                                    for (int i = 0; i < 12; i++)
                                        foliageIndices.Add(numVertices + SharedData.CubeCrossIndices.Data[i]);
                                    
                                    break;
                                }
                                case (long) BlockShape.Block:
                                {
                                    var numVertices = counter.Increment() * 4;
                                    for (int i = 0; i < 4; i++)
                                    {
                                        float3 vertex = SharedData.CubeVertices.Data[SharedData.CubeFaces.Data[i + direction /*0-5*/ * 4]];

                                        vertex[SharedData.DirectionAlignedX.Data[direction]] *= width;
                                        vertex[SharedData.DirectionAlignedY.Data[direction]] *= height;

                                        colors[numVertices + i] = new Color(0, 0, 0, 1);
                                        vertices[numVertices + i] = vertex + gridPosition;
                                        normals[numVertices + i] = SharedData.CubeDirectionOffsets.Data[direction];
                                        uvs[numVertices + i] = new float4
                                        {
                                            xy = new float2(SharedData.CubeUVs.Data[i].x * width, SharedData.CubeUVs.Data[i].y * height),
                                            zw = SharedData.BlockData.Data[UnsafeUtility.EnumToInt(block.type)].GetBlockUV(direction)
                                        };
                                    }

                                    for (int i = 0; i < 6; i++)
                                        blockIndices.AddNoResize(SharedData.CubeIndices.Data[direction * 6 + i] + numVertices);

                                    break;
                                }
                                case (long) BlockShape.Transparent:
                                {
                                    var numVertices = counter.Increment() * 4;
                                    for (int i = 0; i < 4; i++)
                                    {
                                        float3 vertex = SharedData.CubeVertices.Data[SharedData.CubeFaces.Data[i + direction /*0-5*/ * 4]];

                                        vertex[SharedData.DirectionAlignedX.Data[direction]] *= width;
                                        vertex[SharedData.DirectionAlignedY.Data[direction]] *= height;

                                        colors[numVertices + i] = new Color(0, 0, 0, 1);
                                        vertices[numVertices + i] = vertex + gridPosition;
                                        normals[numVertices + i] = SharedData.CubeDirectionOffsets.Data[direction];
                                        uvs[numVertices + i] = new float4
                                        {
                                            xy = new float2(SharedData.CubeUVs.Data[i].x * width, SharedData.CubeUVs.Data[i].y * height),
                                            zw = SharedData.BlockData.Data[UnsafeUtility.EnumToInt(block.type)].GetBlockUV(direction)
                                        };
                                    }

                                    for (int i = 0; i < 6; i++)
                                        transparentIndices.Add(SharedData.CubeIndices.Data[direction * 6 + i] + numVertices);

                                    break;
                                }
                            }
                            
                            y += height;
                        }
                    }
                    hashMap.Clear();
                }
                hashMap.Dispose();
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TransparencyCheck(in NativeArray<Block> blocks, out BlockType neighborBlock, int3 position, int3 chunkSize)
        {
            neighborBlock = BlockType.Air;
            if (!position.BoundaryCheck(chunkSize)) return false;
            neighborBlock = blocks[position.To1DIndex(chunkSize)].type;
            var neighborShape = SharedData.BlockData.Data[UnsafeUtility.EnumToInt(neighborBlock)].GetBlockShape();
            return neighborShape is not ((long) BlockShape.Empty or (long) BlockShape.Foliage or (long) BlockShape.Transparent or (long) BlockShape.Liquid);
        }
        
    }
}