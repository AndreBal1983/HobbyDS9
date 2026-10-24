#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;

namespace HobbyDS9.UnityReview
{
    public sealed class GalaxyV3Particle
    {
        public int Index { get; } public double X { get; } public double Y { get; } public double Z { get; }
        public double Radius { get; } public double Brightness { get; }
        internal GalaxyV3Particle(int i,double x,double y,double z,double radius,double brightness)
        {Index=i;X=x;Y=y;Z=z;Radius=radius;Brightness=brightness;}
    }
    public static class GeneratedGalaxyV3AssetData
    {
        public const string Version="galaxy-nebula-detail-v3";
        public const string VolumeHash="6357f4f109b7efb01af38f638de91677d7670703acad9acd1635a63dfed885ab";
        public const string ParticleHash="94312349f46b7c6fe0c519181929728669237c6b31407da21d2735c2d0a4e974";
        public const int MaximumParticles=256,MaximumFields=8,LocalSamples=48,MaximumLocalFetches=40000000;
        private static void Require(bool condition,string message){if(!condition)throw new InvalidDataException(message);}
        private static void Hash(byte[] bytes,string expected)
        {using(var hash=SHA256.Create())Require(BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-","").ToLowerInvariant()==expected,"V3 asset identity mismatch.");}
        public static IReadOnlyList<GalaxyV3Particle> ReadParticles(byte[] bytes)
        {
            Require(bytes!=null&&bytes.Length<=128*1024,"Invalid V3 particle bytes.");Hash(bytes!,ParticleHash);
            var root=JObject.Parse(System.Text.Encoding.UTF8.GetString(bytes!));
            Require((string?)root["version"]==Version&&(int?)root["visibleTotalCap"]==MaximumParticles,"Invalid particle contract.");
            var points=(JArray?)root["points"];Require(points?.Count==256,"Invalid particle count.");var output=new List<GalaxyV3Particle>();
            foreach(var point in points!)
            {
                var p=(JArray?)point["position"];Require(p?.Count==3,"Invalid particle coordinate.");
                int i=(int)point["index"]!;double x=(double)p![0],y=(double)p[1],z=(double)p[2],r=(double)point["radius"]!,b=(double)point["brightness"]!;
                Require(i==output.Count&&x>=-2.5&&x<=2.5&&y>=-1.65&&y<=1.65&&z>=-1&&z<=1&&r>0&&r<=.1&&b>=0&&b<=1,"Invalid particle values.");
                output.Add(new GalaxyV3Particle(i,x,y,z,r,b));
            }
            return output.AsReadOnly();
        }
        public static void UploadVolume(byte[] bytes,Action<int,int,int,int,byte[]> upload)
        {
            Require(bytes!=null&&bytes.Length==128*96*64*4,"Invalid V3 volume bytes.");Hash(bytes!,VolumeHash);
            int width=128,height=96,depth=64,level=0;var data=bytes!;
            while(true)
            {
                GuardFaces(data,width,height,depth,level==0);upload(level,width,height,depth,data);
                if(width==1&&height==1&&depth==1)break;
                int w=Math.Max(1,width/2),h=Math.Max(1,height/2),d=Math.Max(1,depth/2);var next=new byte[w*h*d*4];
                for(int z=0;z<d;z++)for(int y=0;y<h;y++)for(int x=0;x<w;x++)for(int channel=0;channel<4;channel++)
                {
                    int sum=0,count=0;
                    for(int dz=0;dz<Math.Min(2,depth);dz++)for(int dy=0;dy<Math.Min(2,height);dy++)for(int dx=0;dx<Math.Min(2,width);dx++)
                    {sum+=data[4*(Math.Min(width-1,x*2+dx)+width*(Math.Min(height-1,y*2+dy)+height*Math.Min(depth-1,z*2+dz)))+channel];count++;}
                    next[4*(x+w*(y+h*z))+channel]=(byte)((sum+count/2)/count);
                }
                data=next;width=w;height=h;depth=d;level++;
            }
        }
        private static void GuardFaces(byte[] pixels,int width,int height,int depth,bool validateOnly)
        {
            for(int z=0;z<depth;z++)for(int y=0;y<height;y++)for(int x=0;x<width;x++)
            {
                if(x!=0&&x!=width-1&&y!=0&&y!=height-1&&z!=0&&z!=depth-1)continue;
                for(int c=0;c<4;c++){int i=4*(x+width*(y+height*z))+c;if(validateOnly)Require(pixels[i]==0,"V3 volume guard face is not zero.");else pixels[i]=0;}
            }
        }
        public static (int width,int height) LocalTarget(int finalWidth,int finalHeight,int fields)
        {
            Require(finalWidth>=1&&finalWidth<=960&&finalHeight>=1&&finalHeight<=540&&fields>=0&&fields<=8,"Invalid V3 render bounds.");
            if(fields==0)return(1,1);
            double scale=Math.Min(1,Math.Sqrt((double)MaximumLocalFetches/(LocalSamples*fields*finalWidth*finalHeight)));
            return(Math.Max(1,(int)Math.Floor(finalWidth*scale)),Math.Max(1,(int)Math.Floor(finalHeight*scale)));
        }
        public static (double x,double y) MaskGuard(int localWidth,int localHeight,int finalWidth,int finalHeight,double uiWidth,double uiHeight)
            =>(Math.Max(3,Math.Ceiling(1+(double)localWidth/finalWidth+2*localWidth/uiWidth)+1),
                Math.Max(3,Math.Ceiling(1+(double)localHeight/finalHeight+2*localHeight/uiHeight)+1));
    }
}
