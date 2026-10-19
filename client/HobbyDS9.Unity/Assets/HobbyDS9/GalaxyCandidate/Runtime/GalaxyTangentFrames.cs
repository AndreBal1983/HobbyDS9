using System;
using UnityEngine;

namespace HobbyDS9.GalaxyCandidate
{
    public static class GalaxyTangentFrames
    {
        private static bool Finite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
        private static bool Finite(Vector3 v) => Finite(v.x) && Finite(v.y) && Finite(v.z);
        private static bool Valid(Vector3 normal, Vector4 tangent)
        {
            var t = new Vector3(tangent.x, tangent.y, tangent.z);
            return Finite(normal) && Finite(t) && Finite(tangent.w)
                && Mathf.Abs(normal.sqrMagnitude - 1) < .02f && Mathf.Abs(t.sqrMagnitude - 1) < .02f
                && Mathf.Abs(Vector3.Dot(normal, t)) < .02f && Mathf.Abs(Mathf.Abs(tangent.w) - 1) < .001f;
        }

        // Call on a cloned readable candidate mesh, NEVER on an imported/shared
        // source. Imported MikkTSpace is retained wherever its frame is valid.
        // First ascending-index valid incident UV triangle determines a fallback.
        public static int RepairAndValidate(Mesh mesh, int maximumRepairs = 16)
        {
            if (mesh == null) throw new ArgumentNullException(nameof(mesh));
            if (maximumRepairs < 0 || maximumRepairs > 16) throw new ArgumentOutOfRangeException(nameof(maximumRepairs));
            var p = mesh.vertices;
            var n = mesh.normals;
            var uv = mesh.uv;
            var t = mesh.tangents;
            var indices = mesh.triangles;
            if (p.Length == 0 || n.Length != p.Length || uv.Length != p.Length || t.Length != p.Length)
                throw new InvalidOperationException("Candidate position/normal/UV/tangent streams are required.");
            int repaired = 0;
            for (int vertex = 0; vertex < p.Length; ++vertex)
            {
                if (!Finite(p[vertex]) || !Finite(uv[vertex].x) || !Finite(uv[vertex].y)
                    || !Finite(n[vertex]) || Mathf.Abs(n[vertex].sqrMagnitude - 1) >= .02f)
                    throw new InvalidOperationException("Invalid candidate geometry stream at vertex " + vertex);
                if (Valid(n[vertex], t[vertex])) continue;
                if (++repaired > maximumRepairs) throw new InvalidOperationException("Candidate tangent damage exceeds bounded repair budget.");
                bool found = false;
                for (int i = 0; i < indices.Length; i += 3)
                {
                    int a = indices[i], b = indices[i + 1], c = indices[i + 2];
                    if (a != vertex && b != vertex && c != vertex) continue;
                    var e1 = p[b] - p[a]; var e2 = p[c] - p[a];
                    var d1 = uv[b] - uv[a]; var d2 = uv[c] - uv[a];
                    float determinant = d1.x * d2.y - d1.y * d2.x;
                    if (Mathf.Abs(determinant) < 1e-12f || Vector3.Cross(e1, e2).sqrMagnitude < 1e-16f) continue;
                    var tangent = (e1 * d2.y - e2 * d1.y) / determinant;
                    var bitangent = (e2 * d1.x - e1 * d2.x) / determinant;
                    tangent -= n[vertex] * Vector3.Dot(n[vertex], tangent);
                    if (!Finite(tangent) || !Finite(bitangent) || tangent.sqrMagnitude < 1e-12f) continue;
                    tangent.Normalize();
                    float hand = Vector3.Dot(Vector3.Cross(n[vertex], tangent), bitangent);
                    if (!Finite(hand) || Mathf.Abs(hand) < 1e-12f) continue;
                    t[vertex] = new Vector4(tangent.x, tangent.y, tangent.z, hand < 0 ? -1 : 1);
                    found = true;
                    break;
                }
                if (!found || !Valid(n[vertex], t[vertex])) throw new InvalidOperationException("No valid UV basis for candidate vertex " + vertex);
            }
            // Commit only after the complete mesh passes. Failed validation does
            // not leave a partially repaired mesh in the candidate asset.
            if (repaired > 0) mesh.tangents = t;
            return repaired;
        }
    }
}
