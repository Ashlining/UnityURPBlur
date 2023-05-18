Shader "Hidden/CustomPostEffect/DirectionalBlur"
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
	half3 _Params;	

	#define _Iteration _Params.x
	#define _Direction _Params.yz

	v2f Vert(appdata v)
    {
         v2f o;
         o.vertex = TransformObjectToHClip(v.vertex.xyz);
         o.uv = v.uv;
         return o;
    }
	
	half4 DirectionalBlur(v2f i)
	{
		half4 color = half4(0.0, 0.0, 0.0, 0.0);

		for (int k = -_Iteration; k < _Iteration; k++)
		{
			color += tex2D(_MainTex, i.uv - _Direction * k);
		}
		half4 finalColor = color / (_Iteration * 2.0);

		return finalColor;
	}

	half4 Frag(v2f i): SV_Target
	{
		return DirectionalBlur(i);
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

    
