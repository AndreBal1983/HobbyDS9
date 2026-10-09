using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    public sealed class ValidationReport
    {
        public IReadOnlyList<ValidationIssue> Issues { get; }
        public bool IsValid => Issues.Count == 0;
        internal ValidationReport(IEnumerable<ValidationIssue> issues) { Issues = issues.ToList().AsReadOnly(); }

        public void ThrowIfInvalid(string parameterName)
        {
            if (!IsValid) throw new ArgumentException(Issues[0].Code + " at " + Issues[0].Path, parameterName);
        }
    }
}
