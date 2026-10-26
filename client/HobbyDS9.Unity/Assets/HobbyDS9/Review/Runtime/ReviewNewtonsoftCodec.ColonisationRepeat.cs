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
    public sealed partial class ReviewNewtonsoftCodec:IBackgroundColonisationRepeatCheckpointCodec
    {
        public CampaignColonisationRepeatPackageDto CopyRepeatPackage(CampaignColonisationRepeatPackageDto package)
            =>Decode<CampaignColonisationRepeatPackageDto>(Encode(package));
        public CampaignColonisationRepeatSaveDto CopyRepeatSave(CampaignColonisationRepeatSaveDto root)
            =>DecodeRepeatRoot(EncodeRepeatRoot(root));
        public byte[] EncodeRepeatRoot(CampaignColonisationRepeatSaveDto root)
        {
            RequireColonisationRepeatCarrier(root);
            var bytes = Encode(root);
            if (bytes.Length == 0 || bytes.Length > ReviewColonisationSlotStore.MaximumBytes) throw Invalid();
            return bytes;
        }
        public CampaignColonisationRepeatSaveDto DecodeRepeatRoot(byte[] bytes)
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
                ColonisationWire(wire, typeof(CampaignColonisationRepeatSaveDto), ref visits, new Dictionary<Type, PropertyInfo[]>(),ColonisationCarrierVocabulary.Repeat);
                loadDiagnostic?.Record(phase,ReviewLoadEvent.NormalReturn);
                phase=ReviewLoadPhase.DecodeNatural;
                loadDiagnostic?.Record(phase,ReviewLoadEvent.Begin);
                DecodeNaturalGalaxy(Utf8.GetBytes(wire["BaseCampaign"]!["Map"]!.ToString(Formatting.None)));
                loadDiagnostic?.Record(phase,ReviewLoadEvent.NormalReturn);
                phase=ReviewLoadPhase.DecodeTyped;
                loadDiagnostic?.Record(phase,ReviewLoadEvent.Begin);
                var root = Decode<CampaignColonisationRepeatSaveDto>(bytes);
                loadDiagnostic?.Record(phase,ReviewLoadEvent.NormalReturn);
                phase=ReviewLoadPhase.DecodeCarrier;
                loadDiagnostic?.Record(phase,ReviewLoadEvent.Begin);
                RequireColonisationRepeatCarrier(root);
                loadDiagnostic?.Record(phase,ReviewLoadEvent.NormalReturn);
                return root;
            }
            catch (Exception e) { loadDiagnostic?.Record(phase,ReviewLoadEvent.Error,e);throw new InvalidDataException("Invalid colonisation campaign carrier."); }
        }
        public ColonisationRepeatPolicyDto DecodeColonisationRepeatPolicy(byte[] bytes)=>DecodeActivityResource<ColonisationRepeatPolicyDto>(bytes);
        private static void RequireColonisationRepeatCarrier(CampaignColonisationRepeatSaveDto root)
        {
            if(root==null||root.FormatVersion!=CampaignColonisationRepeatIdentity.Format||root.ProfileId!=CampaignColonisationRepeatIdentity.Profile
                ||!ActivityRows(root.RepeatGoals)||root.RepeatGoals!.Count>4)throw Invalid();
            // This temporary carrier checks shared base/first-plan shape only. It never selects a loader or trusted source.
            RequireColonisationExpansionCarrier(new CampaignColonisationExpansionSaveDto{FormatVersion=CampaignColonisationExpansionIdentity.Format,
                ProfileId=CampaignColonisationGrowthIdentity.Profile,PackageHash=root.PackageHash,BaseCampaign=root.BaseCampaign,AiPlans=root.AiPlans},ColonisationCarrierVocabulary.Repeat);
            var actors=new HashSet<string>(root.AiPlans!.Select(p=>p.ActorId),StringComparer.Ordinal);
            var seen=new HashSet<string>(StringComparer.Ordinal);
            foreach(var g in root.RepeatGoals!)
            {
                if(g.ActorId==null||g.ActorId.Length>512||!actors.Contains(g.ActorId)||!seen.Add(g.ActorId)||!Enum.IsDefined(typeof(ColonisationRepeatState),g.State)
                    ||g.StartedTurn<0||g.ClosedTurn<0||g.SettlementManifestId?.Length>512||g.SupplyCampId?.Length>512||g.Assignment==null
                    ||g.Assignment.ActorId!=g.ActorId||g.Assignment.LatestActions==null||g.Assignment.LatestActions.Count!=0||(int)g.Assignment.Stage>8)throw Invalid();
                RequireColonisationAiPlans(new List<ColonisationAiPlanDto>{g.Assignment},ColonisationCarrierVocabulary.Repeat,CampaignColonisationRepeatIdentity.Goal);
            }
            if(!actors.SetEquals(seen))throw Invalid();
        }
    }
}
