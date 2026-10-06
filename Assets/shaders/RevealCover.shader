Shader "Custom/RevealCover"
{
    Properties
    {
        _BaseMap ("Ground Texture", 2D) = "white" {}
        _RevealMask ("Reveal Mask", 2D) = "black" {}

        _EdgeColor ("Edge Color", Color) =
            (1.0, 0.55, 0.08, 1.0)

        _EdgeWidth ("Edge Width", Range(0.001, 0.3)) =
            0.08

        _EdgeIntensity ("Edge Intensity", Range(0, 5)) =
            1.2


        // =========================================
        // FINAL RADIAL REVEAL
        // =========================================

        _FinalRevealActive (
            "Final Reveal Active",
            Float
        ) = 0

        _FinalRevealRadius (
            "Final Reveal Radius",
            Float
        ) = 0

        _FinalRevealSoftness (
            "Final Reveal Softness",
            Float
        ) = 0.025
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
            ZWrite On
            Cull Back


            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag


            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);


            TEXTURE2D(_RevealMask);
            SAMPLER(sampler_RevealMask);


            CBUFFER_START(UnityPerMaterial)

                float4 _BaseMap_ST;

                float4 _EdgeColor;

                float _EdgeWidth;
                float _EdgeIntensity;


                float _FinalRevealActive;
                float _FinalRevealRadius;
                float _FinalRevealSoftness;

            CBUFFER_END


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


            Varyings vert(
                Attributes IN)
            {
                Varyings OUT;


                OUT.positionHCS =
                    TransformObjectToHClip(
                        IN.positionOS.xyz
                    );


                OUT.uv =
                    TRANSFORM_TEX(
                        IN.uv,
                        _BaseMap
                    );


                return OUT;
            }


            half4 frag(
                Varyings IN)
                : SV_Target
            {
                // =========================================
                // NORMAL DOMINO MASK
                // =========================================

                float mask =
                    SAMPLE_TEXTURE2D(
                        _RevealMask,
                        sampler_RevealMask,
                        IN.uv
                    ).r;


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
                        IN.uv,
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


                // =========================================
                // COMBINE BOTH
                // =========================================

                float reveal =
                    max(
                        mask,
                        radialReveal
                    );


                // Anything revealed becomes a hole
                // in the cover.
                clip(
                    0.5 -
                    reveal
                );


                // =========================================
                // NORMAL GROUND
                // =========================================

                half4 ground =
                    SAMPLE_TEXTURE2D(
                        _BaseMap,
                        sampler_BaseMap,
                        IN.uv
                    );


                return ground;
            }


            ENDHLSL
        }
    }
}