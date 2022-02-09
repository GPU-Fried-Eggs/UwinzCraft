using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Rendering
{
    class SSGIRenderPass : ScriptableRenderPass
    {
        private string m_ProfilerTag;
        private RenderTargetIdentifier m_TmpRT1;
        private RenderTargetIdentifier m_Source;
        
        public Material Material;
        public int SamplesCount;
        public float IndirectAmount;
        public float NoiseAmount;
        public bool Noise;
        public bool Enabled;

        public SSGIRenderPass(string profilerTag) => m_ProfilerTag = profilerTag;
        
        public void Setup(RenderTargetIdentifier source) => m_Source = source;

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            var width = cameraTextureDescriptor.width;
            var height = cameraTextureDescriptor.height;

            m_TmpRT1 = SetupRenderTargetIdentifier(cmd, 0, width, height);
        }
        
        private RenderTargetIdentifier SetupRenderTargetIdentifier(CommandBuffer cmd, int id, int width, int height)
        {
            int tmpId = Shader.PropertyToID($"SSGI_{id}_RT");
            cmd.GetTemporaryRT(tmpId, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32);

            var rt = new RenderTargetIdentifier(tmpId);
            ConfigureTarget(rt);

            return rt;
        }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (Material == null) return;

            var cmd = CommandBufferPool.Get(m_ProfilerTag);
            var opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
            opaqueDesc.depthBufferBits = 0;

            if (Enabled)
            {
                var invProjectionMatrix = GL.GetGPUProjectionMatrix(renderingData.cameraData.camera.projectionMatrix, false).inverse;

                Material.SetFloat("_SamplesCount", SamplesCount);
                Material.SetFloat("_IndirectAmount", IndirectAmount);
                Material.SetFloat("_NoiseAmount", NoiseAmount);
                Material.SetInt("_Noise", Noise ? 1 : 0);
                Material.SetMatrix("_InverseProjectionMatrix", invProjectionMatrix);

                Blit(cmd, m_Source, m_TmpRT1, Material, 0);
                Blit(cmd, m_TmpRT1, m_Source);
            }
            else
            {
                Blit(cmd, m_Source, m_Source);
            }
            cmd.SetGlobalTexture(Shader.PropertyToID("_TempSceneTex"), m_Source);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

}