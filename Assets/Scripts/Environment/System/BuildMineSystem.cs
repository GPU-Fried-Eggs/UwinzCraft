using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Environment.System
{
    [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
    public struct BuildMineSystem : IJobParallelFor
    {
        [ReadOnly] public int3 chunkPosition;
        [ReadOnly] public int3 chunkSize;

        [NativeDisableParallelForRestriction] public NativeArray<Block> blocks;
        
        public void Execute(int index)
        {
            if (blocks[index].type is BlockType.Stone)
            {
                var gridPosition = index.To3DIndex(chunkSize);
                var worldPosition = gridPosition + chunkPosition * chunkSize;
                var block = new Block();
                if (noise.snoise(worldPosition.xz * new float2(0.06f)) > 0.8f)
                {
                    switch (worldPosition.y)
                    {
                        case < 14:
                            break;
                        case < 25:
                            break;
                        default:
                            break;
                    }
                }
                // Caves remove
                if (noise.snoise(worldPosition * new float3(0.035f)) > 0.75f)
                    block = Block.Empty;

                blocks[index] = block;
            }
        }
    }
}