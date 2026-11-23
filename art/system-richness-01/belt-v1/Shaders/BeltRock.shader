// Original opaque vertex-color schematic rock, with manual central-star lighting.
Shader "HobbyDS9/Review/BeltRock"
{
    Properties
    {
        _Tint("Tint",Color)=(1,1,1,1)
        _StarCenter("Presentation star center",Vector)=(0,0,0,0)
        _NightFill("Readability fill",Range(0,0.3))=0.14
        _DayStrength("Direct light",Range(0,1.5))=1.1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="UniversalForwardOnly" }
            ZWrite On
            Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float4 _Tint;
            float4 _StarCenter;
            float _NightFill;
            float _DayStrength;
            CBUFFER_END
            struct A { float4 positionOS:POSITION; float3 normalOS:NORMAL; half4 color:COLOR; };
            struct V { float4 positionCS:SV_POSITION; float3 world:TEXCOORD0; float3 normal:TEXCOORD1; half4 color:COLOR; };
            V Vert(A i)
            {
                V o; o.world=TransformObjectToWorld(i.positionOS.xyz);
                o.positionCS=TransformWorldToHClip(o.world);
                o.normal=TransformObjectToWorldNormal(i.normalOS); o.color=i.color; return o;
            }
            half4 Frag(V i):SV_Target
            {
                float day=saturate(dot(SafeNormalize(i.normal),SafeNormalize(_StarCenter.xyz-i.world)));
                return half4(i.color.rgb*_Tint.rgb*(_NightFill+_DayStrength*day),1);
            }
            ENDHLSL
        }
    }
}
