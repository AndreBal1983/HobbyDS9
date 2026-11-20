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
        {
            if(package==null||package.GetType()!=typeof(CampaignColonisationCyclePackageDto)&&!(package is CampaignSystemRichnessPackageDto)&&!(package is CampaignAuthoredSolPackageDto)&&!(package is CampaignEngineeringRefinementPackageDto)&&!(package is CampaignBeltBodiesPackageDto))throw Invalid();
            return package is CampaignBeltBodiesPackageDto?Decode<CampaignBeltBodiesPackageDto>(Encode(package))
                :package is CampaignEngineeringRefinementPackageDto?Decode<CampaignEngineeringRefinementPackageDto>(Encode(package))
                :package is CampaignAuthoredSolPackageDto?Decode<CampaignAuthoredSolPackageDto>(Encode(package))
                :package is CampaignSystemRichnessPackageDto?Decode<CampaignSystemRichnessPackageDto>(Encode(package)):Decode<CampaignColonisationCyclePackageDto>(Encode(package));
        }
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
                var rich=(string?)wire["ProfileId"]==CampaignSystemRichnessIdentity.Profile;
                var authored=(string?)wire["ProfileId"]==CampaignAuthoredSolIdentity.Profile;
                var refinement=(string?)wire["ProfileId"]==EngineeringRefinementIdentity.Profile;
                var beltBodies=(string?)wire["ProfileId"]==BeltBodiesEditionIdentity.Profile;
                if(beltBodies)RequireBeltBodiesWire(wire);
                else if(refinement)RequireRefinementWire(wire);
                ColonisationWire(wire, beltBodies?typeof(CampaignBeltBodiesSaveDto):refinement?typeof(CampaignEngineeringRefinementSaveDto):authored?typeof(CampaignAuthoredSolSaveDto):rich?typeof(CampaignSystemRichnessSaveDto):typeof(CampaignColonisationCycleSaveDto), ref visits, new Dictionary<Type, PropertyInfo[]>(),ColonisationCarrierVocabulary.Cycle);
                loadDiagnostic?.Record(phase,ReviewLoadEvent.NormalReturn);
                phase=ReviewLoadPhase.DecodeNatural;
                loadDiagnostic?.Record(phase,ReviewLoadEvent.Begin);
                DecodeNaturalGalaxy(Utf8.GetBytes(wire["BaseCampaign"]!["Map"]!.ToString(Formatting.None)));
                loadDiagnostic?.Record(phase,ReviewLoadEvent.NormalReturn);
                phase=ReviewLoadPhase.DecodeTyped;
                loadDiagnostic?.Record(phase,ReviewLoadEvent.Begin);
                CampaignColonisationCycleSaveDto root = beltBodies?Decode<CampaignBeltBodiesSaveDto>(bytes):refinement?Decode<CampaignEngineeringRefinementSaveDto>(bytes):authored?Decode<CampaignAuthoredSolSaveDto>(bytes):rich?Decode<CampaignSystemRichnessSaveDto>(bytes):Decode<CampaignColonisationCycleSaveDto>(bytes);
                loadDiagnostic?.Record(phase,ReviewLoadEvent.NormalReturn);
                phase=ReviewLoadPhase.DecodeCarrier;
                loadDiagnostic?.Record(phase,ReviewLoadEvent.Begin);
                RequireColonisationCycleCarrier(root);
                loadDiagnostic?.Record(phase,ReviewLoadEvent.NormalReturn);
                return root;
            }
            catch (Exception e) { loadDiagnostic?.Record(phase,ReviewLoadEvent.Error,e);throw new InvalidDataException("Invalid colonisation campaign carrier."); }
        }
        public SystemRichnessPolicyDto DecodeSystemRichnessPolicy(byte[] bytes)=>DecodeActivityResource<SystemRichnessPolicyDto>(bytes);
        public ColonisationCyclePolicyDto DecodeColonisationCyclePolicy(byte[] bytes)=>DecodeActivityResource<ColonisationCyclePolicyDto>(bytes);
        private static void RequireColonisationCycleCarrier(CampaignColonisationCycleSaveDto root)
        {
            var rich=root as CampaignSystemRichnessSaveDto;
            var authored=root as CampaignAuthoredSolSaveDto;
            var refinement=root as CampaignEngineeringRefinementSaveDto;
            var beltBodies=root as CampaignBeltBodiesSaveDto;
            if(root!=null&&rich==null&&authored==null&&refinement==null&&beltBodies==null&&root.GetType()!=typeof(CampaignColonisationCycleSaveDto))throw Invalid();
            if(rich!=null&&(rich.GenerationVersion!=CampaignSystemRichnessIdentity.Generation||!ActivityRows(rich.SystemBelts)||rich.SystemBelts!.Count>1000))throw Invalid();
            if(authored!=null&&(authored.GenerationVersion!=CampaignAuthoredSolIdentity.Generation||!ActivityRows(authored.SystemBelts)||authored.SystemBelts!.Count>1000))throw Invalid();
            if(refinement!=null)RequireRefinementCarrier(refinement);
            if(beltBodies!=null)RequireBeltBodiesCarrier(beltBodies);
            if(root==null||root.FormatVersion!=(beltBodies!=null?BeltBodiesEditionIdentity.Format:refinement!=null?EngineeringRefinementIdentity.Format:authored!=null?CampaignAuthoredSolIdentity.Format:rich==null?CampaignColonisationCycleIdentity.Format:CampaignSystemRichnessIdentity.Format)||root.ProfileId!=(beltBodies!=null?BeltBodiesEditionIdentity.Profile:refinement!=null?EngineeringRefinementIdentity.Profile:authored!=null?CampaignAuthoredSolIdentity.Profile:rich==null?CampaignColonisationCycleIdentity.Profile:CampaignSystemRichnessIdentity.Profile)
                ||!ActivityRows(root.CycleActors)||root.CycleActors!.Count>4)throw Invalid();
            // Shared carrier shape only; this does not select a loader or trusted edition.
            RequireColonisationExpansionCarrier(new CampaignColonisationExpansionSaveDto{FormatVersion=CampaignColonisationExpansionIdentity.Format,
                ProfileId=CampaignColonisationGrowthIdentity.Profile,PackageHash=root.PackageHash,BaseCampaign=root.BaseCampaign,AiPlans=root.AiPlans},ColonisationCarrierVocabulary.Cycle,rich==null&&authored==null&&refinement==null&&beltBodies==null?null:CampaignSystemRichnessIdentity.Inventory);
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
