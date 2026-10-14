using System;
using UnityEngine;

namespace HobbyDS9.ArtDemo
{
    public enum ShipPreviewDisclosure { None, Owned, LawfullyVisible }
    public enum ShipPreviewAvailability { Empty, Model, GenericFallback, RenderingUnavailable }

    // Facts supplied only from the caller's already-filtered actor projection.
    // This package cannot query a session, save, hidden roster or ship statistics.
    public sealed class ShipPreviewSelection
    {
        public string CivilizationDefinitionId { get; }
        public string ShipDesignDefinitionId { get; }
        public ShipPreviewDisclosure Disclosure { get; }
        public ShipPreviewSelection(string civilizationDefinitionId, string shipDesignDefinitionId, ShipPreviewDisclosure disclosure)
        {
            CivilizationDefinitionId = civilizationDefinitionId;
            ShipDesignDefinitionId = shipDesignDefinitionId;
            Disclosure = disclosure;
        }
    }

    public sealed class ShipVisualDefinition
    {
        public string VisualKey { get; }
        public string ResourcePath { get; }
        public string Label { get; }
        public float NominalArtLengthMetres { get; }
        internal ShipVisualDefinition(string key, string slug, string label, float length)
        { VisualKey = key; ResourcePath = "ArtDemo/Federation/Prefabs/" + slug; Label = label; NominalArtLengthMetres = length; }
        public GameObject LoadPrefab() => Resources.Load<GameObject>(ResourcePath);
    }

    public static class ShipVisualCatalog
    {
        public const string ExplorationCruiser = "fed.exploration-cruiser.v1";
        public const string PatrolFrigate = "fed.patrol-frigate.v1";
        public const string CompactEscort = "fed.compact-escort.v1";

        // Art-library entries do not by themselves admit a ship to gameplay.
        public static ShipVisualDefinition Find(string visualKey)
        {
            switch (visualKey)
            {
                case ExplorationCruiser: return new ShipVisualDefinition(visualKey, "exploration-cruiser", "Exploration cruiser · provisional hull art", 620);
                case PatrolFrigate: return new ShipVisualDefinition(visualKey, "patrol-frigate", "Patrol frigate · provisional hull art", 280);
                case CompactEscort: return new ShipVisualDefinition(visualKey, "compact-escort", "Compact escort · provisional hull art", 170);
                default: return null;
            }
        }

        public static ShipVisualDefinition Resolve(ShipPreviewSelection selection)
        {
            if (selection == null || (selection.Disclosure != ShipPreviewDisclosure.Owned && selection.Disclosure != ShipPreviewDisclosure.LawfullyVisible)) return null;
            // Exact provisional scenario binding, never infer from role, class name,
            // owner actor ID, statistics, substring or the generic utility hull.
            return string.Equals(selection.CivilizationDefinitionId, "civilization.federation", StringComparison.Ordinal)
                && string.Equals(selection.ShipDesignDefinitionId, "ship-design.source-cruiser", StringComparison.Ordinal)
                ? Find(ExplorationCruiser) : null;
        }
    }
}
