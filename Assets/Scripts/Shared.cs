using System.Runtime.CompilerServices;
using Unity.Burst.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

public static class Shared
{
    #region Constants

    public static readonly int2 AtlasSize = new int2(16, 16);
    
    #endregion

    #region Functions
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int PackUVCoord(this int2 uv) => (uv.x & 0xF) << 4 | ((uv.y & 0xF) << 0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int2 UnpackUVCoord(this int uvs) => new int2((uvs >> 4) & 0xF, (uvs >> 0) & 0xF);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int2 GetBlockUV(this long data, int direction) => ((int) ((data >> (5 - direction) * 8) & 0xFFL)).UnpackUVCoord();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long GetBlockShape(this long data) => (data >> 56) & 0xFFL;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int2 To2DIndex(this int index, int size)
        => new(index / size, index % size);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int To1DIndex(this int2 index, int size)
        => index.x * size + index.y;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int3 To3DIndex(this int index, int3 size)
        => new(index / (size.y * size.z), (index / size.z) % size.y, index % size.z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int To1DIndex(this int3 index, int3 size)
        => index.z + index.y * size.z + index.x * size.y * size.z;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int3 ToChunk(this int3 worldGridPosition, int3 chunkSize)
        => Floor((float3) worldGridPosition / chunkSize);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int3 ToChunk(this Vector3 worldGridPosition, int3 chunkSize)
        => Floor((float3) worldGridPosition / chunkSize);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float3 ToWorld(this int3 chunkPosition, int3 chunkSize)
        => chunkPosition * chunkSize;
        
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int3 ToGrid(this Vector3 worldPosition, int3 chunkPosition, int3 chunkSize)
        => ToGrid(Floor(worldPosition), chunkPosition, chunkSize);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int3 ToGrid(this int3 worldGridPosition, int3 chunkPosition, int3 chunkSize)
        => Mod(worldGridPosition - chunkPosition * chunkSize, chunkSize);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool BoundaryCheck(this int3 position, int3 chunkSize)
        => chunkSize.x > position.x && chunkSize.y > position.y && chunkSize.z > position.z &&
           position.x >= 0 && position.y >= 0 && position.z >= 0;
        
    public static int InvertDirection(int direction)
    {
        int axis = direction / 2; // 0(+x,-x), 1(+y,-y), 2(+z,-z)
        int invDirection = Mathf.Abs(direction - (axis * 2 + 1)) + (axis * 2);

        /*
                direction    x0    abs(x0)    abs(x) + axis * 2 => invDirection
                0            -1    1          1  
                1            0     0          0
                2            -1    1          3
                3            0     0          2
                4            -1    1          5
                5            0     0          4
             */

        return invDirection;
    }
    
    private static int3 Mod(int3 v, int3 m)
    {
        var r = (int3) math.fmod(v, m);
        return math.select(r, r + m, r < 0);
    }

    private static int3 Floor(float3 v)
    {
        var vi = (int3) v;
        return math.select(vi, vi - 1, v < vi);
    }

    #endregion
}