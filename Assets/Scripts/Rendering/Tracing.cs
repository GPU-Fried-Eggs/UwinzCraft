using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;
// ReSharper disable InconsistentNaming

namespace Rendering
{
    public class Tracing : MonoBehaviour
    {
        #region Settings

        [SerializeField] private ComputeShader RayTracingShader;
        
        [SerializeField] private Light DirectionalLight;
        [SerializeField] private Light[] PointLights;
        
        [SerializeField] private Texture SkyboxTexture;
        [SerializeField, Range(0f, 10f)] private float SkyboxIntensity = 1.0f;
        
        [SerializeField, Range(2, 20)] private int TraceDepth = 5;
        [SerializeField, Range(0.01f, 100f)] private float CameraFocalDistance = 1.0f;
        [SerializeField, Range(0f, 2f)] private float CameraAperture = 0.0f;

        #endregion

        private RenderTexture frameTarget;

        private Camera mainCamera;
        private int sampleCount;
        
        private readonly int dispatchGroupX = 32, dispatchGroupY = 32;
        private Vector2 dispatchOffsetLimit;
        private Vector4 dispatchCount;

        private Vector3 directionalLightInfo;
        private Vector4 directionalLightColorInfo;
        // angles in radians
        private float directionalLightYaw = 0.0f;
        private float directionalLightPitch = 0.0f;
        // point lights
        private int pointLightsCount;
        private ComputeBuffer pointLightsBuffer;

        private void Awake()
        {
            // get main camera in the scene
            mainCamera = GetComponent<Camera>();
            // update lights in the scene
            UpdateLights();
            // init directional light pitch and yaw
            var rot = DirectionalLight.transform.eulerAngles;
            directionalLightPitch = -rot.x * Mathf.Deg2Rad;
            directionalLightYaw = 0.5f * Mathf.PI - rot.y * Mathf.Deg2Rad;
        }

        private void Start()
        {
            // init sample counts
            ResetSamples();
        }

        private void Update()
        {
            ResetSamples();
            UpdateLights();
        }

        private void OnDestroy()
        {
            if (frameTarget != null) frameTarget.Release();
            if (pointLightsBuffer != null) pointLightsBuffer.Release();
        }

        public void Render(CommandBuffer cmd)
        {
            // check if textures are ready
            ValidateTextures();
            // set shader parameters
            SetShaderParameters(RayTracingShader, 1000);
            // set frame target
            RayTracingShader.SetTexture(0, "_FrameTarget", frameTarget);
            // dispatch and generate frame
            RayTracingShader.Dispatch(0, dispatchGroupX, dispatchGroupY, 1);
            // to screen
            cmd.Blit(frameTarget, BuiltinRenderTextureType.CurrentActive); // 直接输出
            // update sample count
            IncrementDispatchCount();
        }

        private void EstimateGroups(int width, int height)
        {
            // target dispatch 32x32 groups each group has 8x8 threads
            dispatchOffsetLimit = new Vector2(width - dispatchGroupX * 8, height - dispatchGroupY * 8);
            dispatchOffsetLimit = Vector2.Max(dispatchOffsetLimit, Vector2.zero);
            dispatchCount = new Vector4(0.0f, 0.0f, Mathf.Ceil(width / (float)(dispatchGroupX * 8)), Mathf.Ceil(height / (float)(dispatchGroupY * 8)));
        }
        
        private void ValidateTextures()
        {
            // if frame target is not initialized or screen size has changed reinitialize
            if(frameTarget == null || frameTarget.width != Screen.width || frameTarget.height != Screen.height)
            {
                if (frameTarget != null) frameTarget.Release();
                frameTarget = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear) { enableRandomWrite = true };
                frameTarget.Create();
                EstimateGroups(Screen.width, Screen.height);
            }
        }
        
        private void SetShaderParameters(ComputeShader shader, int targetCount)
        {
            // random pixel offset
            shader.SetVector("_PixelOffset", GeneratePixelOffset());
            // trace depth
            shader.SetInt("_TraceDepth", TraceDepth);
            // frame count
            shader.SetInt("_FrameCount", sampleCount);
            // only update these parameters if redraw
            if (sampleCount % targetCount == 0)
            {
                // set camera info
                shader.SetVector("_CameraPos", mainCamera.transform.position);
                shader.SetVector("_CameraUp", mainCamera.transform.up);
                shader.SetVector("_CameraRight", mainCamera.transform.right);
                shader.SetVector("_CameraForward", mainCamera.transform.forward);
                shader.SetVector("_CameraInfo", new Vector4(
                    Mathf.Tan(Mathf.Deg2Rad * mainCamera.fieldOfView * 0.5f),
                    CameraFocalDistance,
                    CameraAperture,
                    frameTarget.width / (float)frameTarget.height));
                // set directional light
                shader.SetVector("_DirectionalLight", directionalLightInfo);
                shader.SetVector("_DirectionalLightColor", directionalLightColorInfo);
                // set point lights
                shader.SetBuffer(0, "_PointLights", pointLightsBuffer);
                shader.SetInt("_PointLightsCount", pointLightsCount);
                // set skybox and intensity
                shader.SetTexture(0, "_SkyboxTexture", SkyboxTexture);
                shader.SetFloat("_SkyboxIntensity", SkyboxIntensity);
                // set directional light
                shader.SetVector("_DirectionalLight", directionalLightInfo);
                shader.SetVector("_DirectionalLightColor", directionalLightColorInfo);
                // set point lights
                shader.SetBuffer(0, "_PointLights", pointLightsBuffer);
                shader.SetInt("_PointLightsCount", pointLightsCount);
            }
        }

        private void UpdateLights()
        {
            var direction = DirectionalLight.transform.forward;
            directionalLightInfo = Vector3.Normalize(new Vector3(-direction.x, -direction.y, -direction.z));
            var color = DirectionalLight.color;
            directionalLightColorInfo = new Vector4(color.r, color.g, color.b, DirectionalLight.intensity);
            // prepare point lights
            pointLightsBuffer?.Release();
            List<Vector4> pointLightsPosColor = new List<Vector4>();
            foreach(var pointLight in PointLights)
            {
                if (pointLight.type != LightType.Point) continue;
                pointLightsCount++;
                var pointLightPosition = pointLight.transform.position;
                var pointLightColor = pointLight.color;
                pointLightsPosColor.Add(new Vector4(pointLightPosition.x, pointLightPosition.y, pointLightPosition.z, pointLight.range));
                pointLightsPosColor.Add(new Vector4(pointLightColor.r, pointLightColor.g, pointLightColor.b, pointLight.intensity));
            }
            // if no point light, insert empty vector to make buffer happy
            if (pointLightsCount == 0) pointLightsPosColor.Add(Vector4.zero);
            pointLightsBuffer = new ComputeBuffer(pointLightsPosColor.Count, 4 * sizeof(float));
            pointLightsBuffer.SetData(pointLightsPosColor);
        }
        
        private void ResetSamples() => sampleCount = 0;

        private Vector2 GeneratePixelOffset()
        {
            // first create offset for camera pixel
            var offset = new Vector2(Random.value, Random.value);
            offset.x += dispatchCount.x * dispatchGroupX * 8;
            offset.y += dispatchCount.y * dispatchGroupY * 8;
            return offset;
        }
        
        private void IncrementDispatchCount()
        {
            dispatchCount.x += 1.0f;
            if(dispatchCount.x >= dispatchCount.z)
            {
                dispatchCount.x = 0.0f;
                dispatchCount.y += 1.0f;
                if(dispatchCount.y >= dispatchCount.w)
                {
                    dispatchCount.x = 0.0f;
                    dispatchCount.y = 0.0f;
                    sampleCount++;
                }
            }
        }
    }
}