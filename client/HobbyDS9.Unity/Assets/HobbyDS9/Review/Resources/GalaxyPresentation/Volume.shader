Shader "HobbyDS9/GalaxyPresentation/Volume"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            ZTest Always ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert_img
            #pragma fragment Frag
            #include "UnityCG.cginc"
            sampler3D _Field;
            float4 _View;
            float _Depth,_GalaxySeed;
            int _CloudCount;
            float4 _Clouds[8],_CloudParameters[8];
            float Guard(float3 p) { float3 f=smoothstep(0,.18,1-abs(p));return f.x*f.y*f.z; }
            float4 Frag(v2f_img input) : SV_Target
            {
                // Texture rows follow UI top-down coordinates explicitly. Navigation
                // never uses this decorative perspective transform.
                float2 uv=float2(input.uv.x,1-input.uv.y);
                float2 plane=_View.xy+uv*_View.zw;
                float2 cameraCenter=_View.xy+.5*_View.zw;
                // Linear-light floor: UI composition displays this in sRGB. A .03
                // linear pedestal becomes conspicuously grey rather than dark space.
                float3 colour=float3(.0015,.0023,.004);
                float3 light=0;float opacity=0;
                // Continuous trilinear 3D samples, front-to-back optical integration.
                // No billboard surfaces or alpha-composited 32-plane geometry.
                [loop] for(int k=0;k<32;k++)
                {
                    float z=.32-(k+.5)*(.64/32);
                    float q=4/(4-z*_Depth);
                    float2 p=cameraCenter+(plane-cameraCenter)/q;
                    float3 box=float3((p-.5)*2,z/.32);
                    float fade=Guard(box);
                    float2 disk=(p-.5)*float2(2.05,2.30);
                    float radius=length(disk),angle=atan2(disk.y,disk.x);
                    float n=tex3Dlod(_Field,float4(p*.75+.1,z+0.5,1)).g;
                    float arm=.5+.5*cos(angle*3+radius*11+_GalaxySeed*6+n*1.5);
                    float diffuse=exp(-radius*radius*3.8)*(.025+.13*pow(arm,4));
                    float bulge=exp(-radius*radius*52)*.38;
                    float dust=smoothstep(.30,.65,n)*.82;
                    const float stepLength=.64/32;
                    float a=(diffuse+bulge)*exp(-z*z*40)*fade*(1-dust)*1.5*stepLength;
                    float3 emission=float3(1,.90,.75)*a;
                    [loop] for(int i=0;i<_CloudCount;i++)
                    {
                        float4 cloud=_Clouds[i],param=_CloudParameters[i];
                        float2 d=(p-cloud.xy)/cloud.zw;
                        float s=sin(param.x),c=cos(param.x);
                        float3 local=float3(c*d.x-s*d.y,s*d.x+c*d.y,z/.32);
                        if(all(abs(local)<1))
                        {
                            float raw=tex3Dlod(_Field,float4(local*.5+.5,0)).r;
                            // Remove the low-density pedestal, retain asymmetric
                            // ridges and open cavities instead of filling the box.
                            float density=pow(saturate((raw-.065)/.935),1.65)*Guard(local);
                            float3 tint=param.y<.5?float3(.12,.48,.65):param.y<1.5?float3(.16,.42,.48):float3(.42,.19,.46);
                            float ca=density*3*stepLength;a+=ca;emission+=tint*ca;
                        }
                    }
                    float alpha=1-exp(-a);
                    light+=(1-opacity)*emission/max(a,.00001)*alpha;opacity+=(1-opacity)*alpha;
                }
                colour=colour*(1-opacity)+light;
                return float4(saturate(colour),1);
            }
            ENDHLSL
        }
    }
}
