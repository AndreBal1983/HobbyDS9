using System;
using System.Collections.Generic;
using System.Linq;

namespace HobbyDS9.CampaignTacticalV2.Prototypes
{
    // Transaction proof with private owned encounters. No result-submission method.
    public sealed class PrototypeBatch
    {
        private readonly SortedDictionary<string, RoundAccessPrototype> encounters;
        private List<PrototypeShip> campaign;
        public bool Finalized { get; private set; }
        public PrototypeBatch(IEnumerable<PrototypeCheckpoint> fixtures)
        {
            encounters = new SortedDictionary<string, RoundAccessPrototype>(StringComparer.Ordinal);
            foreach (var input in fixtures)
            {
                if (encounters.ContainsKey(input.Encounter)) throw new ArgumentException("Duplicate encounter.");
                encounters.Add(input.Encounter, new RoundAccessPrototype(input));
            }
            if (encounters.Count == 0) throw new ArgumentException("Empty batch.");
            campaign = encounters.Values.SelectMany(e => e.Export().Ships).Select(RoundAccessPrototype.Copy).OrderBy(s => s.Id, StringComparer.Ordinal).ToList();
            if (campaign.Select(s => s.Id).Distinct(StringComparer.Ordinal).Count() != campaign.Count) throw new ArgumentException("Ship participates twice.");
        }
        public PrototypeCheckpoint ExportEncounter(string encounter) => encounters[encounter].Export();
        public IReadOnlyList<PrototypeShip> CampaignShips => campaign.Select(RoundAccessPrototype.Copy).ToList().AsReadOnly();
        public bool Commit(PrototypeRoundCommand command) => !Finalized && encounters.TryGetValue(command.Encounter, out var session) && session.Commit(command);
        public bool FinalizeBatch(string? injectFailureAtEncounter = null)
        {
            if (Finalized || encounters.Values.Any(e => e.Export().NextRound != 4)) return false;
            var staged = new List<PrototypeShip>();
            foreach (var pair in encounters)
            {
                if (pair.Key == injectFailureAtEncounter) return false; // review-only fault seam, never production authority.
                staged.AddRange(pair.Value.Export().Ships.Where(s => s.Hull > 0).Select(RoundAccessPrototype.Copy));
            }
            campaign = staged.OrderBy(s => s.Id, StringComparer.Ordinal).ToList(); Finalized = true; return true;
        }
    }
}
