using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Utilities;

namespace Environment.System
{
    [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
    public struct BuildBaseSystem : IJobParallelFor
    {
        [ReadOnly] public int3 chunkPosition;
        [ReadOnly] public int3 chunkSize;

        [WriteOnly] public int maxHeight;
        [WriteOnly] public NativeArray<Block> blocks;
        
        public void Execute(int index)
        {
            var gridPosition = index.To3DIndex(chunkSize);
            var worldPosition = gridPosition + chunkPosition * chunkSize;
            var block = new Block();
            var rand = Random.CreateFromIndex((uint)index);
            
            var waterLevel = 55;
            
            var simplexHeight = Noise.FractalSimplex(worldPosition.xz + new float2(9.0f, 0.5f), 0.008f, 2) * 5f +
                                    Noise.FractalSimplex(worldPosition.xz + new float2(0.2f, 7.5f), 0.022f, 3) * 4.5f +
                                    Noise.FractalSimplex(worldPosition.xz + new float2(5.3f, 0.2f), 0.001f, 4) * 30f;//43f;

            maxHeight = (int)math.floor(simplexHeight) + waterLevel;
            
            var isBedrock = worldPosition.y == 0 || worldPosition.y < rand.NextInt(5);
            
            var isLake = maxHeight <= waterLevel;
            
            if (isBedrock)
            {
                block.type = BlockType.Bedrock;
            }
            else if (worldPosition.y >= maxHeight - 5 && worldPosition.y <= maxHeight + 1 && isLake)
            {
                if (Noise.FractalSimplex(worldPosition.xz + new float2(0.2f, 7.5f), 0.02f, 3) > 0.2f)
                {
                    block.type = worldPosition.y >= maxHeight - 3 ? BlockType.Sand : // make to layer sand
                                 worldPosition.y >= maxHeight - 5 ? BlockType.SandStone : BlockType.Stone; //make a sand base, then stone
                }
                else
                {
                    if (worldPosition.y < maxHeight)
                    {
                        block.type = worldPosition.y > maxHeight - 3 ? BlockType.Dirt : BlockType.Stone;
                    }
                    else
                    {
                        if (worldPosition.y >= waterLevel)
                        {
                            block.type = worldPosition.y > maxHeight - 1 ? BlockType.GrassDirt :
                                         worldPosition.y > maxHeight - 3 ? BlockType.Dirt : BlockType.Stone;
                        }
                        else
                        {
                            block.type = worldPosition.y > maxHeight - 3 ? BlockType.Dirt : BlockType.Stone;
                        }
                    }
                }
            }
            else if (worldPosition.y < maxHeight - 3)
            {
                block.type = BlockType.Stone;
            }
            else if (isLake && worldPosition.y <= waterLevel)
            {
                block.type = BlockType.Water;
            }
            else if (worldPosition.y < maxHeight)
            {
                block.type = BlockType.Dirt;
            }
            else if (worldPosition.y == maxHeight && worldPosition.y > waterLevel)
            {
                block.type = BlockType.GrassDirt;
            }
            else
            {
                block.type = BlockType.Air;
            }
            blocks[index] = block;
        }
    }
}