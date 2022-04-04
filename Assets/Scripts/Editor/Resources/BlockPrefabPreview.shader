Shader "Hidden/BlockPrefabPreview"
{
    Properties
    {
        [MainTexture] _BaseMap ("MainTex", 2D) = "White" { }
        [MainColor] _BaseColor ("BaseColor", Color) = (1, 1, 1, 1)

        _Cutoff ("_Cutoff (Alpha Cutoff)", Range(0.0, 1.0)) = 0.5
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalRenderPipeline" "RenderType" = "Opaque" }

        HLSLINCLUDE

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)

        float4 _BaseMap_ST;
        half4 _BaseColor;
        float _Cutoff;

        CBUFFER_END

        vector uvs[3];
        float scale;
        
        TEXTURE2D(_BaseMap);    SAMPLER(sampler_BaseMap);

        struct Attributes
        {
            float4 positionOS       : POSITION;
            float3 normalOS         : NORMAL;
            float2 uv               : TEXCOORD;
        };

        struct Varyings
        {
            float4 positionCS       : SV_POSITION;
            float3 normalWS         : NORMAL;
            float2 uv               : TEXCOORD;
        };

        float2 GetFinalUV(float side, float2 uv, float2 tiling, float4 offset, bool notBack = true)
        {
            const float4 scaledUV = float4(offset.x * tiling.x, 1 - (offset.y + 1) * tiling.x, offset.z * tiling.x, 1 - (offset.w + 1) * tiling.x);
            if(notBack)
                return side ? uv * tiling + scaledUV.xy : uv * tiling + scaledUV.zw;
            
            const float temp = (uv * tiling + scaledUV.zw).y - (1- (offset.w + 1) * tiling.x);
            const float dire = temp > 0 ? 1- offset.w * tiling.x - abs(temp) : 1- offset.w * tiling.x + abs(temp);
            return side ? uv * tiling + scaledUV.xy : float2((uv * tiling + scaledUV.zw).x, dire);
        }
        
        ENDHLSL

        pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature_local_fragment _ALPHATEST_ON
            
            Varyings vert(Attributes input)
            {
                Varyings output;

                const VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                const VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
            
                output.positionCS = vertexInput.positionCS;
                output.normalWS = normalInput.normalWS;
            
                if(abs(normalInput.normalWS.r) == 1)
                    output.uv = GetFinalUV(normalInput.normalWS.r > 0, TRANSFORM_TEX(input.uv, _BaseMap), scale, uvs[0]);                  
                else if(abs(normalInput.normalWS.g) == 1)
                    output.uv = GetFinalUV(normalInput.normalWS.g > 0, TRANSFORM_TEX(input.uv, _BaseMap), scale, uvs[1]);                   
                else
                    output.uv = GetFinalUV(normalInput.normalWS.b > 0, TRANSFORM_TEX(input.uv, _BaseMap), scale, uvs[2], false);
            
                return output;
            }

            half4 frag(Varyings input): SV_TARGET
            {
                float4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                clip(tex.a - _Cutoff);
                
                return tex;
            }

            ENDHLSL
        }
    }
}
