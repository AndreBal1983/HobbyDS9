#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using HobbyDS9.Contracts;
using Newtonsoft.Json.Linq;

namespace HobbyDS9.UnityReview
{
    // Presentation radii only. The catalogue cannot supply displayed body instances.
    public sealed class SolMeanOrbitData
    {
        private readonly IReadOnlyDictionary<string,long> distances;
        private SolMeanOrbitData(Dictionary<string,long> values){distances=new ReadOnlyDictionary<string,long>(values);}
        public static SolMeanOrbitData Read(string json)
        {
            var root=JObject.Parse(json);
            if((string?)root["Version"]!="sol-mean-orbits-v1"||(string?)root["DistanceUnit"]!="km"||root["Planets"] is not JArray rows||rows.Count!=8)
                throw new ArgumentException("Unsupported Sol distance resource.");
            var values=new Dictionary<string,long>(StringComparer.Ordinal);
            foreach(var row in rows)
            {
                var id=(string?)row["PlanetId"];var km=(long?)row["MeanSunDistanceKm"];
                if(string.IsNullOrWhiteSpace(id)||!km.HasValue||km<=0||values.ContainsKey(id!))throw new ArgumentException("Invalid Sol distance row.");
                values.Add(id!,km.Value);
            }
            if(!values.ContainsKey("planet.sol"))throw new ArgumentException("Missing Earth reference.");
            return new SolMeanOrbitData(values);
        }
        public IReadOnlyDictionary<string,long>? JoinDisclosed(IReadOnlyList<ColonisationPlanetSnapshot> bodies,IEnumerable<ColonisationPlanetSnapshot> observations)
        {
            var earth=observations.FirstOrDefault(p=>p.PlanetId=="planet.sol");
            if(earth==null||bodies.Count==0||bodies.Any(p=>p.SystemId!=earth.SystemId||!distances.ContainsKey(p.PlanetId)))return null;
            return new ReadOnlyDictionary<string,long>(bodies.ToDictionary(p=>p.PlanetId,p=>distances[p.PlanetId],StringComparer.Ordinal));
        }
    }
}
