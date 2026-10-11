#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HobbyDS9.Contracts;

namespace HobbyDS9.UnityClient
{
    // Local editing state only. No spending, eligibility, normalization of commands or RNG.
    // Changing one share transfers units evenly from/to the other five in enum order.
    public sealed class ScienceAllocationDraft
    {
        private readonly int[] weights;
        private readonly int[] original;
        public bool Changed => !weights.SequenceEqual(original);
        public ScienceAllocationDraft(IReadOnlyList<ScienceAllocationSnapshot> allocations)
        {
            if (allocations == null || allocations.Count != 6 || allocations.Any(a => a == null || (int)a.Discipline < 1 || (int)a.Discipline > 6 || a.Weight < 0 || a.Weight > ResearchUnits.Scale)
                || allocations.Select(a => a.Discipline).Distinct().Count() != 6 || allocations.Sum(a => a.Weight) != ResearchUnits.Scale)
                throw new ArgumentException("Expected six valid Science shares.", nameof(allocations));
            weights = allocations.OrderBy(a => a.Discipline).Select(a => a.Weight).ToArray();
            original = (int[])weights.Clone();
        }
        public int Weight(ScienceDiscipline discipline) => weights[Index(discipline)];
        public decimal Percent(ScienceDiscipline discipline) => 100m * Weight(discipline) / ResearchUnits.Scale;
        public void SetWeight(ScienceDiscipline discipline, int weight)
        {
            int index = Index(discipline);
            if (weight < 0 || weight > ResearchUnits.Scale) throw new ArgumentOutOfRangeException(nameof(weight));
            int delta = weight - weights[index];
            while (delta != 0)
                for (int i = 0; i < weights.Length && delta != 0; i++)
                {
                    if (i == index) continue;
                    if (delta > 0 && weights[i] > 0) { weights[i]--; weights[index]++; delta--; }
                    else if (delta < 0 && weights[i] < ResearchUnits.Scale) { weights[i]++; weights[index]--; delta++; }
                }
        }
        public void Equalize() { for (int i = 0; i < weights.Length; i++) weights[i] = ResearchUnits.Scale / weights.Length; }
        public IReadOnlyList<ScienceAllocationDto> Export() => weights.Select((weight, i) => new ScienceAllocationDto { Discipline = (ScienceDiscipline)(i + 1), Weight = weight }).ToList().AsReadOnly();
        public static string DisplayPercent(decimal percent) => percent == 100m / 6 ? "16⅔%" : percent.ToString("0.##", CultureInfo.InvariantCulture) + "%";
        public static string Conceptual(long internalUnits) => ((decimal)internalUnits / ResearchUnits.Scale).ToString("0.###", CultureInfo.InvariantCulture);
        private static int Index(ScienceDiscipline discipline)
        { int index = (int)discipline - 1; if (index < 0 || index >= 6) throw new ArgumentOutOfRangeException(nameof(discipline)); return index; }
    }
}
