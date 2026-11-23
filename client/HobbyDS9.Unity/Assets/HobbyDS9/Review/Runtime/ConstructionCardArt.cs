#nullable enable
using UnityEngine;

namespace HobbyDS9.UnityReview
{
    internal static class ConstructionCardArt
    {
        internal static Texture2D? Ship(string hullVersionId) => hullVersionId switch
        {
            "hull.constitution-explorer.v1" => Load("constitution"),
            "hull.galaxy-cruiser.v1" => Load("galaxy"),
            "hull.defiant-combat.v1" => Load("defiant"),
            _ => null
        };

        internal static Texture2D? Building(string definitionId) => definitionId switch
        {
            "infrastructure.fabrication-annex.v1" => Load("fusion"),
            "infrastructure.research-laboratory.v1" => Load("research"),
            "infrastructure.hydroponic-extension.v1" => Load("hydroponics"),
            "infrastructure.frontier-mine.v1" => Load("mine"),
            _ => null
        };

        internal static Texture2D? Kit() => Load("kit");

        private static Texture2D? Load(string name) => Resources.Load<Texture2D>("ConstructionCardArt/" + name);
    }
}
