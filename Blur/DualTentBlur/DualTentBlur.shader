Shader "Hidden/CustomPostEffect/DualTentBlur"
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
		float4 pos: SV_POSITION;
		float2 uv: TEXCOORD0;
	};

	v2f Vert(appdata v)
    {
         v2f o;
         o.pos = TransformObjectToHClip(v.vertex.xyz);
         o.uv = v.uv;
         return o;
    }
	
	sampler2D _MainTex;
	half4 _BlurOffset;
	
	// 9-tap tent filter
	half4 TentFilter_9Tap(sampler2D tex, float2 uv, float2 texelSize)
	{
		float4 d = texelSize.xyxy * float4(1.0, 1.0, -1.0, 0.0);
		
		half4 s;
		s = tex2D(tex, uv - d.xy);
		s += tex2D(tex, uv - d.wy) * 2.0; // 1 MAD
		s += tex2D(tex, uv - d.zy); // 1 MAD
		
		s += tex2D(tex, uv + d.zw) * 2.0; // 1 MAD
		s += tex2D(tex, uv) * 4.0; // 1 MAD
		s += tex2D(tex, uv + d.xw) * 2.0; // 1 MAD
		
		s += tex2D(tex, uv + d.zy);
		s += tex2D(tex, uv + d.wy) * 2.0; // 1 MAD
		s += tex2D(tex, uv + d.xy);
		
		return s * (1.0 / 16.0);
	}
	
	float4 FragTentBlur(v2f i): SV_Target
	{
		return TentFilter_9Tap(_MainTex, i.uv, _BlurOffset.xy).rgba;
	}
	
	float4 FragCombine(v2f i): SV_Target
	{
		return tex2D(_MainTex, i.uv);
	}
	
	ENDHLSL
	
	SubShader
	{
		Cull Off ZWrite Off ZTest Always
		
		Pass
		{
			HLSLPROGRAM
			
			#pragma vertex Vert
			#pragma fragment FragTentBlur
			
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


