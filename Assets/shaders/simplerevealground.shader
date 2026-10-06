Shader "Custom/SimpleRevealGround"
{
    Properties
    {
        _MainTexture ("Hidden Image", 2D) = "white" {}
        _RevealMask ("Reveal Mask", 2D) = "black" {}

        // Final radial reveal
        _FinalRevealActive ("Final Reveal Active", Float) = 0
        _FinalRevealRadius ("Final Reveal Radius", Float) = 0
        _FinalRevealSoftness ("Final Reveal Softness", Float) = 0.025

        // Emerald wave
        _WaveWidth ("Wave Width", Float) = 0.025
        _WaveColor ("Wave Color", Color) = (0, 1, 0.45, 1)
        _WaveIntensity ("Wave Intensity", Float) = 2
    }


    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }


        Blend SrcAlpha OneMinusSrcAlpha

        ZWrite Off

        Cull Back


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

                float _FinalRevealActive;
                float _FinalRevealRadius;
                float _FinalRevealSoftness;

                float _WaveWidth;
                float4 _WaveColor;
                float _WaveIntensity;

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
            };


            Varyings vert(
                Attributes input)
            {
                Varyings output;


                VertexPositionInputs positions =
                    GetVertexPositionInputs(
                        input.positionOS.xyz
                    );


                output.positionCS =
                    positions.positionCS;


                output.uv =
                    input.uv;


                return output;
            }


            half4 frag(
                Varyings input)
                : SV_Target
            {
                // =========================================
                // HIDDEN IMAGE
                // =========================================

                float2 imageUV =
                    TRANSFORM_TEX(
                        input.uv,
                        _MainTexture
                    );


                half4 imageColor =
                    SAMPLE_TEXTURE2D(
                        _MainTexture,
                        sampler_MainTexture,
                        imageUV
                    );


                // =========================================
                // EXISTING DOMINO REVEAL MASK
                // =========================================

                float2 maskUV =
                    TRANSFORM_TEX(
                        input.uv,
                        _RevealMask
                    );


                float maskReveal =
                    SAMPLE_TEXTURE2D(
                        _RevealMask,
                        sampler_RevealMask,
                        maskUV
                    ).r;


                maskReveal =
                    saturate(
                        maskReveal
                    );


                // =========================================
                // FINAL RADIAL REVEAL
                // =========================================

                float2 center =
                    float2(
                        0.5,
                        0.5
                    );


                float distanceFromCenter =
                    distance(
                        input.uv,
                        center
                    );


                float radialReveal =
                    1.0 -
                    smoothstep(
                        _FinalRevealRadius,
                        _FinalRevealRadius +
                        _FinalRevealSoftness,
                        distanceFromCenter
                    );


                radialReveal *=
                    _FinalRevealActive;


                // Keep anything already revealed
                // by dominoes.
                float reveal =
                    max(
                        maskReveal,
                        radialReveal
                    );


                // =========================================
                // EMERALD WAVE RING
                // =========================================

                float waveDistance =
                    abs(
                        distanceFromCenter -
                        _FinalRevealRadius
                    );


                float wave =
                    1.0 -
                    smoothstep(
                        0.0,
                        _WaveWidth,
                        waveDistance
                    );


                wave *=
                    _FinalRevealActive;


                // Only show wave while this part
                // of the image is becoming visible.
                float3 finalRGB =
                    imageColor.rgb;


                finalRGB +=
                    _WaveColor.rgb *
                    wave *
                    _WaveIntensity;


                // =========================================
                // FINAL ALPHA
                // =========================================

                float alpha =
                    imageColor.a *
                    saturate(
                        reveal + wave
                    );


                return half4(
                    finalRGB,
                    alpha
                );
            }


            ENDHLSL
        }
    }
}