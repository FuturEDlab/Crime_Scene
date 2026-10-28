// Map border feedback grid (MapBoundary.cs).
//
// Transparent grid that is only visible around _MapBoundaryPoint, a global set
// every frame by MapBoundary: xyz = the player's head, w = intensity (0 far
// away, 1 touching or past the border). UVs are in metres.
Shader "CrimeScene/MapBoundary"
{
    Properties
    {
        _Color ("Color", Color) = (0.25, 0.8, 1, 1)
        _GridSize ("Grid Cell Size (m)", Float) = 0.4
        _LineWidth ("Line Width (m)", Range(0.005, 0.1)) = 0.025
        _FillAlpha ("Fill Alpha", Range(0, 1)) = 0.12
        _Radius ("Reveal Radius (m)", Float) = 1.6
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "BoundaryGrid"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _GridSize;
                float _LineWidth;
                half _FillAlpha;
                float _Radius;
            CBUFFER_END

            float4 _MapBoundaryPoint;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // Fade out with distance from the player's head.
                float distanceToPlayer = distance(input.positionWS, _MapBoundaryPoint.xyz);
                float reveal = saturate(1.0 - distanceToPlayer / _Radius);
                reveal *= reveal;
                reveal *= _MapBoundaryPoint.w;

                // Distance (m) to the nearest grid line, anti-aliased.
                float2 cell = abs(frac(input.uv / _GridSize + 0.5) - 0.5) * _GridSize;
                float lineDistance = min(cell.x, cell.y);
                float aa = max(fwidth(lineDistance), 1e-4);
                float gridLine = 1.0 - smoothstep(_LineWidth - aa, _LineWidth + aa, lineDistance);

                half alpha = saturate(gridLine + _FillAlpha) * reveal * _Color.a;
                return half4(_Color.rgb, alpha);
            }
            ENDHLSL
        }
    }
}
