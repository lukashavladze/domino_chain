Shader "Custom/RevealCover"
{
    Properties
    {
        _BaseMap ("Ground Texture", 2D) = "white" {}
        _RevealMask ("Reveal Mask", 2D) = "black" {}

        _EdgeColor ("Edge Color", Color) = (1.0, 0.55, 0.08, 1.0)
        _EdgeWidth ("Edge Width", Range(0.001, 0.3)) = 0.08
        _EdgeIntensity ("Edge Intensity", Range(0, 5)) = 1.2
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

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                OUT.positionHCS =
                    TransformObjectToHClip(IN.positionOS.xyz);

                OUT.uv = TRANSFORM_TEX(
                    IN.uv,
                    _BaseMap
                );

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float mask =
                    SAMPLE_TEXTURE2D(
                        _RevealMask,
                        sampler_RevealMask,
                        IN.uv
                    ).r;

                // Remove revealed pixels completely.
                clip(0.5 - mask);

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