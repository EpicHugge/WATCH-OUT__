Shader "Hidden/WATCHOUT/Interaction Silhouette Process"
{
    Properties
    {
        _OutlineColor("Outline Color", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Overlay"
            "RenderType" = "Transparent"
        }

        Pass
        {
            Name "SilhouetteDilate"
            ZWrite Off
            ZTest Always
            Cull Off
            Blend One Zero

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragDilate

            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float SampleMask(float2 uv)
            {
                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv).r;
            }

            half4 FragDilate(Varyings input) : SV_Target
            {
                float2 texelSize = _BlitTexture_TexelSize.xy;
                float mask = SampleMask(input.texcoord);
                mask = max(mask, SampleMask(input.texcoord + texelSize * float2(1, 0)));
                mask = max(mask, SampleMask(input.texcoord + texelSize * float2(-1, 0)));
                mask = max(mask, SampleMask(input.texcoord + texelSize * float2(0, 1)));
                mask = max(mask, SampleMask(input.texcoord + texelSize * float2(0, -1)));
                mask = max(mask, SampleMask(input.texcoord + texelSize * float2(1, 1)));
                mask = max(mask, SampleMask(input.texcoord + texelSize * float2(-1, 1)));
                mask = max(mask, SampleMask(input.texcoord + texelSize * float2(1, -1)));
                mask = max(mask, SampleMask(input.texcoord + texelSize * float2(-1, -1)));
                return half4(mask, mask, mask, mask);
            }
            ENDHLSL
        }

        Pass
        {
            Name "SilhouetteErode"
            ZWrite Off
            ZTest Always
            Cull Off
            Blend One Zero

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragErode

            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float SampleMask(float2 uv)
            {
                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv).r;
            }

            half4 FragErode(Varyings input) : SV_Target
            {
                float2 texelSize = _BlitTexture_TexelSize.xy;
                float mask = SampleMask(input.texcoord);
                mask = min(mask, SampleMask(input.texcoord + texelSize * float2(1, 0)));
                mask = min(mask, SampleMask(input.texcoord + texelSize * float2(-1, 0)));
                mask = min(mask, SampleMask(input.texcoord + texelSize * float2(0, 1)));
                mask = min(mask, SampleMask(input.texcoord + texelSize * float2(0, -1)));
                mask = min(mask, SampleMask(input.texcoord + texelSize * float2(1, 1)));
                mask = min(mask, SampleMask(input.texcoord + texelSize * float2(-1, 1)));
                mask = min(mask, SampleMask(input.texcoord + texelSize * float2(1, -1)));
                mask = min(mask, SampleMask(input.texcoord + texelSize * float2(-1, -1)));
                return half4(mask, mask, mask, mask);
            }
            ENDHLSL
        }

        Pass
        {
            Name "SilhouetteComposite"
            ZWrite Off
            ZTest Always
            Cull Off
            Blend One Zero

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragComposite

            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D_X(_InnerMask);
            TEXTURE2D_X(_OuterMask);

            float4 _OutlineColor;

            half4 FragComposite(Varyings input) : SV_Target
            {
                half4 sceneColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord);
                float innerMask = SAMPLE_TEXTURE2D_X(_InnerMask, sampler_LinearClamp, input.texcoord).r;
                float outerMask = SAMPLE_TEXTURE2D_X(_OuterMask, sampler_LinearClamp, input.texcoord).r;

                float outlineAlpha = saturate(outerMask - innerMask) * _OutlineColor.a;
                sceneColor.rgb = lerp(sceneColor.rgb, _OutlineColor.rgb, outlineAlpha);
                sceneColor.a = max(sceneColor.a, outlineAlpha);
                return sceneColor;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
