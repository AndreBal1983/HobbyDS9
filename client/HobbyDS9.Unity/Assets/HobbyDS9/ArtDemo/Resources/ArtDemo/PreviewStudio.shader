Shader "HobbyDS9/ArtDemo/PreviewStudio"
{
    Properties
    {
        _BaseColor ("Hull colour", Color) = (0.52,0.43,0.25,1)
        [HDR] _EmissionColor ("Emission", Color) = (0,0,0,0)
        _Metallic ("Metallic", Range(0,1)) = 0.3
        _Smoothness ("Smoothness", Range(0,1)) = 0.4
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
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _EmissionColor;
                float _Metallic;
                float _Smoothness;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float3 positionWS : TEXCOORD1; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float3 n = normalize(input.normalWS);
                float3 key = normalize(float3(.4,.7,.6));
                float3 fill = normalize(float3(-.6,.25,-.4));
                float diffuse = .24 + .66 * saturate(dot(n,key)) + .2 * saturate(dot(n,fill));
                float3 view = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float specular = pow(saturate(dot(n,normalize(key + view))), lerp(8,96,_Smoothness));
                float3 colour = _BaseColor.rgb * diffuse + specular * lerp(.12,.3,_Metallic) + _EmissionColor.rgb * .45;
                return half4(colour,1);
            }
            ENDHLSL
        }
    }
}
