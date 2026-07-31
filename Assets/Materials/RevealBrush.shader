Shader "Hidden/RevealBrush"
{
    Properties
    {
        _MainTex ("Previous Mask", 2D) = "black" {}
        _BrushPosition ("Brush Position", Vector) = (0.5, 0.5, 0, 0)
        _BrushSize ("Brush Size", Float) = 0.05
        _BrushSoftness ("Brush Softness", Float) = 0.2
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float4 _BrushPosition;
            float _BrushSize;
            float _BrushSoftness;

            Varyings Vert(Attributes input)
            {
                Varyings output;

                output.positionHCS =
                    TransformObjectToHClip(input.positionOS.xyz);

                output.uv = input.uv;

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float previousMask =
                    SAMPLE_TEXTURE2D(
                        _MainTex,
                        sampler_MainTex,
                        input.uv
                    ).r;

                float distanceFromBrush =
                    distance(input.uv, _BrushPosition.xy);

                float softness =
                    max(_BrushSize * _BrushSoftness, 0.0001);

                float brush =
                    1.0 - smoothstep(
                        _BrushSize - softness,
                        _BrushSize,
                        distanceFromBrush
                    );

                float result = max(previousMask, brush);

                return half4(result, result, result, 1);
            }

            ENDHLSL
        }
    }
}