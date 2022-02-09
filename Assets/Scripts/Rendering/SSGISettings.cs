using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Rendering
{
    [Serializable]
    public class SSGISettings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;

        public Material Material = null;

        [Range(8, 128)]
        public int SamplesCount = 8;
        [Range(0.0f, 512.0f)]
        public float IndirectAmount = 8;
        [Range(0.0f, 5.0f)]
        public float NoiseAmount = 2;
        public bool Noise = true;
        public bool Enabled = true;
    }
}