Shader "Custom/RadialGradientBackground"
{
    Properties
    {
        _InnerColor ("Inner Color", Color) = (0.13, 0.11, 0.24, 1)
        _OuterColor ("Outer Color", Color) = (0.03, 0.03, 0.06, 1)
        _Radius ("Radius", Float) = 20
        _Center ("Center (world XZ)", Vector) = (0, 0, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry-100" }

        Pass
        {
            Name "Unlit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            float4 _InnerColor;
            float4 _OuterColor;
            float _Radius;
            float4 _Center;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = positionInputs.positionCS;
                OUT.positionWS = positionInputs.positionWS;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float dist = length(IN.positionWS.xz - _Center.xz) / _Radius;
                float3 color = lerp(_InnerColor.rgb, _OuterColor.rgb, saturate(dist));
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
