using System;
using System.Diagnostics;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    // Only an explicit measured load owns this object. Never retained by a session.
    internal enum ColonisationReplayPhase { ExpansionPreflight, CorePreflight, FrozenMap, InitialState, Journal, CoreEquality, ExpansionEquality }
    internal enum ColonisationReplayKind { Unclassified, Colonisation, Activity, Organization, Travel, Strategic, Cloak, Advance, Round, Finalize }
    internal sealed class ColonisationReplayTiming
    {
        internal readonly long[] Ticks = new long[7];
        internal readonly byte[] State = new byte[7]; // 0 absent, 1 active, 2 returned, 3 failed
        internal readonly int[,] Counts = new int[10,4]; // attempted, accepted, refused, equality verified
        private readonly long[] started = new long[7];
        internal void Begin(ColonisationReplayPhase phase)
        { try { var i=(int)phase;started[i]=Stopwatch.GetTimestamp();State[i]=1; } catch(Exception) { } }
        internal void End(ColonisationReplayPhase phase)
        { try { var i=(int)phase;Ticks[i]=Math.Max(0,Stopwatch.GetTimestamp()-started[i]);State[i]=2; } catch(Exception) { } }
        internal void CloseFailed()
        { try { for(var i=0;i<State.Length;i++)if(State[i]==1){Ticks[i]=Math.Max(0,Stopwatch.GetTimestamp()-started[i]);State[i]=3;} } catch(Exception) { } }
        internal void Count(ColonisationReplayKind kind,int column)
        { try { Counts[(int)kind,column]++; } catch(Exception) { } }
        internal static ColonisationReplayKind Kind(ColonisationOperationDto? op)
        {
            if(op==null||(op.Colonisation==null)==(op.Existing==null))return ColonisationReplayKind.Unclassified;
            if(op.Colonisation!=null)return ColonisationReplayKind.Colonisation;
            var existing=op.Existing!;
            if((existing.Activity==null)==(existing.Common==null))return ColonisationReplayKind.Unclassified;
            if(existing.Activity!=null)return ColonisationReplayKind.Activity;
            var common=existing.Common!;
            if((common.Common==null)==(common.Organization==null))return ColonisationReplayKind.Unclassified;
            if(common.Organization!=null)return ColonisationReplayKind.Organization;
            switch(common.Common!.Kind)
            {
                case FrontierOperationKind.Travel:return ColonisationReplayKind.Travel;
                case FrontierOperationKind.Strategic:return ColonisationReplayKind.Strategic;
                case FrontierOperationKind.Cloak:return ColonisationReplayKind.Cloak;
                case FrontierOperationKind.Advance:return ColonisationReplayKind.Advance;
                case FrontierOperationKind.Round:return ColonisationReplayKind.Round;
                case FrontierOperationKind.Finalize:return ColonisationReplayKind.Finalize;
                default:return ColonisationReplayKind.Unclassified;
            }
        }
    }
}
