using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Utilities;

namespace Environment.System
{
    [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
    public struct BuildMapSystem : IJobParallelFor
    {
        [ReadOnly] public int3 chunkPosition;
        [ReadOnly] public int3 chunkSize;

        [NativeDisableParallelForRestriction] public NativeArray<Block> blocks;

        public void Execute(int index)
        {
            if (blocks[index].type is BlockType.OakLog or BlockType.OakLeaves) return;

            // Parameter
            var gridPosition = index.To3DIndex(chunkSize);
            var worldPosition = gridPosition + chunkPosition * chunkSize;
            var block = new Block();
            var rand = Random.CreateFromIndex((uint)index);

            var waterLevel = 55;

            var simplexHeight = Noise.FractalSimplex(worldPosition.xz + new float2(9.0f, 0.5f), 0.008f, 2) * 5f +
                                    Noise.FractalSimplex(worldPosition.xz + new float2(0.2f, 7.5f), 0.022f, 3) * 4.5f +
                                    Noise.FractalSimplex(worldPosition.xz + new float2(5.3f, 0.2f), 0.001f, 4) * 30f;//43f;

            // Logic
            int heightMap = (int)math.floor(simplexHeight) + waterLevel;

            bool isLake = heightMap <= waterLevel;

            bool isCaves = Noise.FractalSimplex(worldPosition, 0.035f, 8) > 0.75f;

            if (worldPosition.y == 0 || worldPosition.y < rand.NextInt(5))
            {
                block.type = BlockType.Bedrock;
            }
            else if (worldPosition.y >= heightMap - 5 && worldPosition.y <= heightMap + 1 && isLake)
            {
                if (Noise.FractalSimplex(worldPosition.xz + new float2(0.2f, 7.5f), 0.02f, 3) > 0.2f)
                {
                    block.type = worldPosition.y >= heightMap - 3 ? BlockType.Sand : // make to layer sand
                                 worldPosition.y >= heightMap - 5 ? BlockType.SandStone : BlockType.Stone; //make a sand base, then stone
                }
                else
                {
                    if (worldPosition.y < heightMap)
                    {
                        block.type = worldPosition.y > heightMap - 3 ? BlockType.Dirt : BlockType.Stone;
                    }
                    else
                    {
                        if (worldPosition.y >= waterLevel)
                        {
                            block.type = worldPosition.y > heightMap - 1 ? BlockType.GrassDirt :
                                         worldPosition.y > heightMap - 3 ? BlockType.Dirt : BlockType.Stone;
                        }
                        else
                        {
                            block.type = worldPosition.y > heightMap - 3 ? BlockType.Dirt : BlockType.Stone;
                        }
                    }
                }
            }
            else if (worldPosition.y < heightMap - 3 || (isCaves && worldPosition.y < heightMap + 1))
            {
                if (isCaves)
                {
                    if (worldPosition.y >= waterLevel - 5)
                    {
                        block.type = heightMap <= waterLevel + (-0.0556 * worldPosition.y * worldPosition.y + 5.3889 * worldPosition.y - 127.555f) ? BlockType.Stone : BlockType.Air;
                    }
                    else if (worldPosition.y >= waterLevel - 10)
                    {
                        block.type = heightMap <= waterLevel + (-0.0556 * worldPosition.y * worldPosition.y + 5.7222 * worldPosition.y - 144.222f) ? BlockType.Stone : BlockType.Air;
                    }
                    else
                    {
                        block.type = BlockType.Air;
                    }
                }
                else
                {
                    block.type = BlockType.Stone;
                }
            }
            else if (isLake && worldPosition.y <= waterLevel)
            {
                block.type = BlockType.Water;
            }
            else if (worldPosition.y < heightMap)
            {
                block.type = BlockType.Dirt;
            }
            else if (worldPosition.y == heightMap && worldPosition.y > waterLevel)
            {
                block.type = BlockType.GrassDirt;
            }
            else if (worldPosition.y == heightMap + 1 && worldPosition.y > waterLevel + 1 && isCaves == false)
            {
                if (rand.NextBool())
                {
                    if (gridPosition.x < chunkSize.x - 3 && gridPosition.x > 3 &&
                        gridPosition.z < chunkSize.z - 3 && gridPosition.z > 3 &&
                        CalcPixel2D(worldPosition.xz, 0.45f) > 0.9f)
                    {
                        int treeHeight = rand.NextBool() ? 5 : 6;
                        block.type = BlockType.OakLeaves;

                        for (int xAxis = -2; xAxis < 3; xAxis++)
                            for (int zAxis = -2; zAxis < 3; zAxis++)
                            {
                                blocks[(gridPosition + new int3(xAxis, treeHeight - 2, zAxis)).To1DIndex(chunkSize)] = block;
                                blocks[(gridPosition + new int3(xAxis, treeHeight - 3, zAxis)).To1DIndex(chunkSize)] = block;
                            }

                        for (int xAxis = -1; xAxis < 2; xAxis++)
                            for (int zAxis = -1; zAxis < 2; zAxis++)
                            {
                                blocks[(gridPosition + new int3(xAxis, treeHeight - 1, zAxis)).To1DIndex(chunkSize)] = block;
                            }

                        blocks[(gridPosition + new int3(1, treeHeight, 0)).To1DIndex(chunkSize)] = block;
                        blocks[(gridPosition + new int3(-1, treeHeight, 0)).To1DIndex(chunkSize)] = block;
                        blocks[(gridPosition + new int3(0, treeHeight, 0)).To1DIndex(chunkSize)] = block;
                        blocks[(gridPosition + new int3(0, treeHeight, 1)).To1DIndex(chunkSize)] = block;
                        blocks[(gridPosition + new int3(0, treeHeight, -1)).To1DIndex(chunkSize)] = block;

                        block.type = BlockType.OakLog;
                        for (int i = 1; i < treeHeight; i++)
                            blocks[(gridPosition + new int3(0, i, 0)).To1DIndex(chunkSize)] = block;
                    }
                    else if (gridPosition.x < chunkSize.x - 1 && gridPosition.x > 1 &&
                             gridPosition.z < chunkSize.z - 1 && gridPosition.z > 1 &&
                             CalcPixel2D(new float2(worldPosition.x + 10, worldPosition.z + 2), 0.45f) > 0.5f)
                    {
                        block.type = rand.NextBool() ? BlockType.Grass : BlockType.Air;
                    }
                }
            }
            blocks[index] = block;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CalcPixel2D(float2 pos, float scale) => (noise.snoise(pos * new float2(scale)) + 1) * 0.5f;
    }
}