using System.Collections.Generic;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;

namespace HobbyDS9.Application
{
    public static class ScaledGalaxyApplication
    {
        public static ScaledGalaxyReview Generate(ScaledGalaxySettingsDto settings, IEnumerable<CivilizationDefinitionDto> admittedCivilizations)
            => new ScaledGalaxyReview(ScaledGalaxyReviewSession.Generate(settings, admittedCivilizations));
        public static ScaledGalaxyReview Open(ScaledGalaxyDocumentDto document, IEnumerable<CivilizationDefinitionDto> admittedCivilizations)
            => new ScaledGalaxyReview(ScaledGalaxyReviewSession.Open(document, admittedCivilizations));
    }

    public sealed class ScaledGalaxyReview
    {
        private readonly ScaledGalaxyReviewSession session;
        internal ScaledGalaxyReview(ScaledGalaxyReviewSession session) { this.session = session; }
        public ScaledGalaxyDocumentDto Export() => session.Export();
        public StrategicRouteSnapshot FindRoute(string originNodeId, string destinationNodeId) => session.FindRoute(originNodeId, destinationNodeId);
    }
}
