// Skybox for the Main_Menu "soft void": a dark top fading into a soft, lighter
// haze at the horizon, then back down to a dark ground colour. The horizon
// colour should match the scene fog so the floor melts into the sky.
//
// A tiny screen-space dither hides the banding that smooth dark gradients get
// on an 8-bit headset display.
Shader "CrimeScene/MainMenuVoidSkybox"
{
    Properties
    {
        _TopColor ("Top Color", Color) = (0.035, 0.038, 0.05, 1)
        _HorizonColor ("Horizon Color", Color) = (0.16, 0.17, 0.2, 1)
        _BottomColor ("Bottom Color", Color) = (0.06, 0.062, 0.075, 1)
        _TopExponent ("Top Falloff", Range(0.1, 4)) = 0.6
        _BottomExponent ("Bottom Falloff", Range(0.1, 4)) = 0.5
        _Dither ("Dither (1/255 steps)", Range(0, 4)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Background"
            "Queue" = "Background"
            "PreviewType" = "Skybox"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Cull Off
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _TopColor;
                half4 _HorizonColor;
                half4 _BottomColor;
                half _TopExponent;
                half _BottomExponent;
                half _Dither;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 direction : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.direction = input.positionOS.xyz;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half y = normalize(input.direction).y;
                half3 color = y >= 0
                    ? lerp(_HorizonColor.rgb, _TopColor.rgb, pow(saturate(y), _TopExponent))
                    : lerp(_HorizonColor.rgb, _BottomColor.rgb, pow(saturate(-y), _BottomExponent));

                // Interleaved gradient noise, +-0.5 of an 8-bit display step
                // per unit (applied in sRGB, where the display quantises).
                float noise = frac(52.9829189 * frac(dot(input.positionCS.xy, float2(0.06711056, 0.00583715))));
                half3 srgb = LinearToSRGB(color) + (noise - 0.5) * _Dither / 255.0;

                return half4(SRGBToLinear(max(srgb, 0)), 1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
