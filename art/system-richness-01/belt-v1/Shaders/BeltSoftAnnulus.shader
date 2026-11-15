// Original schematic distant-belt haze; never a picking/collision or hidden-fact surface.
Shader "HobbyDS9/Review/BeltSoftAnnulus"
{
    Properties { _Tint("Dust RGB",Color)=(0.35,0.31,0.25,1) _Opacity("Opacity",Range(0,0.12))=0.065 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Tags { "LightMode"="UniversalForwardOnly" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float4 _Tint;
            float _Opacity;
            CBUFFER_END
            struct A { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct V { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };
            V Vert(A i) { V o; o.positionCS=TransformObjectToHClip(i.positionOS.xyz); o.uv=i.uv; return o; }
            half4 Frag(V i):SV_Target
            {
                float radial=pow(saturate(sin(saturate(i.uv.y)*3.141592654)),1.4);
                float mottling=.70+.30*sin(i.uv.x*232.4778564+.6)*sin(i.uv.x*69.1150384);
                return half4(_Tint.rgb,_Opacity*radial*mottling*_Tint.a);
            }
            ENDHLSL
        }
    }
}
