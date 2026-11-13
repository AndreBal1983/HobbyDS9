#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HobbyDS9.Contracts;

namespace HobbyDS9.UnityReview
{
    // Presentation identity only. No acknowledgment is written into campaign state/saves.
    public readonly struct ReviewTurnBoundary
    {
        public string Actor {get;} public long Generation {get;} public int Turn {get;}
        public long Revision {get;} public SessionPhase Phase {get;}
        public ReviewTurnBoundary(string actor,long generation,int turn,long revision,SessionPhase phase)
        {Actor=actor;Generation=generation;Turn=turn;Revision=revision;Phase=phase;}
    }
    public sealed class ReviewCompletionNotice
    {
        public string Identity {get;} public string Title {get;} public string Detail {get;}
        public string? ResearchTab {get;} public string? ColonyId {get;}
        public ReviewCompletionNotice(string identity,string title,string detail,string? researchTab=null,string? colonyId=null)
        {Identity=identity;Title=title;Detail=detail;ResearchTab=researchTab;ColonyId=colonyId;}
    }
    public sealed class ReviewEngineeringNoticeDefinition
    {
        public string Name {get;} public string Effect {get;}
        public ReviewEngineeringNoticeDefinition(string name,string effect){Name=name;Effect=effect;}
    }
    public static class ReviewCompletionFacts
    {
        private static string Number(long value)=>value.ToString(CultureInfo.InvariantCulture);
        private static int Order(string category)=>category=="science-breakthrough"?0:category=="science-level"?1:2;
        public static IReadOnlyList<ReviewCompletionNotice> Research(IEnumerable<ResearchReceiptSnapshot> receipts,
            string actor,int resolvedTurn,IReadOnlyDictionary<string,ReviewEngineeringNoticeDefinition> definitions,bool scoutExcluded)
        {
            var rows=new List<ReviewCompletionNotice>();
            foreach(var receipt in receipts.Where(r=>r.CivilizationId.Value==actor&&
                (r.Category=="science-breakthrough"||r.Category=="science-level"||r.Category=="engineering-mature"))
                .OrderBy(r=>Order(r.Category)).ThenBy(r=>r.Discipline).ThenBy(r=>r.IntervalStartLevel)
                .ThenBy(r=>r.BreakthroughOrdinal).ThenBy(r=>r.ApplicationDefinitionId?.Value,StringComparer.Ordinal).ThenBy(r=>r.Amount))
            {
                var application=receipt.ApplicationDefinitionId?.Value??"";
                var key="research/"+resolvedTurn+"/"+receipt.Category+"/"+receipt.Discipline+"/"+receipt.IntervalStartLevel+"/"+receipt.BreakthroughOrdinal+"/"+application+"/"+Number(receipt.Amount);
                if(receipt.Category=="engineering-mature")
                {
                    definitions.TryGetValue(application,out var definition);
                    var name=definition?.Name??"Engineering project";
                    var effect=scoutExcluded&&application=="engineering.advanced-starship-engineering"
                        ?"Advanced Scout is not offered in this campaign's construction catalogue. This completion does not create a buildable Scout."
                        :definition?.Effect??"Engineering completion recorded. Result details are unavailable in this catalogue.";
                    rows.Add(new ReviewCompletionNotice(key,name+" completed",effect+
                        "\n\nFitting and construction remain subject to the current catalogue and compatibility. Existing ships and published revisions are unchanged.","engineering"));
                }
                else if(receipt.Discipline.HasValue)
                {
                    var name=receipt.Discipline.Value.ToString();
                    var detail=receipt.Category=="science-level"
                        ?name+" reached level "+Number(receipt.Amount)+"."
                        :name+" · level "+receipt.IntervalStartLevel+" → "+(receipt.IntervalStartLevel+1)+" · discovery "+receipt.BreakthroughOrdinal+" reached.";
                    rows.Add(new ReviewCompletionNotice(key,receipt.Category=="science-level"?name+" level completed":name+" discovery completed",
                        detail+"\n\nEngineering eligibility follows its named Science and project prerequisites.","science"));
                }
            }
            return rows.GroupBy(r=>r.Identity,StringComparer.Ordinal).Select(g=>g.First()).ToList().AsReadOnly();
        }
        // Existing lawful structure receipts are usable now. Ship/kit terminal history
        // requires the separately requested actor-facing projection; never infer from deltas.
        public static IReadOnlyList<ReviewCompletionNotice> Structures(IEnumerable<ActivityStructureReceiptDto> receipts,string actor,int resolvedTurn,
            IReadOnlyDictionary<string,string> names,IReadOnlyDictionary<string,string> colonies)
            =>receipts.Where(r=>r.OwnerId==actor&&r.CompletedTurn==resolvedTurn)
                .OrderBy(r=>r.ColonyId,StringComparer.Ordinal).ThenBy(r=>r.QueueItemId,StringComparer.Ordinal)
                .GroupBy(r=>r.QueueItemId,StringComparer.Ordinal).Select(g=>g.First()).Select(r=>
                {
                    var name=names.TryGetValue(r.DefinitionId,out var label)?label:"Planetary building";
                    var place=colonies.TryGetValue(r.ColonyId,out var world)?world:"Recorded construction site";
                    return new ReviewCompletionNotice("production/"+resolvedTurn+"/"+r.QueueItemId,name+" completed",
                        place+"\n\nThe completed structure is recorded. Its outputs follow the next eligible Economy cycle and current conditions.",colonyId:r.ColonyId);
                }).ToList().AsReadOnly();
    }
    public sealed class ReviewTurnCompletionQueue
    {
        private object? owner;private string actor="";private long generation;private int baseline,planningTurn,index;
        private IReadOnlyList<ReviewCompletionNotice> items=Array.Empty<ReviewCompletionNotice>();
        public int Count=>items.Count;public int Position=>index+1;
        public void Synchronize(object current,ReviewTurnBoundary frame)
        {
            if(ReferenceEquals(owner,current)&&actor==frame.Actor&&generation==frame.Generation)return;
            owner=current;actor=frame.Actor;generation=frame.Generation;baseline=frame.Turn;planningTurn=frame.Turn;
            index=0;items=Array.Empty<ReviewCompletionNotice>();
        }
        public bool Capture(object current,ReviewTurnBoundary before,ReviewTurnBoundary after,IEnumerable<ReviewCompletionNotice> notices)
        {
            if(!ReferenceEquals(owner,current)||actor!=before.Actor||generation!=before.Generation
                ||before.Actor!=after.Actor||before.Generation!=after.Generation)
                return false;
            if(before.Turn==int.MaxValue||after.Turn!=before.Turn+1||after.Revision<=before.Revision
                ||after.Phase!=SessionPhase.Planning||after.Turn<=baseline)return false;
            baseline=after.Turn;planningTurn=after.Turn;index=0;
            items=notices.GroupBy(n=>n.Identity,StringComparer.Ordinal).Select(g=>g.First()).ToList().AsReadOnly();return true;
        }
        public ReviewCompletionNotice? Peek(object current,ReviewTurnBoundary frame)
        {
            return ReferenceEquals(owner,current)&&actor==frame.Actor&&generation==frame.Generation
                &&frame.Phase==SessionPhase.Planning&&frame.Turn==planningTurn&&index<items.Count?items[index]:null;
        }
        public bool Acknowledge(object current,ReviewTurnBoundary frame,ReviewCompletionNotice notice)
        {if(!ReferenceEquals(Peek(current,frame),notice))return false;index++;return true;}
    }
}
