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
    public sealed partial class ReviewNewtonsoftCodec:IBackgroundColonisationGrowthCheckpointCodec
    {
        public CampaignColonisationExpansionPackageDto CopyGrowthPackage(CampaignColonisationExpansionPackageDto package)
            =>Decode<CampaignColonisationExpansionPackageDto>(Encode(package));
        public CampaignColonisationExpansionSaveDto CopyGrowthSave(CampaignColonisationExpansionSaveDto root)
            =>DecodeGrowthRoot(EncodeGrowthRoot(root));
        public byte[] EncodeGrowthRoot(CampaignColonisationExpansionSaveDto root)
        {
            RequireColonisationExpansionCarrier(root,ColonisationCarrierVocabulary.Growth);
            var bytes = Encode(root);
            if (bytes.Length == 0 || bytes.Length > ReviewColonisationSlotStore.MaximumBytes) throw Invalid();
            return bytes;
        }
        public CampaignColonisationExpansionSaveDto DecodeGrowthRoot(byte[] bytes)
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
                ColonisationWire(wire, typeof(CampaignColonisationExpansionSaveDto), ref visits, new Dictionary<Type, PropertyInfo[]>(),ColonisationCarrierVocabulary.Growth);
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
                RequireColonisationExpansionCarrier(root,ColonisationCarrierVocabulary.Growth);
                loadDiagnostic?.Record(phase,ReviewLoadEvent.NormalReturn);
                return root;
            }
            catch (Exception e) { loadDiagnostic?.Record(phase,ReviewLoadEvent.Error,e);throw new InvalidDataException("Invalid colonisation campaign carrier."); }
        }
    }
}
