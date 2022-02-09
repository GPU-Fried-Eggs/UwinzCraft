using UnityEngine.Rendering.Universal;

namespace Rendering
{
    public class SSGIRenderFeature : ScriptableRendererFeature
    {
        public SSGISettings settings = new SSGISettings();
        private SSGIRenderPass m_ScriptablePass;

        /// <inheritdoc/>
        public override void Create()
        {
            m_ScriptablePass = new SSGIRenderPass("SSGI")
            {
                Material = settings.Material,
                SamplesCount = settings.SamplesCount,
                IndirectAmount = settings.IndirectAmount,
                NoiseAmount = settings.NoiseAmount,
                Noise = settings.Noise,
                Enabled = settings.Enabled,
                renderPassEvent = settings.renderPassEvent
            };
        }

        // Here you can inject one or multiple render passes in the renderer.
        // This method is called when setting up the renderer once per-camera.
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            m_ScriptablePass.Setup(renderer.cameraColorTarget);
            renderer.EnqueuePass(m_ScriptablePass);
        }
    }
}


