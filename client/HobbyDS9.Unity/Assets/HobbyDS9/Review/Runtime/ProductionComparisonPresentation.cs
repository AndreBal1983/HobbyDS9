#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HobbyDS9.Contracts;
namespace HobbyDS9.UnityReview
{
    // Formatting of the supported frozen publication only. Never an aggregate
    // combat/exploration rating or a live ship-condition/supply observation.
    public static class ProductionComparisonPresentation
    {
        private static string Number(long? value)=>value.HasValue?value.Value.ToString("N0",CultureInfo.InvariantCulture):"unavailable";
        private static string Percent(int basisPoints)=>((decimal)basisPoints/100).ToString("0.##",CultureInfo.InvariantCulture)+"%";
        private static string Weapons(ActivityBuildComparisonDto value)=>value.Weapons==null?"Weapons unavailable":value.Weapons.Count==0?"Unarmed":value.Weapons.Count+" weapon package"+(value.Weapons.Count==1?"":"s")+" · "+value.Weapons.Count(w=>w.Powered)+" powered";
        public static string Summary(ActivityBuildComparisonDto? value)
            =>value==null?"Exploration unavailable · Combat unavailable":"Study work "+Number(value.StudyWork)+" · Contact "+Number(value.ContactCapacity)+" · Hull "+Number(value.MaxHull)+" · Armor "+Number(value.Armor)+" · Shields "+Number(value.ShieldCapacity)+" · Tactical speed "+Number(value.Speed)+" · Movement budget "+Number(value.MovementBudget)+" · "+Weapons(value);
        public static IReadOnlyList<string> Details(ActivityBuildComparisonDto? value)
        {
            if(value==null)return new[]{"Exploration / combat capabilities unavailable for this class."};
            var rows=new List<string>{"Study work · "+Number(value.StudyWork),"Contact capacity · "+Number(value.ContactCapacity),
                "Maximum hull · "+Number(value.MaxHull),"Armor · "+Number(value.Armor),"Shield capacity · "+Number(value.ShieldCapacity),
                "Tactical speed · "+Number(value.Speed),"Strategic movement budget · "+Number(value.MovementBudget)};
            if(value.Weapons==null)rows.Add("Weapons · unavailable");
            else if(value.Weapons.Count==0)rows.Add("Weapons · Unarmed");
            else
            {
                for(var i=0;i<value.Weapons.Count;i++)
                {
                    var weapon=value.Weapons[i];
                    rows.Add("Weapon "+(i+1)+" · "+weapon.Class+" · "+weapon.Coverage+" · Damage "+Number(weapon.Damage));
                    rows.Add("Opportunities / round · "+Number(weapon.OpportunitiesPerRound)+" · Cadence · "+Number(weapon.CadenceRounds)+" rounds");
                    rows.Add("Accuracy · "+Percent(weapon.AccuracyBps)+" · Tracking · "+Percent(weapon.TrackingBps));
                    rows.Add("Power allocated / required · "+Number(weapon.AllocatedPower)+" / "+Number(weapon.RequiredPower)+" · "+(weapon.Powered?"Powered":"Unpowered"));
                }
            }
            return rows.AsReadOnly();
        }
    }
}
