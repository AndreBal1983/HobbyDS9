using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    public static partial class FoundationValidator
    {
        private static void ValidateResearchDefinitions(DefinitionCatalogDto dto, HashSet<DefinitionId> definitions,
            HashSet<string> typed, string path, List<ValidationIssue> issues)
        {
            if (dto.EngineeringApplications == null) { Add(issues, ValidationCode.MissingField, path + ".EngineeringApplications"); return; }
            var nodes = dto.EngineeringApplications.Where(n => n != null && DefinitionId.TryParse(n.DefinitionId, out _))
                .GroupBy(n => n.DefinitionId, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
            for (var i = 0; i < dto.EngineeringApplications.Count; i++)
            {
                var node = dto.EngineeringApplications[i]; var p = path + ".EngineeringApplications[" + i.ToString(CultureInfo.InvariantCulture) + "]";
                if (node == null) { Add(issues, ValidationCode.MissingField, p); continue; }
                if (!DefinitionId.TryParse(node.DefinitionId, out var id)) Add(issues, ValidationCode.InvalidId, p + ".DefinitionId");
                else if (!definitions.Contains(id)) Add(issues, ValidationCode.MissingDefinition, p + ".DefinitionId");
                if (node.DefinitionId != null && !typed.Add(node.DefinitionId)) Add(issues, ValidationCode.DuplicateId, p + ".DefinitionId");
                if (string.IsNullOrWhiteSpace(node.DisplayName)) Add(issues, ValidationCode.MissingField, p + ".DisplayName");
                if (node.ScienceRequirements == null) Add(issues,ValidationCode.MissingField,p+".ScienceRequirements");
                else {
                    var uniqueRequirements=new HashSet<(ScienceDiscipline,int,int)>();
                    foreach(var q in node.ScienceRequirements) {
                        if(q==null||!ScienceRules.ValidDiscipline(q.Discipline)||q.IntervalStartLevel<1||q.IntervalStartLevel>9||q.RequiredBreakthroughOrdinal<1||q.RequiredBreakthroughOrdinal>3) Add(issues,ValidationCode.InvalidResearchValue,p+".ScienceRequirements");
                        else if(!uniqueRequirements.Add((q.Discipline,q.IntervalStartLevel,q.RequiredBreakthroughOrdinal))) Add(issues,ValidationCode.DuplicateId,p+".ScienceRequirements");
                    }
                }
                if(!Enum.IsDefined(typeof(EngineeringProjectKind),node.Kind))Add(issues,ValidationCode.InvalidResearchValue,p+".Kind");
                if(node.Kind==EngineeringProjectKind.Refinement)
                {
                    if(node.ParameterChange==null||!DefinitionId.TryParse(node.ParameterChange.ParameterDefinitionId,out var parameter)||!definitions.Contains(parameter)||nodes.ContainsKey(node.ParameterChange.ParameterDefinitionId)||node.ParameterChange.Value<0||node.ParameterChange.Value>10000)Add(issues,ValidationCode.InvalidResearchValue,p+".ParameterChange");
                }
                else if(node.ParameterChange!=null)Add(issues,ValidationCode.InvalidResearchValue,p+".ParameterChange");
                if (node.BaseEngineeringCost <= 0) Add(issues, ValidationCode.InvalidResearchValue, p + ".BaseEngineeringCost");
                if (node.PrerequisiteDefinitionIds == null) { Add(issues, ValidationCode.MissingField, p + ".PrerequisiteDefinitionIds"); continue; }
                var unique = new HashSet<string>(StringComparer.Ordinal);
                for (var j = 0; j < node.PrerequisiteDefinitionIds.Count; j++)
                {
                    var prerequisite = node.PrerequisiteDefinitionIds[j]; var item = p + ".PrerequisiteDefinitionIds[" + j.ToString(CultureInfo.InvariantCulture) + "]";
                    if (!DefinitionId.TryParse(prerequisite, out _)) { Add(issues, ValidationCode.InvalidId, item); continue; }
                    if (!unique.Add(prerequisite)) Add(issues, ValidationCode.DuplicateId, item);
                    if (!nodes.ContainsKey(prerequisite)) Add(issues, ValidationCode.MissingDefinition, item);
                    if (prerequisite == node.DefinitionId) Add(issues, ValidationCode.SelfPrerequisite, item);
                }
            }
            // Kahn's algorithm: iterative even for a large/deep content graph.
            var indegrees = nodes.ToDictionary(p => p.Key, p => (p.Value.PrerequisiteDefinitionIds ?? new List<string>())
                .Where(id => id != null && nodes.ContainsKey(id)).Distinct(StringComparer.Ordinal).Count(), StringComparer.Ordinal);
            var dependents = nodes.Keys.ToDictionary(id => id, _ => new SortedSet<string>(StringComparer.Ordinal), StringComparer.Ordinal);
            foreach (var pair in nodes.OrderBy(p => p.Key, StringComparer.Ordinal))
            foreach (var prerequisite in (pair.Value.PrerequisiteDefinitionIds ?? new List<string>()).Where(id => id != null && nodes.ContainsKey(id)).Distinct(StringComparer.Ordinal).OrderBy(id => id, StringComparer.Ordinal))
                dependents[prerequisite].Add(pair.Key);
            var ready = new SortedSet<string>(indegrees.Where(p => p.Value == 0).Select(p => p.Key), StringComparer.Ordinal);
            var visited = 0;
            while (ready.Count != 0)
            {
                var next = ready.Min!; ready.Remove(next); visited++;
                foreach (var dependent in dependents[next]) if (--indegrees[dependent] == 0) ready.Add(dependent);
            }
            if (visited != nodes.Count) Add(issues, ValidationCode.ResearchCycle, path + ".EngineeringApplications");
        }

        private static void ValidateResearchState(GameStateDto dto, DefinitionCatalogDto? catalog, string path, List<ValidationIssue> issues)
        {
            var nodes = (catalog?.EngineeringApplications ?? new List<EngineeringApplicationDefinitionDto>()).Where(n => n != null && DefinitionId.TryParse(n.DefinitionId, out _))
                .GroupBy(n => n.DefinitionId, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
            if(!dto.ResearchPaceFactor.HasValue||!ScienceRules.ValidPace(dto.ResearchPaceFactor.Value))Add(issues,ValidationCode.InvalidResearchValue,path+".ResearchPaceFactor");
            if(dto.LastResearchReceipts==null)Add(issues,ValidationCode.MissingField,path+".LastResearchReceipts");
            else
            {
                var actors=new HashSet<string>((dto.Civilizations??new List<CivilizationStateDto>()).Where(c=>c!=null).Select(c=>c.CivilizationId),StringComparer.Ordinal);
                if((System.Numerics.BigInteger)dto.LastResearchReceipts.Count>actors.Count*512L)Add(issues,ValidationCode.InvalidResearchValue,path+".LastResearchReceipts");
                var categories=new HashSet<string>(StringComparer.Ordinal){"allocation","science-allocated","engineering-allocated","science-spent","science-breakthrough","science-level","engineering-spent","engineering-band","engineering-mature"};
                foreach(var receipt in dto.LastResearchReceipts)
                {
                    if(receipt==null){Add(issues,ValidationCode.MissingField,path+".LastResearchReceipts");continue;}
                    bool bad=!EntityId.TryParse(receipt.CivilizationId,out _)||!actors.Contains(receipt.CivilizationId)||receipt.Amount<0||!categories.Contains(receipt.Category);
                    bool science=receipt.Category=="science-spent"||receipt.Category=="science-breakthrough"||receipt.Category=="science-level";
                    bool engineering=receipt.Category=="engineering-spent"||receipt.Category=="engineering-band"||receipt.Category=="engineering-mature";
                    if(science)bad|=!receipt.Discipline.HasValue||!ScienceRules.ValidDiscipline(receipt.Discipline.GetValueOrDefault())||receipt.IntervalStartLevel<1||receipt.IntervalStartLevel>9||receipt.ApplicationDefinitionId!=null;
                    else bad|=receipt.Discipline.HasValue||receipt.IntervalStartLevel!=0||receipt.BreakthroughOrdinal!=0;
                    if(receipt.Category=="science-breakthrough")bad|=receipt.BreakthroughOrdinal<1||receipt.BreakthroughOrdinal>5;
                    else bad|=receipt.BreakthroughOrdinal!=0;
                    if(engineering)bad|=receipt.ApplicationDefinitionId==null||!nodes.ContainsKey(receipt.ApplicationDefinitionId);
                    else bad|=receipt.ApplicationDefinitionId!=null;
                    if(receipt.Category=="engineering-band")bad|=receipt.Amount<1||receipt.Amount>3;
                    if(bad)Add(issues,ValidationCode.InvalidResearchValue,path+".LastResearchReceipts");
                }
            }
            if(dto.ResearchPaceFactor.HasValue&&ScienceRules.ValidPace(dto.ResearchPaceFactor.Value))foreach(var app in nodes.Values.Where(a=>a.BaseEngineeringCost>0))
            { try{ScienceRules.AdjustCost(app.BaseEngineeringCost,dto.ResearchPaceFactor.Value);}catch(OverflowException){Add(issues,ValidationCode.InvalidResearchValue,path+".ResearchPaceFactor");} }
            if (dto.Civilizations == null) return;
            for (var i = 0; i < dto.Civilizations.Count; i++)
            {
                var civ = dto.Civilizations[i]; var p = path + ".Civilizations[" + i.ToString(CultureInfo.InvariantCulture) + "]";
                if (civ == null) continue;
                int pace=dto.ResearchPaceFactor??10000;
                if(ScienceRules.Validate(civ,pace).Count!=0)Add(issues,ValidationCode.InvalidResearchValue,p+".Science");
                if(civ.EngineeringProgress==null)Add(issues,ValidationCode.MissingField,p+".EngineeringProgress");
                else {
                    var uniqueProgress=new HashSet<string>(StringComparer.Ordinal);
                    foreach(var progress in civ.EngineeringProgress) {
                        if(progress==null||!DefinitionId.TryParse(progress.DefinitionId,out _)){Add(issues,ValidationCode.InvalidId,p+".EngineeringProgress");continue;}
                        if(!uniqueProgress.Add(progress.DefinitionId))Add(issues,ValidationCode.DuplicateId,p+".EngineeringProgress");
                        if(!nodes.TryGetValue(progress.DefinitionId,out var app)){Add(issues,ValidationCode.MissingDefinition,p+".EngineeringProgress");continue;}
                        if(app.BaseEngineeringCost<=0||!ScienceRules.ValidPace(pace))continue;
                        long cost;try{cost=ScienceRules.AdjustCost(app.BaseEngineeringCost,pace);}catch(OverflowException){Add(issues,ValidationCode.InvalidResearchValue,p+".EngineeringProgress");continue;}
                        if(progress.Progress<0||progress.Progress>cost)Add(issues,ValidationCode.InvalidResearchValue,p+".EngineeringProgress");
                        bool mature=civ.MatureEngineeringApplicationDefinitionIds?.Contains(progress.DefinitionId)==true;
                        if(mature!=(progress.Progress==cost))Add(issues,ValidationCode.InvalidResearchValue,p+".EngineeringProgress");
                    }
                    if(civ.MatureEngineeringApplicationDefinitionIds!=null)foreach(var id in civ.MatureEngineeringApplicationDefinitionIds)if(!uniqueProgress.Contains(id))Add(issues,ValidationCode.InvalidResearchValue,p+".MatureEngineeringApplicationDefinitionIds");
                }
                var requiredParameters=new HashSet<string>(nodes.Values.Where(n=>n.Kind==EngineeringProjectKind.Refinement&&n.ParameterChange!=null).Select(n=>n.ParameterChange!.ParameterDefinitionId),StringComparer.Ordinal);
                if(civ.EngineeringParameters==null)Add(issues,ValidationCode.MissingField,p+".EngineeringParameters");
                else if(civ.EngineeringParameters.Any(v=>v==null||!DefinitionId.TryParse(v.DefinitionId,out _)||v.Value<0||v.Value>10000)||civ.EngineeringParameters.Select(v=>v.DefinitionId).Distinct(StringComparer.Ordinal).Count()!=civ.EngineeringParameters.Count||!requiredParameters.SetEquals(civ.EngineeringParameters.Select(v=>v.DefinitionId)))Add(issues,ValidationCode.InvalidResearchValue,p+".EngineeringParameters");
                if (civ.EngineeringReserve < 0) Add(issues, ValidationCode.InvalidResearchValue, p + ".EngineeringReserve");
                var unlocked = new HashSet<string>(StringComparer.Ordinal);
                if (civ.MatureEngineeringApplicationDefinitionIds == null) Add(issues, ValidationCode.MissingField, p + ".MatureEngineeringApplicationDefinitionIds");
                else for (var j = 0; j < civ.MatureEngineeringApplicationDefinitionIds.Count; j++)
                {
                    var id = civ.MatureEngineeringApplicationDefinitionIds[j]; var item = p + ".MatureEngineeringApplicationDefinitionIds[" + j.ToString(CultureInfo.InvariantCulture) + "]";
                    if (!DefinitionId.TryParse(id, out _)) { Add(issues, ValidationCode.InvalidId, item); continue; }
                    if (!unlocked.Add(id)) Add(issues, ValidationCode.DuplicateId, item);
                    if (!nodes.ContainsKey(id)) Add(issues, ValidationCode.MissingDefinition, item);
                }
                if(ScienceRules.Validate(civ,pace).Count==0&&civ.EngineeringProgress!=null)
                foreach(var progress in civ.EngineeringProgress.Where(p=>p!=null&&p.Progress>0))
                {
                    if(!nodes.TryGetValue(progress.DefinitionId,out var application)||application.ScienceRequirements==null||application.ScienceRequirements.Any(q=>q==null||!ScienceRules.ValidDiscipline(q.Discipline)||q.IntervalStartLevel<1||q.IntervalStartLevel>9||q.RequiredBreakthroughOrdinal<1||q.RequiredBreakthroughOrdinal>3))continue;
                    if(application.ScienceRequirements.Any(q=>!EngineeringRules.ScienceSatisfied(civ,q,pace))||application.PrerequisiteDefinitionIds?.Any(id=>!unlocked.Contains(id))==true)Add(issues,ValidationCode.EngineeringPrerequisiteMissing,p+".EngineeringProgress");
                }
                if (civ.EngineeringTargetDefinitionId == null) continue;
                var target = civ.EngineeringTargetDefinitionId;
                if (!DefinitionId.TryParse(target, out _)) Add(issues, ValidationCode.InvalidId, p + ".EngineeringTargetDefinitionId");
                else if (!nodes.TryGetValue(target, out var node)) Add(issues, ValidationCode.MissingDefinition, p + ".EngineeringTargetDefinitionId");
                else
                {
                    if(ScienceRules.Validate(civ,pace).Count==0&&node.ScienceRequirements!=null&&node.ScienceRequirements.All(q=>q!=null&&ScienceRules.ValidDiscipline(q.Discipline)&&q.IntervalStartLevel>=1&&q.IntervalStartLevel<=9&&q.RequiredBreakthroughOrdinal>=1&&q.RequiredBreakthroughOrdinal<=3)&&EngineeringRules.Availability(civ,node,pace)==CommandErrorCode.EngineeringScienceRequirementMissing)Add(issues,ValidationCode.EngineeringPrerequisiteMissing,p+".EngineeringTargetDefinitionId");
                    if (unlocked.Contains(target)) Add(issues, ValidationCode.EngineeringAlreadyMature, p + ".EngineeringTargetDefinitionId");
                    if (node.PrerequisiteDefinitionIds != null && node.PrerequisiteDefinitionIds.Any(id => id == null || !unlocked.Contains(id)))
                        Add(issues, ValidationCode.EngineeringPrerequisiteMissing, p + ".EngineeringTargetDefinitionId");
                }
            }
        }
    }
}
