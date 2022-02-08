
using Unity.Mathematics;

namespace Utilities
{
    public static class Noise
    {
        public static float FractalSimplex(float2 v, float frequency, int octaves, float amplitude = 1.0f, float lacunarity = 2.0f, float persistence = 0.5f)
        {
            float output = 0.0f, denom = 0.0f;

            for (int i = 0; i < octaves; i++)
            {
                output += amplitude * noise.snoise(v * new float2(frequency));
                denom += amplitude;
                frequency *= lacunarity;
                amplitude *= persistence;
            }

            return output / denom;   
        }
        
        public static float FractalSimplex(float3 v, float frequency, int octaves, float amplitude = 1.0f, float lacunarity = 2.0f, float persistence = 0.5f)
        {
            float output = 0.0f, denom = 0.0f;

            for (int i = 0; i < octaves; i++)
            {
                output += amplitude * noise.snoise(v * new float3(frequency));
                denom += amplitude;
                frequency *= lacunarity;
                amplitude *= persistence;
            }

            return output / denom;   
        }

        public static float ClampedSimplex(float2 v, float scale) => (noise.snoise(v * new float2(scale)) + 1) * 0.5f;

        public static float ClampedSimplex(float3 v, float scale) => (noise.snoise(v * new float3(scale)) + 1) * 0.5f;
    }
}
