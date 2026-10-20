
Shader "DominoMase/PremiumDominoMetal"
{
    Properties
    {
        [Header(Metal Colors)]
        _BaseColor ("Metal Color", Color) = (0.757, 0.541, 0.188, 1)
        _DarkColor ("Shadow Metal Color", Color) = (0.18, 0.075, 0.012, 1)
        _Brightness ("Minimum Brightness", Range(0, 1)) = 0.38

        [Header(Real Lighting)]
        _Metallic ("Metallic", Range(0, 1)) = 0.9
        _Smoothness ("Smoothness", Range(0, 1)) = 0.85
        _LightStrength ("Main Light Strength", Range(0, 2)) = 0.7

        [Header(Studio Reflections)]
        _ReflectionColor ("Reflection Color", Color) = (1, 0.85, 0.5, 1)
        _ReflectionStrength ("Reflection Strength", Range(0, 4)) = 1.2
        _ReflectionSharpness ("Reflection Sharpness", Range(2, 256)) = 48
        _ReflectionAngle ("Reflection Angle", Range(0, 360)) = 45

        [Header(Second Reflection)]
        _SecondReflection ("Second Reflection Strength", Range(0, 3)) = 0.45
        _SecondAngle ("Second Reflection Angle", Range(0, 360)) = 135

        [Header(Edge Shine)]
        _FresnelColor ("Edge Shine Color", Color) = (1, 0.78, 0.35, 1)
        _FresnelStrength ("Edge Shine Strength", Range(0, 3)) = 0.5
        _FresnelPower ("Edge Shine Sharpness", Range(1, 12)) = 4

        [Header(Final Appearance)]
        _Contrast ("Contrast", Range(0.5, 2)) = 1.1
        _Exposure ("Exposure", Range(0.2, 3)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)

                float4 _BaseColor;
                float4 _DarkColor;

                float _Brightness;
                float _Metallic;
                float _Smoothness;
                float _LightStrength;

                float4 _ReflectionColor;
                float _ReflectionStrength;
                float _ReflectionSharpness;
                float _ReflectionAngle;

                float _SecondReflection;
                float _SecondAngle;

                float4 _FresnelColor;
                float _FresnelStrength;
                float _FresnelPower;

                float _Contrast;
                float _Exposure;

            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs pos =
                    GetVertexPositionInputs(input.positionOS.xyz);

                VertexNormalInputs normals =
                    GetVertexNormalInputs(input.normalOS);

                output.positionCS = pos.positionCS;
                output.positionWS = pos.positionWS;
                output.normalWS = normals.normalWS;

                return output;
            }

            float3 GetStudioDirection(float angle)
            {
                float radians = angle * 0.01745329252;

                return normalize(float3(
                    sin(radians),
                    0.65,
                    cos(radians)
                ));
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 N = normalize(input.normalWS);

                float3 V = normalize(
                    GetWorldSpaceViewDir(input.positionWS)
                );

                Light mainLight = GetMainLight();

                float3 L = normalize(mainLight.direction);

                float NdotL = saturate(dot(N, L));

                // Keep metal visible even on shadow-facing sides.
                float lighting = lerp(
                    _Brightness,
                    1.0,
                    NdotL
                );

                lighting = lerp(
                    1.0,
                    lighting,
                    saturate(_LightStrength)
                );

                float3 metalColor = lerp(
                    _DarkColor.rgb,
                    _BaseColor.rgb,
                    saturate(lighting)
                );

                // First artificial studio reflection.
                float3 studioDir =
                    GetStudioDirection(_ReflectionAngle);

                float3 H1 = normalize(V + studioDir);

                float spec1 = pow(
                    saturate(dot(N, H1)),
                    max(2.0, _ReflectionSharpness *
                        lerp(0.25, 1.0, _Smoothness))
                );

                // Second artificial studio reflection.
                float3 studioDir2 =
                    GetStudioDirection(_SecondAngle);

                float3 H2 = normalize(V + studioDir2);

                float spec2 = pow(
                    saturate(dot(N, H2)),
                    max(2.0, _ReflectionSharpness * 0.4)
                );

                // Main directional light highlight.
                float3 H = normalize(V + L);

                float realSpec = pow(
                    saturate(dot(N, H)),
                    lerp(8.0, 160.0, _Smoothness)
                );

                float3 reflection =
                    _ReflectionColor.rgb *
                    (
                        spec1 * _ReflectionStrength +
                        spec2 * _SecondReflection
                    );

                reflection *= lerp(0.35, 1.0, _Metallic);

                float3 lightSpec =
                    _ReflectionColor.rgb *
                    realSpec *
                    _Metallic *
                    _LightStrength *
                    NdotL;

                // Fresnel highlights grazing edges.
                float fresnel = pow(
                    1.0 - saturate(dot(N, V)),
                    _FresnelPower
                );

                float3 edgeShine =
                    _FresnelColor.rgb *
                    fresnel *
                    _FresnelStrength;

                float3 finalColor =
                    metalColor +
                    reflection +
                    lightSpec +
                    edgeShine;

                // Final color controls.
                finalColor =
                    (finalColor - 0.5) * _Contrast + 0.5;

                finalColor *= _Exposure;

                return half4(max(finalColor, 0.0), 1.0);
            }

            ENDHLSL
        }
    }

    FallBack Off
}
