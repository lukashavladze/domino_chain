Shader "Hidden/RevealBrush"
{
    Properties
    {
        _MainTex ("Previous Mask", 2D) = "black" {}
        _BrushPosition ("Brush Position", Vector) = (0.5, 0.5, 0, 0)
        _BrushSize ("Brush Size", Vector) = (0.04, 0.015, 0, 0)
        _BrushRotation ("Brush Rotation", Float) = 0
        _BrushSoftness ("Brush Softness", Float) = 0.05
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
            float4 _BrushSize;
            float _BrushRotation;
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

                float2 localUV =
                    input.uv - _BrushPosition.xy;

                float angle = radians(_BrushRotation);

                float cosine = cos(angle);
                float sine = sin(angle);

                float2 rotatedUV;

                rotatedUV.x =
                    localUV.x * cosine -
                    localUV.y * sine;

                rotatedUV.y =
                    localUV.x * sine +
                    localUV.y * cosine;

                float2 halfSize =
                    max(_BrushSize.xy * 0.5, 0.0001);

                float2 normalizedDistance =
                    abs(rotatedUV) / halfSize;

                float rectangleDistance =
                    max(
                        normalizedDistance.x,
                        normalizedDistance.y
                    );

                float softness =
                    max(_BrushSoftness, 0.0001);

                float brush =
                    1.0 - smoothstep(
                        1.0 - softness,
                        1.0,
                        rectangleDistance
                    );

                float result =
                    max(previousMask, brush);

                return half4(
                    result,
                    result,
                    result,
                    1
                );
            }

            ENDHLSL
        }
    }
}