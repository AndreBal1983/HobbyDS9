using HobbyDS9.Contracts;
using HobbyDS9.UnityReview;
using Xunit;
using Xunit.Abstractions;

namespace HobbyDS9.UnityReviewAdapter.Tests;

public sealed class GalaxyV3AssetTests
{
    private readonly ITestOutputHelper output;
    public GalaxyV3AssetTests(ITestOutputHelper output){this.output=output;}
    private static byte[] Bytes(string file)=>File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"galaxy-v3",file));
    [Fact] public void OriginalParticleIdentityValuesAndWholeVolumeGuardedMipsAreVerified()
    {
        var particles=GeneratedGalaxyV3AssetData.ReadParticles(Bytes("particles.json"));Assert.Equal(256,particles.Count);
        Assert.Equal(-.9251968503937008,particles[0].X,12);Assert.Throws<NotSupportedException>(()=>((IList<GalaxyV3Particle>)particles).Clear());
        int levels=0,total=0;GeneratedGalaxyV3AssetData.UploadVolume(Bytes("nebula.bytes"),(level,w,h,d,data)=>{
            Assert.Equal(levels++,level);Assert.Equal(w*h*d*4,data.Length);total+=data.Length;
            for(int z=0;z<d;z++)for(int y=0;y<h;y++)for(int x=0;x<w;x++)if(x==0||x==w-1||y==0||y==h-1||z==0||z==d-1)
                for(int c=0;c<4;c++)Assert.Equal(0,data[4*(x+w*(y+h*z))+c]);
        });
        Assert.Equal(8,levels);Assert.InRange(total,3145728,3600000);output.WriteLine($"Actual guarded volume levels={levels}, payloadBytes={total}");
    }
    [Fact] public void CorruptOriginalAssetsFailBeforeAnyUploadOrPublication()
    {
        var raw=Bytes("nebula.bytes");raw[raw.Length/2]^=1;bool uploaded=false;
        Assert.Throws<InvalidDataException>(()=>GeneratedGalaxyV3AssetData.UploadVolume(raw,(_,_,_,_,_)=>uploaded=true));Assert.False(uploaded);
        var particles=Bytes("particles.json");particles[particles.Length/2]^=1;
        Assert.Throws<InvalidDataException>(()=>GeneratedGalaxyV3AssetData.ReadParticles(particles));
    }
    [Theory] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(8)]
    public void OnlyLocalResolutionAdaptsToConservativeFetchCap(int fields)
    {
        var local=GeneratedGalaxyV3AssetData.LocalTarget(960,540,fields);
        int upper=48*fields*local.width*local.height;Assert.InRange(upper,1,40000000);
        if(fields==1){Assert.Equal(960,local.width);Assert.Equal(540,local.height);}
        output.WriteLine($"Fields={fields}, finalMacro=960x540, local={local.width}x{local.height}, conservativeFetchBound={upper}");
    }
    [Theory] [InlineData(1)] [InlineData(8)]
    public void BothBilinearStagesPreserveFinalExteriorZeroWithAdaptiveLocalResolution(int fields)
    {
        const int fw=960,fh=540,uw=1280,uh=720;var size=GeneratedGalaxyV3AssetData.LocalTarget(fw,fh,fields);
        var guard=GeneratedGalaxyV3AssetData.MaskGuard(size.width,size.height,fw,fh,uw,uh);
        var view=new GeneratedGalaxyViewport();view.SetMap(100,100);view.Resize(uw,uh);view.Fit();view.Pan(.375,-.625);
        var region=new GeneratedGalaxyRegionDto{NodeId="triangle",Boundary=new(){new(){X=0,Y=0},new(){X=100,Y=0},new(){X=0,Y=100}}};
        var mask=GeneratedGalaxyChartedMask.Build(new[]{region},view,uw,uh,size.width,size.height,guard.x,guard.y);Assert.True(mask.Succeeded,mask.Reason);
        double Local(int x,int y)=>mask.Pixels[Math.Clamp(x,0,size.width-1)+Math.Clamp(y,0,size.height-1)*size.width]/255.0;
        double Final(int x,int y)
        {
            double px=(Math.Clamp(x,0,fw-1)+.5)*size.width/fw-.5,py=(Math.Clamp(y,0,fh-1)+.5)*size.height/fh-.5;
            int ix=(int)Math.Floor(px),iy=(int)Math.Floor(py);double a=px-ix,b=py-iy;
            return(1-b)*((1-a)*Local(ix,iy)+a*Local(ix+1,iy))+b*((1-a)*Local(ix,iy+1)+a*Local(ix+1,iy+1));
        }
        for(int y=0;y<uh;y++)for(int x=0;x<uw;x++)
        {
            double wx=view.WorldX(x+.5),wy=view.WorldY(y+.5);if(wx>=0&&wy>=0&&wx+wy<=100)continue;
            double px=(x+.5)*fw/uw-.5,py=(y+.5)*fh/uh-.5;int ix=(int)Math.Floor(px),iy=(int)Math.Floor(py);double a=px-ix,b=py-iy;
            Assert.Equal(0,(1-b)*((1-a)*Final(ix,iy)+a*Final(ix+1,iy))+b*((1-a)*Final(ix,iy+1)+a*Final(ix+1,iy+1)));
        }
    }
}
