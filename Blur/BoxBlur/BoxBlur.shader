Shader "Hidden/CustomPostEffect/BoxBlur"
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
	
	sampler2D _MainTex;
	half4 _BlurOffset;

	v2f Vert(appdata v)
    {
         v2f o;
         o.vertex = TransformObjectToHClip(v.vertex.xyz);
         o.uv = v.uv;
         return o;
    }
	
	half4 BoxFilter_4Tap(sampler2D _MainTex, float2 uv, float2 texelSize)
	{
		float4 d = texelSize.xyxy * float4(-1.0, -1.0, 1.0, 1.0);
		
		half4 s = 0;
		s = tex2D(_MainTex, uv + d.xy) * 0.25h;
		s += tex2D(_MainTex, uv + d.zy) * 0.25h;
		s += tex2D(_MainTex, uv + d.xw) * 0.25h;
		s += tex2D(_MainTex, uv + d.zw) * 0.25h;
		return s;
	}
	
	
	float4 FragBoxBlur(v2f i): SV_Target
	{
		return BoxFilter_4Tap(_MainTex, i.uv,  _BlurOffset.xy).rgba;
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
			#pragma fragment FragBoxBlur
			
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


