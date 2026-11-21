#nullable enable
using System;
using System.IO;
using HobbyDS9.Contracts;
using Newtonsoft.Json.Linq;

namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewNewtonsoftCodec
    {
        public byte[] EncodeNaturalGalaxy(NaturalGalaxyDocumentDto document)
        {
            var bytes=Encode(document);if(bytes.Length>ScaledGalaxyIdentity.MaximumJsonBytes)throw Invalid();return bytes;
        }
        // Exact format4 wire admission, followed by portable Open before publication. No fallback/migration.
        public NaturalGalaxyDocumentDto DecodeNaturalGalaxy(byte[] bytes)
        {
            try
            {
                var root=GeneratedRoot(bytes,ScaledGalaxyIdentity.MaximumJsonBytes);
                GeneratedObject(root,"FormatVersion","ProfileId","GeneratorVersion","TuningVersion","ContentHash","Settings","Tuning","Features","StarGroups","Layout","RegionInfluences");
                if(root["FormatVersion"]?.Type!=JTokenType.Integer||root["FormatVersion"]!.Value<int>()!=NaturalGalaxyIdentity.FormatVersion)throw Invalid();
                var settings=root["Settings"]!;GeneratedObject(settings,"Map","IsolatedStarSharePermille","AssociationSpreadPermille");
                var map=settings["Map"]!;
                GeneratedObject(map,"Seed","SystemCount","EmptyRegionCount","Width","Height","QuietEmptyAreaTargetPermille","BlackHolesPerBaselineArea","NebulaePerBaselineArea",
                    "HomeCivilizationDefinitionIds","StellarInfluenceEnabled","BlackHoleInfluenceEnabled","NebulaInfluenceEnabled");
                GeneratedRows(map["HomeCivilizationDefinitionIds"],2);
                if(map["Seed"]?.Type!=JTokenType.String||map["Seed"]!.Value<string>()!.Length>20)throw Invalid();
                var tuning=root["Tuning"]!;
                GeneratedObject(tuning,"Environment","IsolatedStarSeparation","MinimumAssociationSize","MaximumAssociationSize","MinimumAssociationRadius","MaximumAssociationRadius",
                    "MinimumAssociationSeparation","MaximumAssociationSeparation","ExtraSiteSeparation","StellarPlacementPressurePermille","QuietDisturbanceThreshold");
                GeneratedObject(tuning["Environment"],"BaseDensity","MaximumDensity","MinimumSiteSeparation","MinimumStarSeparation","OuterMargin","BlackHoleCount","NebulaCount","CandidateDrawsPerRegion","PlacementAttempts");
                int memberships=0;
                foreach(var group in GeneratedRows(root["StarGroups"],1000))
                {
                    GeneratedObject(group,"GroupId","Kind","Center","RadiusX","RadiusY","StarFeatureIds");GeneratedObject(group["Center"],"X","Y");
                    memberships+=GeneratedRows(group["StarFeatureIds"],9).Count;if(memberships>1000)throw Invalid();
                }
                ValidateScaledLayout(root);CheckScaledText(root);
                return Decode<NaturalGalaxyDocumentDto>(bytes);
            }
            catch(Exception){throw new InvalidDataException("Invalid natural-map document.");}
        }
    }
}
