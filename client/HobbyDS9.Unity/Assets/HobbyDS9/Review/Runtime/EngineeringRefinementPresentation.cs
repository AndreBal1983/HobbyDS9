#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using HobbyDS9.Contracts;
namespace HobbyDS9.UnityReview
{
    // Display of already captured facts only. No candidate statistics or eligibility are generated here.
    public static class EngineeringRefinementPresentation
    {
        public static string Goal(EngineeringRefinementGoal goal)=>goal==EngineeringRefinementGoal.ReduceCapacity?"Reduce module hull space":goal==EngineeringRefinementGoal.IncreasePowerSupply?"Increase reactor output":goal==EngineeringRefinementGoal.ReduceIndustry?"Reduce Industry cost":"Unsupported target";
        public static string Maturity(int? value)=>value.HasValue?((decimal)value.Value/EngineeringRefinementIdentity.MaturityScale).ToString("0.###",CultureInfo.InvariantCulture):"Not tracked";
        public static string Work(long value)=>((decimal)value/ResearchUnits.Scale).ToString("0.###",CultureInfo.InvariantCulture);
        public static long Target(EngineeringRefinementVectorSnapshot vector,EngineeringRefinementGoal goal)=>goal==EngineeringRefinementGoal.ReduceCapacity?vector.Capacity:goal==EngineeringRefinementGoal.IncreasePowerSupply?vector.PowerSupply:vector.Industry;
        public static bool Improved(EngineeringRefinementVectorSnapshot before,EngineeringRefinementVectorSnapshot after,EngineeringRefinementGoal goal)=>goal==EngineeringRefinementGoal.IncreasePowerSupply?Target(after,goal)>Target(before,goal):Target(after,goal)<Target(before,goal);
        public static string Unit(EngineeringRefinementGoal goal)=>goal==EngineeringRefinementGoal.ReduceCapacity?"hull space":goal==EngineeringRefinementGoal.IncreasePowerSupply?"power supplied":"Industry";
        public static string Improvement(EngineeringRefinementProjectSnapshot project)=>project.Outcome==null?"Outcome not resolved":Target(project.Before,project.Goal)+" → "+Target(project.Outcome,project.Goal)+" "+Unit(project.Goal);
        public static string SideEffects(EngineeringRefinementProjectSnapshot project)
        {
            if(project.Outcome==null)return "Not resolved";var before=project.Before;var after=project.Outcome;var changes=new List<string>();
            if(project.Goal!=EngineeringRefinementGoal.ReduceCapacity&&before.Capacity!=after.Capacity)changes.Add("Hull space "+before.Capacity+" → "+after.Capacity);
            if(project.Goal!=EngineeringRefinementGoal.IncreasePowerSupply&&before.PowerSupply!=after.PowerSupply)changes.Add("Reactor output "+before.PowerSupply+" → "+after.PowerSupply);
            if(project.Goal!=EngineeringRefinementGoal.ReduceIndustry&&before.Industry!=after.Industry)changes.Add("Industry cost "+before.Industry+" → "+after.Industry);
            return changes.Count==0?"None":string.Join(" · ",changes);
        }
        public static bool HasDownside(EngineeringRefinementProjectSnapshot project)
        {
            var after=project.Outcome;if(after==null)return false;var before=project.Before;
            return project.Goal!=EngineeringRefinementGoal.ReduceCapacity&&after.Capacity>before.Capacity
                ||project.Goal!=EngineeringRefinementGoal.IncreasePowerSupply&&after.PowerSupply<before.PowerSupply
                ||project.Goal!=EngineeringRefinementGoal.ReduceIndustry&&after.Industry>before.Industry;
        }
        public static string Reason(EngineeringRefinementError error)
        {
            switch(error)
            {
                case EngineeringRefinementError.None:return "Available";
                case EngineeringRefinementError.ActiveProject:return "Another Engineering project is active.";
                case EngineeringRefinementError.PendingOutcome:return "Review the pending Engineering outcome first.";
                case EngineeringRefinementError.GenerationLocked:return "The required scientific generation is unavailable.";
                case EngineeringRefinementError.NoRepresentativeFit:return "A supported representative ship design is required.";
                case EngineeringRefinementError.Exhausted:return "This target has reached its supported limit.";
                case EngineeringRefinementError.StaleTurn:case EngineeringRefinementError.StaleRevision:return "The campaign changed. Reopen this review.";
                case EngineeringRefinementError.WrongPhase:return "Finish the current battle before changing Engineering.";
                case EngineeringRefinementError.ActorNotControlled:return "This empire is not under your control.";
                case EngineeringRefinementError.UnknownParent:return "This parent version is no longer available.";
                case EngineeringRefinementError.UnsupportedGoal:return "This target is unsupported for the selected module.";
                case EngineeringRefinementError.UnknownOutcome:case EngineeringRefinementError.InvalidDecision:return "This outcome is no longer available for that decision.";
                case EngineeringRefinementError.Capacity:return "The campaign's retained project limit has been reached.";
                case EngineeringRefinementError.Duplicate:return "This request was already handled. Refresh the outcome list.";
                default:return "Targeted refinement is unavailable at this boundary.";
            }
        }
    }
}
