// HPIS_VideoAlpha.shader
// Shader optimizado para Meta Quest 3 usando técnica Split-Alpha
// Soporta tanto videos con alfa nativo como videos divididos (vertical stack)

Shader "HPIS/VideoAlpha"
{
    Properties
    {
        [PerRendererData] _MainTex ("Video Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)

        [Header(Modo de Transparencia)]
        [KeywordEnum(NATIVO, SPLIT)] _Mode ("Modo", Float) = 1 
        
        [Header(Ajustes de Split)]
        [Toggle] _SwapHalves ("Alpha arriba / RGB abajo", Float) = 0

        [Header(Orientacion)]
        [Toggle] _FlipX ("Espejo Horizontal", Float) = 0
        [Toggle] _FlipY ("Flip Y", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off Lighting Off ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                fixed4 color : COLOR;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float _Mode;
            float _SwapHalves;
            float _FlipX;
            float _FlipY;

            v2f vert(appdata_t v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                float2 uv = i.texcoord;
                if (_FlipX > 0.5) uv.x = 1.0 - uv.x;
                if (_FlipY > 0.5) uv.y = 1.0 - uv.y;

                fixed4 finalCol;

                if (_Mode < 0.5) { 
                    // MODO NATIVO (Solo para PC si el video tiene alfa real)
                    finalCol = tex2D(_MainTex, uv);
                } 
                else { 
                    // MODO SPLIT (Obligatorio para Quest 3 con el video que creamos)
                    // El video tiene 1920x2160 (Color arriba, Mascara abajo)
                    float2 rgbUV = float2(uv.x, uv.y * 0.5 + (_SwapHalves > 0.5 ? 0.0 : 0.5));
                    float2 alphaUV = float2(uv.x, uv.y * 0.5 + (_SwapHalves > 0.5 ? 0.5 : 0.0));
                    
                    fixed3 rgb = tex2D(_MainTex, rgbUV).rgb;
                    fixed alpha = tex2D(_MainTex, alphaUV).r;
                    finalCol = fixed4(rgb, alpha);
                }

                return finalCol * i.color;
            }
            ENDCG
        }
    }
}
