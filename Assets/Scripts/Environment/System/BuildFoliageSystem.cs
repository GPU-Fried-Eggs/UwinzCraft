using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Utilities;

namespace Environment.System
{
    [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
    public struct BuildFoliageSystem : IJobParallelFor
    {
        [ReadOnly] public int3 chunkPosition;
        [ReadOnly] public int3 chunkSize;

        [NativeDisableParallelForRestriction] public NativeArray<Block> blocks;
        
        public void Execute(int index)
        {
            if (blocks[index].type is BlockType.GrassDirt)
            {
                var gridPosition = index.To3DIndex(chunkSize);
                var worldPosition = gridPosition + chunkPosition * chunkSize;
                var rand = Random.CreateFromIndex((uint)(index + math.lengthsq(worldPosition)));
                if (gridPosition.x < chunkSize.x - 3 && gridPosition.x > 3 &&
                    gridPosition.z < chunkSize.z - 3 && gridPosition.z > 3 &&
                    Noise.ClampedSimplex(worldPosition.xz, 0.45f) > 0.9f)
                {
                    var treeHeight = rand.NextBool() ? 5 : 6;
                    for (int xAxis = -2; xAxis < 3; xAxis++)
                        for (int zAxis = -2; zAxis < 3; zAxis++)
                        {
                            blocks[(gridPosition + new int3(xAxis, treeHeight - 2, zAxis)).To1DIndex(chunkSize)] = new Block(BlockType.OakLeaves);
                            blocks[(gridPosition + new int3(xAxis, treeHeight - 3, zAxis)).To1DIndex(chunkSize)] = new Block(BlockType.OakLeaves);
                        }

                    for (int xAxis = -1; xAxis < 2; xAxis++)
                        for (int zAxis = -1; zAxis < 2; zAxis++)
                        {
                            blocks[(gridPosition + new int3(xAxis, treeHeight - 1, zAxis)).To1DIndex(chunkSize)] = new Block(BlockType.OakLeaves);
                        }
                    
                    for (int xAxis = -1; xAxis < 2; xAxis++)
                        blocks[(gridPosition + new int3(xAxis, treeHeight, 0)).To1DIndex(chunkSize)] = new Block(BlockType.OakLeaves);
                    
                    for (int zAxis = -1; zAxis < 2; zAxis++)
                        blocks[(gridPosition + new int3(0, treeHeight, zAxis)).To1DIndex(chunkSize)] = new Block(BlockType.OakLeaves);

                    for (int i = 1; i < treeHeight; i++)
                        blocks[(gridPosition + new int3(0, i, 0)).To1DIndex(chunkSize)] = new Block(BlockType.OakLog);
                    
                }
                else if (gridPosition.x < chunkSize.x - 1 && gridPosition.x > 1 &&
                         gridPosition.z < chunkSize.z - 1 && gridPosition.z > 1)
                {
                    if (noise.cnoise(worldPosition.xz * new float2(0.45f)) > 0.5f)
                    {
                        if (rand.NextBool())
                        {
                            blocks[(gridPosition + new int3(0, 1, 0)).To1DIndex(chunkSize)] = new Block(BlockType.Grass);
                        }
                        else
                        {
                            blocks[(gridPosition + new int3(0, 1, 0)).To1DIndex(chunkSize)] = new Block(BlockType.Grass);
                            // flowers?
                        }
                    }
                }
            }
        }
    }
    
    
}