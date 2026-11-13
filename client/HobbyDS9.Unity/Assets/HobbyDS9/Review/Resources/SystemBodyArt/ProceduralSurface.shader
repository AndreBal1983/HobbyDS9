Shader "HobbyDS9/Review/ProceduralSystemSurface"
{
 Properties
 {
  _Surface("Surface RGB, decorative water mask A",2D)="white"{}
  _Clouds("Clouds",2D)="black"{}
  _Tint("Tint",Color)=(1,1,1,1)
  _StarCenter("Star centre",Vector)=(0,0,0,0)
  _Lit("Star lighting",Float)=1
  _Unknown("Unresolved",Float)=0
  _Atmosphere("Atmosphere",Float)=0
 }
 SubShader
 {
  Tags {"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry"}
  Pass
  {
   Tags {"LightMode"="UniversalForwardOnly"}
   ZWrite On Cull Off
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_Surface);SAMPLER(sampler_Surface);
   TEXTURE2D(_Clouds);SAMPLER(sampler_Clouds);
   CBUFFER_START(UnityPerMaterial)
   float4 _Tint,_StarCenter;
   float _Lit,_Unknown,_Atmosphere;
   CBUFFER_END
   struct A{float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;};
   struct V{float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;float3 normalWS:TEXCOORD1;float2 uv:TEXCOORD2;};
   V Vert(A input)
   {
    V o;o.positionWS=TransformObjectToWorld(input.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.positionWS);
    o.normalWS=TransformObjectToWorldNormal(input.normalOS);o.uv=input.uv;return o;
   }
   half4 Frag(V input):SV_Target
   {
    float3 n=SafeNormalize(input.normalWS),light=SafeNormalize(_StarCenter.xyz-input.positionWS);
    float day=saturate(dot(n,light));float rim=pow(1-saturate(dot(n,SafeNormalize(_WorldSpaceCameraPos-input.positionWS))),3);
    half4 map=SAMPLE_TEXTURE2D(_Surface,sampler_Surface,input.uv);
    half3 color=map.rgb*_Tint.rgb;
    if(_Lit>.5)
    {
     color*=.16+.84*day;
     half cloud=SAMPLE_TEXTURE2D(_Clouds,sampler_Clouds,input.uv).r;
     color=lerp(color,half3(.86,.91,1)*(.16+.84*day),cloud*.8);
     color+=half3(.08,.19,.29)*rim*_Atmosphere;
     if(_Unknown>.5)color=half3(.09,.14,.20)*(.35+.65*day)+half3(.11,.23,.34)*rim;
    }
    return half4(color,1);
   }
   ENDHLSL
  }
 }
}