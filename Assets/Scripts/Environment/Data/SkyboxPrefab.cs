using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

namespace Environment.Data
{
    public enum CloudMode { Off, Static };

    [CreateAssetMenu(fileName = "New Skybox", menuName = "Skybox Prefab")]
    public class SkyboxPrefab : ScriptableObject
    {
        public bool setTimeByCurve = false;
        public AnimationCurve dayLengthCurve = AnimationCurve.Linear(0.0f, 0.0f, 24.0f, 24.0f);

        public Texture sunTexture;
        public Texture moonTexture;
        public Texture cloudTexture;
        public Cubemap starfieldTexture;
        public Material skyMaterial;
        
        // Scattering
        public AnimationCurve rayleighCurve = AnimationCurve.Linear(0.0f, 1.0f, 24.0f, 1.0f);
        public AnimationCurve mieCurve = AnimationCurve.Linear(0.0f, 1.0f, 24.0f, 1.0f);
        public AnimationCurve krCurve = AnimationCurve.Linear(0.0f, 8.4f, 24.0f, 8.4f);
        public AnimationCurve kmCurve = AnimationCurve.Linear(0.0f, 1.25f, 24.0f, 1.25f);
        public AnimationCurve scatteringCurve = AnimationCurve.Linear(0.0f, 15.0f, 24.0f, 15.0f);
        public AnimationCurve sunIntensityCurve = AnimationCurve.Linear(0.0f, 3.0f, 24.0f, 3.0f);
        public AnimationCurve nightIntensityCurve = AnimationCurve.Linear(0.0f, 0.5f, 24.0f, 0.5f);
        public AnimationCurve exposureCurve = AnimationCurve.Linear(0.0f, 2.0f, 24.0f, 2.0f);

        public Gradient rayleighGradientColor = new Gradient();
        public Gradient mieGradientColor = new Gradient();
        
        // Night Sky
        public Gradient moonDiskGradientColor = new Gradient();
        public Gradient moonBrightGradientColor = new Gradient();
        public AnimationCurve moonBrightRangeCurve = AnimationCurve.Linear(0.0f, 0.9f, 24.0f, 0.9f);
        public AnimationCurve starfieldIntensityCurve = AnimationCurve.Linear(0.0f, 0.0f, 24.0f, 0.0f);
        public AnimationCurve milkyWayIntensityCurve = AnimationCurve.Linear(0.0f, 0.0f, 24.0f, 0.0f);
        public Vector3 starfieldColorBalance = new Vector3(1.0f, 1.0f, 1.0f);
        public Vector3 starfieldPosition;
        
        // Clouds
        public Gradient cloudGradientColor = new Gradient();
        public AnimationCurve cloudScatteringCurve = AnimationCurve.Linear(0.0f, 1.0f, 24.0f, 1.0f);
        public AnimationCurve cloudExtinctionCurve = AnimationCurve.Linear(0.0f, 0.25f, 24.0f, 0.25f);
        public AnimationCurve cloudPowerCurve = AnimationCurve.Linear(0.0f, 2.2f, 24.0f, 2.2f);
        public AnimationCurve cloudIntensityCurve = AnimationCurve.Linear(0.0f, 1.0f, 24.0f, 1.0f);
        public float cloudRotationSpeed = 0.0f;

        // Fog scattering
        public AnimationCurve fogDistanceCurve = AnimationCurve.Linear(0.0f, 3500.0f, 24.0f, 3500.0f);
        public float fogBlend = 0.5f;
        public float mieDistance = 0.5f;
        
        // Lighting
        public AnimationCurve lightIntensityCurve = AnimationCurve.Linear(0.0f, 0.0f, 24.0f, 0.0f);
        public AnimationCurve flareIntensityCurve = AnimationCurve.Linear(0.0f, 0.0f, 24.0f, 0.0f);
        public Gradient lightGradientColor = new Gradient();
        public AnimationCurve ambientIntensityCurve = AnimationCurve.Linear(0.0f, 1.0f, 24.0f, 1.0f);

        // Options
        public float sunDiskSize = 0.5f;
        public float moonDiskSize = 0.5f;
        
        public CloudMode cloudMode = CloudMode.Static;
        
        // Environment Reflection
        public bool enableReflection = false;
        public int environmentReflectionResolution;
        public ReflectionProbeTimeSlicingMode environmentReflectionTimeSlicingMode;
        
        // Event
        public UnityEvent onSunRise;
        public UnityEvent onSunSet;

        public void UpdateSkySettings()
        {
            RenderSettings.skybox = skyMaterial;
            skyMaterial.shader = Shader.Find("UwinzCraft/Skybox");
            switch (cloudMode)
            {
                case CloudMode.Static:
                    skyMaterial.EnableKeyword("_ENABLE_CLOUD");
                    break;
                case CloudMode.Off:
                    skyMaterial.DisableKeyword("_ENABLE_CLOUD");
                    break;
            }
            skyMaterial.SetTexture(ShaderIDs.SunTexture, sunTexture);
            skyMaterial.SetTexture(ShaderIDs.MoonTexture, moonTexture);
            skyMaterial.SetTexture(ShaderIDs.CloudTexture, cloudTexture);
            skyMaterial.SetTexture(ShaderIDs.StarfieldTexture, starfieldTexture);
        }
    }
}