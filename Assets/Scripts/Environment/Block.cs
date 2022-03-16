using System.Runtime.InteropServices;
using UnityEngine;

namespace Environment
{
    [StructLayout(LayoutKind.Explicit)]
    public struct Block
    {
        [FieldOffset(0)] public BlockType type;
        
        [FieldOffset(1)] public Color32 color; // humidity and tempareture effect the weather; 

        public Block(BlockType type, Color32 color)
        {
            this.type = type;
            this.color = color;
        }

        public static Block Empty => new Block(BlockType.Air, new Color32(0, 0, 0, 0));
    }

    public enum BlockType : byte
    {
        Air, Dirt, GrassDirt, Stone, Sand, SandStone, Bedrock,
        CoalOre, IronOre, GoldOre, DiamondOre, Water,
        Glowstone, OakLog, OakPlanks, OakLeaves, Foliage, Grass
    }

    public enum BlockShape : byte
    {
        Empty, Block, Transparent, Foliage, Liquid
    }

}