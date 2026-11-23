using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    internal static class GeneratedGalaxyGeometry
    {
        // Homogeneous exact coordinates. Normalize at every intersection to bound arithmetic.
        private readonly struct Vertex
        {
            internal readonly BigInteger X, Y, D;
            internal Vertex(BigInteger x, BigInteger y, BigInteger d)
            {
                if (d.Sign < 0) { x = -x; y = -y; d = -d; }
                if (d.IsZero) throw new InvalidOperationException("Parallel clipping intersection.");
                var gcd = BigInteger.GreatestCommonDivisor(BigInteger.GreatestCommonDivisor(x, y), d);
                X = x / gcd; Y = y / gcd; D = d / gcd;
            }
            internal GeneratedGalaxyPointDto Round() => new GeneratedGalaxyPointDto {
                X = (int)((2 * X + D) / (2 * D)), Y = (int)((2 * Y + D) / (2 * D)) };
        }

        internal static List<GeneratedGalaxyPointDto> Cell(IReadOnlyList<GeneratedGalaxyPointDto> sites, int index, int width, int height)
        {
            var polygon = new List<Vertex> { new Vertex(0, 0, 1), new Vertex(width, 0, 1),
                new Vertex(width, height, 1), new Vertex(0, height, 1) };
            var own = sites[index];
            for (int j = 0; j < sites.Count; j++)
            {
                if (j == index) continue;
                var other = sites[j];
                long a = 2L * (other.X - own.X), b = 2L * (other.Y - own.Y);
                long c = (long)other.X * other.X + (long)other.Y * other.Y - (long)own.X * own.X - (long)own.Y * own.Y;
                BigInteger Side(Vertex v) => a * v.X + b * v.Y - c * v.D;
                var clipped = new List<Vertex>();
                for (int k = 0; k < polygon.Count; k++)
                {
                    var first = polygon[k]; var next = polygon[(k + 1) % polygon.Count];
                    var s1 = Side(first); var s2 = Side(next);
                    if (s1.Sign <= 0) clipped.Add(first);
                    if (s1.Sign < 0 && s2.Sign > 0 || s1.Sign > 0 && s2.Sign < 0)
                        clipped.Add(new Vertex(next.X * s1 - first.X * s2, next.Y * s1 - first.Y * s2, next.D * s1 - first.D * s2));
                }
                polygon = clipped;
            }
            var points = new List<GeneratedGalaxyPointDto>();
            foreach (var p in polygon.Select(v => v.Round()))
                if (points.Count == 0 || !Same(points[points.Count - 1], p)) points.Add(p);
            if (points.Count > 1 && Same(points[0], points[points.Count - 1])) points.RemoveAt(points.Count - 1);
            // Shared canonical rotation, never angle/trigonometric sorting.
            int start = Enumerable.Range(0, points.Count).OrderBy(i => points[i].X).ThenBy(i => points[i].Y).First();
            return points.Skip(start).Concat(points.Take(start)).ToList();
        }

        internal static bool Same(GeneratedGalaxyPointDto a, GeneratedGalaxyPointDto b) => a.X == b.X && a.Y == b.Y;
        internal static long Cross(GeneratedGalaxyPointDto a, GeneratedGalaxyPointDto b, GeneratedGalaxyPointDto c)
            => (long)(b.X - a.X) * (c.Y - a.Y) - (long)(b.Y - a.Y) * (c.X - a.X);
        internal static (int, int, int, int) Edge(GeneratedGalaxyPointDto a, GeneratedGalaxyPointDto b)
            => a.X < b.X || a.X == b.X && a.Y < b.Y ? (a.X, a.Y, b.X, b.Y) : (b.X, b.Y, a.X, a.Y);

        internal static List<NavigationConnectionDto> Connect(List<GeneratedGalaxyRegionDto> regions, int width, int height)
        {
            var edges = new Dictionary<(int, int, int, int), List<string>>();
            long area = 0;
            foreach (var r in regions)
            {
                if (r.Boundary.Count < 3 || r.Boundary.Count > regions.Count + 4) throw new InvalidOperationException("Invalid cell vertex count.");
                for (int i = 0; i < r.Boundary.Count; i++)
                {
                    var a = r.Boundary[i]; var b = r.Boundary[(i + 1) % r.Boundary.Count];
                    var c = r.Boundary[(i + 2) % r.Boundary.Count];
                    if (a.X < 0 || a.X > width || a.Y < 0 || a.Y > height || Same(a, b)
                        || Cross(a, b, c) < 0 || Cross(a, b, r.Center) <= 0)
                        throw new InvalidOperationException("Rounded cells must remain convex and contain their sites.");
                    area = checked(area + (long)a.X * b.Y - (long)a.Y * b.X);
                    var key = Edge(a, b);
                    if (!edges.TryGetValue(key, out var owners)) edges.Add(key, owners = new List<string>());
                    owners.Add(r.NodeId);
                }
            }
            if (area != 2L * width * height) throw new InvalidOperationException("Cells do not tile the map.");
            var links = new List<(string A, string B)>();
            foreach (var edge in edges)
            {
                var (x1, y1, x2, y2) = edge.Key;
                bool border = x1 == x2 && (x1 == 0 || x1 == width) || y1 == y2 && (y1 == 0 || y1 == height);
                if (edge.Value.Count == 1 && border) continue;
                if (edge.Value.Count != 2 || border) throw new InvalidOperationException("Rounded cells must share complete boundaries.");
                var ids = edge.Value.OrderBy(id => id, StringComparer.Ordinal).ToArray();
                links.Add((ids[0], ids[1]));
            }
            if (links.Distinct().Count() != links.Count) throw new InvalidOperationException("Duplicate adjacency.");
            return links.OrderBy(l => l.A, StringComparer.Ordinal).ThenBy(l => l.B, StringComparer.Ordinal)
                .Select((l, i) => new NavigationConnectionDto { ConnectionId = GeneratedGalaxyData.Id("connection", i),
                    EndpointA = l.A, EndpointB = l.B, Kind = GalaxyEdgeKind.Normal, BaseTraversalCost = 1 }).ToList();
        }
    }
}
