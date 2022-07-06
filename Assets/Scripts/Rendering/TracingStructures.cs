using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// ReSharper disable InconsistentNaming

namespace Rendering
{
    /// <summary>
    /// Information for each material
    /// </summary>
    public struct MaterialData
    {
        public Vector4 Color;
        public Vector3 Emission;
        public float Metallic;
        public float Smoothness;
        public float IOR;
        public float RenderMode;
        public int AlbedoIdx;
        public int EmitIdx;
        public int MetallicIdx;
        public int NormalIdx;
        public int RoughIdx;

        public static int TypeSize = sizeof(float) * 11 + sizeof(int) * 5;
    }
    
    public struct MeshObject
    {
        public Matrix4x4 localToWorldMatrix;
        public int indices_offset;
        public int indices_count;
        public Vector3 albedo;
        public Vector3 specular;
        public float smoothness;
        public float ior;
        public Vector3 emission;
    }
    
    public struct Sphere
    {
        public Vector3 position;
        public float radius;
        public Vector3 albedo;
        public Vector3 specular;
        public float smoothness;
        public Vector3 emission;
    }
}