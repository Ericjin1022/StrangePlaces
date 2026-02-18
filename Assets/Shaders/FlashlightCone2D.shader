Shader "StrangePlaces/FlashlightCone2D"
{
    Properties
    {
        _Color("Color", Color) = (1, 1, 0.3, 0.35)
        _EdgeSoftness("Edge Softness", Range(0, 0.5)) = 0.18
        _LengthSoftness("Length Softness", Range(0, 1)) = 0.35
        _CenterBoost("Center Boost", Range(0, 2)) = 0.35
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent+50"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }

        ZWrite Off
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

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

            fixed4 _Color;
            half _EdgeSoftness;
            half _LengthSoftness;
            half _CenterBoost;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // UV convention from mesh:
                // - i.uv.x: 0..1 across cone width (0=left edge, 0.5=center, 1=right edge)
                // - i.uv.y: 0..1 from origin to tip (0=origin, 1=tip)
                half xDist = abs(i.uv.x - 0.5h) * 2.0h; // 0 center, 1 edges
                half edge = 1.0h - smoothstep(1.0h - _EdgeSoftness, 1.0h, xDist);
                half len = 1.0h - smoothstep(1.0h - _LengthSoftness, 1.0h, i.uv.y);

                // Slightly brighter center line
                half centerLine = 1.0h - smoothstep(0.0h, 0.45h, xDist);
                half boost = lerp(1.0h, 1.0h + _CenterBoost, centerLine);

                half a = saturate(edge * len) * _Color.a;
                fixed3 rgb = _Color.rgb * boost;
                return fixed4(rgb, a);
            }
            ENDCG
        }
    }
}
