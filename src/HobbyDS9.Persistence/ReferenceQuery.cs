using System.Collections.Generic;
using System.Linq;

namespace HobbyDS9.Persistence
{
    public sealed class ReferenceCursor
    {
        public string QueryIdentity { get; }
        public long CatalogueSequence { get; }
        public string UpdatedUtc { get; }
        public string Id { get; }
        public ReferenceCursor(string queryIdentity, long catalogueSequence, string updatedUtc, string id)
        { QueryIdentity = queryIdentity; CatalogueSequence = catalogueSequence; UpdatedUtc = updatedUtc; Id = id; }
    }
    public sealed class ReferenceQuery
    {
        public string NameContains { get; }
        public SaveReferenceKind? Kind { get; }
        public string? CheckpointBranchId { get; }
        public int Limit { get; }
        public ReferenceCursor? Cursor { get; }
        public ReferenceQuery(string nameContains = "", SaveReferenceKind? kind = null, string? checkpointBranchId = null, int limit = 50, ReferenceCursor? cursor = null)
        { NameContains = nameContains; Kind = kind; CheckpointBranchId = checkpointBranchId; Limit = limit; Cursor = cursor; }
    }
    public sealed class ReferencePage
    {
        public IReadOnlyList<SaveReference> Rows { get; }
        public ReferenceCursor? Next { get; }
        public long CatalogueSequence { get; }
        public bool HasMore => Next != null;
        public ReferencePage(IEnumerable<SaveReference> rows, ReferenceCursor? next, long catalogueSequence)
        { Rows = rows.ToList().AsReadOnly(); Next = next; CatalogueSequence = catalogueSequence; }
    }
}
