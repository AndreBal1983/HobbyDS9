using System;
using System.Collections.Generic;
using System.Linq;

namespace HobbyDS9.Persistence.Sqlite
{
    public sealed partial class SqliteCampaignRepository
    {
        // Stored metadata only. A coherent cursor is invalidated by any catalogue
        // mutation rather than silently mixing pages from different generations.
        public ReferencePage QueryReferences(ReferenceQuery query)
        {
            if (query == null || query.Limit < 1 || query.Limit > 500 || query.Kind.HasValue && !Enum.IsDefined(typeof(SaveReferenceKind), query.Kind.Value)) throw Error(RepositoryError.InvalidRequest);
            Text(query.NameContains, 120);
            if (query.CheckpointBranchId != null) Id(query.CheckpointBranchId);
            var key = Fingerprint("reference-query-v1", query.NameContains, query.Kind.HasValue ? N((int)query.Kind.Value) : null, query.CheckpointBranchId, N(query.Limit));
            if (query.Cursor != null)
            {
                Id(query.Cursor.Id); Text(query.Cursor.UpdatedUtc, 64);
                if (query.Cursor.QueryIdentity != key || query.Cursor.CatalogueSequence < 0) throw Error(RepositoryError.InvalidRequest);
            }
            lock (gate)
            {
                Alive();
                var sequence = Number("SELECT NextSequence-1 FROM RepositoryMeta WHERE Singleton=1");
                if (query.Cursor != null && query.Cursor.CatalogueSequence != sequence) throw Error(RepositoryError.Conflict);
                var rows = new List<SaveReference>();
                using var c = Command("SELECT " + ReferenceColumns + " FROM SaveReference r JOIN Checkpoint c ON c.Id=r.CheckpointId WHERE r.Deleted=0 AND instr(r.Name,$name)>0 AND ($kind IS NULL OR r.Kind=$kind) AND ($branch IS NULL OR c.BranchId=$branch) AND ($has=0 OR r.UpdatedUtc<$utc OR (r.UpdatedUtc=$utc AND r.Id>$id)) ORDER BY r.UpdatedUtc DESC,r.Id COLLATE BINARY LIMIT $limit", null,
                    ("$name", query.NameContains), ("$kind", query.Kind.HasValue ? (object)(int)query.Kind.Value : null), ("$branch", query.CheckpointBranchId),
                    ("$has", query.Cursor == null ? 0 : 1), ("$utc", query.Cursor?.UpdatedUtc ?? ""), ("$id", query.Cursor?.Id ?? ""), ("$limit", query.Limit + 1));
                using var reader = c.ExecuteReader();
                while (reader.Read()) rows.Add(ReadReference(reader));
                ReferenceCursor? next = null;
                if (rows.Count > query.Limit)
                {
                    rows.RemoveAt(rows.Count - 1); var last = rows.Last();
                    next = new ReferenceCursor(key, sequence, last.UpdatedUtc, last.Id);
                }
                return new ReferencePage(rows, next, sequence);
            }
        }
    }
}
