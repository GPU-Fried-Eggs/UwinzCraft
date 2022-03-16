using Unity.Mathematics;
using UnityEngine;

namespace Environment.Data
{
    [CreateAssetMenu(fileName = "New Block", menuName = "Block Prefab")]
    public class BlockPrefab : ScriptableObject
    {
        public BlockType block;

        public BlockShape shape;
        
        /// <summary>
        /// right - left - top - bottom - front - back
        /// </summary>
        public int2[] atlasPositions = new int2[6]; // 0 - 255
        
    }
}