Shader "HobbyDS9/GalaxyPresentationV3/Presentation"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        HLSLINCLUDE
        #include "UnityCG.cginc"
        sampler2D _Atlas,_Mask,_Local;
        sampler3D _Volume;
        float4 _View,_LocalSize,_ProbeUv;
        float _Depth,_Effects,_MacroMip;
        int _FieldCount;
        float4 _Fields[8],_FieldParameters[8];
        static const float Edges[13]={-.33,-.165,-.105,-.054,-.0525,-.027,0,.027,.0525,.054,.105,.165,.33};
        float Ratio(float tau){return tau<1e-5?1-tau*.5+tau*tau/6:(1-exp(-tau))/tau;}
        float Erf3(float x)
        {
            float a=abs(x),t=1/(1+.3275911*a);
            return (x<0?-1:1)*(1-(((((1.061405429*t-1.453152027)*t)+1.421413741)*t-.284496736)*t+.254829592)*t*exp(-a*a));
        }
        float Cdf(float z,float h){return .5*(1+Erf3(clamp(z/h,-3,3))/Erf3(3));}
        float3 Disk(float3 world)
        {
            const float c=.6691306064,s=.7431448255,r=.8480480962,t=.5299192642;
            float a=(r*world.x+t*world.y)/.47,b=(-t*world.x+r*world.y)/.47,z=world.z/.47;
            return float3(a,c*b+s*z,-s*b+c*z);
        }
        bool Slab(float p,float d,float extent,inout float nearZ,inout float farZ)
        {
            if(abs(d)<1e-7)return abs(p)<=extent;
            float a=(-extent-p)/d,b=(extent-p)/d;
            nearZ=max(nearZ,min(a,b));farZ=min(farZ,max(a,b));return nearZ<=farZ;
        }
        float2 Plane(float2 uv){return _View.xy+float2(uv.x,1-uv.y)*_View.zw;}
        float3 LocalPoint(float2 world,float z,int field)
        {
            float2 d=(world-_Fields[field].xy)/_Fields[field].zw;
            float2 cs=_FieldParameters[field].xy;
            return float3(cs.x*d.x-cs.y*d.y,cs.y*d.x+cs.x*d.y,z/.8);
        }
        float4 LocalLayer(v2f_img input):SV_Target
        {
            float2 maskUv=float2(input.uv.x,1-input.uv.y);
            float mask=tex2Dlod(_Mask,float4(maskUv,0,0)).r;
            if(mask<.5||_Effects<.5||_FieldCount==0)return 0;
            float2 plane=Plane(input.uv),camera=_View.xy+_View.zw*.5;
            float2 slope=(camera-plane)*(_Depth/4);
            float2 intervals[8];
            [loop] for(int f=0;f<_FieldCount;f++)
            {
                float3 o=LocalPoint(plane,0,f),d=LocalPoint(plane+slope,1,f)-o;
                float lo=-.8,hi=.8;
                bool hit=Slab(o.x,d.x,2.5,lo,hi)&&Slab(o.y,d.y,1.65,lo,hi)&&Slab(o.z,d.z,1,lo,hi);
                intervals[f]=hit?float2(lo,hi):float2(1,-1);
            }
            float3 light=0;float transmission=1;
            // Correct48 centre samples in projected local-z; no Euclidean-world
            // secant or extra .8 factor. Ray boxes and mask only reduce fetches.
            [loop] for(int k=0;k<48;k++)
            {
                float z=.8*(1-(k+.5)*(2.0/48)),tau=0;float3 source=0;
                float2 world=plane+slope*z;
                [loop] for(int f=0;f<_FieldCount;f++)
                {
                    if(z<intervals[f].x||z>intervals[f].y)continue;
                    float3 p=LocalPoint(world,z,f);
                    float3 uv=(p+float3(2.5,1.65,1))/float3(5,3.3,2);
                    // Authored coordinates are voxel CENTRES, including guard faces.
                    uv=uv*float3(127.0/128,95.0/96,63.0/64)+float3(.5/128,.5/96,.5/64);
                    float4 data=tex3Dlod(_Volume,float4(uv,_FieldParameters[f].z))*_FieldParameters[f].w;
                    source+=.30*data.rgb*(2.0/48);tau+=4*data.a*(2.0/48);
                }
                light+=transmission*source*Ratio(tau);transmission*=exp(-tau);
                if(transmission<1e-5)break;
            }
            return float4(light,1-transmission)*mask;
        }
        float3 Macro(float2 plane)
        {
            float2 camera=_View.xy+_View.zw*.5;
            float3 o=Disk(float3(plane,0)),d=Disk(float3((camera-plane)*(_Depth/4),1));
            float mu=abs(d.z)/length(d);if(mu<1e-5)return 0;
            // Actual ray/cylinder support, not midpoint-only radius rejection.
            float a=dot(d.xy,d.xy),b=2*dot(o.xy,d.xy),c=dot(o.xy,o.xy)-1;
            float nearZ=-1e10,farZ=1e10;
            if(a<1e-10){if(c>0)return 0;}
            else{float disc=b*b-4*a*c;if(disc<0)return 0;float root=sqrt(max(0,disc));nearZ=(-b-root)/(2*a);farZ=(-b+root)/(2*a);}
            float3 light=0;float transmission=1;
            [unroll] for(int k=0;k<12;k++)
            {
                int i=d.z>0?11-k:k;
                float t0=(Edges[i]-o.z)/d.z,t1=(Edges[i+1]-o.z)/d.z;
                float lo=max(nearZ,min(t0,t1)),hi=min(farZ,max(t0,t1));if(lo>=hi)continue;
                float3 diskPoint=o+d*((lo+hi)*.5);
                float z0=o.z+d.z*lo,z1=o.z+d.z*hi,zlo=min(z0,z1),zhi=max(z0,z1);
                float thin=(Cdf(zhi,.035)-Cdf(zlo,.035))/mu,core=(Cdf(zhi,.11)-Cdf(zlo,.11))/mu,dust=(Cdf(zhi,.018)-Cdf(zlo,.018))/mu;
                float2 uv=(diskPoint.xy+1.15)/2.3;uv=uv*(2047.0/2048)+.5/2048;
                uv.y=1-uv.y; // ONE authored PNG row-top to imported texture UV correction.
                float4 data=tex2Dlod(_Atlas,float4(uv,0,_MacroMip));
                float bulge=1-smoothstep(.08,.16,length(diskPoint.xy));
                float3 source=.12*data.rgb*((1-bulge)*thin+bulge*core);float tau=4*data.a*dust;
                light+=transmission*source*Ratio(tau);transmission*=exp(-tau);
            }
            return light;
        }
        float4 FinalLayer(v2f_img input):SV_Target
        {
            const float3 floorLight=float3(.0015,.0023,.004);
            float3 macro=floorLight+Macro(Plane(input.uv));
            // Gate the completed local layer again AFTER its bilinear upsample.
            // Same point-mask coordinate as LocalLayer; gates opacity AND radiance.
            float4 local=tex2D(_Local,input.uv);
            float mask=tex2Dlod(_Mask,float4(input.uv.x,1-input.uv.y,0,0)).r;
            local*=step(.5,mask);
            return float4(macro*(1-saturate(local.a))+local.rgb,1);
        }
        struct ParticleInput{float4 vertex:POSITION;float2 uv:TEXCOORD0;float2 mask:TEXCOORD1;float4 colour:COLOR;};
        struct ParticleOutput{float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;float2 mask:TEXCOORD1;float4 colour:COLOR;};
        ParticleOutput ParticleVertex(ParticleInput input)
        {
            ParticleOutput output;output.vertex=input.vertex;
            // Raw clip-space meshes must match Graphics.Blit's RT convention.
            #if UNITY_UV_STARTS_AT_TOP
            output.vertex.y=-output.vertex.y;
            #endif
            output.uv=input.uv;output.mask=input.mask;output.colour=input.colour;return output;
        }
        float4 ParticlePixel(ParticleOutput input):SV_Target
        {
            float2 p=input.uv*2-1;float r=dot(p,p);clip(1-r);
            float mask=tex2Dlod(_Mask,float4(input.mask,0,0)).r;clip(mask-.5);
            return float4(input.colour.rgb*exp(-3*r)*mask,0); // Additive radiance; retains volume opacity.
        }
        float4 AtlasProbe(v2f_img input):SV_Target{return tex2Dlod(_Atlas,float4(_ProbeUv.xy,0,0));}
        ENDHLSL
        Pass
        {
            ZTest Always ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert_img
            #pragma fragment LocalLayer
            ENDHLSL
        }
        Pass
        {
            ZTest Always ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert_img
            #pragma fragment FinalLayer
            ENDHLSL
        }
        Pass
        {
            ZTest Always ZWrite Off Cull Off Blend One One
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ParticleVertex
            #pragma fragment ParticlePixel
            ENDHLSL
        }
        Pass
        {
            ZTest Always ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert_img
            #pragma fragment AtlasProbe
            ENDHLSL
        }
    }
}
