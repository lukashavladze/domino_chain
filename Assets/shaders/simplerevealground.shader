Shader "Custom/SimpleRevealGround"
{
    Properties
    {
        _MainTexture ("Hidden Image", 2D) = "white" {}
        _RevealMask ("Reveal Mask", 2D) = "black" {}
        _CoverColor ("Cover Color", Color) = (0,0,0,1)
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

                float4 _CoverColor;

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

            Varyings vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs positions =
                    GetVertexPositionInputs(input.positionOS.xyz);

                output.positionCS = positions.positionCS;
                output.uv = input.uv;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
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

                float reveal =
                    SAMPLE_TEXTURE2D(
                        _RevealMask,
                        sampler_RevealMask,
                        input.uv
                    ).r;

                reveal = saturate(reveal);

                float3 finalColor =
                    lerp(
                        _CoverColor.rgb,
                        imageColor,
                        reveal
                    );

                return half4(finalColor, 1);
            }

            ENDHLSL
        }
    }
}