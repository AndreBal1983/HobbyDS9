#nullable enable
using System;
using System.Linq;
using HobbyDS9.Contracts.ShipDesign;
using UnityEngine.Scripting;
using UnityEngine.UIElements;
namespace HobbyDS9.UnityClient
{
    [Preserve]public static class LcarsCapacityDetails
    {
        private static void Fact(VisualElement root,string text){var label=new Label(text);label.style.whiteSpace=WhiteSpace.Normal;root.Add(label);}
        private static void Meter(VisualElement root,string name,long value,long? old,bool higher=false)
            =>root.Add(new LcarsCapacityMeter(name,value,Math.Max(value,old??value),old,higher));
        [Preserve]public static VisualElement Module(ComponentVersionSnapshot value,ComponentVersionSnapshot? parent,OperatingProfileKind profile)
        {
            var root=new VisualElement{name="module-capacity-facts"};root.style.minWidth=0;root.style.flexShrink=0;
            Fact(root,value.DisplayName+" · authored version "+value.VersionNumber+(parent==null?"":" · parent "+parent.VersionId));
            Fact(root,"Per module · "+profile+" · catalogue full-power figures; fitted allocation is separate. Reference positions depict this module, not a ship budget.");
            Meter(root,"Module hull space",value.Capacity,parent?.Capacity);Meter(root,"Crew required",value.CrewRequired,parent?.CrewRequired);
            if(value.PowerSupply>0||parent?.PowerSupply>0){Meter(root,"Raw reactor supply",value.PowerSupply,parent?.PowerSupply,true);Fact(root,"Usable power is still limited by the fitted hull's distribution.");}
            var mode=value.Modes.SingleOrDefault(m=>m.Profile==profile);var previous=parent?.Modes.SingleOrDefault(m=>m.Profile==profile);
            if(mode!=null)
            {
                Meter(root,"Full power demand",mode.FullPower,previous?.FullPower);Meter(root,"Minimum powered demand",mode.MinimumPower,previous?.MinimumPower);
                var support=mode.Outputs.SingleOrDefault(o=>o.Kind==ShipOutputKind.CrewSupport);var oldSupport=previous?.Outputs.SingleOrDefault(o=>o.Kind==ShipOutputKind.CrewSupport);
                if(support!=null||oldSupport!=null)Meter(root,"Full-powered crew support",support?.Amount??0,oldSupport?.Amount,true);
            }
            Fact(root,"Industry "+(parent==null?value.Cost.Industry.ToString():parent.Cost.Industry+" → "+value.Cost.Industry)+" · catalogue credits "+(parent==null?value.Cost.Credits.ToString():parent.Cost.Credits+" → "+value.Cost.Credits));
            Fact(root,"Credit charges depend on the actual construction profile/order. Catalogue value is not a new payment rule.");return root;
        }
        [Preserve]public static VisualElement Hull(HullVersionSnapshot value,HullVersionSnapshot? parent)
        {
            var root=new VisualElement{name="hull-capacity-facts"};root.style.minWidth=0;root.style.flexShrink=0;
            Fact(root,value.DisplayName+" · authored hull version "+value.VersionNumber+(parent==null?"":" · parent "+parent.VersionId));
            Meter(root,"Hull space supplied",value.Capacity,parent?.Capacity,true);Meter(root,"Power distribution limit",value.PowerDistributionLimit,parent?.PowerDistributionLimit,true);Meter(root,"Minimum crew required",value.MinimumCrew,parent?.MinimumCrew);
            Fact(root,"Industry "+(parent==null?value.Cost.Industry.ToString():parent.Cost.Industry+" → "+value.Cost.Industry)+" · catalogue credits "+(parent==null?value.Cost.Credits.ToString():parent.Cost.Credits+" → "+value.Cost.Credits)+" · actual charges follow the construction order.");return root;
        }
    }
}
