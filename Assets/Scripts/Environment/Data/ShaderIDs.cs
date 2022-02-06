using UnityEngine;

namespace Environment.Data
{
    public class ShaderIDs
    {
        internal static readonly int AtlasX = Shader.PropertyToID("_AtlasX");
        internal static readonly int AtlasY = Shader.PropertyToID("_AtlasY");
        internal static readonly int AtlasRec = Shader.PropertyToID("_AtlasRec");

        // Textures
        internal static readonly int SunTexture = Shader.PropertyToID("_SunTexture");
        internal static readonly int MoonTexture = Shader.PropertyToID("_MoonTexture");
        internal static readonly int CloudTexture = Shader.PropertyToID("_CloudTexture");
        internal static readonly int StarfieldTexture = Shader.PropertyToID("_StarfieldTexture");
        
        // Scattering
        internal static readonly int Br = Shader.PropertyToID("_Br");
        internal static readonly int Bm = Shader.PropertyToID("_Bm");
        internal static readonly int Kr = Shader.PropertyToID("_Kr");
        internal static readonly int Km = Shader.PropertyToID("_Km");
        internal static readonly int Scattering = Shader.PropertyToID("_Scattering");
        internal static readonly int SunIntensity = Shader.PropertyToID("_SunIntensity");
        internal static readonly int NightIntensity = Shader.PropertyToID("_NightIntensity");
        internal static readonly int Exposure = Shader.PropertyToID("_Exposure");
        internal static readonly int RayleighColor = Shader.PropertyToID("_RayleighColor");
        internal static readonly int MieColor = Shader.PropertyToID("_MieColor");
        internal static readonly int MieG = Shader.PropertyToID("_MieG");

        // Night sky
        internal static readonly int MoonDiskColor = Shader.PropertyToID("_MoonDiskColor");
        internal static readonly int MoonBrightColor = Shader.PropertyToID("_MoonBrightColor");
        internal static readonly int MoonBrightRange = Shader.PropertyToID("_MoonBrightRange");
        internal static readonly int StarfieldIntensity = Shader.PropertyToID("_StarfieldIntensity");
        internal static readonly int MilkyWayIntensity = Shader.PropertyToID("_MilkyWayIntensity");
        internal static readonly int StarfieldColorBalance = Shader.PropertyToID("_StarfieldColorBalance");

        // Clouds
        internal static readonly int CloudColor = Shader.PropertyToID("_CloudColor");
        internal static readonly int CloudScattering = Shader.PropertyToID("_CloudScattering");
        internal static readonly int CloudExtinction = Shader.PropertyToID("_CloudExtinction");
        internal static readonly int CloudPower = Shader.PropertyToID("_CloudPower");
        internal static readonly int CloudIntensity = Shader.PropertyToID("_CloudIntensity");
        internal static readonly int CloudRotationSpeed = Shader.PropertyToID("_CloudRotationSpeed");

        // Fog scattering
        internal static readonly int FogDistance = Shader.PropertyToID("_FogDistance");
        internal static readonly int FogBlend = Shader.PropertyToID("_FogBlend");
        internal static readonly int MieDistance = Shader.PropertyToID("_MieDistance");

        // Options
        internal static readonly int SunDiskSize = Shader.PropertyToID("_SunDiskSize");
        internal static readonly int MoonDiskSize = Shader.PropertyToID("_MoonDiskSize");

        // Directions
        internal static readonly int SunDirection = Shader.PropertyToID("_SunDirection");
        internal static readonly int MoonDirection = Shader.PropertyToID("_MoonDirection");

        // Matrix
        internal static readonly int SkyUpDirectionMatrix = Shader.PropertyToID("_SkyUpDirectionMatrix");
        internal static readonly int SunMatrix = Shader.PropertyToID("_SunMatrix");
        internal static readonly int MoonMatrix = Shader.PropertyToID("_MoonMatrix");
        internal static readonly int StarfieldMatrix = Shader.PropertyToID("_StarfieldMatrix");
    }
}