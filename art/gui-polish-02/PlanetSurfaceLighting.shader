// GUI-POLISH-02 receiving candidate. Retains the existing shader/resource identity.
// Source only: native Unity compilation, image parity and performance remain QA gates.
Shader "HobbyDS9/Review/ProceduralSystemSurface"
{
 Properties
 {
  _Surface("Surface RGB",2D)="white"{}
  _Clouds("Decorative cloud R",2D)="black"{}
  _Tint("Tint",Color)=(1,1,1,1)
  _StarCenter("Presentation light position",Vector)=(0,0,0,0)
  _Lit("Star lighting",Float)=1
  _Unknown("Unresolved",Float)=0
  _Atmosphere("Existing disclosed atmosphere strength",Float)=0
  _NightFill("Readability fill",Range(0,0.12))=0.04
  _DayStrength("Direct light",Range(0,1.5))=1.15
  _CloudOpacity("Cloud opacity multiplier",Range(0,1))=0.55
  _LimbStrength("Limb intensity",Range(0,0.35))=0.12
  _LimbTint("Limb tint",Color)=(0.055,0.24,0.70,1)
 }
 SubShader
 {
  Tags {"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry"}
  Pass
  {
   Tags {"LightMode"="UniversalForwardOnly"}
   ZWrite On
   Cull Off
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_Surface);SAMPLER(sampler_Surface);
   TEXTURE2D(_Clouds);SAMPLER(sampler_Clouds);
   CBUFFER_START(UnityPerMaterial)
   float4 _Tint,_StarCenter,_LimbTint;
   float _Lit,_Unknown,_Atmosphere,_NightFill,_DayStrength,_CloudOpacity,_LimbStrength;
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
    float signedDay=dot(n,light),day=saturate(signedDay);
    float facing=saturate(dot(n,SafeNormalize(_WorldSpaceCameraPos-input.positionWS)));
    float rim=pow(1-facing,5);
    // Unknown stays neutral and does not sample authored surfaces or cloud maps.
    if(_Lit>.5 && _Unknown>.5)
     return half4(half3(.09,.14,.20)*(.35+.65*day)+half3(.11,.23,.34)*pow(1-facing,3),1);
    half3 albedo=SAMPLE_TEXTURE2D(_Surface,sampler_Surface,input.uv).rgb*_Tint.rgb;
    if(_Lit<.5)return half4(albedo,1); // Existing star/orbit unlit roles retained.
    float illumination=clamp(_NightFill,0,.12)+clamp(_DayStrength,0,1.5)*day;
    half3 color=albedo*illumination;
    float cloud=SAMPLE_TEXTURE2D(_Clouds,sampler_Clouds,input.uv).r*saturate(_CloudOpacity);
    color=lerp(color,half3(.86,.91,1)*illumination,saturate(cloud));
    // A narrow, lit-side limb; no independently glowing blue ring across the night side.
    float limbLight=smoothstep(-.12,.28,signedDay)*(.10+.90*sqrt(day));
    color+=_LimbTint.rgb*rim*saturate(_Atmosphere)*clamp(_LimbStrength,0,.35)*limbLight;
    return half4(color,1);
   }
   ENDHLSL
  }
 }
}
