using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    // Arithmetic for the explicit streamed policy. The caller owns source,
    // stock, phase and transaction validation; this helper never moves goods.
    internal static class StreamedMaterialWork
    {
        internal static long Cumulative(long required, long totalWork, long completedWork)
        {
            if (required < 0 || totalWork <= 0 || completedWork < 0 || completedWork > totalWork)
                throw new ArgumentException("Invalid streamed work domain.");
            return checked((long)(((BigInteger)required * completedWork + totalWork - 1) / totalWork));
        }

        internal static (long CompletedWork, List<ResourceAmountDto> ConsumedDelta) Plan(
            IReadOnlyList<ResourceAmountDto> required, IReadOnlyList<ResourceAmountDto> localStocks,
            long totalWork, long completedWork, long industry)
        {
            if (required == null || localStocks == null || totalWork <= 0 || completedWork < 0
                || completedWork >= totalWork || industry < 0)
                throw new ArgumentException("Invalid streamed work domain.");
            var needs = Canonical(required, true);
            var stocks = Canonical(localStocks, false).ToDictionary(r => r.ResourceDefinitionId, r => r.Amount, StringComparer.Ordinal);
            var previous = needs.Select(r => Cumulative(r.Amount, totalWork, completedWork)).ToArray();
            bool Affordable(long work)
            {
                for (int i = 0; i < needs.Count; i++)
                {
                    stocks.TryGetValue(needs[i].ResourceDefinitionId, out var available);
                    if (Cumulative(needs[i].Amount, totalWork, work) - previous[i] > available) return false;
                }
                return true;
            }
            long low = completedWork, high = checked(completedWork + Math.Min(industry, totalWork - completedWork));
            while (low < high)
            {
                long distance = high - low;
                long middle = low + distance / 2 + distance % 2;
                if (Affordable(middle)) low = middle;
                else high = middle - 1;
            }
            var delta = new List<ResourceAmountDto>(needs.Count);
            for (int i = 0; i < needs.Count; i++)
                delta.Add(new ResourceAmountDto { ResourceDefinitionId = needs[i].ResourceDefinitionId,
                    Amount = Cumulative(needs[i].Amount, totalWork, low) - previous[i] });
            return (low, delta);
        }

        private static List<ResourceAmountDto> Canonical(IReadOnlyList<ResourceAmountDto> rows, bool requirements)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var copy = new List<ResourceAmountDto>(rows.Count);
            foreach (var row in rows)
            {
                if (row == null || !DefinitionId.TryParse(row.ResourceDefinitionId, out _) || !ids.Add(row.ResourceDefinitionId)
                    || row.Amount < 0 || requirements && row.Amount == 0)
                    throw new ArgumentException("Invalid streamed resource vector.");
                copy.Add(new ResourceAmountDto { ResourceDefinitionId = row.ResourceDefinitionId, Amount = row.Amount });
            }
            copy.Sort((a, b) => StringComparer.Ordinal.Compare(a.ResourceDefinitionId, b.ResourceDefinitionId));
            return copy;
        }
    }
}
