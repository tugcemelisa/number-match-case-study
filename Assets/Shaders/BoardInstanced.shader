Shader "Custom/BoardInstanced"
{
    Properties
    {
        _NumberAtlas ("Number Atlas", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_NumberAtlas);
            SAMPLER(sampler_NumberAtlas);

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
                UNITY_DEFINE_INSTANCED_PROP(float4, _CellUV)
            UNITY_INSTANCING_BUFFER_END(Props)

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = positionInputs.positionCS;
                OUT.positionWS = positionInputs.positionWS;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(Props, _BaseColor);
                float4 cellUV = UNITY_ACCESS_INSTANCED_PROP(Props, _CellUV);

                float3 normalWS = normalize(IN.normalWS);
                float3 lightDir = normalize(float3(0.3, 0.85, -0.45));
                float ndotl = saturate(dot(normalWS, lightDir)) * 0.6 + 0.4;

                float3 color = baseColor.rgb * ndotl;

                if (normalWS.y > 0.9 && baseColor.a > 0.5)
                {
                    float2 localUV = frac(IN.positionWS.xz);
                    float2 atlasUV = cellUV.xy + localUV * cellUV.zw;
                    float4 atlasSample = SAMPLE_TEXTURE2D(_NumberAtlas, sampler_NumberAtlas, atlasUV);
                    color = lerp(color, atlasSample.rgb, atlasSample.a);
                }

                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
