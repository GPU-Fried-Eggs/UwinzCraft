using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Utilities;

namespace Environment.System
{
    [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
    public struct BuildMineSystem : IJobParallelFor
    {
        [ReadOnly] public int3 chunkPosition;
        [ReadOnly] public int3 chunkSize;
        
        public NativeArray<Block> blocks;
        
        public unsafe void Execute(int index)
        {
            var block = blocks[index];

            if (block.type is BlockType.Stone)
            {
                var gridPosition = index.To3DIndex(chunkSize);
                var worldPosition = gridPosition + chunkPosition * chunkSize;
   
                if (Noise.ClampedSimplex(worldPosition, 0.15f) > 0.8f)
                {
                    blocks[index] = Block.Empty;
                    //switch (worldPosition.y) // prefab 懒得做怎么办，直接搞空算了
                    //{
                    //    case < 14:
                    //        blocks[index] = new Block(BlockType.DiamondOre); 
                    //        break;
                    //    case < 30:
                    //        blocks[index] = new Block(BlockType.IronOre);
                    //        break;
                    //    default:
                    //        break;
                    //}
                }
            }
        }
    }
}