#nullable enable
using System;
using System.Collections.Generic;
using HobbyDS9.Contracts;
using HobbyDS9.Application;

namespace HobbyDS9.UnityReview
{
    // Presentation labels only. Neither categories nor estimates grant access or spend work.
    public static class EngineeringPresentation
    {
        private static readonly IReadOnlyDictionary<string,string> categories=new Dictionary<string,string>(StringComparer.Ordinal) {
            ["advanced-starship-engineering"]="Hulls & ship capabilities",["expanded-hull"]="Hulls & ship capabilities",["reinforced-hull"]="Hulls & ship capabilities",
            ["compact-energy-bank"]="Weapons",["improved-launcher"]="Weapons",["long-range-energy-bank"]="Weapons",
            ["compact-laboratories"]="Laboratories",["compact-life-support"]="Life support",["efficient-life-support"]="Life support",
            ["compact-reactor"]="Reactors",["high-output-reactor"]="Reactors",
            ["efficient-drive"]="Propulsion",["high-speed-drive"]="Propulsion",["maneuver-drive"]="Propulsion",
            ["efficient-shield"]="Shields",["multilayer-shield"]="Shields",["resilient-shield"]="Shields",
            ["expanded-computing"]="Computing",["precision-fire-control"]="Computing",["long-range-sensors"]="Sensors"
        };
        public static string Category(string id)=>id.StartsWith("engineering.",StringComparison.Ordinal)&&categories.TryGetValue(id.Substring(12),out var category)?category:"Other technologies";
        public static bool IsRefinement(EngineeringProjectKind kind)=>kind==EngineeringProjectKind.Refinement;
        public static bool SupportedOutput(EngineeringCardSnapshot project,bool scoutExcluded)=>!scoutExcluded||project.Definition.DefinitionId.Value!="engineering.advanced-starship-engineering";
        public static bool Visible(EngineeringCardSnapshot project,bool scoutExcluded)=>project.IsTarget||project.Band==EngineeringMaturityBand.Mature||project.CanTarget&&SupportedOutput(project,scoutExcluded);
        public static long? ResearchPhases(long cost,long progress,long reserve,long income)
        {
            if(cost<0||progress<0||reserve<0||income<0)throw new ArgumentOutOfRangeException();
            var remaining=Math.Max(0,cost-progress);
            if(remaining==0)return 0;
            if(reserve>=remaining)return 1;
            if(income==0)return null;
            var work=remaining-reserve;
            return work/income+(work%income==0?0:1);
        }
    }
    public sealed class ReviewEngineeringDialog
    {
        public string Title {get;} public string Detail {get;} public string ActionLabel {get;}
        public Action? Commit {get;} public Action? Pause {get;}
        public IReadOnlyList<EngineeringOutputSnapshot> Outputs {get;}
        public ReviewEngineeringDialog(string title,string detail,string actionLabel,Action? commit,Action? pause,IReadOnlyList<EngineeringOutputSnapshot>? outputs=null)
        {Title=title;Detail=detail;ActionLabel=actionLabel;Commit=commit;Pause=pause;Outputs=outputs??Array.Empty<EngineeringOutputSnapshot>();}
    }
}
