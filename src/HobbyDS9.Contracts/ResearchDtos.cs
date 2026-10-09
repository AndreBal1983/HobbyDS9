using System.Collections.Generic;

namespace HobbyDS9.Contracts
{
    public sealed class ResearchNodeDefinitionDto
    {
        public string DefinitionId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public long ResearchCost { get; set; }
        public List<string>? PrerequisiteDefinitionIds { get; set; }
    }
}
