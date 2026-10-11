using System;
using System.Linq;
using System.Collections.Generic;
using HobbyDS9.Contracts;
namespace HobbyDS9.Simulation
{
    internal static class ResearchPhase
    {
        internal static void Execute(GameStateDto staging,DefinitionCatalogDto definitions)
        {
            var receipts=new List<ResearchReceiptDto>();var pace=staging.ResearchPaceFactor!.Value;
            foreach(var civilization in staging.Civilizations!.OrderBy(c=>c.CivilizationId,StringComparer.Ordinal))
            {
                long output=0;foreach(var colony in staging.Colonies!.OrderBy(c=>c.ColonyId,StringComparer.Ordinal))if(colony.OwnerCivilizationId==civilization.CivilizationId)output=checked(output+WorldEconomy.Project(colony,staging,definitions).ResearchOutput);
                ScienceRules.Allocate(civilization,output);
                if(output>0) {
                    receipts.Add(new ResearchReceiptDto{CivilizationId=civilization.CivilizationId,Category="allocation",Amount=output});
                    long science=(long)((System.Numerics.BigInteger)output*civilization.ScienceAllocationBps/10000);
                    receipts.Add(new ResearchReceiptDto{CivilizationId=civilization.CivilizationId,Category="science-allocated",Amount=science});
                    receipts.Add(new ResearchReceiptDto{CivilizationId=civilization.CivilizationId,Category="engineering-allocated",Amount=output-science});
                }
                ScienceRules.SpendScience(civilization,pace,civilization.CivilizationId,receipts);
                EngineeringRules.Spend(civilization,pace,definitions,civilization.CivilizationId,receipts);
            }
            // A bounded one-turn explanation, canonically grouped independently of DTO order.
            var categories=new[]{"allocation","science-allocated","engineering-allocated","science-spent","science-breakthrough","science-level","engineering-spent","engineering-band","engineering-mature"};
            staging.LastResearchReceipts=receipts.OrderBy(r=>r.CivilizationId,StringComparer.Ordinal).ThenBy(r=>Array.IndexOf(categories,r.Category)).ThenBy(r=>r.Discipline).ThenBy(r=>r.IntervalStartLevel).ThenBy(r=>r.ApplicationDefinitionId,StringComparer.Ordinal).ThenBy(r=>r.Amount).ToList();
        }
    }
}
