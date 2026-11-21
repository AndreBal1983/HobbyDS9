#nullable enable
using System.Collections.Generic;
using HobbyDS9.Contracts;
using Newtonsoft.Json.Linq;
namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewNewtonsoftCodec
    {
        private static void RequireBeltBodiesWire(JToken wire)
        {
            RequireRefinementWire(wire);
            var s=wire["BeltBodies"] as JObject;
            if(s==null||!(s["Definitions"] is JArray d)||d.Count>BeltBodiesEditionIdentity.MaximumBodies
                ||!(s["Deposits"] is JArray r)||r.Count>BeltBodiesEditionIdentity.MaximumBodies
                ||!(s["Observations"] is JArray o)||o.Count>BeltBodiesEditionIdentity.MaximumObservations
                ||!(s["Surveys"] is JArray q)||q.Count>BeltBodiesEditionIdentity.MaximumSurveys
                ||!(s["Mines"] is JArray m)||m.Count>BeltBodiesEditionIdentity.MaximumMines
                ||!(s["LastShipments"] is JArray f)||f.Count>BeltBodiesEditionIdentity.MaximumLastShipments
                ||!(s["Operations"] is JArray x)||x.Count>BeltBodiesEditionIdentity.MaximumCommands)throw Invalid();
        }
        private static void RequireBeltBodiesCarrier(CampaignBeltBodiesSaveDto root)
        {
            RequireRefinementCarrier(new CampaignEngineeringRefinementSaveDto{GenerationVersion=root.GenerationVersion,SystemBelts=root.SystemBelts,Refinement=root.Refinement,BaseCampaign=root.BaseCampaign});
            var s=root.BeltBodies;
            if(s==null||s.PolicyId!=BeltBodiesEditionIdentity.Policy||!ActivityRows(s.Definitions)||s.Definitions!.Count>BeltBodiesEditionIdentity.MaximumBodies
                ||!ActivityRows(s.Deposits)||s.Deposits!.Count>BeltBodiesEditionIdentity.MaximumBodies
                ||!ActivityRows(s.Observations)||s.Observations!.Count>BeltBodiesEditionIdentity.MaximumObservations
                ||!ActivityRows(s.Surveys)||s.Surveys!.Count>BeltBodiesEditionIdentity.MaximumSurveys
                ||!ActivityRows(s.Mines)||s.Mines!.Count>BeltBodiesEditionIdentity.MaximumMines
                ||!ActivityRows(s.LastShipments)||s.LastShipments!.Count>BeltBodiesEditionIdentity.MaximumLastShipments
                ||!ActivityRows(s.Operations)||s.Operations!.Count>BeltBodiesEditionIdentity.MaximumCommands||root.BaseCampaign?.Journal==null)throw Invalid();
            var stamps=new HashSet<(int,long)>();var previous=0;long revision=-1;
            foreach(var op in root.Refinement!.Operations!)
            {if(op.Command==null||op.BaseJournalOffset<previous||op.BaseJournalOffset==previous&&op.Command.ExpectedRevision<=revision||!stamps.Add((op.BaseJournalOffset,op.Command.ExpectedRevision)))throw Invalid();previous=op.BaseJournalOffset;revision=op.Command.ExpectedRevision;}
            previous=0;revision=-1;
            foreach(var op in s.Operations)
            {if(op.Command==null||op.BaseJournalOffset<previous||op.BaseJournalOffset>root.BaseCampaign.Journal.Count||op.BaseJournalOffset==previous&&op.Command.ExpectedRevision<=revision||!stamps.Add((op.BaseJournalOffset,op.Command.ExpectedRevision)))throw Invalid();previous=op.BaseJournalOffset;revision=op.Command.ExpectedRevision;}
            bool Text(string? x)=>x==null||x.Length<=512;
            foreach(var d in s.Definitions)if(!Text(d.BodyId)||!Text(d.FeatureId)||!Text(d.SystemId)||!Text(d.DisplayName)||!Text(d.ResourceDefinitionId))throw Invalid();
            foreach(var d in s.Deposits)if(!Text(d.BodyId))throw Invalid();
            foreach(var o in s.Observations)if(!Text(o.ActorId)||!Text(o.BodyId))throw Invalid();
            foreach(var q in s.Surveys)if(!Text(q.SurveyId)||!Text(q.ActorId)||!Text(q.ShipId)||!Text(q.BodyId))throw Invalid();
            foreach(var m in s.Mines)if(!Text(m.InstallationId)||!Text(m.QueueItemId)||!Text(m.ActorId)||!Text(m.BodyId)||!Text(m.ColonyId)||!Text(m.LatestReport))throw Invalid();
            foreach(var f in s.LastShipments)if(!Text(f.InstallationId)||!Text(f.ActorId)||!Text(f.BodyId)||!Text(f.ColonyId)||!Text(f.ResourceDefinitionId))throw Invalid();
            foreach(var op in s.Operations){var c=op.Command!;if(!Text(c.ActorId)||!Text(c.CommandId)||!Text(c.BodyId)||!Text(c.ShipId)||!Text(c.ColonyId)||!Text(c.SurveyId)||!Text(c.MineId))throw Invalid();}
        }
    }
}
