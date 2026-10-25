using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    // Private-to-Simulation era binding. Not a caller-supplied validation policy or save authority.
    internal sealed class PlanetaryOwnershipContext
    {
        private readonly DefinitionCatalogDto definitions;
        private readonly GameStateDto state;
        private readonly Dictionary<string,PlanetControlDto> controls;
        private readonly HashSet<string> independentActors;
        private PlanetaryOwnershipContext(DefinitionCatalogDto ownedDefinitions,GameStateDto ownedState,IEnumerable<PlanetControlDto> ownedControls,IEnumerable<string> minors)
        {
            definitions=ownedDefinitions;state=ownedState;independentActors=new HashSet<string>(minors,StringComparer.Ordinal);
            controls=new Dictionary<string,PlanetControlDto>(StringComparer.Ordinal);
            foreach(var row in ownedControls)
            {
                if(row==null||!EntityId.TryParse(row.PlanetId,out _)||controls.ContainsKey(row.PlanetId))throw new ArgumentException("Invalid planetary control row.");
                controls.Add(row.PlanetId,Copy(row));
            }
            ValidateOwnership();
        }
        // The future typed factory calls this ONLY after package/initial-state validation and private retention.
        // No public old factory, hash, saved marker or advisory DTO invokes this constructor path.
        internal static PlanetaryOwnershipContext CreateForColonisation(DefinitionCatalogDto ownedDefinitions,GameStateDto ownedState,
            IReadOnlyList<PlanetControlDto> controls,IReadOnlyList<ActivityMinorStateDto> minors)
        {
            if(ownedDefinitions==null||ownedState==null||controls==null||minors==null||minors.Any(m=>m==null||!EntityId.TryParse(m.ActorId,out _))
                ||minors.Select(m=>m.ActorId).Distinct(StringComparer.Ordinal).Count()!=minors.Count)throw new ArgumentException("Complete owned planetary context required.");
            return new PlanetaryOwnershipContext(ownedDefinitions,ownedState,controls,minors.Select(m=>m.ActorId));
        }
        internal void RequireOwner(DefinitionCatalogDto candidateDefinitions,GameStateDto candidateState)
        {
            if(!ReferenceEquals(definitions,candidateDefinitions)||!ReferenceEquals(state,candidateState))throw new ArgumentException("Different planetary context era or definition owner.");
        }
        internal PlanetaryOwnershipContext CopyForState(GameStateDto detachedState)
        {
            if(detachedState==null)throw new ArgumentNullException(nameof(detachedState));
            // Definition ownership is fixed; never accept a replacement caller definition catalogue.
            return new PlanetaryOwnershipContext(definitions,detachedState,controls.Values,independentActors);
        }
        internal IReadOnlyList<PlanetControlDto> CopyControls()=>controls.Values.OrderBy(c=>c.PlanetId,StringComparer.Ordinal).Select(Copy).ToList().AsReadOnly();
        internal bool EmpireControls(string planetId,string actor)=>controls.TryGetValue(planetId,out var c)&&c.Kind==PlanetControllerKind.Empire&&c.ActorId==actor;
        internal void ValidateOwnership()
        {
            if(state.Planets==null||state.Planets.Count>1040||state.Planets.Any(p=>p==null)||state.StarSystems==null||state.StarSystems.Any(s=>s==null||s.OwnerCivilizationId!=null)
                ||state.Civilizations==null||state.Civilizations.Any(c=>c==null)||state.Colonies==null||state.Colonies.Any(c=>c==null)
                ||state.Planets.Select(p=>p.PlanetId).Distinct(StringComparer.Ordinal).Count()!=state.Planets.Count
                ||controls.Count!=state.Planets.Count||state.Planets.Any(p=>!controls.ContainsKey(p.PlanetId)))throw new ArgumentException("Different planetary ownership inventory.");
            var empires=new HashSet<string>(state.Civilizations.Select(c=>c.CivilizationId),StringComparer.Ordinal);
            if(independentActors.Overlaps(empires))throw new ArgumentException("Independent and empire identities overlap.");
            foreach(var c in controls.Values)
            {
                if(c.Kind==PlanetControllerKind.Unclaimed?c.ActorId!=null:c.Kind==PlanetControllerKind.Empire?!empires.Contains(c.ActorId??"")
                    :c.Kind==PlanetControllerKind.IndependentGovernment?!independentActors.Contains(c.ActorId??""):true)throw new ArgumentException("Invalid planet controller.");
            }
            var occupied=new HashSet<string>(StringComparer.Ordinal);
            foreach(var colony in state.Colonies)
            {
                if(!occupied.Add(colony.PlanetId)||!controls.TryGetValue(colony.PlanetId,out var control)||control.Kind!=PlanetControllerKind.Empire
                    ||control.ActorId!=colony.OwnerCivilizationId||!state.Planets.Any(p=>p.PlanetId==colony.PlanetId&&p.SystemId==colony.SystemId))
                    throw new ArgumentException("Colony does not match its unique planet controller.");
            }
            foreach(var control in controls.Values.Where(c=>c.Kind==PlanetControllerKind.Empire))
                if(state.Colonies.Count(c=>c.PlanetId==control.PlanetId&&c.OwnerCivilizationId==control.ActorId)!=1)
                    throw new ArgumentException("Empire control requires its retained colony, including empty outposts.");
        }
        private static PlanetControlDto Copy(PlanetControlDto c)=>new PlanetControlDto{PlanetId=c.PlanetId,Kind=c.Kind,ActorId=c.ActorId};
    }
}
