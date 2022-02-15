using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using Utilities;

public class NoiseTextureViewer : EditorWindow
{
    private enum NoiseShape { Perlin, Simplex }

    private enum LayerMode { Multiply, Add, Subtract, Divide, Max, Min }

    private struct NoiseLayer
    {
        public LayerMode mode;
        public NoiseShape shape;
        public float frequency;
        public int octaves;
        public float amplitude;
        public float lacunarity;
        public float persistence;

        public NoiseLayer(LayerMode mode, NoiseShape shape, float frequency) : this(mode, shape, frequency, 0, 1.0f, 2.0f, 0.5f) { }
        
        public NoiseLayer(LayerMode mode, NoiseShape shape, float frequency, int octaves) : this(mode, shape, frequency, octaves, 1.0f, 2.0f, 0.5f) { }

        public NoiseLayer(LayerMode mode, NoiseShape shape, float frequency, int octaves, float amplitude, float lacunarity, float persistence)
            => (this.mode, this.shape, this.frequency, this.octaves, this.amplitude, this.lacunarity, this.persistence) = (mode, shape, frequency, octaves, amplitude, lacunarity, persistence);
    }

    private List<NoiseLayer> settings = new List<NoiseLayer>(15);

    private Texture2D m_Texture = null;

    private int2 m_Resolution = 128;
    
    [MenuItem("Window/Noise Viewer")]
    private static void ShowWindow() => GetWindow(typeof(NoiseTextureViewer));

    private void OnEnable()
    {
        
        
    }

    private void OnGUI()
    {
        EditorGUI.DrawPreviewTexture(GUILayoutUtility.GetRect(position.x, position.y), m_Texture);
    }

    private IEnumerator Paint()
    {
        m_Texture = new Texture2D(m_Resolution.x, m_Resolution.y);
        var result = new NativeArray<float>(m_Resolution.x * m_Resolution.y, Allocator.TempJob);
        var jobHandle = new GenerateNoiseTexture
        {
            layers = settings.ToNativeArray(new AllocatorManager.AllocatorHandle {Index = 3}),
            resolution = m_Resolution,
            buffer = result
        }.Schedule(m_Resolution.x * m_Resolution.y, 64);
        m_Texture.SetPixelData(result, 0);
        
        int frameCount = 1;
        yield return new WaitUntil(() =>
        {
            frameCount++;
            return jobHandle.IsCompleted || frameCount >= 4;
        });

        jobHandle.Complete();
    }
    
    [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
    private struct GenerateNoiseTexture : IJobParallelFor
    {
        [ReadOnly] public NativeArray<NoiseLayer> layers;
        [ReadOnly] public int2 resolution;
        
        public NativeArray<float> buffer;

        public void Execute(int index)
        {
            var position = index.To2DIndex(resolution.x);

            for (int i = 0, iMax = layers.Length; i < iMax; i++)
            {
                var frequency = layers[i].frequency;
                var octaves = layers[i].octaves;
                var temp = 0f;
                if (layers[i].octaves == 0) // pure noise
                {
                    switch (layers[i].mode)
                    {
                        case LayerMode.Add:
                            temp = layers[i].shape switch
                            {
                                NoiseShape.Perlin => noise.cnoise(position * new float2(frequency)),
                                NoiseShape.Simplex => noise.snoise(position * new float2(frequency)),
                                _ => 0
                            };
                            buffer[index] += temp;
                            break;
                        case LayerMode.Multiply:
                            temp = layers[i].shape switch
                            {
                                NoiseShape.Perlin => noise.cnoise(position * new float2(frequency)),
                                NoiseShape.Simplex => noise.snoise(position * new float2(frequency)),
                                _ => 0
                            };
                            buffer[index] *= temp;
                            break;
                        case LayerMode.Subtract:
                            temp = layers[i].shape switch
                            {
                                NoiseShape.Perlin => noise.cnoise(position * new float2(frequency)),
                                NoiseShape.Simplex => noise.snoise(position * new float2(frequency)),
                                _ => 0
                            };
                            buffer[index] -= temp;
                            break;
                        case LayerMode.Divide:
                            temp = layers[i].shape switch
                            {
                                NoiseShape.Perlin => noise.cnoise(position * new float2(frequency)),
                                NoiseShape.Simplex => noise.snoise(position * new float2(frequency)),
                                _ => 0
                            };
                            buffer[index] /= temp == 0 ? 1 : temp;
                            break;
                        case LayerMode.Max:
                            temp = layers[i].shape switch
                            {
                                NoiseShape.Perlin => noise.cnoise(position * new float2(frequency)),
                                NoiseShape.Simplex => noise.snoise(position * new float2(frequency)),
                                _ => 0
                            };
                            buffer[index] = buffer[index] > temp ? buffer[index] : temp;
                            break;
                        case LayerMode.Min:
                            temp = layers[i].shape switch
                            {
                                NoiseShape.Perlin => noise.cnoise(position * new float2(frequency)),
                                NoiseShape.Simplex => noise.snoise(position * new float2(frequency)),
                                _ => 0
                            };
                            buffer[index] = buffer[index] < temp ? buffer[index] : temp;
                            break;
                    }
                }
                else // fractal noise
                {
                    var amplitude = layers[i].amplitude; var lacunarity = layers[i].lacunarity; var persistence = layers[i].persistence;
                    switch (layers[i].mode)
                    {
                        case LayerMode.Add:
                            temp = layers[i].shape switch
                            {
                                NoiseShape.Perlin => Noise.FractalPerlin(position, frequency, octaves, amplitude, lacunarity, persistence),
                                NoiseShape.Simplex => Noise.FractalSimplex(position, frequency, octaves, amplitude, lacunarity, persistence),
                                _ => 0
                            };
                            buffer[index] += temp;
                            break;
                        case LayerMode.Multiply:
                            temp = layers[i].shape switch
                            {
                                NoiseShape.Perlin => Noise.FractalPerlin(position, frequency, octaves, amplitude, lacunarity, persistence),
                                NoiseShape.Simplex => Noise.FractalSimplex(position, frequency, octaves, amplitude, lacunarity, persistence),
                                _ => 0
                            };
                            buffer[index] *= temp;
                            break;
                        case LayerMode.Subtract:
                            temp = layers[i].shape switch
                            {
                                NoiseShape.Perlin => Noise.FractalPerlin(position, frequency, octaves, amplitude, lacunarity, persistence),
                                NoiseShape.Simplex => Noise.FractalSimplex(position, frequency, octaves, amplitude, lacunarity, persistence),
                                _ => 0
                            };
                            buffer[index] -= temp;
                            break;
                        case LayerMode.Divide:
                            temp = layers[i].shape switch
                            {
                                NoiseShape.Perlin => Noise.FractalPerlin(position, frequency, octaves, amplitude, lacunarity, persistence),
                                NoiseShape.Simplex => Noise.FractalSimplex(position, frequency, octaves, amplitude, lacunarity, persistence),
                                _ => 0
                            };
                            buffer[index] /= temp == 0 ? 1 : temp;
                            break;
                        case LayerMode.Max:
                            temp = layers[i].shape switch
                            {
                                NoiseShape.Perlin => Noise.FractalPerlin(position, frequency, octaves, amplitude, lacunarity, persistence),
                                NoiseShape.Simplex => Noise.FractalSimplex(position, frequency, octaves, amplitude, lacunarity, persistence),
                                _ => 0
                            };
                            buffer[index] = buffer[index] > temp ? buffer[index] : temp;
                            break;
                        case LayerMode.Min:
                            temp = layers[i].shape switch
                            {
                                NoiseShape.Perlin => Noise.FractalPerlin(position, frequency, octaves, amplitude, lacunarity, persistence),
                                NoiseShape.Simplex => Noise.FractalSimplex(position, frequency, octaves, amplitude, lacunarity, persistence),
                                _ => 0
                            };
                            buffer[index] = buffer[index] < temp ? buffer[index] : temp;
                            break;
                    }
                }
            }
        }
    }
}
