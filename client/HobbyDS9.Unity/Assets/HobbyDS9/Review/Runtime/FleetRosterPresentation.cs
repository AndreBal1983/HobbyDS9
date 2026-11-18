#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using HobbyDS9.Contracts;
namespace HobbyDS9.UnityReview
{
    // Session-local view preferences; a replacement controller never inherits filters.
    public sealed class FleetRosterViewState
    {
        private static readonly ConditionalWeakTable<ReviewWorkspace,FleetRosterViewState> States=new ConditionalWeakTable<ReviewWorkspace,FleetRosterViewState>();
        private object? controller;
        public string Search {get;set;}="";
        public string Fleet {get;set;}="All fleets";
        public string Condition {get;set;}="All conditions";
        public static FleetRosterViewState For(ReviewWorkspace workspace)
        {
            var state=States.GetValue(workspace,_=>new FleetRosterViewState());
            if(!ReferenceEquals(state.controller,workspace.Model.Current)){state.controller=workspace.Model.Current;state.Search="";state.Fleet="All fleets";state.Condition="All conditions";}
            return state;
        }
    }
    public sealed class FleetShieldSummary
    {
        public bool Reported {get;}public decimal Current {get;}public decimal Maximum {get;}public string State {get;}
        public string Caption=>Reported?(Maximum==0?"No shields":Current.ToString("0",CultureInfo.InvariantCulture)+" / "+Maximum.ToString("0",CultureInfo.InvariantCulture)+" · "+State):"Not reported";
        internal FleetShieldSummary(bool reported,decimal current,decimal maximum,string state){Reported=reported;Current=current;Maximum=maximum;State=state;}
    }
    public static class FleetRosterPresentation
    {
        public static FleetShieldSummary Shields(IReadOnlyList<long>? current,IReadOnlyList<long>? maximum)
        {
            if(current==null||maximum==null||current.Count!=4||maximum.Count!=4||current.Any(n=>n<0)||maximum.Any(n=>n<0))return new FleetShieldSummary(false,0,0,"Not reported");
            var total=current.Sum(n=>(decimal)n);var limit=maximum.Sum(n=>(decimal)n);
            if(current.Where((n,i)=>n>maximum[i]).Any())return new FleetShieldSummary(false,0,0,"Not reported");
            return new FleetShieldSummary(true,total,limit,limit==0?"No shields":total==0?"Depleted":total==limit?"Full":"Reduced");
        }
        public static string Mission(CampaignTravelIntent? intent,int routeSteps,string? destination,bool activeStudy,bool inTransit)
        {
            if(activeStudy)return "Scientific study";
            if(inTransit||routeSteps>0)return "Travel"+(string.IsNullOrEmpty(destination)?"":" → "+destination);
            if(!intent.HasValue)return "Not reported";
            return intent==CampaignTravelIntent.StayEngage?"Defend":intent==CampaignTravelIntent.Follow?"Follow contact":intent==CampaignTravelIntent.Stay?"Remain":"Travel · no remaining route";
        }
        public static bool Matches(string query,string condition,string fleetFilter,string fleet,string name,string design,string location,string mission,bool? damaged,FleetShieldSummary shields)
        {
            if(fleetFilter!="All fleets"&&!string.Equals(fleetFilter,fleet,StringComparison.Ordinal))return false;
            if(condition=="Hull damage"&&damaged!=true)return false;
            if(condition=="Reduced shields"&&(!shields.Reported||shields.Maximum==0||shields.Current>=shields.Maximum))return false;
            if(condition=="Status not reported"&&damaged.HasValue&&shields.Reported)return false;
            return string.IsNullOrWhiteSpace(query)||string.Join(" ",new[]{fleet,name,design,location,mission}).IndexOf(query.Trim(),StringComparison.OrdinalIgnoreCase)>=0;
        }
    }
}
