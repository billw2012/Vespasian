Shader "SupGames/Mobile/PostProcess"
{
	Properties
	{
		_MainTex("Base (RGB)", 2D) = "" {}
	}

	CGINCLUDE

#include "UnityCG.cginc"

	struct appdata {
		fixed4 pos : POSITION;
		fixed2 uv : TEXCOORD0;
		UNITY_VERTEX_INPUT_INSTANCE_ID
	};

	struct v2fb {
		fixed4 pos : SV_POSITION;
		fixed4 uv : TEXCOORD0;
		UNITY_VERTEX_OUTPUT_STEREO
	};
	struct v2f {
		fixed4 pos : SV_POSITION;
		fixed4 uv  : TEXCOORD0;
		fixed4 uv1 : TEXCOORD1;
		fixed4 uv2 : TEXCOORD2;
		fixed2 uv3  : TEXCOORD3;
		UNITY_VERTEX_OUTPUT_STEREO
	};

	uniform UNITY_DECLARE_TEX3D(_LutTex3D);
	uniform UNITY_DECLARE_SCREENSPACE_TEXTURE(_LutTex2D);
	uniform UNITY_DECLARE_SCREENSPACE_TEXTURE(_MainTex);
	uniform UNITY_DECLARE_SCREENSPACE_TEXTURE(_MaskTex);
	uniform UNITY_DECLARE_SCREENSPACE_TEXTURE(_BlurTex);
	uniform fixed _LutAmount;
	uniform fixed _BloomThreshold;
	uniform fixed4 _BloomColor;
	uniform fixed _BloomAmount;
	uniform fixed _BlurAmount;
	uniform fixed _BloomDiffuse;
	uniform fixed _LutDimension;
	uniform fixed4 _Color;
	uniform fixed _Contrast;
	uniform fixed _Brightness;
	uniform fixed _Saturation;
	uniform fixed _Exposure;
	uniform fixed _Gamma;
	uniform fixed _CentralFactor;
	uniform fixed _SideFactor;
	uniform fixed _Offset;
	uniform fixed _FishEye;
	uniform fixed _LensDistortion;
	uniform fixed4 _VignetteColor;
	uniform fixed _VignetteAmount;
	uniform fixed _VignetteSoftness;
	uniform fixed4 _MainTex_TexelSize;

	v2fb vertBlur(appdata i)
	{
		v2fb o;
		UNITY_SETUP_INSTANCE_ID(i);
		UNITY_INITIALIZE_OUTPUT(v2fb, o);
		UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
		o.pos = UnityObjectToClipPos(i.pos);
#if defined(BLOOM) && !defined(BLUR)
		fixed2 offset = _MainTex_TexelSize.xy * _BloomDiffuse;
#else
		fixed2 offset = _MainTex_TexelSize.xy * _BlurAmount;
#endif
		o.uv = fixed4(i.uv - offset, i.uv + offset);
		return o;
	}

	v2f vert(appdata i)
	{
		v2f o;
		UNITY_SETUP_INSTANCE_ID(i);
		UNITY_INITIALIZE_OUTPUT(v2f, o);
		UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
		o.pos = UnityObjectToClipPos(i.pos);
		o.uv.xy = UnityStereoTransformScreenSpaceTex(i.uv);
		o.uv.zw = i.uv;
		o.uv1 = fixed4(o.uv.xy - _MainTex_TexelSize.xy, o.uv.xy + _MainTex_TexelSize.xy);
		o.uv2.x = o.uv.x - _Offset * _MainTex_TexelSize.x - 0.5h;
		o.uv2.y = o.uv.x + _Offset * _MainTex_TexelSize.x - 0.5h;
		o.uv2.zw = i.uv - 0.5h;
		o.uv3 = o.uv.xy - 0.5h;
		return o;
	}

	fixed4 fragBloom(v2fb i) : SV_Target
	{
		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
		fixed4 b = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, i.uv.xy);
		b += UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, i.uv.xw);
		b += UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, i.uv.zy);
		b += UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, i.uv.zw);
		b *= 0.25h;
		fixed br = max(b.r, max(b.g, b.b));
		return b * max(0.0h, br - _BloomThreshold) / max(br, 0.00001h);
	}

	fixed4 fragBlur(v2fb i) : SV_Target
	{ 
		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
		fixed4 b = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, i.uv.xy);
		b += UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, i.uv.xw);
		b += UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, i.uv.zy);
		b += UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, i.uv.zw);
		return b * 0.25h;
	}

	fixed4 fragAll2D(v2f i) : SV_Target
	{
UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

		fixed q = dot(i.uv2.zw, i.uv2.zw);
		fixed q2 = sqrt(q);

#ifdef DISTORTION
		fixed q3 = q * _LensDistortion * q2;
		i.uv.xy = (1.0h + q3) * i.uv3 + fixed2(0.5h, 0.5h);
#endif

		fixed4 c = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, i.uv.xy);

#if defined(BLUR) || defined(BLOOM)
		fixed4 b = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_BlurTex, i.uv.xy);
#endif

#ifdef BLUR
		fixed4 m = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MaskTex, i.uv.zw);
#endif

#ifdef CHROMA
		fixed r = dot(i.uv2.xw, i.uv2.xw);
#ifdef DISTORTION
		fixed2 r2 = (1.0h + r * _FishEye * sqrt(r) + q3) * i.uv2.xw + fixed2(0.5h, 0.5h);
#else
		fixed2 r2 = (1.0h + r * _FishEye * sqrt(r)) * i.uv2.xw + fixed2(0.5h, 0.5h);
#endif
		c.r = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, r2).r;
#ifdef BLUR 
		b.r = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_BlurTex, r2).r;
#endif
		r = dot(i.uv2.yw, i.uv2.yw);
#ifdef DISTORTION
		r2 = (1.0h - r * _FishEye * sqrt(r) + q3) * i.uv2.xw + fixed2(0.5h, 0.5h);
#else
		r2 = (1.0h - r * _FishEye * sqrt(r)) * i.uv2.yw + fixed2(0.5h, 0.5h);
#endif
		c.b = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, r2).b;
#ifdef BLUR
		b.b = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_BlurTex, r2).b;
#endif
#endif

#ifdef SHARPEN
		c *= _CentralFactor;
		c -= UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, i.uv1.xy) * _SideFactor;
		c -= UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, i.uv1.xw) * _SideFactor;
		c -= UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, i.uv1.zy) * _SideFactor;
		c -= UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, i.uv1.zw) * _SideFactor;
#endif

#ifdef LUT
		fixed bx = floor(c.b * 256.0h);
		fixed by = floor(bx * 0.0625h);
		c = lerp(c, UNITY_SAMPLE_SCREENSPACE_TEXTURE(_LutTex2D, c.rg * 0.05859375h + 0.001953125h + fixed2(floor(bx - by * 16.0h), by) * 0.0625h), _LutAmount);
#if (defined(BLOOM) || defined(BLUR))
		bx = floor(b.b * 256.0h);
		by = floor(bx * 0.0625h);
		b = lerp(b, UNITY_SAMPLE_SCREENSPACE_TEXTURE(_LutTex2D, b.rg * 0.05859375h + 0.001953125h + fixed2(floor(bx - by * 16.0h), by) * 0.0625h), _LutAmount);
#endif
#endif

#if defined(BLUR) && defined(BLOOM)
		fixed br = max(b.r, max(b.g, b.b));
		c = lerp(c, b, m.r) + b * max(0.0h, br - _BloomThreshold) / max(br, 0.0001h) * _BloomAmount * _BloomColor;
#elif defined(BLUR)
		c = lerp(c, b, m.r);
#elif defined(BLOOM)
		c = c + b * _BloomAmount * _BloomColor;
#endif

#ifdef FILTER
		c.rgb = (c.rgb - 0.5f) * _Contrast + _Brightness;
		c.rgb = lerp(dot(c.rgb, fixed3(0.3h, 0.587h, 0.114h)), c.rgb, _Saturation);
		c.rgb *= (pow(2, _Exposure) - _Gamma) * _Color.rgb;
#endif

		c.rgb = lerp(_VignetteColor.rgb, c.rgb, smoothstep(_VignetteAmount, _VignetteSoftness, q2));
		return c;
	}


	fixed4 fragAll3D(v2f i) : SV_Target
	{
		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

		fixed q = dot(i.uv2.zw, i.uv2.zw);
		fixed q2 = sqrt(q);

#ifdef DISTORTION
		fixed q3 = q * _LensDistortion * q2;
		i.uv.xy = (1.0h + q3) * i.uv3 + fixed2(0.5h, 0.5h);
#endif

		fixed4 c = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, i.uv.xy);

#if defined(BLUR) || defined(BLOOM)
		fixed4 b = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_BlurTex, i.uv.xy);
#endif

#ifdef BLUR
		fixed4 m = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MaskTex, i.uv.zw);
#endif

#ifdef CHROMA
		fixed r = dot(i.uv2.xw, i.uv2.xw);
#ifdef DISTORTION
		fixed2 r2 = (1.0h + r * _FishEye * sqrt(r) + q3) * i.uv2.xw + fixed2(0.5h, 0.5h);
#else
		fixed2 r2 = (1.0h + r * _FishEye * sqrt(r)) * i.uv2.xw + fixed2(0.5h, 0.5h);
#endif
		c.r = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, r2).r;
#ifdef BLUR 
		b.r = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_BlurTex, r2).r;
#endif
		r = dot(i.uv2.yw, i.uv2.yw);
#ifdef DISTORTION
		r2 = (1.0h - r * _FishEye * sqrt(r) + q3) * i.uv2.xw + fixed2(0.5h, 0.5h);
#else
		r2 = (1.0h - r * _FishEye * sqrt(r)) * i.uv2.yw + fixed2(0.5h, 0.5h);
#endif
		c.b = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, r2).b;
#ifdef BLUR
		b.b = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_BlurTex, r2).b;
#endif
#endif

#ifdef SHARPEN
		c *= _CentralFactor;
		c -= UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, i.uv1.xy) * _SideFactor;
		c -= UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, i.uv1.xw) * _SideFactor;
		c -= UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, i.uv1.zy) * _SideFactor;
		c -= UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, i.uv1.zw) * _SideFactor;
#endif

#ifdef LUT
		c = lerp(c, UNITY_SAMPLE_TEX3D(_LutTex3D, c.rgb * 0.9375h + 0.03125h), _LutAmount);
#if defined(BLOOM)|| defined(BLUR)
		b = lerp(b, UNITY_SAMPLE_TEX3D(_LutTex3D, b.rgb * 0.9375h + 0.03125h), _LutAmount);
#endif
#endif

#if defined(BLUR) && defined(BLOOM)
		fixed br = max(b.r, max(b.g, b.b));
		c = lerp(c, b, m.r) + b * max(0.0h, br - _BloomThreshold) / max(br, 0.0001h) * _BloomAmount * _BloomColor;
#elif defined(BLUR)
		c = lerp(c, b, m.r);
#elif defined(BLOOM)
		c = c + b * _BloomAmount * _BloomColor;
#endif

#ifdef FILTER
		c.rgb = (c.rgb - 0.5f) * _Contrast + _Brightness;
		c.rgb = lerp(dot(c.rgb, fixed3(0.3h, 0.587h, 0.114h)), c.rgb, _Saturation);
		c.rgb *= (pow(2, _Exposure) - _Gamma) * _Color.rgb;
#endif

		c.rgb = lerp(_VignetteColor.rgb, c.rgb, smoothstep(_VignetteAmount, _VignetteSoftness, q2));
		return c;
	}
	ENDCG


	Subshader
	{
		Pass //0
		{
		  ZTest Always Cull Off ZWrite Off
		  Fog { Mode off }
		  CGPROGRAM
		  #pragma vertex vertBlur
		  #pragma fragment fragBlur
		  #pragma shader_feature_local BLOOM
		  #pragma shader_feature_local BLUR
		  #pragma fragmentoption ARB_precision_hint_fastest
		  ENDCG
		}
		Pass //1
		{
		  ZTest Always Cull Off ZWrite Off
		  Fog { Mode off }
		  CGPROGRAM
		  #pragma vertex vertBlur
		  #pragma fragment fragBloom
		  #pragma fragmentoption ARB_precision_hint_fastest
		  ENDCG
		}
		Pass //2
		{
		  ZTest Always Cull Off ZWrite Off
		  Fog { Mode off }
		  CGPROGRAM
		  #pragma vertex vert
		  #pragma fragment fragAll2D
		  #pragma fragmentoption ARB_precision_hint_fastest
		  #pragma shader_feature_local BLOOM
		  #pragma shader_feature_local BLUR
		  #pragma shader_feature_local CHROMA
		  #pragma shader_feature_local LUT
		  #pragma shader_feature_local FILTER
		  #pragma shader_feature_local SHARPEN
		  #pragma shader_feature_local DISTORTION
		  ENDCG
		}
		Pass //3
		{
		  ZTest Always Cull Off ZWrite Off
		  Fog { Mode off }
		  CGPROGRAM
		  #pragma vertex vert
		  #pragma fragment fragAll3D
		  #pragma fragmentoption ARB_precision_hint_fastest
		  #pragma shader_feature_local BLOOM
		  #pragma shader_feature_local BLUR
		  #pragma shader_feature_local CHROMA
		  #pragma shader_feature_local LUT
		  #pragma shader_feature_local FILTER
		  #pragma shader_feature_local SHARPEN
		  #pragma shader_feature_local DISTORTION
		  ENDCG
		}
	}
	Fallback off
}