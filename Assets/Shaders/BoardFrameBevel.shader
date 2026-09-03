Shader "Custom/BoardFrameBevel"
{
    // Fake-3D platform border: no real geometry depth, just a highlight/
    // shadow gradient across the strip's short axis so it reads as a
    // chunky raised ledge even under a perfectly top-down camera.
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.19, 0.14, 0.32, 1)
        _HighlightColor ("Highlight Color", Color) = (0.46, 0.37, 0.68, 1)
        _ShadowColor ("Shadow Color", Color) = (0.03, 0.02, 0.07, 1)
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

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float4 _BaseColor;
            float4 _HighlightColor;
            float4 _ShadowColor;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // "Pillow" cross-section across the strip's short
                // (thickness) axis: bright ridge down the middle, dark at
                // both outer edges - reads as a raised, rounded bar
                // regardless of which way the strip is rotated, so left/
                // right/top/bottom strips all need the same UV axis.
                float centerDist = abs(IN.uv.y - 0.5) * 2;
                float3 color = lerp(_HighlightColor.rgb, _BaseColor.rgb, smoothstep(0.0, 0.55, centerDist));
                color = lerp(color, _ShadowColor.rgb, smoothstep(0.6, 1.0, centerDist));

                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
