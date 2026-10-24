#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;

namespace HobbyDS9.UnityReview
{
    // Presentation of an already actor-filtered observation, never a sensor query.
    public sealed class GeneratedObservationStatus
    {
        private readonly HashSet<string> known;
        private readonly HashSet<string> covered;
        private readonly IReadOnlyList<CampaignKnownContact> contacts;
        public GeneratedObservationStatus(CampaignGeneratedMapDto map,string actor,IEnumerable<CampaignKnownContact> contacts)
        {
            if(map.ActorId!=actor)throw new ArgumentException("Observation belongs to a different actor.",nameof(map));
            known=new HashSet<string>(map.Regions.Select(r=>r.NodeId),StringComparer.Ordinal);
            covered=new HashSet<string>(map.ObservedNodeIds.Where(known.Contains),StringComparer.Ordinal);
            this.contacts=contacts.Where(c=>known.Contains(c.LastSeenNodeId)).ToList().AsReadOnly();
        }
        public string? Describe(string node)
        {
            if(!known.Contains(node))return null;
            var here=contacts.Where(c=>c.LastSeenNodeId==node).ToList();
            if(covered.Contains(node))
            {
                var detected=here.Count(c=>c.Fresh);
                return detected==0?"Current sensors · no contacts detected":"Current sensors · "+detected+" hostile "+(detected==1?"contact":"contacts");
            }
            if(here.Count>0)return "Last seen turn "+here.Max(c=>c.LastSeenTurn)+" · current presence unknown";
            return "Charted · no current sensor coverage";
        }
        public const string CoverageHelp="Current coverage uses the campaign's provisional local observation policy. No detection does not rule out cloaked ships. Charting is remembered after coverage is lost.";
    }
}
