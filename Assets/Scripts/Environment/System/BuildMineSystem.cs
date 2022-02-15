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

        public int maxHeight;
        public NativeArray<Block> blocks;
        
        public void Execute(int index)
        {
            var block = blocks[index];
            
            if(block.type is BlockType.Water) return;
            
            var gridPosition = index.To3DIndex(chunkSize);
            var worldPosition = gridPosition + chunkPosition * chunkSize;
            
            //if (noise.snoise(worldPosition.xz * new float2(0.06f)) > 0.8f)
            //{
            //    switch (worldPosition.y)
            //    {
            //        case < 14:
            //            break;
            //        case < 25:
            //            break;
            //        default:
            //            break;
            //    }
            //}
            
            var isCave = noise.snoise(worldPosition * new float3(0.08f)) > 0f;
            
            // Caves remove
            if (isCave)
                block = Block.Empty;

            blocks[index] = block;
        }
    }
}