Shader "Hidden/CustomPostEffect/GrainyBlur"
{
	Properties
    {
        _MainTex ("Texture", 2D) = "white" { }
    }
	HLSLINCLUDE
	
	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

	TEXTURE2D (_MainTex);
	SAMPLER(sampler_MainTex);
	half2 _Params;	
	half _MainTex_ST;

	#define _BlurRadius _Params.x
	#define _Iteration _Params.y

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
	

	VaryingsDefault Vert(appdata v)
    {
         VaryingsDefault o;
         o.pos = TransformObjectToHClip(v.vertex.xyz);
         o.uv = v.uv;
         return o;
    }
	
	float Rand(float2 n)
	{
		return sin(dot(n, half2(1233.224, 1743.335)));
	}
	
	half4 GrainyBlur(VaryingsDefault i)
	{
		half2 randomOffset = float2(0.0, 0.0);
		half4 finalColor = half4(0.0, 0.0, 0.0, 0.0);
		float random = Rand(i.uv);
		
		for (int k = 0; k < int(_Iteration); k ++)
		{
			random = frac(43758.5453 * random + 0.61432);;
			randomOffset.x = (random - 0.5) * 2.0;
			random = frac(43758.5453 * random + 0.61432);
			randomOffset.y = (random - 0.5) * 2.0;
			
			finalColor += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, half2(i.uv + randomOffset * _BlurRadius));
		}
		return finalColor / _Iteration;
	}
	
	half4 Frag(VaryingsDefault i): SV_Target
	{
		return GrainyBlur(i);
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


