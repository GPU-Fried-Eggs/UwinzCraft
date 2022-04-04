using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

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

        public static float FractalPerlin(float2 v, float frequency, int octaves, float amplitude = 1.0f, float lacunarity = 2.0f, float persistence = 0.5f)
        {
            float output = 0.0f, denom = 0.0f;

            for (int i = 0; i < octaves; i++)
            {
                output += amplitude * noise.cnoise(v * new float2(frequency));
                denom += amplitude;
                frequency *= lacunarity;
                amplitude *= persistence;
            }

            return output / denom;
        }

        /// <summary>
        /// Slow Voronoi Noise
        /// https://towardsdatascience.com/replicating-minecraft-world-generation-in-python-1b491bc9b9a4
        /// </summary>
        /// <param name="v">position</param>
        /// <param name="scale"></param>
        /// <returns>x -> distance to closest point, yzw -> color 32 bit</returns>
        public static float4 Voronoi(float2 v, float scale)
        {
            var p = math.floor(v * scale);
            var f = math.frac(v * scale);
            var res = new float4(8.0f, 0.0f, 0.0f, 0.0f);

            for (int j = -1; j <= 1; j++)
                for (int i = -1; i <= 1; i++)
                {
                    var lattice = new float2(i, j);
                    var offset = math.frac(math.sin(math.mul(p + lattice, new float2x2(15.27f, 47.63f, 99.41f, 89.98f))) * 46839.32f);
                    var dist = math.lengthsq(lattice - f + offset);
                    var color = 0.5f + 0.5f * math.sin(math.frac(math.sin(math.dot(p + lattice, new float2(7.0f, 113.0f))) * 46839.32f) * 2.5f + 3.5f + new float3(2.0f, 3.0f, 0.0f));
                    if (dist < res.x)
                        res = new float4(dist, color * byte.MaxValue);
                }
            
            return res;
        }

        public static float ClampedSimplex(float2 v, float scale) => (noise.snoise(v * new float2(scale)) + 1) * 0.5f;

        public static float ClampedSimplex(float3 v, float scale) => (noise.snoise(v * new float3(scale)) + 1) * 0.5f;
    }
}
