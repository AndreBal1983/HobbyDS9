#nullable enable
using HobbyDS9.Contracts;
using Newtonsoft.Json.Linq;
namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewNewtonsoftCodec
    {
        private static void RequireRefinementWire(JToken wire)
        {
            var state=wire["Refinement"] as JObject;
            if(state==null||!(state["Projects"] is JArray projects)||projects.Count>EngineeringRefinementIdentity.MaximumProjects
                ||!(state["Operations"] is JArray operations)||operations.Count>EngineeringRefinementIdentity.MaximumCommands)throw Invalid();
            foreach(var project in projects)
            {
                var context=project["Context"];
                if(context==null||!(context["FrozenCatalogue"]?["Components"] is JArray components)||components.Count>512
                    ||!(context["FrozenCatalogue"]?["Hulls"] is JArray hulls)||hulls.Count>64
                    ||!(context["Fit"]?["Installations"] is JArray installations)||installations.Count>128
                    ||!(context["Fit"]?["Profiles"] is JArray profiles)||profiles.Count>3)throw Invalid();
                foreach(var profile in profiles)
                    if(!(profile["Allocations"] is JArray allocations)||allocations.Count>128)throw Invalid();
            }
        }
        private static void RequireRefinementCarrier(CampaignEngineeringRefinementSaveDto root)
        {
            var state=root.Refinement;
            if(root.GenerationVersion!=CampaignAuthoredSolIdentity.Generation||!ActivityRows(root.SystemBelts)||root.SystemBelts!.Count>1000
                ||state==null||state.PolicyId!=EngineeringRefinementIdentity.Policy||!ActivityRows(state.Projects)||!ActivityRows(state.Operations)
                ||state.Projects!.Count>EngineeringRefinementIdentity.MaximumProjects||state.Operations!.Count>EngineeringRefinementIdentity.MaximumCommands
                ||root.BaseCampaign?.Journal==null)throw Invalid();
            var previous=0;
            foreach(var op in state.Operations)
            {
                if(op.Command==null||op.BaseJournalOffset<previous||op.BaseJournalOffset>root.BaseCampaign.Journal.Count)throw Invalid();
                previous=op.BaseJournalOffset;
            }
            foreach(var p in state.Projects)
            {
                var c=p.Context;
                if(c?.FrozenCatalogue?.Components==null||c.FrozenCatalogue.Components.Count>512||c.FrozenCatalogue.Hulls==null||c.FrozenCatalogue.Hulls.Count>64
                    ||c.Fit?.Installations==null||c.Fit.Installations.Count>128||c.Fit.Profiles==null||c.Fit.Profiles.Count>3)throw Invalid();
                foreach(var profile in c.Fit.Profiles)
                    if(profile==null||profile.Allocations==null||profile.Allocations.Count>128)throw Invalid();
            }
        }
    }
}
