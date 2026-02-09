Shader "Custom/ToonShader"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        _Shininess("Shininess", Range(1, 100)) = 20
        _MinLight("Min Light", Range(0, 1)) = 0.3
        _MaxLight("Max Light", Range(0, 5)) = 1
        _NumberOfShades("Number of Shades", Range(0, 1)) = 0.49
        _AmbientPower("Ambient Power", Range(0, 10)) = 1
        _RimPower("Rim Power", Range(0, 10)) = 1
        _RimSize("Rim Size", Range(0, 100)) = 2
        _MinShadow("Min Shadow", Range(0, 1)) = 0.3
        _MaxShadow("Max Shadow", Range(0, 1)) = 0.6
        _OutlineColor("OutlineColor", Color) = (0, 0, 0, 1)
        _OutlineSize("OutlineSize", Range(-0.01, 0.2)) = 0.00
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _BaseMap_ST;
                float _Shininess;
                float _NumberOfShades;
                float _MinLight;
                float _MaxLight;
                float _AmbientPower;
                float _RimPower;
                float _RimSize;
                float _MinShadow;
                float _MaxShadow;
                float _OutlineSize;
                float4 _OutlineColor;
            CBUFFER_END

            void Remap(float4 In, float2 InMinMax, float2 OutMinMax, out float4 Out)
            {
                Out = OutMinMax.x + (In - InMinMax.x) * (OutMinMax.y - OutMinMax.x) / (InMinMax.y - InMinMax.x);
            }

            float3 FresnelEffect(float3 normalVector, float3 viewDirection, float power, float size, float3 lighting)
            {
                float fresnelFactor = pow( saturate(1 - dot(viewDirection, normalVector)), 2) * size;
                return lighting * fresnelFactor;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.shadowCoord = TransformWorldToShadowCoord(OUT.positionWS);
                return OUT;
            }


            half4 frag(Varyings IN) : SV_Target
            { 
                Light light = GetMainLight(IN.shadowCoord);
                //Lambertian diffuse
                float nDotL = saturate(dot(IN.normalWS, light.direction) * 1);
                float shadowToon = smoothstep(_MinShadow, _MaxShadow, light.shadowAttenuation);
                float3 diffuse = ((light.color * light.distanceAttenuation) * nDotL) * shadowToon;
                //Add specular highlight
                float3 viewDir = normalize(_WorldSpaceCameraPos - IN.positionWS);
                float3 halfVector = normalize(light.direction + viewDir);
                float3 specular = pow(saturate(dot(IN.normalWS, halfVector)), _Shininess) * light.color * light.distanceAttenuation * light.shadowAttenuation;
                //Add ambient light
                float3 ambient = SampleSH(IN.normalWS) * _AmbientPower;
                
                float3 lightColor = diffuse + specular + ambient;
                //Add rim lighting
                float3 fresnelColor = FresnelEffect(IN.normalWS, viewDir, _RimPower, _RimSize, diffuse);
                lightColor += fresnelColor;
                //Add additional lights
                int lightCount = GetAdditionalLightsCount();
                for (int i = 0; i < lightCount; ++i) {
                    Light additionalLight = GetAdditionalLight(i, IN.positionWS);
                    
                    float additionalNdotL = saturate(dot(IN.normalWS, additionalLight.direction));
                     float additionalShadowToon = smoothstep(_MinShadow, _MaxShadow, additionalLight.shadowAttenuation);
                    float3 additionalDiffuse = ((additionalLight.color * additionalLight.distanceAttenuation) * additionalNdotL) * additionalShadowToon;
                    float3 additionalHalfVector = normalize(additionalLight.direction + viewDir);
                    float3 additionalSpecular = pow(saturate(dot(IN.normalWS, additionalHalfVector)), _Shininess) * additionalLight.color * additionalLight.distanceAttenuation * additionalShadowToon;
                    float3 additionalColor = additionalDiffuse + additionalSpecular;
                    float3 additionalFresnel = FresnelEffect(IN.normalWS, viewDir, _RimPower, _RimSize, additionalDiffuse);
                    lightColor += additionalColor + additionalFresnel;
                    }
                //Quantize the light color to create a toon shading effect
                float4 lightColor4 = float4(lightColor, 1);
                Remap(lightColor4, float2(-1, 1), float2(0,1), lightColor4);
                Remap(lightColor4, float2(0, 1/_NumberOfShades), float2(_MinLight, _MaxLight), lightColor4);
                //Sample the texture and multiply by the quantized light color
                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;
                half4 color = baseColor * lightColor4;
                return color;
            }
           
            ENDHLSL
        }
       
    }
}
