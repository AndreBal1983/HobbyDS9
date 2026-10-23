#nullable enable
using System.IO;
using System;
using System.Linq;
using HobbyDS9.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewNewtonsoftCodec
    {
        public byte[] EncodeGeneratedGalaxy(GeneratedGalaxyDocumentDto document)=>Encode(document);
        public byte[] EncodeEnvironmentGalaxy(EnvironmentGalaxyDocumentDto document)=>Encode(document);
        public int GeneratedGalaxyFormat(byte[] bytes)
        {
            var root=GeneratedRoot(bytes,GeneratedGalaxyMapStore.MaximumBytes);
            if(root["FormatVersion"]?.Type!=JTokenType.Integer)throw Invalid();
            try
            {
                var format=root["FormatVersion"]!.Value<int>();
                if(format!=ScaledGalaxyIdentity.FormatVersion&&format!=NaturalGalaxyIdentity.FormatVersion&&bytes.Length>GeneratedGalaxyMapStore.LegacyMaximumBytes)throw Invalid();
                return format;
            }
            catch(Exception){throw new InvalidDataException("Invalid generated-map format.");}
        }
        public EnvironmentGalaxyDocumentDto DecodeEnvironmentGalaxy(byte[] bytes)
        {
            try
            {
                var root=GeneratedRoot(bytes);
                GeneratedObject(root,"FormatVersion","ProfileId","GeneratorVersion","TuningVersion","ContentHash",
                    "Settings","Tuning","Features","Layout","RegionInfluences");
                if(root["FormatVersion"]?.Type!=JTokenType.Integer||root["FormatVersion"]!.Value<int>()!=2)throw Invalid();
                var settings=root["Settings"];
                GeneratedObject(settings,"Map","StellarInfluenceEnabled","BlackHoleInfluenceEnabled","NebulaInfluenceEnabled");
                GeneratedSettingsShape(settings!["Map"]);
                GeneratedObject(root["Tuning"],"BaseDensity","MaximumDensity","MinimumSiteSeparation","MinimumStarSeparation","OuterMargin",
                    "BlackHoleCount","NebulaCount","CandidateDrawsPerRegion","PlacementAttempts");
                GeneratedObject(root["Layout"],"Width","Height","Regions","Systems","Connections","HomeCandidates");
                GeneratedLayoutShape(root["Layout"]!);
                foreach(var feature in GeneratedRows(root["Features"],55))
                {
                    GeneratedObject(feature,"FeatureId","Kind","DisplayName","Position","RadiusX","RadiusY","DensityWeight","SystemId","NodeId");
                    GeneratedObject(feature["Position"],"X","Y");
                }
                foreach(var influence in GeneratedRows(root["RegionInfluences"],100))
                {
                    GeneratedObject(influence,"NodeId","BaseDensity","TargetDensity","Contributions");
                    foreach(var contribution in GeneratedRows(influence["Contributions"],55))GeneratedObject(contribution,"FeatureId","Amount");
                }
                return Decode<EnvironmentGalaxyDocumentDto>(bytes);
            }
            catch(Exception){throw new InvalidDataException("Invalid environment-map document.");}
        }
        public GeneratedGalaxyDocumentDto DecodeGeneratedGalaxy(byte[] bytes)
        {
            try
            {
                var root=GeneratedRoot(bytes);
                GeneratedObject(root,"FormatVersion","ProfileId","GeneratorVersion","TuningVersion","ContentHash","Settings",
                    "Width","Height","Regions","Systems","Connections","HomeCandidates");
                if(root["FormatVersion"]?.Type!=JTokenType.Integer||root["FormatVersion"]!.Value<int>()!=1)throw Invalid();
                GeneratedSettingsShape(root["Settings"]);GeneratedLayoutShape(root);
                // Shared strict scalar/type checks, followed by portable Open at the caller.
                return Decode<GeneratedGalaxyDocumentDto>(bytes);
            }
            catch(Exception){throw new InvalidDataException("Invalid generated-map document.");}
        }
        private static JToken GeneratedRoot(byte[] bytes,int maximumBytes=GeneratedGalaxyMapStore.LegacyMaximumBytes)
        {
            if(bytes==null||bytes.Length==0||bytes.Length>maximumBytes)
                throw new InvalidDataException("Generated map file size unsupported.");
            try
            {
                using(var text=new StringReader(Utf8.GetString(bytes)))
                using(var reader=new JsonTextReader(text){MaxDepth=16,DateParseHandling=DateParseHandling.None})
                {
                    var root=JToken.Load(reader,new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error});
                    if(!(root is JObject)||reader.Read())throw Invalid();return root;
                }
            }
            catch(Exception){throw new InvalidDataException("Invalid generated-map document.");}
        }
        private static void GeneratedObject(JToken? value,params string[] fields)
        {
            if(!(value is JObject obj)||obj.Properties().Count()!=fields.Length||fields.Any(f=>obj.Property(f,StringComparison.Ordinal)==null))throw Invalid();
        }
        private static JArray GeneratedRows(JToken? value,int maximum)
        {if(!(value is JArray array)||array.Count>maximum)throw Invalid();return array;}
        private static void GeneratedSettingsShape(JToken? settings)
        {
            GeneratedObject(settings,"Seed","SystemCount","HomeCivilizationDefinitionIds");
            GeneratedRows(settings!["HomeCivilizationDefinitionIds"],2);
        }
        private static void GeneratedLayoutShape(JToken layout)
        {
            foreach(var region in GeneratedRows(layout["Regions"],100))
            {
                GeneratedObject(region,"NodeId","DisplayName","SystemId","Center","Boundary");GeneratedObject(region["Center"],"X","Y");
                foreach(var point in GeneratedRows(region["Boundary"],104))GeneratedObject(point,"X","Y");
            }
            foreach(var system in GeneratedRows(layout["Systems"],50))GeneratedObject(system,"SystemId","DisplayName","NodeId");
            foreach(var edge in GeneratedRows(layout["Connections"],300))GeneratedObject(edge,"ConnectionId","EndpointA","EndpointB","Kind","BaseTraversalCost");
            foreach(var home in GeneratedRows(layout["HomeCandidates"],2))GeneratedObject(home,"CivilizationDefinitionId","SystemId","NodeId");
        }
    }
}
