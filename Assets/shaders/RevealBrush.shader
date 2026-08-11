Shader "Hidden/CircleRevealBrush"
{
    Properties
    {
        _MainTex ("Previous Mask", 2D) = "black" {}
        _BrushPosition ("Brush Position", Vector) = (0.5, 0.5, 0, 0)
        _BrushSize ("Brush Size", Vector) = (0.1, 0.1, 0, 0)
    }

    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;

            float4 _BrushPosition;
            float4 _BrushSize;

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
                float previousMask =
                    tex2D(_MainTex, i.uv).r;

                float2 safeBrushSize =
                    max(
                        _BrushSize.xy,
                        float2(0.00001, 0.00001)
                    );

                float2 local =
                    (i.uv - _BrushPosition.xy)
                    / safeBrushSize;

                float dist =
                    length(local);

                float circle =
                    1.0 -
                    smoothstep(
                        0.42,
                        0.50,
                        dist
                    );

                float result =
                    max(
                        previousMask,
                        circle
                    );

                return fixed4(
                    result,
                    result,
                    result,
                    1.0
                );
            }

            ENDHLSL
        }
    }
}