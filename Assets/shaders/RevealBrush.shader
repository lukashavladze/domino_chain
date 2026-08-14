Shader "Hidden/DominoRevealBrush"
{
    Properties
    {
        _MainTex ("Previous Mask", 2D) = "black" {}
        _BrushPosition ("Brush Position", Vector) = (0.5, 0.5, 0, 0)
        _BrushSize ("Brush Size", Vector) = (0.1, 0.05, 0, 0)
        _BrushRotation ("Brush Rotation", Float) = 0
        _BrushSoftness ("Brush Softness", Float) = 0.1
    }

    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;

            float4 _BrushPosition;
            float4 _BrushSize;
            float _BrushRotation;
            float _BrushSoftness;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;

                o.vertex =
                    UnityObjectToClipPos(v.vertex);

                o.uv = v.uv;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Previous reveal state.
                float previousMask =
                    tex2D(_MainTex, i.uv).r;


                // Position relative to brush center.
                float2 delta =
                    i.uv -
                    _BrushPosition.xy;


                // Convert degrees manually to radians.
                float angle =
                    -_BrushRotation *
                    0.01745329251;


                float s = sin(angle);
                float c = cos(angle);


                // Rotate UV around brush center.
                float2 rotated;

                rotated.x =
                    delta.x * c -
                    delta.y * s;

                rotated.y =
                    delta.x * s +
                    delta.y * c;


                // Half size of rectangle.
                float2 halfSize =
                    max(
                        _BrushSize.xy * 0.5,
                        float2(
                            0.00001,
                            0.00001
                        )
                    );


                // Normalize against rectangle size.
                float2 normalizedPos =
                    abs(rotated) /
                    halfSize;


                // Rectangle distance.
                float edge =
                    max(
                        normalizedPos.x,
                        normalizedPos.y
                    );


                // Soft edge relative to rectangle.
                float softness =
                    max(
                        _BrushSoftness,
                        0.0001
                    );


                float brush =
                    1.0 -
                    smoothstep(
                        1.0 - softness,
                        1.0,
                        edge
                    );


                // Preserve everything already revealed.
                float result =
                    max(
                        previousMask,
                        brush
                    );


                return fixed4(
                    result,
                    result,
                    result,
                    1.0
                );
            }

            ENDCG
        }
    }
}