using System.Collections;
using HobbyDS9.UnityReview;
using Newtonsoft.Json.Linq;
using Xunit;
namespace HobbyDS9.UnityReviewAdapter.Tests;
public sealed class FactionThemeTokensTests
{
    static string ArtPath(string name)
    {
        for(var dir=new DirectoryInfo(AppContext.BaseDirectory);dir!=null;dir=dir.Parent)
        {var file=Path.Combine(dir.FullName,"art","faction-style-v1",name);if(File.Exists(file))return file;}
        throw new FileNotFoundException("Actual frozen Art schema is required.");
    }
    static string Audit()=>File.ReadAllText(ArtPath("catalogue-audit.json"));
    static string Json()=>File.ReadAllText(ArtPath("theme-tokens.json"));
    static FactionThemeTokens Tokens(){Assert.True(FactionThemeTokens.TryParse(Json(),Audit(),out var tokens,out var error),error);return tokens!;}
    [Fact]public void ActualAuditedCatalogueBindsExactlyThreeCivilizationsAndTwoIndependentMinors()
    {
        var tokens=Tokens();var audit=JObject.Parse(File.ReadAllText(ArtPath("catalogue-audit.json")));
        Assert.Equal((string)audit["auditPin"]!,tokens.CatalogueAuditPin);Assert.Equal(FactionThemeTokens.ProposedStatus,tokens.ArtifactStatus);
        Assert.Equal(audit["civilizations"]!.Select(c=>(string)c["id"]!).OrderBy(x=>x,StringComparer.Ordinal),tokens.Profiles.SelectMany(p=>p.CivilizationDefinitionIds).OrderBy(x=>x,StringComparer.Ordinal));
        Assert.Equal(audit["minorSocieties"]!.Select(c=>(string)c["id"]!).OrderBy(x=>x,StringComparer.Ordinal),tokens.Profiles.SelectMany(p=>p.MinorDefinitionIds).OrderBy(x=>x,StringComparer.Ordinal));
        Assert.Equal(8,tokens.Profiles.Count);Assert.Equal(3,tokens.Profiles.Sum(p=>p.CivilizationDefinitionIds.Count));Assert.Equal(2,tokens.Profiles.Sum(p=>p.MinorDefinitionIds.Count));
        Assert.Equal(6,tokens.FamilyTokens.Values.Select(f=>f.Frame+"/"+f.SectionMotif).Distinct().Count());
    }
    [Theory]
    [InlineData(FactionThemeIdentityKind.Civilization,"civilization.federation","federation")]
    [InlineData(FactionThemeIdentityKind.Civilization,"civilization.cardassian","cardassian")]
    [InlineData(FactionThemeIdentityKind.Civilization,"civilization.dominion","dominion")]
    [InlineData(FactionThemeIdentityKind.Minor,"minor.ilex.v1","ilex")]
    [InlineData(FactionThemeIdentityKind.Minor,"minor.namar.v1","namar")]
    public void ExactLawfulScopeResolvesOnlyItsListedIdentity(FactionThemeIdentityKind kind,string id,string expected)
    {var result=Tokens().Resolve(new(kind,id,true));Assert.Equal(expected,result.Profile.Id);Assert.False(result.IsProfilePreview);}
    [Theory]
    [InlineData(FactionThemeIdentityKind.Civilization,"minor.ilex.v1",true)]
    [InlineData(FactionThemeIdentityKind.Minor,"civilization.federation",true)]
    [InlineData(FactionThemeIdentityKind.Civilization,"civilization.federation",false)]
    [InlineData(FactionThemeIdentityKind.Civilization,"civilization.klingon",true)]
    [InlineData(FactionThemeIdentityKind.Civilization,"civilization.romulan",true)]
    [InlineData(FactionThemeIdentityKind.Civilization,"Federation",true)]
    [InlineData(FactionThemeIdentityKind.Civilization,"CIVILIZATION.FEDERATION",true)]
    [InlineData(FactionThemeIdentityKind.Civilization,"custom.federation",true)]
    [InlineData((FactionThemeIdentityKind)99,"civilization.federation",true)]
    [InlineData(FactionThemeIdentityKind.Civilization,null,true)]
    public void UnknownUndisclosedCustomAndWrongScopeRemainNeutral(FactionThemeIdentityKind kind,string? id,bool disclosed)
    {Assert.Equal("neutral",Tokens().Resolve(new(kind,id,disclosed)).Profile.Id);Assert.Equal("neutral",Tokens().Resolve(default).Profile.Id);}
    [Theory][InlineData("klingon")][InlineData("romulan")]
    public void ProposedProfilePreviewDoesNotRegisterCatalogueIdentity(string id)
    {var tokens=Tokens();var preview=tokens.PreviewProfile(id);Assert.True(preview.IsProfilePreview);Assert.Equal(id,preview.Profile.Id);Assert.Empty(preview.Profile.CivilizationDefinitionIds);Assert.Empty(preview.Profile.MinorDefinitionIds);Assert.Equal("neutral",tokens.Resolve(new(FactionThemeIdentityKind.Civilization,"civilization."+id,true)).Profile.Id);}
    [Fact]public void ExplicitAuditedAuthoringMappingDoesNotBecomeAHardcodedGameplayAdmissionRule()
    {
        var theme=JObject.Parse(Json());var audit=JObject.Parse(Audit());const string authored="civilization.authored-presentation-test";
        theme["profiles"]![0]!["civilizationDefinitionIds"]![0]=authored;audit["civilizations"]![0]!["id"]=authored;audit["profiles"]![0]!["civilizationDefinitionIds"]![0]=authored;
        Assert.True(FactionThemeTokens.TryParse(theme.ToString(),audit.ToString(),out var tokens,out var error),error);
        Assert.Equal("federation",tokens!.Resolve(new(FactionThemeIdentityKind.Civilization,authored,true)).Profile.Id);
        Assert.Equal("neutral",tokens.Resolve(new(FactionThemeIdentityKind.Civilization,"civilization.federation",true)).Profile.Id);
    }
    [Fact]public void MissingDifferentOrMisScopedAuditCannotAuthorizeThemeMapping()
    {
        Assert.False(FactionThemeTokens.TryParse(Json(),null,out _,out _));
        var audit=JObject.Parse(Audit());audit["auditPin"]=new string('0',40);Assert.False(FactionThemeTokens.TryParse(Json(),audit.ToString(),out _,out _));
        audit=JObject.Parse(Audit());audit["civilizations"]![0]!["id"]="minor.ilex.v1";Assert.False(FactionThemeTokens.TryParse(Json(),audit.ToString(),out _,out _));
    }
    [Fact]public void ReturnedCollectionsRejectMutationAndRepeatedLookupKeepsSemanticTokens()
    {
        var tokens=Tokens();var before=tokens.Resolve(new(FactionThemeIdentityKind.Civilization,"civilization.federation",true));
        Assert.Throws<NotSupportedException>(()=>((IDictionary<string,string>)before.Profile.Colors)["background"]="#FFFFFF");
        Assert.Throws<NotSupportedException>(()=>((IList)tokens.Profiles).Clear());
        Assert.Throws<NotSupportedException>(()=>((IList)before.Profile.CivilizationDefinitionIds).Clear());
        Assert.Throws<NotSupportedException>(()=>((IDictionary<string,FactionThemeState>)tokens.States).Clear());
        Assert.Equal(44,tokens.MinimumControlPx);Assert.Equal(0,tokens.ReducedMotionTransitionMs);Assert.False(tokens.ReducedMotionAutoRotation);Assert.False(tokens.Flashing);
        foreach(var p in tokens.Profiles){Assert.Equal(5,p.Colors.Count);Assert.Equal(8,tokens.States.Count);}
        Assert.Equal("#EFB16B",tokens.States["active"].Color);Assert.Equal("clock",tokens.States["active"].Shape);Assert.Equal("Not reported",tokens.States["unknown"].Label);
    }
    [Theory]
    [InlineData("schemaVersion","2")]
    [InlineData("artifactStatus","\"accepted\"")]
    [InlineData("presentationOnly","false")]
    [InlineData("shared.layout.minimumControlPx","43")]
    [InlineData("shared.typography.bodyPx","12")]
    [InlineData("shared.motion.flashing","true")]
    [InlineData("shared.motion.reducedMotionAutoRotation","true")]
    [InlineData("shared.audio.newRuntimeBindings","true")]
    [InlineData("shared.audio.previewAudioEnabled","true")]
    [InlineData("shared.states.warning.color","\"#91C9AD\"")]
    [InlineData("shared.states.unknown.label","\"Current\"")]
    [InlineData("profiles[1].civilizationDefinitionIds","[\"civilization.klingon\"]")]
    [InlineData("profiles[5].civilizationDefinitionIds","[\"minor.ilex.v1\"]")]
    [InlineData("profiles[0].colors.surface","\"invalid\"")]
    [InlineData("profiles[0].colors.background","\"#FFFFFF\"")]
    [InlineData("profiles[0].family","\"romulan\"")]
    [InlineData("families.klingon.frame","\"asymmetric-elbow-rail\"")]
    public void InvalidPolicyOrSemanticMutationFailsClosed(string path,string replacement)
    {var json=JObject.Parse(Json());json.SelectToken(path)!.Replace(JToken.Parse(replacement));Assert.False(FactionThemeTokens.TryParse(json.ToString(),Audit(),out var tokens,out var error));Assert.Null(tokens);Assert.NotEmpty(error);}
    [Fact]public void BoundedReaderRejectsDuplicateKeysExtraContentUnknownFieldsCommentsAndMissingFields()
    {
        var json=Json();var missing=JObject.Parse(json);missing.Remove("profiles");var extra=JObject.Parse(json);extra["hiddenRoot"]=true;
        foreach(var invalid in new[]{"",new string(' ',FactionThemeTokens.MaximumJsonCharacters+1),new string('[',20)+new string(']',20),"{\"schemaVersion\":1,\"schemaVersion\":1}",json+"{}","/*comment*/"+json,json.Replace('"','\''),json.Insert(json.LastIndexOf('}'),","),json.Replace("\"schemaVersion\": 1","\"schemaVersion\": 0x1"),json.Replace("\"schemaVersion\": 1","\"schemaVersion\": 01"),missing.ToString(),extra.ToString(),json.Replace("\"schemaVersion\": 1","\"schemaVersion\": 1.0")})
        {Assert.False(FactionThemeTokens.TryParse(invalid,Audit(),out var tokens,out _));Assert.Null(tokens);}
        Assert.False(FactionThemeTokens.TryParse(null,Audit(),out _,out _));
    }
}
