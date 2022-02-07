using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Environment.System
{
    [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
    public struct BuildFoliageSystem : IJobParallelFor
    {
        [ReadOnly] public int3 chunkPosition;
        [ReadOnly] public int3 chunkSize;

        public NativeArray<Block> blocks;
        
        public void Execute(int index)
        {
            if (blocks[index].type is BlockType.GrassDirt)
            {
                var gridPosition = index.To3DIndex(chunkSize);
                var rand = Random.CreateFromIndex((uint)index);
                if (gridPosition.x < chunkSize.x - 3 && gridPosition.x > 3 && gridPosition.z < chunkSize.z - 3 && gridPosition.z > 3)
                {
                    if (rand.NextInt(0, 5) > 4)
                    {
                        var treeHeight = rand.NextBool() ? 5 : 6;
                        var block = new Block(BlockType.OakLeaves);
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
                }
                else if (gridPosition.x < chunkSize.x - 1 && gridPosition.x > 1 &&
                         gridPosition.z < chunkSize.z - 1 && gridPosition.z > 1)
                {
                    var worldPosition = gridPosition + chunkPosition * chunkSize;
                    if (noise.cnoise(worldPosition.xz * new float2(0.45f)) > 0.5f)
                    {
                        if (rand.NextBool())
                        {
                            blocks[(gridPosition + new int3(0, 1, 0)).To1DIndex(chunkSize)] = new Block(BlockType.Grass);
                        }
                        else
                        {
                            // flowers?
                        }
                    }
                }
            }
        }
    }
    
    
}