Shader "Custom/RevealLavaGround"
{
    Properties
    {
        [Header(Reveal)]
        _MainTexture ("Hidden Image", 2D) = "white" {}
        _RevealMask ("Reveal Mask", 2D) = "black" {}

        [Header(Lava)]
        _RockColor ("Rock Color", Color) = (0.015, 0.01, 0.01, 1)
        [HDR]_LavaColor ("Lava Color", Color) = (1.0, 0.12, 0.0, 1)

        _LavaIntensity ("Lava Intensity", Range(0, 10)) = 3
        _LavaScale ("Voronoi Scale", Range(0.1, 20)) = 4

        _CrackWidth ("Crack Width", Range(0.01, 0.5)) = 0.12
        _EdgeSoftness ("Edge Softness", Range(0.001, 0.3)) = 0.06

        [Header(Animation)]
        _FlowSpeed ("Top To Bottom Speed", Range(-3, 3)) = 0.25

        _DistortionStrength ("Distortion", Range(0, 1)) = 0.15

        [Header(Image)]
        _ImageBrightness ("Image Brightness", Range(0, 2)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "Queue"="Geometry"
            "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            Name "ForwardUnlit"

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTexture);
            SAMPLER(sampler_MainTexture);

            TEXTURE2D(_RevealMask);
            SAMPLER(sampler_RevealMask);

            CBUFFER_START(UnityPerMaterial)

                float4 _MainTexture_ST;
                float4 _RevealMask_ST;

                float4 _RockColor;
                float4 _LavaColor;

                float _LavaIntensity;
                float _LavaScale;

                float _CrackWidth;
                float _EdgeSoftness;

                float _FlowSpeed;
                float _DistortionStrength;

                float _ImageBrightness;

            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;

                float2 uv : TEXCOORD0;

                // World position lets us make the lava
                // independent of stretched Ground UVs.
                float3 positionWS : TEXCOORD1;
            };


            // --------------------------------------------------
            // RANDOM
            // --------------------------------------------------

            float2 Random2(float2 p)
            {
                float2 result;

                result.x =
                    dot(
                        p,
                        float2(127.1, 311.7)
                    );

                result.y =
                    dot(
                        p,
                        float2(269.5, 183.3)
                    );

                return frac(
                    sin(result) *
                    43758.5453
                );
            }


            // --------------------------------------------------
            // VORONOI
            // --------------------------------------------------

            float Voronoi(float2 uv)
            {
                float2 cell =
                    floor(uv);

                float2 local =
                    frac(uv);

                float minimumDistance =
                    10.0;

                for (int y = -1; y <= 1; y++)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 neighbor =
                            float2(x, y);

                        float2 randomPoint =
                            Random2(
                                cell +
                                neighbor
                            );

                        float2 difference =
                            neighbor +
                            randomPoint -
                            local;

                        float distanceToPoint =
                            length(difference);

                        minimumDistance =
                            min(
                                minimumDistance,
                                distanceToPoint
                            );
                    }
                }

                return minimumDistance;
            }


            Varyings vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs positions =
                    GetVertexPositionInputs(
                        input.positionOS.xyz
                    );

                output.positionCS =
                    positions.positionCS;

                output.positionWS =
                    positions.positionWS;

                output.uv =
                    input.uv;

                return output;
            }


            half4 frag(Varyings input) : SV_Target
            {
                // ==============================================
                // IMAGE + REVEAL MASK
                // ==============================================

                float2 imageUV =
                    TRANSFORM_TEX(
                        input.uv,
                        _MainTexture
                    );

                float3 imageColor =
                    SAMPLE_TEXTURE2D(
                        _MainTexture,
                        sampler_MainTexture,
                        imageUV
                    ).rgb;

                imageColor *=
                    _ImageBrightness;


                float reveal =
                    SAMPLE_TEXTURE2D(
                        _RevealMask,
                        sampler_RevealMask,
                        input.uv
                    ).r;


                // ==============================================
                // WORLD SPACE LAVA
                // ==============================================
                //
                // IMPORTANT:
                //
                // We use XZ world position instead of Ground UV.
                // Therefore stretching/scaling the Ground should
                // not turn Voronoi into long stripes.
                //

                float2 lavaUV =
                    input.positionWS.xz *
                    _LavaScale;


                // ==============================================
                // TOP -> BOTTOM FLOW
                // ==============================================

                lavaUV.y +=
                    _Time.y *
                    _FlowSpeed;


                // ==============================================
                // SECOND VORONOI FOR DISTORTION
                // ==============================================

                float distortion =
                    Voronoi(
                        lavaUV * 0.55 +
                        float2(
                            7.31,
                            _Time.y * 0.08
                        )
                    );

                lavaUV.x +=
                    (distortion - 0.5) *
                    _DistortionStrength;


                // ==============================================
                // MAIN VORONOI
                // ==============================================

                float voronoi =
                    Voronoi(lavaUV);


                // Voronoi is dark near centers and brighter
                // near boundaries.
                //
                // Invert it into lava/rock structure.

                float lavaMask =
                    smoothstep(
                        _CrackWidth,
                        _CrackWidth +
                        _EdgeSoftness,
                        voronoi
                    );


                // Slightly sharpen the result.
                lavaMask =
                    saturate(
                        lavaMask
                    );


                // ==============================================
                // LAVA COLORS
                // ==============================================

                float3 lavaGlow =
                    _LavaColor.rgb *
                    _LavaIntensity;


                float3 coveredColor =
                    lerp(
                        lavaGlow,
                        _RockColor.rgb,
                        lavaMask
                    );


                // ==============================================
                // FINAL REVEAL
                // ==============================================
                //
                // mask = 0 -> lava
                // mask = 1 -> picture

                float3 finalColor =
                    lerp(
                        coveredColor,
                        imageColor,
                        reveal
                    );


                return half4(
                    finalColor,
                    1
                );
            }

            ENDHLSL
        }
    }
}