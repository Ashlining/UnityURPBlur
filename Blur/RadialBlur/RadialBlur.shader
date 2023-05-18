Shader "Hidden/CustomPostEffect/RadialBlur"
{
	Properties
    {
        _MainTex ("Texture", 2D) = "white" { }
    }
	HLSLINCLUDE
	
	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
	
	uniform half4 _Params;
	
	#define _BlurRadius _Params.x
	#define _Iteration _Params.y
	#define _RadialCenter _Params.zw

	TEXTURE2D (_MainTex);
	SAMPLER(sampler_MainTex);

	struct appdata
    {
		float4 vertex: POSITION;
        float2 uv: TEXCOORD0;
    };
	
	struct VaryingsDefault
	{
		float4 pos: SV_POSITION;
		float2 uv: TEXCOORD0;
	};

	VaryingsDefault VertDefault(appdata v)
    {
         VaryingsDefault o;
         o.pos = TransformObjectToHClip(v.vertex.xyz);
         o.uv = v.uv;
         return o;
    }
	
	half4 RadialBlur(VaryingsDefault i)
	{
		float2 blurVector = (_RadialCenter - i.uv.xy) * _BlurRadius;
		
		half4 acumulateColor = half4(0, 0, 0, 0);
		
		[unroll(30)]
		for (int j = 0; j < _Iteration; j ++)
		{
			acumulateColor += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
			i.uv.xy += blurVector;
		}
		
		return acumulateColor / _Iteration;
	}
	
	half4 Frag(VaryingsDefault i): SV_Target
	{
		return RadialBlur(i);
	}
	
	ENDHLSL
	
	SubShader
	{
		Cull Off ZWrite Off ZTest Always
		
		Pass
		{
			HLSLPROGRAM
			
			#pragma vertex VertDefault
			#pragma fragment Frag
			
			ENDHLSL
		}
	}
}

