using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
// ReSharper disable InconsistentNaming

namespace Utilities
{
    [BurstCompatible(GenericTypeArguments = new[] { typeof(int), typeof(int3), typeof(long), typeof(float2), typeof(float3) })]
    public unsafe struct FixedArray<T> where T : unmanaged
    {
        private void* m_Buffer;

        private Allocator m_Allocator;

        public T this[int index]
        {
            get => UnsafeUtility.ReadArrayElement<T>(m_Buffer, index);
            set => UnsafeUtility.WriteArrayElement(m_Buffer, index, value);
        }

        public FixedArray(int length, Allocator allocator = Allocator.Persistent)
        {
            m_Buffer = UnsafeUtility.Malloc(UnsafeUtility.SizeOf<T>() * (long) length, UnsafeUtility.AlignOf<T>(), allocator);
            m_Allocator = allocator;
        }
        
        public void CopyFrom(params T[] array) => Copy(array, this);

        private static void Copy(T[] src, FixedArray<T> dst)
        {
            var gcHandle = GCHandle.Alloc(src, GCHandleType.Pinned);
            UnsafeUtility.MemCpy(dst.m_Buffer, (void*) gcHandle.AddrOfPinnedObject(), src.Length * UnsafeUtility.SizeOf<T>());
            gcHandle.Free();
        }

        public void Dispose()
        {
            if (m_Allocator > Allocator.None)
            {
                UnsafeUtility.Free(m_Buffer, m_Allocator);
                m_Allocator = Allocator.Invalid;
            }

            m_Buffer = null;
        }
    }
}