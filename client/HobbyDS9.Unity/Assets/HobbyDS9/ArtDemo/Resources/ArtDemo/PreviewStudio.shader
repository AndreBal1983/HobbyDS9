Shader "HobbyDS9/ArtDemo/PreviewStudio"
{
    Properties
    {
        _BaseColor ("Hull colour", Color) = (0.52,0.43,0.25,1)
        _BaseMap ("Hull texture", 2D) = "white" {}
        [Normal] _BumpMap ("Normal texture", 2D) = "bump" {}
        _BumpScale ("Normal strength", Range(0,2)) = 1
        _UseNormalMap ("Use authored normal map", Float) = 0
        _EmissionMap ("Emission texture", 2D) = "white" {}
        [HDR] _EmissionColor ("Emission", Color) = (0,0,0,0)
        _Metallic ("Metallic", Range(0,1)) = 0.3
        _Smoothness ("Smoothness", Range(0,1)) = 0.4
        _MetallicRoughnessMap ("Linear roughness G / metallic B", 2D) = "white" {}
        _UseMetallicRoughnessMap ("Use authored metallic roughness", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Pass
        {
            Name "PreviewStudio"
            Tags { "LightMode"="UniversalForwardOnly" }
            ZWrite On
            Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
            TEXTURE2D(_EmissionMap); SAMPLER(sampler_EmissionMap);
            TEXTURE2D(_MetallicRoughnessMap); SAMPLER(sampler_MetallicRoughnessMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _EmissionColor;
                float4 _BaseMap_ST;
                float4 _BumpMap_ST;
                float4 _EmissionMap_ST;
                float _BumpScale;
                float _UseNormalMap;
                float _Metallic;
                float _Smoothness;
                float4 _MetallicRoughnessMap_ST;
                float _UseMetallicRoughnessMap;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float4 tangentOS : TANGENT; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float3 positionWS : TEXCOORD1; float4 tangentWS : TEXCOORD2; float2 uv : TEXCOORD3; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.tangentWS=float4(TransformObjectToWorldDir(input.tangentOS.xyz),input.tangentOS.w*GetOddNegativeScale());
                output.uv=input.uv;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float3 n = normalize(input.normalWS);
                if(_UseNormalMap>.5 && dot(input.tangentWS.xyz,input.tangentWS.xyz)>.001)
                {
                    float3 tangent=normalize(input.tangentWS.xyz);
                    float3 bitangent=cross(n,tangent)*input.tangentWS.w;
                    float3 sampled=UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,input.uv*_BumpMap_ST.xy+_BumpMap_ST.zw),_BumpScale);
                    n=normalize(tangent*sampled.x+bitangent*sampled.y+n*sampled.z);
                }
                float3 key = normalize(float3(.4,.7,.6));
                float3 fill = normalize(float3(-.6,.25,-.4));
                float diffuse = .24 + .66 * saturate(dot(n,key)) + .2 * saturate(dot(n,fill));
                float3 view = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float metallic=_Metallic, smoothness=_Smoothness;
                if(_UseMetallicRoughnessMap>.5)
                {
                    float3 mr=SAMPLE_TEXTURE2D(_MetallicRoughnessMap,sampler_MetallicRoughnessMap,input.uv*_MetallicRoughnessMap_ST.xy+_MetallicRoughnessMap_ST.zw).rgb;
                    metallic*=mr.b;
                    smoothness=1-mr.g;
                }
                float specular = pow(saturate(dot(n,normalize(key + view))), lerp(8,96,smoothness));
                float3 albedo=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv*_BaseMap_ST.xy+_BaseMap_ST.zw).rgb*_BaseColor.rgb;
                float3 emission=SAMPLE_TEXTURE2D(_EmissionMap,sampler_EmissionMap,input.uv*_EmissionMap_ST.xy+_EmissionMap_ST.zw).rgb*_EmissionColor.rgb;
                float3 colour = albedo * diffuse + specular * lerp(.12,.3,metallic) + emission * .45;
                return half4(colour,1);
            }
            ENDHLSL
        }
    }
}
