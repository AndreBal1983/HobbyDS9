using System.Collections.Generic;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;

namespace HobbyDS9.Application
{
    public static class GeneratedGalaxyApplication
    {
        public static GeneratedGalaxyReview Generate(GeneratedGalaxySettingsDto settings, IEnumerable<CivilizationDefinitionDto> admittedCivilizations)
            => new GeneratedGalaxyReview(GeneratedGalaxyReviewSession.Generate(settings, admittedCivilizations));
        public static GeneratedGalaxyReview Open(GeneratedGalaxyDocumentDto document, IEnumerable<CivilizationDefinitionDto> admittedCivilizations)
            => new GeneratedGalaxyReview(GeneratedGalaxyReviewSession.Open(document, admittedCivilizations));
    }

    // A full-map inspection context, deliberately separate from any campaign session.
    public sealed class GeneratedGalaxyReview
    {
        private readonly GeneratedGalaxyReviewSession session;
        internal GeneratedGalaxyReview(GeneratedGalaxyReviewSession session) { this.session = session; }
        public GeneratedGalaxyDocumentDto Export() => session.Export();
        public StrategicRouteSnapshot FindRoute(string originNodeId, string destinationNodeId) => session.FindRoute(originNodeId, destinationNodeId);
    }
}
