Shader "HobbyDS9/Review/SolSurface"
{
    Properties
    {
        _Surface ("Surface", 2D) = "white" {}
        _Tint ("Tint", Color) = (1,1,1,1)
        _StarCenter ("Star centre", Vector) = (0,0,0,0)
        _Lit ("Star lighting", Float) = 0
        _Striped ("Ring bands", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Pass
        {
            Name "SolSchematic"
            Tags { "LightMode"="UniversalForwardOnly" }
            ZWrite On
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_Surface); SAMPLER(sampler_Surface);
            CBUFFER_START(UnityPerMaterial)
                float4 _Tint;
                float4 _StarCenter;
                float _Lit;
                float _Striped;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float3 normalWS : TEXCOORD1; float2 uv : TEXCOORD2; };
            Varyings Vert(Attributes input)
            {
                Varyings result;
                result.positionWS=TransformObjectToWorld(input.positionOS.xyz);
                result.positionCS=TransformWorldToHClip(result.positionWS);
                result.normalWS=TransformObjectToWorldNormal(input.normalOS);
                result.uv=input.uv;
                return result;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half3 albedo=SAMPLE_TEXTURE2D(_Surface,sampler_Surface,input.uv).rgb*_Tint.rgb;
                if(_Lit>0.5)
                {
                    float3 lightDir=SafeNormalize(_StarCenter.xyz-input.positionWS);
                    float day=saturate(dot(SafeNormalize(input.normalWS),lightDir));
                    albedo*=0.19+0.81*day;
                }
                if(_Striped>0.5)
                {
                    float stripes=0.74+0.26*sin(input.uv.y*42);
                    albedo*=stripes*lerp(0.48,1,smoothstep(0.022,0.062,abs(input.uv.y-0.64)));
                }
                return half4(albedo,1);
            }
            ENDHLSL
        }
    }
}
