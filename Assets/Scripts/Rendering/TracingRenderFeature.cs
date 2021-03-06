using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
// ReSharper disable InconsistentNaming

namespace Rendering
{
    public class TracingRenderFeature : ScriptableRendererFeature
    {
        private class TracingPass : ScriptableRenderPass
        {
            private static readonly string k_RenderTag = "Ray Tracing";

            private static readonly int CameraToWorldId = Shader.PropertyToID("_CameraToWorld");
            private static readonly int CameraInverseProjectionId = Shader.PropertyToID("_CameraInverseProjection");
            private static readonly int SkyboxTextureId = Shader.PropertyToID("_SkyboxTexture");
            private static readonly int ResultTextureId = Shader.PropertyToID("Result");
            private static readonly int PixelOffsetId = Shader.PropertyToID("_PixelOffset");
            private static readonly int RayBounceId = Shader.PropertyToID("_RayBounce");
            private static readonly int SeedId = Shader.PropertyToID("_Seed");
            private static readonly int DirectionalLightId = Shader.PropertyToID("_DirectionalLight");

            Tracing m_rayTracing;
            RTHandle currentTarget;
            RenderTexture resultTexture;
            RenderTexture cachingTexture;

            private uint currentSample = 0;
            Material progressiveSampleMat;
            Matrix4x4 cachingC2W = Matrix4x4.identity;

            public TracingPass(RenderPassEvent evt)
            {
                renderPassEvent = evt;
            }

            public void Setup(in RTHandle currentTarget)
            {
                this.currentTarget = currentTarget;
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (!renderingData.cameraData.postProcessEnabled) return;
                var stack = VolumeManager.instance.stack;
                m_rayTracing = stack.GetComponent<Tracing>();
                if (m_rayTracing == null) return;
                if (!m_rayTracing.IsActive()) return;

                var cmd = CommandBufferPool.Get(k_RenderTag);
                Render(cmd, ref renderingData);
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            void InitResultTexture(int width, int height)
            {
                if (resultTexture == null || resultTexture.width != width || resultTexture.height != height)
                {
                    if (resultTexture != null)
                        resultTexture.Release();

                    resultTexture = new RenderTexture(width, height, 0, RenderTextureFormat.DefaultHDR, RenderTextureReadWrite.Linear);
                    resultTexture.enableRandomWrite = true;
                    resultTexture.Create();
                }
            }

            void Render(CommandBuffer cmd, ref RenderingData renderingData)
            {
                ref var cameraData = ref renderingData.cameraData;
                var w = cameraData.camera.scaledPixelWidth;
                var h = cameraData.camera.scaledPixelHeight;

                var source = currentTarget;
                InitResultTexture(w, h);

                //SetShaderParameter
                {
                    m_rayTracing.RayTracingShader.SetMatrix(CameraToWorldId, cameraData.GetViewMatrix().inverse);
                    m_rayTracing.RayTracingShader.SetMatrix(CameraInverseProjectionId, cameraData.GetProjectionMatrix().inverse);
                    m_rayTracing.RayTracingShader.SetTexture(0, SkyboxTextureId, m_rayTracing.SkyboxTexture.value);
                    m_rayTracing.RayTracingShader.SetTexture(0, ResultTextureId, resultTexture);
                    m_rayTracing.RayTracingShader.SetVector(PixelOffsetId, new Vector2(Random.value, Random.value));
                    m_rayTracing.RayTracingShader.SetInt(RayBounceId, m_rayTracing.rayBounce.value);
                    m_rayTracing.RayTracingShader.SetFloat(SeedId, Random.value);

                    //SceneLights
                    var mainLight = renderingData.lightData.visibleLights[renderingData.lightData.mainLightIndex].light;
                    m_rayTracing.RayTracingShader.SetVector(DirectionalLightId, new Vector4(mainLight.transform.forward.x, mainLight.transform.forward.y, mainLight.transform.forward.z, mainLight.intensity));

                    //RayTracing Objects
                    m_rayTracing.SetRayTracingObjectsParameters();

                    //Compute
                    int threadGroupsX = Mathf.CeilToInt(w / 8.0f);
                    int threadGroupsY = Mathf.CeilToInt(h / 8.0f);
                    m_rayTracing.RayTracingShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);
                }

                //Accumulate Sampling
                if (m_rayTracing.AccSample.value)
                {
                    if (cameraData.GetViewMatrix().inverse != cachingC2W)
                    {
                        cachingC2W = cameraData.GetViewMatrix().inverse;
                        cachingTexture = new RenderTexture(w, h, 0, RenderTextureFormat.DefaultHDR);
                        Tracing.isSetObjects = false;
                        currentSample = 0;
                    }

                    if (progressiveSampleMat == null)
                    {
                        progressiveSampleMat = new Material(Shader.Find("Hidden/RayTracing/ProgressiveSample"));
                    }

                    progressiveSampleMat.SetFloat("_Sample", currentSample);
                    cmd.Blit(resultTexture, cachingTexture, progressiveSampleMat);
                    cmd.Blit(cachingTexture, resultTexture);
                    currentSample++;
                }

                cmd.Blit(resultTexture, source);
            }
        }

        private TracingPass m_TracingPass;
    
        public override void Create()
        {
            m_TracingPass = new TracingPass(RenderPassEvent.BeforeRenderingPostProcessing);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(m_TracingPass);
        }

        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            m_TracingPass.Setup(renderer.cameraColorTargetHandle);
        }
    }
}


