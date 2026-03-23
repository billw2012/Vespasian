Shader "Custom/KawaseBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlurAmount ("Blur Amount", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off Cull Off ZTest Always

        Pass
        {
            Name "KawaseBlur"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _BlurAmount;

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float offset = _BlurAmount * 4.0;
                float2 texelSize = _BlitTexture_TexelSize.xy;

                half4 col = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv);
                col += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + float2( offset,  offset) * texelSize);
                col += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + float2(-offset,  offset) * texelSize);
                col += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + float2( offset, -offset) * texelSize);
                col += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + float2(-offset, -offset) * texelSize);
                return col / 5.0;
            }
            ENDHLSL
        }
    }
}
