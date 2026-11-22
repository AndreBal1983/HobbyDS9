#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HobbyDS9.UnityReview
{
    public enum FactionThemeIdentityKind { Civilization=1,Minor=2 }
    // The caller supplies only a selected own identity or an already lawfully disclosed identity.
    // This presentation value does not discover identities or confer authorization.
    public readonly struct FactionThemeIdentity
    {
        public FactionThemeIdentityKind Kind {get;}
        public string? DefinitionId {get;}
        public bool LawfullyDisclosed {get;}
        public FactionThemeIdentity(FactionThemeIdentityKind kind,string? definitionId,bool lawfullyDisclosed)
        {Kind=kind;DefinitionId=definitionId;LawfullyDisclosed=lawfullyDisclosed;}
    }
    public sealed class FactionThemeState
    {
        public string Color {get;} public string Label {get;} public string Shape {get;}
        internal FactionThemeState(string color,string label,string shape){Color=color;Label=label;Shape=shape;}
    }
    public sealed class FactionThemeFamily
    {
        public string Id {get;} public string Frame {get;} public string SectionMotif {get;}
        public int CornerRadius {get;} public int CutSize {get;} public int HeadingWeight {get;}
        internal FactionThemeFamily(string id,string frame,string motif,int corner,int cut,int weight)
        {Id=id;Frame=frame;SectionMotif=motif;CornerRadius=corner;CutSize=cut;HeadingWeight=weight;}
    }
    public sealed class FactionThemeProfile
    {
        public string Id {get;} public string DisplayName {get;} public string CatalogueStatus {get;} public string SourceStatus {get;}
        public FactionThemeFamily Family {get;} public IReadOnlyDictionary<string,string> Colors {get;}
        public IReadOnlyList<string> CivilizationDefinitionIds {get;} public IReadOnlyList<string> MinorDefinitionIds {get;}
        internal FactionThemeProfile(string id,string name,string status,string source,FactionThemeFamily family,IDictionary<string,string> colors,IEnumerable<string> civilizations,IEnumerable<string> minors)
        {Id=id;DisplayName=name;CatalogueStatus=status;SourceStatus=source;Family=family;Colors=FactionThemeTokens.Freeze(colors);CivilizationDefinitionIds=Array.AsReadOnly(civilizations.ToArray());MinorDefinitionIds=Array.AsReadOnly(minors.ToArray());}
    }
    public sealed class FactionThemeSelection
    {
        public FactionThemeProfile Profile {get;} public bool IsProfilePreview {get;} public string ArtifactStatus {get;}
        internal FactionThemeSelection(FactionThemeProfile profile,bool preview,string status){Profile=profile;IsProfilePreview=preview;ArtifactStatus=status;}
    }
    // Strict schema-1 authoring parser. No Resources, file/provider access, mutable JSON tree,
    // gameplay catalogue registration, audio binding or dependency on campaign/private state.
    public sealed class FactionThemeTokens
    {
        public const int MaximumJsonCharacters=65536;
        public const string ProposedStatus="proposed-design-no-default-activation";
        private static readonly string[] ProfileIds={"federation","klingon","cardassian","romulan","dominion","ilex","namar","neutral"};
        private static readonly string[] StateKeys={"available","active","completed","warning","error","information","unknown","unavailable"};
        private static readonly string[] StateLabels={"Available","Active","Completed","Warning","Rejected","Information","Not reported","Unavailable"};
        private static readonly string[] StateShapes={"check","clock","double-check","triangle","cross","info","dash","bar"};
        private static readonly string[] StateColors={"#91C9AD","#EFB16B","#B9ACE6","#F3C478","#FF91A4","#91C9EC","#BCC8D8","#BCC8D8"};
        private static readonly string[] Families={"federation","klingon","cardassian","romulan","dominion","neutral"};
        private static readonly string[] Frames={"asymmetric-elbow-rail","stepped-angular-brackets","layered-curved-junctions","swept-open-junctions","interlocked-ribs","plain-rectilinear"};
        private static readonly string[] Motifs={"segmented-rounded-rail","stepped-tabs","nested-lens-edges","paired-open-spines","symmetric-faceted-ribs","simple-rules"};
        public string ArtifactStatus=>ProposedStatus;
        public string CatalogueAuditPin {get;}
        public IReadOnlyDictionary<string,string> SharedColors {get;}
        public IReadOnlyDictionary<string,FactionThemeState> States {get;}
        public IReadOnlyDictionary<string,FactionThemeFamily> FamilyTokens {get;}
        public IReadOnlyList<FactionThemeProfile> Profiles {get;}
        public IReadOnlyList<int> SpacingPx {get;}
        public int BodyPx {get;} public int SecondaryPx {get;} public int NumericPx {get;} public int HeadingPx {get;} public int LineHeightPx {get;}
        public int MinimumControlPx {get;} public int CompactRowPx {get;} public int NavigationWidthPx {get;}
        public int DefaultReviewWidth {get;} public int DefaultReviewHeight {get;}
        public int TransitionMs {get;} public int ReducedMotionTransitionMs=>0;
        public bool ReducedMotionAutoRotation=>false; public bool Flashing=>false;
        private readonly IReadOnlyDictionary<string,FactionThemeProfile> byId,civilizations,minors;
        internal static IReadOnlyDictionary<string,T> Freeze<T>(IDictionary<string,T> values)=>new ReadOnlyDictionary<string,T>(new Dictionary<string,T>(values,StringComparer.Ordinal));
        private FactionThemeTokens(JObject root,JObject audit)
        {
            Keys(root,"schemaVersion","artifactStatus","catalogueAuditPin","presentationOnly","shared","families","profiles","bindingPolicy");
            Require(Integer(root,"schemaVersion",1,1)==1&&Text(root,"artifactStatus")==ProposedStatus&&Boolean(root,"presentationOnly"));
            CatalogueAuditPin=Text(root,"catalogueAuditPin");Require(CatalogueAuditPin.Length==40&&CatalogueAuditPin.All(Uri.IsHexDigit));
            Require(Integer(audit,"schemaVersion",1,1)==1&&Text(audit,"auditPin")==CatalogueAuditPin);
            string[] AuditedIds(string key)=>Array(audit,key,null).Select(t=>{Require(t is JObject);return Text((JObject)t,"id");}).ToArray();
            var allowedCivilizations=AuditedIds("civilizations");var allowedMinors=AuditedIds("minorSocieties");
            Require(allowedCivilizations.Distinct(StringComparer.Ordinal).Count()==allowedCivilizations.Length&&allowedMinors.Distinct(StringComparer.Ordinal).Count()==allowedMinors.Length&&!allowedCivilizations.Intersect(allowedMinors,StringComparer.Ordinal).Any());
            var auditedProfiles=new Dictionary<string,JObject>(StringComparer.Ordinal);
            foreach(var value in Array(audit,"profiles",8)){Require(value is JObject);var row=(JObject)value;auditedProfiles.Add(Text(row,"id"),row);}
            var shared=Object(root,"shared");Keys(shared,"colors","states","typography","layout","motion","audio");
            SharedColors=Freeze(Colors(Object(shared,"colors"),"textPrimary","textSecondary","ink","controlBorder","focus"));
            var states=Object(shared,"states");Keys(states,StateKeys);var parsedStates=new Dictionary<string,FactionThemeState>(StringComparer.Ordinal);
            for(var i=0;i<StateKeys.Length;i++)
            {
                var state=Object(states,StateKeys[i]);Keys(state,"color","label","shape");
                var color=Hex(state,"color");var label=Text(state,"label");var shape=Text(state,"shape");
                Require(color==StateColors[i]&&label==StateLabels[i]&&shape==StateShapes[i]);parsedStates.Add(StateKeys[i],new FactionThemeState(color,label,shape));
            }
            States=Freeze(parsedStates);
            var type=Object(shared,"typography");Keys(type,"family","bodyPx","secondaryPx","numericPx","headingPx","lineHeightPx","tabularNumerals");
            Require(Text(type,"family")=="project-default-sans"&&Boolean(type,"tabularNumerals"));
            BodyPx=Integer(type,"bodyPx",18,32);SecondaryPx=Integer(type,"secondaryPx",16,32);NumericPx=Integer(type,"numericPx",18,40);HeadingPx=Integer(type,"headingPx",24,48);LineHeightPx=Integer(type,"lineHeightPx",24,48);
            var layout=Object(shared,"layout");Keys(layout,"defaultReviewWidth","defaultReviewHeight","minimumControlPx","compactRowPx","navigationWidthPx","spacingPx");
            DefaultReviewWidth=Integer(layout,"defaultReviewWidth",1920,1920);DefaultReviewHeight=Integer(layout,"defaultReviewHeight",1080,1080);
            MinimumControlPx=Integer(layout,"minimumControlPx",44,64);CompactRowPx=Integer(layout,"compactRowPx",MinimumControlPx,96);NavigationWidthPx=Integer(layout,"navigationWidthPx",144,256);
            var spacing=Array(layout,"spacingPx",6).Select(t=>ScalarInt(t,0,64)).ToArray();Require(spacing.SequenceEqual(new[]{4,8,12,16,24,32}));SpacingPx=System.Array.AsReadOnly(spacing);
            var motion=Object(shared,"motion");Keys(motion,"transitionMs","reducedMotionTransitionMs","reducedMotionAutoRotation","flashing");
            TransitionMs=Integer(motion,"transitionMs",0,250);Require(Integer(motion,"reducedMotionTransitionMs",0,0)==0&&!Boolean(motion,"reducedMotionAutoRotation")&&!Boolean(motion,"flashing"));
            var audio=Object(shared,"audio");Keys(audio,"roles","newRuntimeBindings","previewAudioEnabled");Require(!Boolean(audio,"newRuntimeBindings")&&!Boolean(audio,"previewAudioEnabled"));
            Require(Strings(audio,"roles",8).SequenceEqual(new[]{"press","accepted","rejected","processing","navigation","search","alert","music"}));
            var familyRows=Object(root,"families");Keys(familyRows,Families);var families=new Dictionary<string,FactionThemeFamily>(StringComparer.Ordinal);
            for(var i=0;i<Families.Length;i++)
            {
                var f=Object(familyRows,Families[i]);Keys(f,"frame","cornerRadius","cutSize","sectionMotif","headingWeight");
                Require(Text(f,"frame")==Frames[i]&&Text(f,"sectionMotif")==Motifs[i]);
                families.Add(Families[i],new FactionThemeFamily(Families[i],Frames[i],Motifs[i],Integer(f,"cornerRadius",0,40),Integer(f,"cutSize",0,24),Integer(f,"headingWeight",600,700)));
            }
            FamilyTokens=Freeze(families);var profiles=new Dictionary<string,FactionThemeProfile>(StringComparer.Ordinal);
            foreach(var row in Array(root,"profiles",8))
            {
                var p=row as JObject;Require(p!=null);Keys(p!,"id","displayName","family","catalogueStatus","civilizationDefinitionIds","minorDefinitionIds","colors","sourceStatus","audio");
                var id=Text(p!,"id");Require(ProfileIds.Contains(id,StringComparer.Ordinal)&&!profiles.ContainsKey(id));
                var family=Text(p!,"family");Require(family==(id=="ilex"||id=="namar"?"neutral":id));
                var cs=Strings(p!,"civilizationDefinitionIds",null);var ms=Strings(p!,"minorDefinitionIds",null);
                Require(auditedProfiles.TryGetValue(id,out var audited));
                Require(Text(audited!,"family")==family&&cs.SequenceEqual(Strings(audited!,"civilizationDefinitionIds",null))&&ms.SequenceEqual(Strings(audited!,"minorDefinitionIds",null)));
                Require(cs.All(c=>allowedCivilizations.Contains(c,StringComparer.Ordinal))&&ms.All(m=>allowedMinors.Contains(m,StringComparer.Ordinal)));
                var a=Object(p!,"audio");Keys(a,"semanticRoleSet","cueFamily","bindingStatus");Require(Text(a,"semanticRoleSet")=="ui-semantic-v1"&&Text(a,"cueFamily")==id&&Text(a,"bindingStatus")=="proposal-no-new-runtime-binding");
                var colors=Colors(Object(p!,"colors"),"background","surface","raised","brandPrimary","brandSecondary");
                foreach(var background in new[]{colors["background"],colors["surface"],colors["raised"]})
                    Require(Contrast(SharedColors["textPrimary"],background)>=4.5&&Contrast(SharedColors["textSecondary"],background)>=4.5);
                foreach(var state in States.Values)Require(Contrast(state.Color,colors["surface"])>=4.5);
                profiles.Add(id,new FactionThemeProfile(id,Text(p!,"displayName"),Text(p!,"catalogueStatus"),Text(p!,"sourceStatus"),families[family],colors,cs,ms));
            }
            Require(profiles.Count==8);byId=Freeze(profiles);Profiles=System.Array.AsReadOnly(ProfileIds.Select(id=>profiles[id]).ToArray());
            civilizations=Freeze(Profiles.SelectMany(p=>p.CivilizationDefinitionIds.Select(id=>new{Id=id,Profile=p})).ToDictionary(x=>x.Id,x=>x.Profile,StringComparer.Ordinal));
            minors=Freeze(Profiles.SelectMany(p=>p.MinorDefinitionIds.Select(id=>new{Id=id,Profile=p})).ToDictionary(x=>x.Id,x=>x.Profile,StringComparer.Ordinal));
            Require(civilizations.Keys.OrderBy(x=>x,StringComparer.Ordinal).SequenceEqual(allowedCivilizations.OrderBy(x=>x,StringComparer.Ordinal))&&minors.Keys.OrderBy(x=>x,StringComparer.Ordinal).SequenceEqual(allowedMinors.OrderBy(x=>x,StringComparer.Ordinal)));
            var policy=Object(root,"bindingPolicy");Keys(policy,"lookup","unknown","application","visibility","custom");Require(Text(policy,"unknown")=="neutral");
            foreach(var key in new[]{"lookup","application","visibility","custom"})Text(policy,key);
        }
        public FactionThemeSelection Resolve(FactionThemeIdentity identity)
        {
            FactionThemeProfile? profile=null;
            if(identity.LawfullyDisclosed&&identity.DefinitionId!=null)
            {
                if(identity.Kind==FactionThemeIdentityKind.Civilization)civilizations.TryGetValue(identity.DefinitionId,out profile);
                else if(identity.Kind==FactionThemeIdentityKind.Minor)minors.TryGetValue(identity.DefinitionId,out profile);
            }
            return new FactionThemeSelection(profile??byId["neutral"],false,ArtifactStatus);
        }
        // Proposed profiles remain inspectable without pretending they are catalogue entries.
        public FactionThemeSelection PreviewProfile(string? profileId)=>new FactionThemeSelection(profileId!=null&&byId.TryGetValue(profileId,out var p)?p:byId["neutral"],true,ArtifactStatus);
        public static bool TryParse(string? json,string? catalogueAuditJson,out FactionThemeTokens? tokens,out string error)
        {
            tokens=null;error="Invalid or unsupported proposed theme schema.";
            if(json==null||json.Length==0||json.Length>MaximumJsonCharacters||catalogueAuditJson==null||catalogueAuditJson.Length==0||catalogueAuditJson.Length>MaximumJsonCharacters)return false;
            try
            {
                tokens=new FactionThemeTokens(ReadObject(json),ReadObject(catalogueAuditJson));error="";return true;
            }
            catch(JsonException){return false;}catch(ArgumentException){return false;}catch(OverflowException){return false;}
        }
        private static JObject ReadObject(string json)
        {
            // JsonTextReader deliberately accepts some JavaScript extensions. Reject those
            // lexically before reading the bounded schema, without a second serializer.
            char previous='\0';
            for(var i=0;i<json.Length;i++)
            {
                var c=json[i];if(char.IsWhiteSpace(c))continue;
                if(c=='"')
                {
                    for(i++;i<json.Length;i++){if(json[i]=='\\'){i++;continue;}if(json[i]=='"')break;}
                    Require(i<json.Length);previous='"';continue;
                }
                if(c=='}'||c==']')Require(previous!=',');
                if("{}[],:".IndexOf(c)>=0){previous=c;continue;}
                var start=i;while(i<json.Length&&!char.IsWhiteSpace(json[i])&&"{}[],:\"".IndexOf(json[i])<0)i++;
                var word=json.Substring(start,i-start);Require(word=="true"||word=="false"||word=="null"||JsonNumber(word));previous='v';i--;
            }
            using(var text=new StringReader(json))using(var reader=new StrictReader(text){MaxDepth=16,DateParseHandling=DateParseHandling.None})
            {
                var root=JObject.Load(reader,new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error});
                Require(!reader.Read());return root;
            }
        }
        private static bool JsonNumber(string value)
        {
            var i=0;bool Digit()=>i<value.Length&&value[i]>='0'&&value[i]<='9';
            if(i<value.Length&&value[i]=='-')i++;if(!Digit())return false;
            if(value[i]=='0')i++;else while(Digit())i++;
            if(i<value.Length&&value[i]=='.'){i++;if(!Digit())return false;while(Digit())i++;}
            if(i<value.Length&&(value[i]=='e'||value[i]=='E')){i++;if(i<value.Length&&(value[i]=='+'||value[i]=='-'))i++;if(!Digit())return false;while(Digit())i++;}
            return i==value.Length;
        }
        private sealed class StrictReader:JsonTextReader
        {
            internal StrictReader(TextReader reader):base(reader){}
            public override bool Read()
            {
                var result=base.Read();
                if(TokenType==JsonToken.Comment||((TokenType==JsonToken.String||TokenType==JsonToken.PropertyName)&&QuoteChar!='"'))throw new JsonReaderException("Strict quoted JSON required.");
                return result;
            }
        }
        private static void Require(bool condition){if(!condition)throw new ArgumentException("Invalid proposed theme.");}
        private static void Keys(JObject obj,params string[] keys)=>Require(obj.Properties().Count()==keys.Length&&keys.All(k=>obj.Property(k,StringComparison.Ordinal)!=null));
        private static JObject Object(JObject obj,string key){var result=obj[key] as JObject;Require(result!=null);return result!;}
        private static JArray Array(JObject obj,string key,int? count){var result=obj[key] as JArray;Require(result!=null&&result.Count<=8&&(!count.HasValue||result.Count==count.Value));return result!;}
        private static string Text(JObject obj,string key){var value=obj[key];Require(value?.Type==JTokenType.String);var s=value!.Value<string>()!;Require(s.Length>0&&s.Length<=512&&!s.Any(char.IsControl));return s;}
        private static bool Boolean(JObject obj,string key){Require(obj[key]?.Type==JTokenType.Boolean);return (bool)obj[key]!;}
        private static int ScalarInt(JToken token,int min,int max){Require(token.Type==JTokenType.Integer);var n=(long)token;Require(n>=min&&n<=max);return (int)n;}
        private static int Integer(JObject obj,string key,int min,int max){Require(obj[key]!=null);return ScalarInt(obj[key]!,min,max);}
        private static string[] Strings(JObject obj,string key,int? count)=>Array(obj,key,count).Select(t=>{Require(t.Type==JTokenType.String);var s=t.Value<string>()!;Require(s.Length>0&&s.Length<=80&&!s.Any(char.IsControl));return s;}).ToArray();
        private static string Hex(JObject obj,string key){var value=Text(obj,key);Require(value.Length==7&&value[0]=='#'&&value.Skip(1).All(Uri.IsHexDigit));return value.ToUpperInvariant();}
        private static Dictionary<string,string> Colors(JObject obj,params string[] keys){Keys(obj,keys);return keys.ToDictionary(k=>k,k=>Hex(obj,k),StringComparer.Ordinal);}
        private static double Contrast(string first,string second)
        {
            double Luminance(string hex)
            {
                double Channel(int offset){var value=Convert.ToInt32(hex.Substring(offset,2),16)/255d;return value<=.04045?value/12.92:Math.Pow((value+.055)/1.055,2.4);}
                return .2126*Channel(1)+.7152*Channel(3)+.0722*Channel(5);
            }
            var a=Luminance(first);var b=Luminance(second);return (Math.Max(a,b)+.05)/(Math.Min(a,b)+.05);
        }
    }
}
