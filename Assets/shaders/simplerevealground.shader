Shader "Custom/SimpleRevealGround"
{
    Properties
    {
        _BoardTexture ("Board Texture", 2D) = "white" {}
        _MainTexture ("Hidden Image", 2D) = "white" {}
        _RevealMask ("Reveal Mask", 2D) = "black" {}
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

            TEXTURE2D(_BoardTexture);
            SAMPLER(sampler_BoardTexture);

            TEXTURE2D(_MainTexture);
            SAMPLER(sampler_MainTexture);

            TEXTURE2D(_RevealMask);
            SAMPLER(sampler_RevealMask);

            CBUFFER_START(UnityPerMaterial)

                float4 _BoardTexture_ST;
                float4 _MainTexture_ST;
                float4 _RevealMask_ST;

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
                    GetVertexPositionInputs(
                        input.positionOS.xyz
                    );

                output.positionCS =
                    positions.positionCS;

                output.uv =
                    input.uv;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // -----------------------------------------
                // BOARD
                // -----------------------------------------

                float2 boardUV =
                    TRANSFORM_TEX(
                        input.uv,
                        _BoardTexture
                    );

                float3 boardColor =
                    SAMPLE_TEXTURE2D(
                        _BoardTexture,
                        sampler_BoardTexture,
                        boardUV
                    ).rgb;


                // -----------------------------------------
                // HIDDEN IMAGE
                // -----------------------------------------

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


                // -----------------------------------------
                // REVEAL MASK
                // -----------------------------------------

                float reveal =
                    SAMPLE_TEXTURE2D(
                        _RevealMask,
                        sampler_RevealMask,
                        input.uv
                    ).r;

                reveal =
                    saturate(reveal);


                // -----------------------------------------
                // FINAL BLEND
                // -----------------------------------------

                float3 finalColor =
                    lerp(
                        boardColor,
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