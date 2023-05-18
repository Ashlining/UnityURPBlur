Shader "Hidden/CustomPostEffect/BokehBlur"
{
	Properties
    {
        _MainTex ("Texture", 2D) = "white" { }
    }
	HLSLINCLUDE

	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

	struct appdata
    {
		float4 vertex: POSITION;
        float2 uv: TEXCOORD0;
    };

    struct v2f
    {
         float2 uv: TEXCOORD0;
         float4 vertex: SV_POSITION;
    };
	
	half4 _GoldenRot;
	half4 _Params;
	sampler2D _MainTex;
	#define _Iteration _Params.x
	#define _Radius _Params.y
	#define _PixelSize _Params.zw

	v2f Vert(appdata v)
    {
         v2f o;
         o.vertex = TransformObjectToHClip(v.vertex.xyz);
         o.uv = v.uv;
         return o;
    }

	half4 Frag(v2f i): SV_Target
	{
		half2x2 rot = half2x2(_GoldenRot);
		half4 accumulator = 0.0;
		half4 divisor = 0.0;

		half r = 1.0;
		half2 angle = half2(0.0, _Radius);

		for (int j = 0; j < _Iteration; j++)
		{
			r += 1.0 / r;
			angle = mul(rot, angle);
			half4 bokeh = tex2D(_MainTex, float2(i.uv + _PixelSize * (r - 1.0) * angle));
			accumulator += bokeh * bokeh;
			divisor += bokeh;
		}
		return accumulator / divisor;
	}
	
	ENDHLSL
	
	SubShader
	{
		Cull Off ZWrite Off ZTest Always
		
		Pass
		{
			HLSLPROGRAM
			
			#pragma vertex Vert
			#pragma fragment Frag
			
			ENDHLSL
			
		}
	}
}


