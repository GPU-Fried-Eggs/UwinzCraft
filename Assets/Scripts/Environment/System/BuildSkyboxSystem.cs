using Environment.Data;
using Environment.Interface;
using UnityEngine;
using UnityEngine.Rendering;

namespace Environment.System
{
    public class BuildSkyboxSystem : ISharedSystem
    {
        private SkyboxPrefab m_Data;
        private TimePrefab m_Time;

        public BuildSkyboxSystem()
        {
            m_Data = Resources.Load<SkyboxPrefab>("Skybox");
            m_Time = Resources.Load<TimePrefab>("Time");
        }

        private readonly Vector3 m_Br = new Vector3(0.00519673f, 0.0121427f, 0.0296453f);
        private readonly Vector3 m_Bm = new Vector3(0.005721017f, 0.004451339f, 0.003146905f);
        private readonly Vector3 m_MieG = new Vector3(0.4375f, 1.5625f, 1.5f);
        
        private float m_curveTime, m_gradientTime;

        private Transform m_HolderTransform, m_SunTransform, m_MoonTransform, m_LightTransform;

        private float m_cloudRotationSpeed = 0.0f;
        
        private Light m_lightComponent;
        private LensFlareComponentSRP m_SunFlareComponent;

        public void Init()
        {
            m_HolderTransform = GameObject.Find("World").transform;
            m_SunTransform = GameObject.Find("Sun").transform;
            m_MoonTransform = GameObject.Find("Moon").transform;
            m_LightTransform = GameObject.Find("Light").transform;
            
            m_lightComponent = m_LightTransform.GetComponent<Light>();
            m_SunFlareComponent = m_LightTransform.GetComponent<LensFlareComponentSRP>();
            
            m_Data.UpdateSkySettings();
            
            m_Data.skyMaterial.SetTexture(ShaderIDs.SunTexture, m_Data.sunTexture);
            m_Data.skyMaterial.SetTexture(ShaderIDs.MoonTexture, m_Data.moonTexture);
            m_Data.skyMaterial.SetTexture(ShaderIDs.CloudTexture, m_Data.cloudTexture);
            m_Data.skyMaterial.SetTexture(ShaderIDs.StarfieldTexture, m_Data.starfieldTexture);
            
            Refresh();
        }

        public void Refresh()
        {
            m_curveTime = (m_Data.setTimeByCurve) ? m_Data.dayLengthCurve.Evaluate(m_Time.time) : m_Time.time;
            m_gradientTime = m_curveTime / 24f;

            // Scattering
            Shader.SetGlobalVector(ShaderIDs.Br, m_Br * m_Data.rayleighCurve.Evaluate(m_curveTime));
            Shader.SetGlobalVector(ShaderIDs.Bm, m_Bm * m_Data.mieCurve.Evaluate(m_curveTime));
            Shader.SetGlobalFloat(ShaderIDs.Kr, m_Data.krCurve.Evaluate(m_curveTime));
            Shader.SetGlobalFloat(ShaderIDs.Km, m_Data.kmCurve.Evaluate(m_curveTime));
            Shader.SetGlobalFloat(ShaderIDs.Scattering, m_Data.scatteringCurve.Evaluate(m_curveTime));
            Shader.SetGlobalFloat(ShaderIDs.SunIntensity, m_Data.sunIntensityCurve.Evaluate(m_curveTime));
            Shader.SetGlobalFloat(ShaderIDs.NightIntensity, m_Data.nightIntensityCurve.Evaluate(m_curveTime));
            Shader.SetGlobalFloat(ShaderIDs.Exposure, m_Data.exposureCurve.Evaluate(m_curveTime));
            Shader.SetGlobalColor(ShaderIDs.RayleighColor, m_Data.rayleighGradientColor.Evaluate(m_gradientTime));
            Shader.SetGlobalColor(ShaderIDs.MieColor, m_Data.mieGradientColor.Evaluate(m_gradientTime));
            Shader.SetGlobalVector(ShaderIDs.MieG, m_MieG);

            // Night sky
            Shader.SetGlobalColor(ShaderIDs.MoonDiskColor, m_Data.moonDiskGradientColor.Evaluate(m_gradientTime));
            Shader.SetGlobalColor(ShaderIDs.MoonBrightColor, m_Data.moonBrightGradientColor.Evaluate(m_gradientTime));
            Shader.SetGlobalFloat(ShaderIDs.MoonBrightRange, Mathf.Lerp(150.0f, 5.0f, m_Data.moonBrightRangeCurve.Evaluate(m_curveTime)));
            Shader.SetGlobalFloat(ShaderIDs.StarfieldIntensity, m_Data.starfieldIntensityCurve.Evaluate(m_curveTime));
            Shader.SetGlobalFloat(ShaderIDs.MilkyWayIntensity, m_Data.milkyWayIntensityCurve.Evaluate(m_curveTime));
            Shader.SetGlobalVector(ShaderIDs.StarfieldColorBalance, m_Data.starfieldColorBalance);

            // Clouds
            Shader.SetGlobalColor(ShaderIDs.CloudColor, m_Data.cloudGradientColor.Evaluate(m_gradientTime));
            Shader.SetGlobalFloat(ShaderIDs.CloudScattering, m_Data.cloudScatteringCurve.Evaluate(m_curveTime));
            Shader.SetGlobalFloat(ShaderIDs.CloudExtinction, m_Data.cloudExtinctionCurve.Evaluate(m_curveTime));
            Shader.SetGlobalFloat(ShaderIDs.CloudPower, m_Data.cloudPowerCurve.Evaluate(m_curveTime));
            Shader.SetGlobalFloat(ShaderIDs.CloudIntensity, m_Data.cloudIntensityCurve.Evaluate(m_curveTime));
            if (m_Data.cloudRotationSpeed != 0f)
            {
                m_cloudRotationSpeed += m_Data.cloudRotationSpeed * Time.deltaTime;
                if (m_cloudRotationSpeed >= 1f)
                    m_cloudRotationSpeed = 0f;
            }
            Shader.SetGlobalFloat(ShaderIDs.CloudRotationSpeed, m_cloudRotationSpeed);
            
            // Fog scattering
            Shader.SetGlobalFloat(ShaderIDs.FogDistance, m_Data.fogDistanceCurve.Evaluate(m_curveTime));
            Shader.SetGlobalFloat(ShaderIDs.FogBlend, m_Data.fogBlend);
            Shader.SetGlobalFloat(ShaderIDs.MieDistance, m_Data.mieDistance);

            // Options
            Shader.SetGlobalFloat(ShaderIDs.SunDiskSize, Mathf.Lerp(5.0f, 1.0f, m_Data.sunDiskSize));
            Shader.SetGlobalFloat(ShaderIDs.MoonDiskSize, Mathf.Lerp(20.0f, 1.0f, m_Data.moonDiskSize));

            // Directions
            var sunLocalDirection = m_HolderTransform.InverseTransformDirection(m_SunTransform.transform.forward);
            var moonLocalDirection = m_HolderTransform.InverseTransformDirection(m_MoonTransform.transform.forward);
            Shader.SetGlobalVector(ShaderIDs.SunDirection, -sunLocalDirection);
            Shader.SetGlobalVector(ShaderIDs.MoonDirection, -moonLocalDirection);

            // Matrix
            Shader.SetGlobalMatrix(ShaderIDs.SkyUpDirectionMatrix, m_HolderTransform.worldToLocalMatrix);
            Shader.SetGlobalMatrix(ShaderIDs.SunMatrix, m_SunTransform.transform.worldToLocalMatrix);
            Shader.SetGlobalMatrix(ShaderIDs.MoonMatrix, m_MoonTransform.transform.worldToLocalMatrix);
            Shader.SetGlobalMatrix(ShaderIDs.StarfieldMatrix, Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(m_Data.starfieldPosition), Vector3.one).inverse);

            m_SunTransform.transform.localRotation = Quaternion.Euler((m_curveTime * 360.0f / 24.0f) - 90.0f, 180.0f, 0.0f);
            m_MoonTransform.transform.localRotation = Quaternion.LookRotation(-sunLocalDirection);
            
            if (Vector3.Dot(-m_SunTransform.transform.forward, m_HolderTransform.up) >= 0.0f)
            {
                m_LightTransform.transform.localRotation = Quaternion.LookRotation(sunLocalDirection);
                m_Data.onSunRise.Invoke();
            }
            else
            {
                m_LightTransform.transform.localRotation = Quaternion.LookRotation(moonLocalDirection);
                m_Data.onSunSet.Invoke();
            }
            
            // Lighting
            m_lightComponent.intensity = m_Data.lightIntensityCurve.Evaluate(m_curveTime);
            m_SunFlareComponent.intensity = m_Data.flareIntensityCurve.Evaluate(m_curveTime);
            m_lightComponent.color = m_Data.lightGradientColor.Evaluate(m_gradientTime);
            
            RenderSettings.ambientIntensity = m_Data.ambientIntensityCurve.Evaluate(m_curveTime);
        }
    }
}