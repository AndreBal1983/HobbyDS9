// Optional new asset source. Native URP compilation/render acceptance is outstanding.
Shader "HobbyDS9/ArtProduction01/PlanetLimb"
{
    Properties
    {
        _BaseColor ("Limb tint and alpha", Color) = (0.035,0.24,0.65,0.25)
        _LimbPower ("Limb falloff", Range(1,12)) = 5
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Name "PlanetLimb"
            Tags { "LightMode"="UniversalForwardOnly" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Front
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _LimbPower;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float3 normalWS : TEXCOORD1; };
            Varyings Vert(Attributes input)
            {
                Varyings result;
                result.positionWS=TransformObjectToWorld(input.positionOS.xyz);
                result.positionCS=TransformWorldToHClip(result.positionWS);
                result.normalWS=TransformObjectToWorldNormal(input.normalOS);
                return result;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float3 viewDirection=SafeNormalize(_WorldSpaceCameraPos-input.positionWS);
                float facing=saturate(abs(dot(SafeNormalize(input.normalWS),viewDirection)));
                half alpha=saturate(pow(1-facing,max(1,_LimbPower))*_BaseColor.a);
                return half4(_BaseColor.rgb,alpha);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
