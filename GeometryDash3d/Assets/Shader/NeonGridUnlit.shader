Shader "Unlit/NeonGridPro"
{
    Properties
    {
        // Couleurs
        _BgTop     ("Background Top", Color) = (0.015,0.02,0.04,1)
        _BgBottom  ("Background Bottom", Color) = (0.005,0.01,0.02,1)
        _LineColA  ("Line Color A", Color) = (0.0,1.0,1.0,1)
        _LineColB  ("Line Color B (second grid)", Color) = (0.8,0.2,1.0,1)

        // Intensité / animation
        _Emission  ("Emission", Range(0,10)) = 3
        _PulseAmt  ("Pulse Amount", Range(0,1)) = 0.25
        _PulseSpd  ("Pulse Speed", Float) = 1.8

        // Grille A (large)
        _ScaleA    ("Grid A Scale", Float) = 9
        _WidthA    ("Line A Width", Range(0.001,0.08)) = 0.025
        _ScrollAX  ("Grid A Scroll X", Float) = 0.18
        _ScrollAY  ("Grid A Scroll Y", Float) = -0.04

        // Grille B (fine)
        _ScaleB    ("Grid B Scale", Float) = 20
        _WidthB    ("Line B Width", Range(0.001,0.08)) = 0.012
        _ScrollBX  ("Grid B Scroll X", Float) = 0.05
        _ScrollBY  ("Grid B Scroll Y", Float) = -0.015
        _GridBMix  ("Grid B Blend", Range(0,1)) = 0.35

        // Style / lisibilité
        _Tilt      ("Diagonal Tilt", Range(-0.6,0.6)) = 0.15
        _Perspective("Perspective Curve", Range(0,0.6)) = 0.18
        _Vignette  ("Vignette Strength", Range(0,1)) = 0.35
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Background" }
        Cull Off ZWrite Off ZTest Always
        Lighting Off
        Blend One Zero

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _BgTop, _BgBottom, _LineColA, _LineColB;
            float  _Emission, _PulseAmt, _PulseSpd;
            float  _ScaleA, _WidthA, _ScrollAX, _ScrollAY;
            float  _ScaleB, _WidthB, _ScrollBX, _ScrollBY, _GridBMix;
            float  _Tilt, _Perspective, _Vignette;

            struct appdata {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };
            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            // distance anti-aliasée à une ligne de largeur w
            float aaLine(float x, float w)
            {
                // fwidth = dérivée écran → adoucit naturellement
                float fw = fwidth(x) * 1.0;
                return smoothstep(w + fw, w - fw, x);
            }

            // masque grille anti-aliasé
            float gridAA(float2 uv, float scale, float width)
            {
                float2 g = frac(uv * scale);
                // distance aux axes
                float2 d = abs(g - 0.5);
                // deux directions
                float lx = aaLine(d.x, width);
                float ly = aaLine(d.y, width);
                return saturate(lx + ly);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // UV 0..1 plein écran
                float2 uv = i.uv;

                // légère perspective : comprime vers le bas
                float curve = _Perspective * (1.0 - uv.y);
                uv.x += (uv.y - 0.5) * _Tilt;    // diagonale
                uv.y = lerp(uv.y, uv.y * (1.0 - curve), _Perspective);

                // dégradé de fond
                fixed3 bg = lerp(_BgBottom.rgb, _BgTop.rgb, uv.y);

                // scroll dans le temps
                float t = _Time.y;
                float2 uvA = uv + float2(_ScrollAX, _ScrollAY) * t;
                float2 uvB = uv + float2(_ScrollBX, _ScrollBY) * t;

                // masques AA
                float mA = gridAA(uvA, _ScaleA, _WidthA);
                float mB = gridAA(uvB, _ScaleB, _WidthB) * _GridBMix;

                // pulsation douce
                float pulse = 1.0 + sin(t * _PulseSpd) * _PulseAmt;

                // couleurs lignes émissives
                fixed3 lineA = _LineColA.rgb * (_Emission * pulse);
                fixed3 lineB = _LineColB.rgb * (_Emission * 0.7 * pulse);

                // mélange
                fixed3 col = bg;
                col = lerp(col, lineA, mA);
                col = lerp(col, lineB, mB);

                // vignette (assombrit les bords)
                float2 d = uv - 0.5;
                float v = 1.0 - saturate(dot(d, d) * 3.0); // disque doux
                col = lerp(_BgBottom.rgb, col, lerp(1.0, v, _Vignette));

                return fixed4(col, 1);
            }
            ENDCG
        }
    }
}
