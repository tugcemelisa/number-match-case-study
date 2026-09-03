Shader "Custom/BoardInstanced"
{
    Properties
    {
        _NumberAtlas ("Number Atlas", 2D) = "white" {}
        _NoiseTex ("Dissolve Noise", 2D) = "gray" {}
        _MaskColor ("Mask Color", Color) = (0.32, 0.32, 0.34, 1)
        _RimColor ("Rim Glow Color", Color) = (1.6, 1.2, 0.4, 1)
        _DissolveEdge ("Dissolve Edge Width", Range(0.01, 0.3)) = 0.09
        _GapColor ("Cell Gap Color", Color) = (0.08, 0.06, 0.15, 1)
        _ButtonMargin ("Cell Gap Width", Range(0.0, 0.2)) = 0.035
        _ButtonRadius ("Cell Corner Radius", Range(0.0, 0.25)) = 0.15
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
            float4 _GapColor;
            float _ButtonMargin;
            float _ButtonRadius;

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _TrueColor)
                UNITY_DEFINE_INSTANCED_PROP(float4, _CellUV)
                UNITY_DEFINE_INSTANCED_PROP(float, _RevealProgress)
                UNITY_DEFINE_INSTANCED_PROP(float, _Filled)
                UNITY_DEFINE_INSTANCED_PROP(float, _WrongFlashTime)
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
                float filled = UNITY_ACCESS_INSTANCED_PROP(Props, _Filled);
                float wrongFlashTime = UNITY_ACCESS_INSTANCED_PROP(Props, _WrongFlashTime);

                // Wrong-drop socket flash: fades out over 0.35s purely from
                // GPU time, so a single one-time CPU write (the moment the
                // wrong drop happens) is enough - no per-frame updates.
                float wrongFlash = saturate(1.0 - (_Time.y - wrongFlashTime) / 0.35);
                wrongFlash *= wrongFlash;

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
                    // Each cell is a thin rounded square with a small gap
                    // to its neighbors, in cell-local space. Signed
                    // distance to a rounded box: p is -0.5..0.5, boxHalf is
                    // the cell's half-size after leaving _ButtonMargin as
                    // the gap.
                    float2 p = localUV - 0.5;
                    float boxHalf = 0.5 - _ButtonMargin;
                    float2 d = abs(p) - (boxHalf - _ButtonRadius);
                    float dist = length(max(d, 0.0)) + min(max(d.x, d.y), 0.0) - _ButtonRadius;
                    float cellMask = 1 - smoothstep(-0.01, 0.01, dist);

                    // An empty cell reads as a recessed socket (dimmer
                    // toward its edges, like a shallow inset shadow); once
                    // a piece is placed (or the group reveals) it reads as
                    // raised instead (brighter, with a soft diagonal
                    // highlight) - the shading contrast alone tells the
                    // player what they've already placed, with no true
                    // color leaking before the whole group completes.
                    float edgeDist = boxHalf - max(abs(p.x), abs(p.y));
                    float recessed = lerp(0.42, 1.0, smoothstep(0.0, 0.34, edgeDist));
                    float lightGrad = saturate(0.5 - (p.x + p.y));
                    float raised = lerp(0.85, 1.28, lightGrad);
                    float placed = saturate(filled + revealed);
                    color *= lerp(recessed, raised, placed);

                    // Glossy bevel lip: a thin bright line hugging the
                    // top-left of the rounded edge and a thin dark line
                    // along the bottom-right, like a real molded button
                    // catching light - independent of fill state so even
                    // empty sockets read as more than a flat rectangle.
                    float rimBand = 1 - smoothstep(0.0, 0.045, edgeDist);
                    float rimLight = saturate(0.5 - (p.x + p.y) * 1.4);
                    color += rimBand * rimLight * 0.22;
                    color -= rimBand * (1 - rimLight) * 0.16;

                    float2 atlasUV = cellUV.xy + localUV * cellUV.zw;
                    float4 atlasSample = SAMPLE_TEXTURE2D(_NumberAtlas, sampler_NumberAtlas, atlasUV);
                    float numberAlpha = atlasSample.a * (1 - revealed) * (1 - saturate(progress)) * cellMask;
                    color = lerp(color, atlasSample.rgb, numberAlpha);

                    color = lerp(_GapColor.rgb, color, cellMask);

                    // Wrong-drop flash on top of everything else, clipped
                    // to this cell's own rounded shape.
                    color = lerp(color, float3(1.0, 0.2, 0.2), wrongFlash * cellMask);
                }

                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
