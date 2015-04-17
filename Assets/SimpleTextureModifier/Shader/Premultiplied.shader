Shader "Custom/Premultiplied" {
	Properties {
		_MainTex ("Base", 2D) = "white" {}
		_Color ("Color", Color) = (1.0, 1.0, 1.0, 1.0)
	}
	SubShader {
		Pass {
			Cull Off
			Lighting Off
			ZWrite Off
			AlphaTest Off
			Fog { Mode Off }
			Blend One OneMinusSrcAlpha
			CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"

sampler2D _MainTex;
float4 _Color;

struct appdata_t
{
	float4 vertex : POSITION;
	float2 texcoord : TEXCOORD0;
	half4 color : COLOR;
};

struct v2f {
    float4 pos : POSITION;
    float2 uv1 : TEXCOORD0;
    half4 color : COLOR;
};

float4 _MainTex_ST;

v2f vert (appdata_t v)
{
    v2f o;
    o.pos = mul (UNITY_MATRIX_MVP, v.vertex);
    o.uv1 = TRANSFORM_TEX (v.texcoord, _MainTex);
    o.color = v.color * _Color;
    return o;
}

half4 frag (v2f i) : COLOR
{
    half4 base = tex2D (_MainTex, i.uv1);
    return base * i.color;
}
            ENDCG
        }
    } 
    FallBack Off
}