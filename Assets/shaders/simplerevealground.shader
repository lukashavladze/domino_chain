Shader "Custom/SimpleRevealGround"
{
    Properties
    {
        _MainTexture ("Hidden Image", 2D) = "white" {}
        _RevealMask ("Reveal Mask", 2D) = "black" {}
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

        // Important:
        // Transparent unrevealed parts should not write
        // invisible geometry into the depth buffer.
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
                // REVEAL MASK
                // =========================================

                float2 maskUV =
                    TRANSFORM_TEX(
                        input.uv,
                        _RevealMask
                    );

                float reveal =
                    SAMPLE_TEXTURE2D(
                        _RevealMask,
                        sampler_RevealMask,
                        maskUV
                    ).r;

                reveal =
                    saturate(reveal);


                // =========================================
                // RESULT
                //
                // reveal = 0 -> invisible
                // reveal = 1 -> image fully visible
                // =========================================

                float alpha =
                    imageColor.a * reveal;

                return half4(
                    imageColor.rgb,
                    alpha
                );
            }

            ENDHLSL
        }
    }
}