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
        [Preserve]public static VisualElement EngineeringModule(ComponentVersionSnapshot value,ComponentVersionSnapshot? parent,string maturity)
        {
            var root=new VisualElement{name="engineering-module-facts"};root.style.minWidth=0;root.style.flexShrink=0;
            Fact(root,value.DisplayName+" · Cruise · per module");
            var grid=new VisualElement();grid.style.flexDirection=FlexDirection.Row;grid.style.flexWrap=Wrap.Wrap;root.Add(grid);
            void Capacity(string label,long current,long? previous,bool higher=false)
            {
                var meter=new LcarsCapacityMeter(label,current,Math.Max(current,previous??current),previous,higher);meter.Compact();meter.style.flexBasis=Length.Percent(48);meter.style.flexGrow=1;meter.style.minWidth=170;meter.style.marginRight=8;grid.Add(meter);
            }
            Capacity("Hull space",value.Capacity,parent?.Capacity);
            var mode=value.Modes.SingleOrDefault(m=>m.Profile==OperatingProfileKind.Cruise);var old=parent?.Modes.SingleOrDefault(m=>m.Profile==OperatingProfileKind.Cruise);
            if(value.PowerSupply>0||parent?.PowerSupply>0)Capacity("Power supplied",value.PowerSupply,parent?.PowerSupply,true);
            if(mode!=null)
            {
                Capacity("Power · full demand",mode.FullPower,old?.FullPower);
                Fact(root,"Minimum powered demand · "+(old==null?mode.MinimumPower.ToString():old.MinimumPower+" → "+mode.MinimumPower));
                foreach(var output in mode.Outputs)Capacity("Output · "+OutputName(output.Kind),output.Amount,old?.Outputs.FirstOrDefault(o=>o.Kind==output.Kind)?.Amount,true);
                if(mode.Outputs.Count==0)Fact(root,"Output · No rated output in this profile");
            }
            else {Fact(root,"Power demand · Not reported");Fact(root,"Output · Not reported");}
            Fact(root,"Industry cost · "+(parent==null?value.Cost.Industry.ToString():parent.Cost.Industry+" → "+value.Cost.Industry));
            Fact(root,"Version maturity · "+maturity);
            Fact(root,"Catalogue credits · "+(parent==null?value.Cost.Credits.ToString():parent.Cost.Credits+" → "+value.Cost.Credits)+" · actual charge follows the construction order.");
            return root;
        }
        private static string OutputName(ShipOutputKind value)
        {
            var text=value.ToString();return System.Text.RegularExpressions.Regex.Replace(text,"([a-z])([A-Z])","$1 $2").ToLowerInvariant();
        }
        [Preserve]public static VisualElement EngineeringHull(HullVersionSnapshot value,HullVersionSnapshot? parent,string maturity)
        {
            var root=new VisualElement{name="engineering-hull-facts"};Fact(root,value.DisplayName);
            void Capacity(string label,long current,long? previous,bool higher)
            {var meter=new LcarsCapacityMeter(label,current,Math.Max(current,previous??current),previous,higher);meter.Compact();root.Add(meter);}
            Capacity("Hull space",value.Capacity,parent?.Capacity,true);Capacity("Power distribution",value.PowerDistributionLimit,parent?.PowerDistributionLimit,true);
            Fact(root,"Crew required · "+value.MinimumCrew);Fact(root,"Industry cost · "+(parent==null?value.Cost.Industry.ToString():parent.Cost.Industry+" → "+value.Cost.Industry));
            Fact(root,"Version maturity · "+maturity);Fact(root,"Catalogue credits · "+value.Cost.Credits+" · actual charge follows the construction order.");return root;
        }
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
