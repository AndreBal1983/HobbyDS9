using HobbyDS9.Contracts;
using HobbyDS9.UnityReview;
using Xunit;

namespace HobbyDS9.UnityReviewAdapter.Tests;
public sealed class SolReadabilityTests
{
    static readonly string[] Ids={"planet.mercury","planet.venus","planet.sol","planet.mars","planet.jupiter","planet.saturn","planet.uranus","planet.neptune"};
    [Theory][InlineData(520,260)][InlineData(640,305)][InlineData(1200,420)][InlineData(1900,420)]
    public void SchematicBodiesStaySeparatedAndEveryLabelFitsAtAllOverviewRotations(double width,double height)
    {
        const double elevation=24*Math.PI/180;double aspect=width/height,fraction=1-240/width;
        double size=Math.Max(46*Math.Sin(elevation)+1.5,47.5/(aspect*fraction))*1.1;
        for(int yaw=-180;yaw<=180;yaw+=5)
        {
            var points=Ids.Select((id,i)=>{double a=(SolSchematicLayout.Angle(i)+yaw)*Math.PI/180,r=SolSchematicLayout.Radius(i);return new SolSchematicLayout.Point(id,width/2+r*Math.Cos(a)/size*height/2,height/2+r*Math.Sin(a)*Math.Sin(elevation)/size*height/2);}).ToArray();
            for(int i=0;i<points.Length;i++)
            {
                Assert.InRange(points[i].X,120,width-120);Assert.InRange(points[i].Y,12,height-12);
                for(int j=0;j<i;j++)Assert.True(Math.Sqrt(Math.Pow(points[i].X-points[j].X,2)+Math.Pow(points[i].Y-points[j].Y,2))>height*.062,"Actual planet glyphs must not overlap.");
                Assert.True(Math.Sqrt(Math.Pow(points[i].X-width/2,2)+Math.Pow(points[i].Y-height/2,2))>height*(.075+.062)/2,"The central Sun must not be covered by a planet glyph.");
            }
            var labels=SolSchematicLayout.Labels(points,width,height);Assert.Equal(8,labels.Count);
            foreach(var label in labels){Assert.InRange(label.X,0,width-108);Assert.InRange(label.Y,0,height-44);}
            foreach(var rail in labels.GroupBy(p=>p.X))foreach(var pair in rail.OrderBy(p=>p.Y).Zip(rail.OrderBy(p=>p.Y).Skip(1)))Assert.True(pair.Second.Y-pair.First.Y>=44);
        }
        Assert.Equal(Enumerable.Range(0,8),Ids.Select(SolSchematicLayout.Ordinal));Assert.Equal(1,SolSchematicLayout.Au(149600000));
        Assert.All(Ids,id=>Assert.InRange(SolSchematicLayout.GlyphFraction(id,true),.05,.062));
    }
    [Theory][InlineData("planet.mercury","mercury")][InlineData("planet.venus","venus_atmosphere")][InlineData("planet.jupiter","jupiter")]
    [InlineData("planet.saturn","saturn")][InlineData("planet.uranus","uranus")][InlineData("planet.neptune","neptune")]
    public void ExplicitPublicSolAppearanceDoesNotRevealSurveyEnvironmentOrCapabilities(string id,string map)
    {
        var body=new ColonisationPlanetSnapshot(new ColonisationPlanetObservationDto{PlanetId=id,BodyId="body."+id,SystemId="observed.sol",DisplayName=id,Surveyed=false,Environment=new PlanetDefinitionDto{PlanetClass="Temperate",Atmosphere="hidden",Pressure=10000}});
        var neutral=PlanetVisualProfile.From(body);var known=PlanetVisualProfile.From(body,true);
        Assert.Null(neutral.AuthoredMap);Assert.Equal(map,known.AuthoredMap);Assert.Equal(PlanetSurfaceKind.Unknown,known.Kind);Assert.False(known.HasAtmosphere);Assert.Equal(0,known.CloudCoverage);Assert.Equal(0,known.AtmosphereStrength);Assert.Contains("UNSURVEYED",known.Caption);
        Assert.False(body.Surveyed);Assert.Equal("Temperate",body.PlanetClass); // Malformed hidden input stays unused, not rewritten.
        var other=new ColonisationPlanetSnapshot(new ColonisationPlanetObservationDto{PlanetId="random.planet",BodyId="random.body",SystemId="random.system",DisplayName="Jupiter",Surveyed=false});
        Assert.Null(PlanetVisualProfile.From(other,true).AuthoredMap);
    }
}
