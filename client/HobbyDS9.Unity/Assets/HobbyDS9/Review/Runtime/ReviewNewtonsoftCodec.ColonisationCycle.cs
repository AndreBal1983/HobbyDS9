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
    public sealed partial class ReviewNewtonsoftCodec:IBackgroundColonisationCycleCheckpointCodec
    {
        public CampaignColonisationCyclePackageDto CopyCyclePackage(CampaignColonisationCyclePackageDto package)
            =>Decode<CampaignColonisationCyclePackageDto>(Encode(package));
        public CampaignColonisationCycleSaveDto CopyCycleSave(CampaignColonisationCycleSaveDto root)
            =>DecodeCycleRoot(EncodeCycleRoot(root));
        public byte[] EncodeCycleRoot(CampaignColonisationCycleSaveDto root)
        {
            RequireColonisationCycleCarrier(root);
            var bytes = Encode(root);
            if (bytes.Length == 0 || bytes.Length > ReviewColonisationSlotStore.MaximumBytes) throw Invalid();
            return bytes;
        }
        public CampaignColonisationCycleSaveDto DecodeCycleRoot(byte[] bytes)
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
                ColonisationWire(wire, typeof(CampaignColonisationCycleSaveDto), ref visits, new Dictionary<Type, PropertyInfo[]>(),ColonisationCarrierVocabulary.Cycle);
                loadDiagnostic?.Record(phase,ReviewLoadEvent.NormalReturn);
                phase=ReviewLoadPhase.DecodeNatural;
                loadDiagnostic?.Record(phase,ReviewLoadEvent.Begin);
                DecodeNaturalGalaxy(Utf8.GetBytes(wire["BaseCampaign"]!["Map"]!.ToString(Formatting.None)));
                loadDiagnostic?.Record(phase,ReviewLoadEvent.NormalReturn);
                phase=ReviewLoadPhase.DecodeTyped;
                loadDiagnostic?.Record(phase,ReviewLoadEvent.Begin);
                var root = Decode<CampaignColonisationCycleSaveDto>(bytes);
                loadDiagnostic?.Record(phase,ReviewLoadEvent.NormalReturn);
                phase=ReviewLoadPhase.DecodeCarrier;
                loadDiagnostic?.Record(phase,ReviewLoadEvent.Begin);
                RequireColonisationCycleCarrier(root);
                loadDiagnostic?.Record(phase,ReviewLoadEvent.NormalReturn);
                return root;
            }
            catch (Exception e) { loadDiagnostic?.Record(phase,ReviewLoadEvent.Error,e);throw new InvalidDataException("Invalid colonisation campaign carrier."); }
        }
        public ColonisationCyclePolicyDto DecodeColonisationCyclePolicy(byte[] bytes)=>DecodeActivityResource<ColonisationCyclePolicyDto>(bytes);
        private static void RequireColonisationCycleCarrier(CampaignColonisationCycleSaveDto root)
        {
            if(root==null||root.FormatVersion!=CampaignColonisationCycleIdentity.Format||root.ProfileId!=CampaignColonisationCycleIdentity.Profile
                ||!ActivityRows(root.CycleActors)||root.CycleActors!.Count>4)throw Invalid();
            // Shared carrier shape only; this does not select a loader or trusted edition.
            RequireColonisationExpansionCarrier(new CampaignColonisationExpansionSaveDto{FormatVersion=CampaignColonisationExpansionIdentity.Format,
                ProfileId=CampaignColonisationGrowthIdentity.Profile,PackageHash=root.PackageHash,BaseCampaign=root.BaseCampaign,AiPlans=root.AiPlans},ColonisationCarrierVocabulary.Cycle);
            var actors=new HashSet<string>(root.AiPlans!.Select(p=>p.ActorId),StringComparer.Ordinal);
            var seen=new HashSet<string>(StringComparer.Ordinal);long records=0;
            bool Text(string? value)=>value==null||value.Length<=512;
            foreach(var actor in root.CycleActors!)
            {
                if(actor.ActorId==null||!Text(actor.ActorId)||!actors.Contains(actor.ActorId)||!seen.Add(actor.ActorId)
                    ||actor.NextOrdinal<1||!Text(actor.SupplyCampId)||!ActivityRows(actor.Receipts))throw Invalid();
                records+=actor.Receipts!.Count+(actor.Active==null?0:1);
                if(records>8192)throw Invalid();
                var ordinals=new HashSet<int>();
                foreach(var receipt in actor.Receipts)
                {
                    if(receipt.Ordinal<1||receipt.Ordinal>=actor.NextOrdinal||!ordinals.Add(receipt.Ordinal)
                        ||receipt.StartedTurn<0||receipt.TerminalTurn<receipt.StartedTurn||receipt.ClosedTurn<receipt.TerminalTurn
                        ||!Enum.IsDefined(typeof(ColonisationCycleOutcome),receipt.Outcome)||receipt.SettlementManifestId==null||receipt.TargetPlanetId==null
                        ||!Text(receipt.SettlementManifestId)||!Text(receipt.TargetPlanetId)||!Text(receipt.CampId))throw Invalid();
                }
                var active=actor.Active;
                if(active==null)continue;
                if(active.Ordinal<1||active.Ordinal>=actor.NextOrdinal||!ordinals.Add(active.Ordinal)||active.StartedTurn<0
                    ||!Text(active.SettlementManifestId)||!Text(active.CommittedTargetPlanetId)||active.Assignment==null
                    ||active.Assignment.ActorId!=actor.ActorId||active.Assignment.LatestActions==null||active.Assignment.LatestActions.Count!=0
                    ||(int)active.Assignment.Stage>8)throw Invalid();
                RequireColonisationAiPlans(new List<ColonisationAiPlanDto>{active.Assignment},ColonisationCarrierVocabulary.Cycle,CampaignColonisationCycleIdentity.Goal);
            }
            if(!actors.SetEquals(seen))throw Invalid();
        }
    }
}
