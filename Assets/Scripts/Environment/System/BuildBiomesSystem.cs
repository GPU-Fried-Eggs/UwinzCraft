using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Environment.System
{
    [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
    public struct BuildBiomesSystem : IJobParallelFor
    {
        [ReadOnly] public int3 chunkPosition;
        [ReadOnly] public int3 chunkSize;

        [NativeDisableParallelForRestriction] public NativeArray<Block> blocks;

        public void Execute(int index)
        {
            
        }
    }
}