Shader "Custom/BoardInstanced"
{
    Properties
    {
        _NumberAtlas ("Number Atlas", 2D) = "white" {}
        _NoiseTex ("Dissolve Noise", 2D) = "gray" {}
        _MaskColor ("Mask Color", Color) = (0.32, 0.32, 0.34, 1)
        _RimColor ("Rim Glow Color", Color) = (1.6, 1.2, 0.4, 1)
        _DissolveEdge ("Dissolve Edge Width", Range(0.01, 0.3)) = 0.09
        _GridLineColor ("Grid Line Color", Color) = (0.08, 0.03, 0.18, 1)
        _GridLineWidth ("Grid Line Width", Range(0.0, 0.15)) = 0.045
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
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            float4 _MaskColor;
            float4 _RimColor;
            float _DissolveEdge;
            float4 _GridLineColor;
            float _GridLineWidth;

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _TrueColor)
                UNITY_DEFINE_INSTANCED_PROP(float4, _CellUV)
                UNITY_DEFINE_INSTANCED_PROP(float, _RevealProgress)
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
                float4 trueColor = UNITY_ACCESS_INSTANCED_PROP(Props, _TrueColor);
                float4 cellUV = UNITY_ACCESS_INSTANCED_PROP(Props, _CellUV);
                float progress = UNITY_ACCESS_INSTANCED_PROP(Props, _RevealProgress);

                float3 normalWS = normalize(IN.normalWS);
                float3 lightDir = normalize(float3(0.3, 0.85, -0.45));
                float ndotl = saturate(dot(normalWS, lightDir)) * 0.6 + 0.4;

                // Cube instances are centered exactly on integer cell
                // coordinates (cell (x,z) spans world [x-0.5,x+0.5]), but
                // the baked atlas / noise tiling and _CellUV both use the
                // [x,x+1) convention - the +0.5 shift lines the wrap
                // boundary up with the cube's actual edges.
                float2 localUV = frac(IN.positionWS.xz + 0.5);

                // Per-cell noise offset (derived from the cell's own atlas
                // UV) so neighboring cells don't dissolve in lockstep.
                float2 noiseUV = localUV + cellUV.xy * 13.37;
                float n = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV).r;

                float revealed = progress > 0.001 ? step(n, progress) : 0;
                float rim = progress > 0.001 ? (step(progress, n) * step(n, progress + _DissolveEdge)) : 0;

                float3 baseColor = lerp(_MaskColor.rgb, trueColor.rgb, revealed) * ndotl;
                float3 color = lerp(baseColor, _RimColor.rgb, rim);

                if (normalWS.y > 0.9)
                {
                    float2 atlasUV = cellUV.xy + localUV * cellUV.zw;
                    float4 atlasSample = SAMPLE_TEXTURE2D(_NumberAtlas, sampler_NumberAtlas, atlasUV);
                    float numberAlpha = atlasSample.a * (1 - revealed) * (1 - saturate(progress));
                    color = lerp(color, atlasSample.rgb, numberAlpha);

                    // Thin dark line at each cell's edge, like a Sudoku
                    // grid, so masked cells read as individual squares
                    // instead of loose floating numbers.
                    float edgeDist = min(min(localUV.x, 1 - localUV.x), min(localUV.y, 1 - localUV.y));
                    float gridLine = 1 - smoothstep(0, _GridLineWidth, edgeDist);
                    color = lerp(color, _GridLineColor.rgb, gridLine);
                }

                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
