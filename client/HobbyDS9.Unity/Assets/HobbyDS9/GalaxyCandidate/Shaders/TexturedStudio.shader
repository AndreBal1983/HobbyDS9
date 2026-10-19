Shader "HobbyDS9/GalaxyCandidate/TexturedStudio"
{
    Properties
    {
        _BaseMap ("Base colour (sRGB)", 2D) = "white" {}
        _BaseColor ("Base factor", Color) = (1,1,1,1)
        [Normal] _BumpMap ("Normal", 2D) = "bump" {}
        _BumpScale ("Normal scale", Range(0,2)) = 1
        _MetallicGlossMap ("Linear metallic R / smoothness A", 2D) = "white" {}
        _Smoothness ("Smoothness factor", Range(0,1)) = 1
        _EmissionMap ("Emission (sRGB)", 2D) = "black" {}
        [HDR] _EmissionColor ("Emission factor", Color) = (1,1,1,1)
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Pass
        {
            Name "TexturedStudio"
            Tags { "LightMode"="UniversalForwardOnly" }
            ZWrite On
            Cull [_Cull]
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
            TEXTURE2D(_MetallicGlossMap); SAMPLER(sampler_MetallicGlossMap);
            TEXTURE2D(_EmissionMap); SAMPLER(sampler_EmissionMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST, _BumpMap_ST, _MetallicGlossMap_ST, _EmissionMap_ST;
                float4 _BaseColor, _EmissionColor;
                float _BumpScale, _Smoothness, _Cull;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float4 tangentOS : TANGENT; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float4 tangentWS : TEXCOORD1; float3 positionWS : TEXCOORD2; float2 uv : TEXCOORD3; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w * GetOddNegativeScale());
                output.uv = input.uv;
                return output;
            }
            half4 Frag(Varyings input, FRONT_FACE_TYPE facing : FRONT_FACE_SEMANTIC) : SV_Target
            {
                float3 n = normalize(input.normalWS);
                float3 t = normalize(input.tangentWS.xyz - n * dot(n,input.tangentWS.xyz));
                float3 b = cross(n,t) * input.tangentWS.w;
                float3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,TRANSFORM_TEX(input.uv,_BumpMap)),_BumpScale);
                n = normalize(t*normalTS.x + b*normalTS.y + n*normalTS.z) * IS_FRONT_VFACE(facing,1.0,-1.0);
                float3 baseColour = SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,TRANSFORM_TEX(input.uv,_BaseMap)).rgb * _BaseColor.rgb;
                float4 mr = SAMPLE_TEXTURE2D(_MetallicGlossMap,sampler_MetallicGlossMap,TRANSFORM_TEX(input.uv,_MetallicGlossMap));
                float3 emission = SAMPLE_TEXTURE2D(_EmissionMap,sampler_EmissionMap,TRANSFORM_TEX(input.uv,_EmissionMap)).rgb * _EmissionColor.rgb;
                // Fixed studio directions; no scene lights, probes, shadows, fog
                // or volumes. This is a textured preview, not a world PBR renderer.
                float3 key = normalize(float3(.4,.7,.6));
                float3 fill = normalize(float3(-.6,.25,-.4));
                float diffuse = .24 + .66*saturate(dot(n,key)) + .2*saturate(dot(n,fill));
                float3 view = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float specular = pow(saturate(dot(n,normalize(key+view))),lerp(8,96,saturate(mr.a*_Smoothness)));
                float3 colour = baseColour*diffuse + specular*lerp(float3(.12,.12,.12),baseColour*.3,saturate(mr.r)) + emission*.45;
                return half4(colour,1);
            }
            ENDHLSL
        }
    }
}
