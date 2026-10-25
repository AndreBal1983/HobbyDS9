using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using HobbyDS9.Contracts;
using static HobbyDS9.Simulation.CampaignTacticalReviewData;

namespace HobbyDS9.Simulation
{
    internal static partial class ColonisationData
    {
        internal static FrozenColonisationPlanetsDto Freeze(DefinitionCatalogDto definitions,GameStateDto game,CampaignColonisationStateDto inventory)
        {
            var result=new FrozenColonisationPlanetsDto{Definitions=definitions.Planets!.Select(PopulationData.Copy).ToList(),InitialPlanets=game.Planets!.Select(PopulationData.Copy).ToList(),
                Bodies=inventory.Bodies!.Select(Copy).ToList(),GenesisControls=inventory.PlanetControls!.Select(Copy).ToList()};
            result.ContentHash=InventoryHash(result);return result;
        }
        internal static string InventoryHash(FrozenColonisationPlanetsDto f)
        {
            Require(f!=null&&f.PolicyId==CampaignColonisationIdentity.InventoryPolicy&&f.Definitions!=null&&f.InitialPlanets!=null&&f.Bodies!=null&&f.GenesisControls!=null
                &&f.Definitions.Count>0&&f.Definitions.Count<=1040&&f.Definitions.Count==f.InitialPlanets.Count&&f.InitialPlanets.Count==f.Bodies.Count&&f.Bodies.Count==f.GenesisControls.Count
                &&f.Definitions.All(p=>p!=null&&p.Habitats!=null&&p.Habitats.Count==9&&p.Habitats.All(h=>h!=null)&&p.Deposits!=null&&p.Deposits.Count<=32&&p.Deposits.All(r=>r!=null))
                &&f.InitialPlanets.All(p=>p!=null&&p.RemainingDeposits!=null&&p.RemainingDeposits.Count<=32&&p.RemainingDeposits.All(r=>r!=null))
                &&f.Bodies.All(b=>b!=null)&&f.GenesisControls.All(c=>c!=null),"Incomplete frozen physical planet inventory.");
            Require(f!.Definitions!.Select(p=>p.DefinitionId).Distinct(StringComparer.Ordinal).Count()==f.Definitions.Count
                &&f.InitialPlanets!.Select(p=>p.PlanetId).Distinct(StringComparer.Ordinal).Count()==f.InitialPlanets.Count
                &&f.Bodies!.Select(b=>b.PlanetId).Distinct(StringComparer.Ordinal).Count()==f.Bodies.Count&&f.Bodies.Select(b=>b.BodyId).Distinct(StringComparer.Ordinal).Count()==f.Bodies.Count
                &&f.GenesisControls!.Select(c=>c.PlanetId).Distinct(StringComparer.Ordinal).Count()==f.GenesisControls.Count
                &&f.InitialPlanets.All(p=>EntityId.TryParse(p.PlanetId,out _)&&EntityId.TryParse(p.SystemId,out _)&&f.Definitions.Count(d=>d.DefinitionId==p.DefinitionId)==1
                    &&f.Bodies.Count(b=>b.PlanetId==p.PlanetId&&b.SystemId==p.SystemId)==1&&f.GenesisControls.Count(c=>c.PlanetId==p.PlanetId)==1)
                &&f.Definitions.All(p=>p.Habitats!.Select(h=>h.Kind).Distinct().Count()==9&&p.Habitats.All(h=>Enum.IsDefined(typeof(HabitatKind),h.Kind)&&h.ShareBps>=0&&h.NaturalCapacity>=0)
                    &&p.Habitats.Sum(h=>(long)h.ShareBps)==10000&&p.Deposits!.All(r=>r.Amount>=0)&&p.Deposits.Select(r=>r.ResourceDefinitionId).Distinct(StringComparer.Ordinal).Count()==p.Deposits!.Count)
                &&f.InitialPlanets.All(p=>p.RemainingDeposits!.All(r=>r.Amount>=0)&&p.RemainingDeposits.Select(r=>r.ResourceDefinitionId).Distinct(StringComparer.Ordinal).Count()==p.RemainingDeposits!.Count),"Duplicate or mismatched frozen physical planet facts.");
            using(var bytes=new MemoryStream())using(var writer=new BinaryWriter(bytes,new UTF8Encoding(false,true),true))
            {
                void S(string? value){Require(value!=null&&value.Length<=512,"Frozen inventory string bound.");writer.Write(value!);}
                void Resources(System.Collections.Generic.List<ResourceAmountDto> rows)
                {writer.Write(rows.Count);foreach(var r in rows.OrderBy(r=>r.ResourceDefinitionId,StringComparer.Ordinal)){S(r.ResourceDefinitionId);writer.Write(r.Amount);}}
                S(f!.PolicyId);S(CampaignColonisationIdentity.InventoryAuthoring);writer.Write(f.Definitions!.Count);
                foreach(var p in f.Definitions.OrderBy(p=>p.DefinitionId,StringComparer.Ordinal))
                {
                    S(p.DefinitionId);S(p.DisplayName);S(p.SystemDefinitionId);S(p.PlanetClass);writer.Write(p.Gravity);writer.Write(p.Pressure);writer.Write(p.Radiation);S(p.Atmosphere);
                    foreach(var h in p.Habitats!.OrderBy(h=>h.Kind)){writer.Write((int)h.Kind);writer.Write(h.ShareBps);writer.Write(h.NaturalCapacity);}Resources(p.Deposits!);
                }
                foreach(var p in f.InitialPlanets!.OrderBy(p=>p.PlanetId,StringComparer.Ordinal)){S(p.PlanetId);S(p.DefinitionId);S(p.SystemId);Resources(p.RemainingDeposits!);}
                foreach(var b in f.Bodies!.OrderBy(b=>b.PlanetId,StringComparer.Ordinal)){S(b.PlanetId);S(b.BodyId);S(b.SystemId);writer.Write(b.MinorActorId!=null);if(b.MinorActorId!=null)S(b.MinorActorId);}
                foreach(var c in f.GenesisControls!.OrderBy(c=>c.PlanetId,StringComparer.Ordinal)){S(c.PlanetId);writer.Write((int)c.Kind);writer.Write(c.ActorId!=null);if(c.ActorId!=null)S(c.ActorId);}
                writer.Flush();Require(bytes.Length<=4*1024*1024,"Frozen inventory encoding bound.");
                using(var sha=SHA256.Create())return string.Concat(sha.ComputeHash(bytes.ToArray()).Select(b=>b.ToString("x2",CultureInfo.InvariantCulture)));
            }
        }
    }
}
