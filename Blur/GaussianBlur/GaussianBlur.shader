Shader "Hidden/CustomPostEffect/GaussianBlur"
{
	Properties
    {
        _MainTex ("Texture", 2D) = "white" { }
    }
	HLSLINCLUDE
	
	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

	TEXTURE2D (_MainTex);
	SAMPLER(sampler_MainTex);
	half4 _BlurOffset;
	
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
	
	
	struct v2f
	{
		float4 pos: SV_POSITION;
		float2 uv: TEXCOORD0;	
		float4 uv01: TEXCOORD1;
		float4 uv23: TEXCOORD2;
		float4 uv45: TEXCOORD3;
	};

	VaryingsDefault Vert(appdata v)
    {
         VaryingsDefault o;
         o.pos = TransformObjectToHClip(v.vertex.xyz);
         o.uv = v.uv;
         return o;
    }
	
	v2f VertGaussianBlur(appdata v)
	{
		v2f o;
		o.pos = TransformObjectToHClip(v.vertex.xyz);
		o.uv = v.uv;
		
		o.uv01 = o.uv.xyxy + _BlurOffset.xyxy * float4(1, 1, -1, -1);
		o.uv23 = o.uv.xyxy + _BlurOffset.xyxy * float4(1, 1, -1, -1) * 2.0;
		o.uv45 = o.uv.xyxy + _BlurOffset.xyxy * float4(1, 1, -1, -1) * 6.0;
		
		return o;
	}
	
	float4 FragGaussianBlur(v2f i): SV_Target
	{
		half4 color = float4(0, 0, 0, 0);
		
		color += 0.40 * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
		color += 0.15 * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv01.xy);
		color += 0.15 * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv01.zw);
		color += 0.10 * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv23.xy);
		color += 0.10 * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv23.zw);
		color += 0.05 * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv45.xy);
		color += 0.05 * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv45.zw);
		
		return color;
	}

	half4 Tex2DBlurring(sampler2D tex, half2 texcood, half2 blur)
	{
		//快速模糊
		//const int KERNEL_SIZE = 3;
		//const float KERNEL_[3] = { 0.4566, 1.0, 0.4566 };
 
		//中等模糊
		const int KERNEL_SIZE = 5;
		const float KERNEL_[5] = { 0.2486, 0.7046, 1.0, 0.7046, 0.2486 };
 
		//高级模糊
		//const int KERNEL_SIZE = 7;
		//const float KERNEL_[7] = { 0.1719, 0.4566, 0.8204, 1.0, 0.8204, 0.4566, 0.1719 };
		float4 o = 0;
		float sum = 0;
		float2 shift = 0;
		for (int x = 0; x < KERNEL_SIZE; x++)
		{
			shift.x = blur.x * (float(x) - KERNEL_SIZE / 2);
			for (int y = 0; y < KERNEL_SIZE; y++)
			{
				shift.y = blur.y * (float(y) - KERNEL_SIZE / 2);
				float2 uv = texcood + shift;
				float weight = KERNEL_[x] * KERNEL_[y];
				sum += weight;
				o += tex2D(tex, uv) * weight;
			}
		}
		return o / sum;
	}
	
	
	float4 FragCombine(VaryingsDefault i): SV_Target
	{
		return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
	}
	
	
	ENDHLSL
	
	SubShader
	{
		Cull Off ZWrite Off ZTest Always
		
		Pass
		{
			HLSLPROGRAM
			
			#pragma vertex VertGaussianBlur
			#pragma fragment FragGaussianBlur
			
			ENDHLSL
			
		}
		
		Pass
		{
			HLSLPROGRAM
			
			#pragma vertex Vert
			#pragma fragment FragCombine
			
			ENDHLSL
			
		}
	}
}


