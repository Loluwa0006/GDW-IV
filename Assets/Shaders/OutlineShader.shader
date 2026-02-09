Shader "Custom/OutlineShader"
{   
    // Shader from https://medium.com/@chitranshnishad27/creating-a-full-screen-outline-shader-in-unity-urp-6c70744932c1
    Properties
    {
         _OutlineColor("Outline Color", Color) = (0, 0, 0, 1)
         _OutlineThickness("Outline Size", Range(0, 10)) = 0.00
         _DepthSensitivity("Depth Sensitivity", Range(0, 50)) = 0.1
         _NormalSensitivty("Normal Sensitivity", Range(0, 10)) = 0.1
         _EdgeThreshold("Edge Threshold", Range(0, 1)) = 0.1

    }
    SubShader
    {
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _OutlineColor;
            float _OutlineThickness;
            float _DepthSensitivity;
            float _NormalSensitivty;
            float _EdgeThreshold;
            CBUFFER_END
        ENDHLSL

        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "OutlineShader"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            
            #pragma vertex Vert
            #pragma fragment Frag

            float3 ReconstructWorldPos(float2 uv, float depth)
            {
                float4 clipPos = float4(uv * 2 - 1, depth, 1);
                #if UNITY_UV_STARTS_AT_TOP
                    clipPos.y = -clipPos.y;
                #endif
                float4 worldPos = mul(UNITY_MATRIX_I_VP, clipPos);
                return worldPos.xyz / worldPos.w;
            }

            float3 ReconstructNormalFromDepth(float2 uv, float2 texelSize) {
                float depthCenter = SampleSceneDepth(uv);
                float depthLeft = SampleSceneDepth(uv + float2(-texelSize.x, 0));
                float depthRight = SampleSceneDepth(uv + float2(texelSize.x, 0));
                float depthUp = SampleSceneDepth(uv + float2(0, texelSize.y));
                float depthDown = SampleSceneDepth(uv + float2(0, -texelSize.y));

                float3 posCenter = ReconstructWorldPos(uv, depthCenter);
                float3 posLeft = ReconstructWorldPos(uv + float2(-texelSize.x, 0), depthLeft);
                float3 posRight = ReconstructWorldPos(uv + float2(texelSize.x, 0), depthRight);
                float3 posUp = ReconstructWorldPos(uv + float2(0, texelSize.y), depthUp);
                float3 posDown = ReconstructWorldPos(uv + float2(0, -texelSize.y), depthDown);

                float3 dx = (posRight - posLeft) * 0.5;
                float3 dy = (posUp - posDown) * 0.5;

                return normalize(cross(dy, dx));
                }


            float DetectDepthEdge(float2 uv, float2 texelSize) {
                float sobelX = 0.0;
                float sobelY = 0.0;
                float sobelXWeights[9] = {-1, 0, 1, -2, 0, 2, -1, 0, 1};
                float sobelYWeights[9] = {-1, -2, -1, 0, 0, 0, 1, 2, 1};

                int index = 0;
                for (int y = -1; y <= 1; y++) {
                    for (int x = -1; x <= 1; x++) {
                        float2 offset = float2(x,y) * texelSize;
                        float depth = LinearEyeDepth(SampleSceneDepth(uv + offset), _ZBufferParams);
                        sobelX += depth * sobelXWeights[index];
                        sobelY += depth * sobelYWeights[index];
                        index++;
                }
                }
                return sqrt(sobelX * sobelX + sobelY * sobelY);
                }


                float DetectNormalEdge(float2 uv, float2 texelSize) 
                {
                    float3 normalCenter = ReconstructNormalFromDepth(uv, texelSize);
                    float3 normalLeft = ReconstructNormalFromDepth(uv + float2(-texelSize.x, 0), texelSize);
                    float3 normalRight = ReconstructNormalFromDepth(uv + float2(texelSize.x, 0), texelSize);
                    float3 normalUp = ReconstructNormalFromDepth(uv + float2(0, texelSize.y), texelSize);
                    float3 normalDown = ReconstructNormalFromDepth(uv + float2(0, -texelSize.y), texelSize);

                    float edgeX = length(normalRight - normalLeft);
                    float edgeY = length(normalUp - normalDown);
                    return (edgeX + edgeY) * 0.5;
                }
            float4 Frag (Varyings input) : SV_Target
            {
                float4 originalColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord);
                float2 texelSize = _OutlineThickness * float2(1.0 / _ScreenParams.x, 1.0 / _ScreenParams.y);

                float centerDepth = SampleSceneDepth(input.texcoord);
                if (centerDepth >= 0.9999) {
                    return originalColor;
                }
                float depthEdge = DetectDepthEdge(input.texcoord, texelSize) * _DepthSensitivity;
                float normalEdge = DetectNormalEdge(input.texcoord, texelSize) * _NormalSensitivty;

                float combinedEdge = max(depthEdge, normalEdge);
                combinedEdge = smoothstep(_EdgeThreshold, _EdgeThreshold + 0.05, combinedEdge);
                combinedEdge = saturate(combinedEdge * 2.0);

                float3 finalColor = lerp(originalColor.rgb, _OutlineColor.rgb, combinedEdge);

                return float4 (finalColor, originalColor.a);
            }
            
            ENDHLSL
        }
    }
}
