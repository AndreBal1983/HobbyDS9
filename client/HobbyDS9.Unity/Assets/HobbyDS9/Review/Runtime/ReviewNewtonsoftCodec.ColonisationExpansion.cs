#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewNewtonsoftCodec:IBackgroundColonisationExpansionCheckpointCodec
    {
        public CampaignColonisationExpansionPackageDto CopyExpansionPackage(CampaignColonisationExpansionPackageDto package)
            =>Decode<CampaignColonisationExpansionPackageDto>(Encode(package));
        public CampaignColonisationExpansionSaveDto CopyExpansionSave(CampaignColonisationExpansionSaveDto root)
            =>DecodeColonisationExpansionRoot(EncodeColonisationExpansionRoot(root));
        public ColonisationPlannerPolicyDto DecodeColonisationPlanner(byte[] bytes)=>DecodeActivityResource<ColonisationPlannerPolicyDto>(bytes);
        public byte[] EncodeColonisationExpansionRoot(CampaignColonisationExpansionSaveDto root)
        {
            RequireColonisationExpansionCarrier(root);
            var bytes = Encode(root);
            if (bytes.Length == 0 || bytes.Length > ReviewColonisationSlotStore.MaximumBytes) throw Invalid();
            return bytes;
        }
        public CampaignColonisationExpansionSaveDto DecodeColonisationExpansionRoot(byte[] bytes)
        {
            var phase=ReviewLoadPhase.DecodeParse;
            try
            {
                loadDiagnostic?.Record(phase,ReviewLoadEvent.Begin);
                if (bytes == null || bytes.Length == 0 || bytes.Length > ReviewColonisationSlotStore.MaximumBytes) throw Invalid();
                JToken wire;
                using (var text = new StringReader(Utf8.GetString(bytes)))
                using (var reader = new JsonTextReader(text) { MaxDepth = 128, DateParseHandling = DateParseHandling.None })
                {
                    wire = JToken.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
                    if (!(wire is JObject) || reader.Read()) throw Invalid();
                }
                loadDiagnostic?.Record(phase,ReviewLoadEvent.NormalReturn);
                phase=ReviewLoadPhase.DecodeShape;
                loadDiagnostic?.Record(phase,ReviewLoadEvent.Begin);
                // Unlike DTO default initializers, omitted fields are not retained history.
                // This adapter-only reflection closes the fixed contract graph; portable
                // simulation uses its independently versioned non-reflective preflight.
                int visits = 2000000;
                ColonisationWire(wire, typeof(CampaignColonisationExpansionSaveDto), ref visits, new Dictionary<Type, PropertyInfo[]>());
                loadDiagnostic?.Record(phase,ReviewLoadEvent.NormalReturn);
                phase=ReviewLoadPhase.DecodeNatural;
                loadDiagnostic?.Record(phase,ReviewLoadEvent.Begin);
                DecodeNaturalGalaxy(Utf8.GetBytes(wire["BaseCampaign"]!["Map"]!.ToString(Formatting.None)));
                loadDiagnostic?.Record(phase,ReviewLoadEvent.NormalReturn);
                phase=ReviewLoadPhase.DecodeTyped;
                loadDiagnostic?.Record(phase,ReviewLoadEvent.Begin);
                var root = Decode<CampaignColonisationExpansionSaveDto>(bytes);
                loadDiagnostic?.Record(phase,ReviewLoadEvent.NormalReturn);
                phase=ReviewLoadPhase.DecodeCarrier;
                loadDiagnostic?.Record(phase,ReviewLoadEvent.Begin);
                RequireColonisationExpansionCarrier(root);
                loadDiagnostic?.Record(phase,ReviewLoadEvent.NormalReturn);
                return root;
            }
            catch (Exception e) { loadDiagnostic?.Record(phase,ReviewLoadEvent.Error,e);throw new InvalidDataException("Invalid colonisation campaign carrier."); }
        }
        private static void RequireColonisationExpansionCarrier(CampaignColonisationExpansionSaveDto root,ColonisationCarrierVocabulary vocabulary=ColonisationCarrierVocabulary.Original)
        {
            if(root==null||root.FormatVersion!=CampaignColonisationExpansionIdentity.Format
                ||root.ProfileId!=(vocabulary!=ColonisationCarrierVocabulary.Original?CampaignColonisationGrowthIdentity.Profile:CampaignColonisationExpansionIdentity.Profile)||root.PackageHash==null||root.PackageHash.Length!=64
                ||!root.PackageHash.All(c=>c>='0'&&c<='9'||c>='a'&&c<='f')||(!ActivityRows(root.AiPlans)||root.AiPlans!.Count>4))throw Invalid();
            RequireColonisationCarrier(root.BaseCampaign,CampaignColonisationExpansionIdentity.InventoryPolicy,vocabulary);
            RequireColonisationAiPlans(root.AiPlans!,vocabulary,"establish-first-frontier-colony-v1");
        }
        private static void RequireColonisationAiPlans(List<ColonisationAiPlanDto> plans,ColonisationCarrierVocabulary vocabulary,string goal)
        {
            bool Text(string? value)=>value==null||value.Length<=512;
            var actors=new HashSet<string>(StringComparer.Ordinal);
            foreach(var p in plans)
            {
                if(p.ActorId==null||!actors.Add(p.ActorId)||(!Enum.IsDefined(typeof(ColonisationAiStage),p.Stage)||vocabulary==ColonisationCarrierVocabulary.Original&&(int)p.Stage>8)
                    ||!ActivityRows(p.LatestActions)||p.LatestActions!.Count>8||!ActivityRows(p.OwnAttemptedTargets)||p.OwnAttemptedTargets!.Count>8)throw Invalid();
                if(p.GoalId!=goal||p.HoldReason==null||p.HoldReason.Length>128
                    ||!new[]{p.ActorId,p.GoalId,p.HomeColonyId,p.YardColonyId,p.SourceCohortId,p.ScoutShipId,p.TransportShipId,p.BlueprintId,
                        p.TargetPlanetId,p.TargetNodeId,p.KitQueueId,p.TransportQueueId,p.ManifestId,p.CampId,p.EstablishedColonyId}.All(Text)
                    ||p.OwnAttemptedTargets.Any(id=>id==null||!Text(id)))throw Invalid();
                foreach(var a in p.LatestActions)if(a.CommandId==null||a.CommandId.Length>128||(!Enum.IsDefined(typeof(ColonisationAiAction),a.Action)||vocabulary==ColonisationCarrierVocabulary.Original&&(int)a.Action>14)||!Enum.IsDefined(typeof(CampaignTacticalError),a.Error))throw Invalid();
            }
        }
    }
}
