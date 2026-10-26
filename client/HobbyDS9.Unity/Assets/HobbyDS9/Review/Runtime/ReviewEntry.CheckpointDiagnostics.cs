#nullable enable
using System;
using System.Globalization;
using HobbyDS9.TacticalReviewClient;
namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewEntry
    {
        private static readonly bool checkpointDiagnosticsEnabled=CheckpointDiagnosticsEnabled();
        private static bool CheckpointDiagnosticsEnabled()
        {
            try {foreach(var arg in Environment.GetCommandLineArgs())if(arg=="--hobbyds9-checkpoint-diagnostics")return true;}
            catch(Exception) { }
            return false;
        }
        private static ReviewLoadDiagnostic? NewCheckpointDiagnostic()
            =>checkpointDiagnosticsEnabled?ReviewLoadDiagnostic.Create(row=>UnityEngine.Debug.Log(
                "HOBBYDS9_CHECKPOINT phase="+row.Phase+" event="+row.Event+" failure="+row.Failure+
                " error="+row.Error+" ticks="+row.ElapsedTicks.ToString(CultureInfo.InvariantCulture)+
                " frequency="+System.Diagnostics.Stopwatch.Frequency.ToString(CultureInfo.InvariantCulture))):null;
    }
}
