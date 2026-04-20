Shader "Hidden/PSX_Fog_FullScreen"
{
    Properties
    {
        _FogColor ("Fog Color", Color) = (0.5, 0.5, 0.5, 1)
        _FogStart ("Fog Start", Float) = 10.0
        _FogEnd ("Fog End", Float) = 50.0
        _ColorSteps ("Color Steps", Float) = 32.0 
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZTest Always 
        ZWrite Off 
        Cull Off

        Pass
        {
            Name "PSX Fog Pass"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _FogColor;
                float _FogStart;
                float _FogEnd;
                float _ColorSteps;
            CBUFFER_END

            half4 frag(Varyings input) : SV_Target
            {
                // Sample original color from the screen
                half4 screenColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord);

                // Sample and linearize depth
                float rawDepth = SampleSceneDepth(input.texcoord);
                float linearDepth = LinearEyeDepth(rawDepth, _ZBufferParams);

                // Calculate linear fog factor
                float fogFactor = saturate((linearDepth - _FogStart) / (_FogEnd - _FogStart));

                // Blend fog
                half4 finalColor = lerp(screenColor, _FogColor, fogFactor);

                // PSX 15-bit color banding (32 steps per channel)
                finalColor.rgb = floor(finalColor.rgb * _ColorSteps) / _ColorSteps;

                return finalColor;
            }
            ENDHLSL
        }
    }
}