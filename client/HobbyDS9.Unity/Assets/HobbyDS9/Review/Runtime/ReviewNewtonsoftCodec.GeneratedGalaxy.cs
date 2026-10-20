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
        public GeneratedGalaxyDocumentDto DecodeGeneratedGalaxy(byte[] bytes)
        {
            if(bytes==null||bytes.Length==0||bytes.Length>GeneratedGalaxyMapStore.MaximumBytes)
                throw new InvalidDataException("Generated map file size unsupported.");
            try
            {
                using(var text=new StringReader(Utf8.GetString(bytes)))
                using(var reader=new JsonTextReader(text){MaxDepth=16,DateParseHandling=DateParseHandling.None})
                {
                    var root=JToken.Load(reader,new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error});
                    if(reader.Read())throw Invalid();
                    void Object(JToken? value,params string[] fields)
                    {
                        if(!(value is JObject obj)||obj.Properties().Count()!=fields.Length||fields.Any(f=>obj.Property(f,StringComparison.Ordinal)==null))throw Invalid();
                    }
                    JArray Rows(JToken? value,int maximum)
                    {if(!(value is JArray array)||array.Count>maximum)throw Invalid();return array;}
                    Object(root,"FormatVersion","ProfileId","GeneratorVersion","TuningVersion","ContentHash","Settings",
                        "Width","Height","Regions","Systems","Connections","HomeCandidates");
                    Object(root["Settings"],"Seed","SystemCount","HomeCivilizationDefinitionIds");
                    Rows(root["Settings"]!["HomeCivilizationDefinitionIds"],2);
                    foreach(var region in Rows(root["Regions"],100))
                    {
                        Object(region,"NodeId","DisplayName","SystemId","Center","Boundary");Object(region["Center"],"X","Y");
                        foreach(var point in Rows(region["Boundary"],104))Object(point,"X","Y");
                    }
                    foreach(var system in Rows(root["Systems"],50))Object(system,"SystemId","DisplayName","NodeId");
                    foreach(var edge in Rows(root["Connections"],300))Object(edge,"ConnectionId","EndpointA","EndpointB","Kind","BaseTraversalCost");
                    foreach(var home in Rows(root["HomeCandidates"],2))Object(home,"CivilizationDefinitionId","SystemId","NodeId");
                }
                // Shared strict scalar/type checks, followed by portable Open at the caller.
                return Decode<GeneratedGalaxyDocumentDto>(bytes);
            }
            catch(Exception){throw new InvalidDataException("Invalid generated-map document.");}
        }
    }
}
