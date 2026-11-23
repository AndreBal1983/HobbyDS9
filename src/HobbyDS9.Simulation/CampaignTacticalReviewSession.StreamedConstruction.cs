using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    public sealed partial class CampaignTacticalReviewSession
    {
        private List<StreamedColonyConstructionDto> CaptureStreamedConstruction(IEnumerable<ColonyStateDto> own,GameStateDto game)
        {
            StreamedConstructionOutputDto Output(ColonyStateDto colony)
            {
                var value=WorldEconomy.Project(colony,game,definitions);
                return new StreamedConstructionOutputDto{Industry=value.Industry,Research=value.ResearchOutput,Food=value.FoodOutput};
            }
            StreamedConstructionProjectionDto Hypothetical(ColonyStateDto colony,IEnumerable<string> additions)
            {
                // Only forecast arithmetic is optional. Invalid actual state/source
                // invariants must still fail through their ordinary validation.
                try
                {
                    var copy=PopulationData.Copy(colony);
                    foreach(var id in additions)copy.Infrastructure!.Add(new InfrastructureStateDto{DefinitionId=id,Count=1});
                    return new StreamedConstructionProjectionDto{Status=StreamedConstructionProjectionStatus.Available,Output=Output(copy)};
                }
                catch(OverflowException)
                {return new StreamedConstructionProjectionDto{Status=StreamedConstructionProjectionStatus.ArithmeticOverflow,Reason="This output projection exceeds the supported arithmetic range."};}
            }
            var result=new List<StreamedColonyConstructionDto>();
            foreach(var colony in own.OrderBy(c=>c.ColonyId,StringComparer.Ordinal))
            {
                var pending=Streams!.Production.Where(q=>q.ColonyId==colony.ColonyId&&q.OwnerId==colony.OwnerCivilizationId&&q.Kind==ActivityProductionKind.Infrastructure)
                    .Select(q=>q.DefinitionId).ToList();
                var row=new StreamedColonyConstructionDto{ColonyId=colony.ColonyId,Current=Output(colony),AfterPendingStructures=Hypothetical(colony,pending)};
                foreach(var source in activities!.Content.Structures!.OrderBy(s=>s.DefinitionId,StringComparer.Ordinal))
                {
                    var built=colony.Infrastructure!.Where(i=>i.DefinitionId==source.DefinitionId).Sum(i=>i.Count);
                    var queued=pending.Count(id=>id==source.DefinitionId);
                    var item=new StreamedBuildingConstructionDto{DefinitionId=source.DefinitionId,BuiltCount=built,QueuedCount=queued,
                        Unavailable=built>0?StreamedBuildingUnavailable.AlreadyBuilt:queued>0?StreamedBuildingUnavailable.AlreadyQueued:StreamedBuildingUnavailable.None};
                    if(item.Unavailable==StreamedBuildingUnavailable.None)
                    {item.AfterOne=Hypothetical(colony,new[]{source.DefinitionId});item.AfterPendingAndOne=Hypothetical(colony,pending.Concat(new[]{source.DefinitionId}));}
                    row.Buildings.Add(item);
                }
                result.Add(row);
            }
            return result;
        }
    }
}
