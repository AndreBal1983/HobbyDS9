#nullable enable
using System.Globalization;
using HobbyDS9.Contracts;
namespace HobbyDS9.UnityReview
{
    public static class BeltBodyPresentation
    {
        public static string Habitability(BeltBodySnapshot body)=>body.HabitabilityBps.HasValue
            ?((decimal)body.HabitabilityBps.Value/100).ToString("0.##",CultureInfo.InvariantCulture)+"%":"Not surveyed";
        public static string Ore(BeltBodySnapshot body)=>body.ObservedRemaining.HasValue
            ?body.ObservedRemaining.Value.ToString(CultureInfo.InvariantCulture):"Unknown";
        public static string Resource(BeltBodySnapshot body)=>body.ResourceDefinitionId=="resource.dilithium"?"Dilithium"
            :body.ResourceDefinitionId==null?"Resource unknown":"Surveyed resource";
        public static string ResourceStamp(BeltBodySnapshot body)=>!body.Surveyed?"Survey required"
            :(body.ResourceIsCurrent?"Current report":"Last reported")+" · "+(body.ResourceObservedTurn.HasValue?"week "+body.ResourceObservedTurn.Value:"date not supplied");
        public static string SurveyStamp(BeltBodySnapshot body)=>body.Surveyed
            ?"Surveyed · "+(body.SurveyedTurn.HasValue?"week "+body.SurveyedTurn.Value:"date not supplied")
            :body.ActiveSurveyId!=null?"Survey active · "+body.CompletedSurveyWeeks+" weeks completed":"Not surveyed";
        public static string Reason(BeltBodyError error)
        {
            switch(error)
            {
                case BeltBodyError.None:return "Available";
                case BeltBodyError.StaleTurn:case BeltBodyError.StaleRevision:return "The campaign changed. Review this action again.";
                case BeltBodyError.WrongPhase:return "Finish the current battle before changing belt operations.";
                case BeltBodyError.BodyNotObserved:return "Current observation of this belt is required.";
                case BeltBodyError.SurveyRequired:return "Complete a survey of this belt first.";
                case BeltBodyError.AlreadySurveyed:return "This belt has already been surveyed.";
                case BeltBodyError.ShipUnavailable:return "Choose an eligible stationary ship in this system.";
                case BeltBodyError.ShipBusy:return "The selected ship already has a survey or study assignment.";
                case BeltBodyError.StudyUnavailable:return "The selected ship lacks the required powered study capability.";
                case BeltBodyError.SurveyUnavailable:return "This survey is no longer available to cancel.";
                case BeltBodyError.ColonyUnavailable:case BeltBodyError.WrongSystem:return "Choose an eligible established sponsor in this system.";
                case BeltBodyError.MineExists:return "A mining installation is already queued or recorded here.";
                case BeltBodyError.MineUnavailable:return "This installation is no longer available for that action.";
                case BeltBodyError.DepositExhausted:return "The surveyed deposit is exhausted.";
                case BeltBodyError.UnsupportedHabitat:return "This belt has no supported settlement habitat.";
                case BeltBodyError.Capacity:return "The campaign's operation limit has been reached.";
                case BeltBodyError.InsufficientCredits:return "The required credits are unavailable.";
                case BeltBodyError.Duplicate:return "This request was already handled. Refresh the view.";
                case BeltBodyError.ActorNotControlled:return "This empire is not under your control.";
                default:return "Belt operations are unavailable at this boundary.";
            }
        }
    }
}
