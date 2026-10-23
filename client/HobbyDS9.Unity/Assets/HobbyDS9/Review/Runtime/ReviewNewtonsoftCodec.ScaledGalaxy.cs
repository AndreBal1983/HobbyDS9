#nullable enable
using System;
using System.IO;
using HobbyDS9.Contracts;
using Newtonsoft.Json.Linq;

namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewNewtonsoftCodec
    {
        public byte[] EncodeScaledGalaxy(ScaledGalaxyDocumentDto document)
        {
            var bytes=Encode(document);
            if(bytes.Length>ScaledGalaxyIdentity.MaximumJsonBytes)throw Invalid();
            return bytes;
        }

        // Wire admission only. A decoded document must still pass portable ScaledGalaxyApplication.Open
        // before it can replace the current review. No semantic acceptance or migration happens here.
        public ScaledGalaxyDocumentDto DecodeScaledGalaxy(byte[] bytes)
        {
            try
            {
                var root=GeneratedRoot(bytes,ScaledGalaxyIdentity.MaximumJsonBytes);
                GeneratedObject(root,"FormatVersion","ProfileId","GeneratorVersion","TuningVersion","ContentHash",
                    "Settings","Tuning","Features","Layout","RegionInfluences");
                if(root["FormatVersion"]?.Type!=JTokenType.Integer||root["FormatVersion"]!.Value<int>()!=ScaledGalaxyIdentity.FormatVersion)throw Invalid();
                var settings=root["Settings"];
                GeneratedObject(settings,"Seed","SystemCount","EmptyRegionCount","Width","Height","QuietEmptyAreaTargetPermille",
                    "BlackHolesPerBaselineArea","NebulaePerBaselineArea","HomeCivilizationDefinitionIds",
                    "StellarInfluenceEnabled","BlackHoleInfluenceEnabled","NebulaInfluenceEnabled");
                GeneratedRows(settings!["HomeCivilizationDefinitionIds"],2);
                if(settings["Seed"]?.Type!=JTokenType.String||settings["Seed"]!.Value<string>()!.Length>20)throw Invalid();
                GeneratedObject(root["Tuning"],"Environment","StarClusterSize","ClusterRadius","MinimumClusterSeparation",
                    "ExtraSiteSeparation","QuietDisturbanceThreshold");
                GeneratedObject(root["Tuning"]!["Environment"],"BaseDensity","MaximumDensity","MinimumSiteSeparation","MinimumStarSeparation",
                    "OuterMargin","BlackHoleCount","NebulaCount","CandidateDrawsPerRegion","PlacementAttempts");
                var layout=root["Layout"]!;
                GeneratedObject(layout,"Width","Height","Regions","Systems","Connections","HomeCandidates");
                int vertices=0;
                foreach(var region in GeneratedRows(layout["Regions"],ScaledGalaxyIdentity.MaximumRegions))
                {
                    GeneratedObject(region,"NodeId","DisplayName","SystemId","Center","Boundary");GeneratedObject(region["Center"],"X","Y");
                    var boundary=GeneratedRows(region["Boundary"],ScaledGalaxyIdentity.MaximumVerticesPerRegion);
                    vertices+=boundary.Count;if(vertices>ScaledGalaxyIdentity.MaximumTotalVertices)throw Invalid();
                    foreach(var point in boundary)GeneratedObject(point,"X","Y");
                }
                foreach(var system in GeneratedRows(layout["Systems"],ScaledGalaxyIdentity.MaximumSystems))GeneratedObject(system,"SystemId","DisplayName","NodeId");
                foreach(var edge in GeneratedRows(layout["Connections"],ScaledGalaxyIdentity.MaximumConnections))GeneratedObject(edge,"ConnectionId","EndpointA","EndpointB","Kind","BaseTraversalCost");
                foreach(var home in GeneratedRows(layout["HomeCandidates"],2))GeneratedObject(home,"CivilizationDefinitionId","SystemId","NodeId");
                var features=GeneratedRows(root["Features"],ScaledGalaxyIdentity.MaximumFeatures);
                foreach(var feature in features)
                {
                    GeneratedObject(feature,"FeatureId","Kind","DisplayName","Position","RadiusX","RadiusY","DensityWeight","SystemId","NodeId");
                    GeneratedObject(feature["Position"],"X","Y");
                }
                int contributions=0;
                foreach(var influence in GeneratedRows(root["RegionInfluences"],ScaledGalaxyIdentity.MaximumRegions))
                {
                    GeneratedObject(influence,"NodeId","BaseDensity","TargetDensity","Contributions");
                    var rows=GeneratedRows(influence["Contributions"],features.Count);
                    contributions+=rows.Count;if(contributions>ScaledGalaxyIdentity.MaximumTotalContributions)throw Invalid();
                    foreach(var contribution in rows)GeneratedObject(contribution,"FeatureId","Amount");
                }
                CheckScaledText(root);
                return Decode<ScaledGalaxyDocumentDto>(bytes);
            }
            catch(Exception){throw new InvalidDataException("Invalid scaled-map document.");}
        }

        private static void CheckScaledText(JToken token)
        {
            if(token.Type==JTokenType.String&&token.Value<string>()!.Length>128)throw Invalid();
            foreach(var child in token.Children())CheckScaledText(child);
        }
    }
}
