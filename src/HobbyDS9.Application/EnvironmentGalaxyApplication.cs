using System.Collections.Generic;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;

namespace HobbyDS9.Application
{
    public static class EnvironmentGalaxyApplication
    {
        public static EnvironmentGalaxyReview Generate(EnvironmentGalaxySettingsDto settings, IEnumerable<CivilizationDefinitionDto> admittedCivilizations)
            => new EnvironmentGalaxyReview(EnvironmentGalaxyReviewSession.Generate(settings, admittedCivilizations));
        public static EnvironmentGalaxyReview Open(EnvironmentGalaxyDocumentDto document, IEnumerable<CivilizationDefinitionDto> admittedCivilizations)
            => new EnvironmentGalaxyReview(EnvironmentGalaxyReviewSession.Open(document, admittedCivilizations));
    }

    public sealed class EnvironmentGalaxyReview
    {
        private readonly EnvironmentGalaxyReviewSession session;
        internal EnvironmentGalaxyReview(EnvironmentGalaxyReviewSession session) { this.session = session; }
        public EnvironmentGalaxyDocumentDto Export() => session.Export();
        public GalaxyReviewRoutePlanSnapshot FindRoutePlan(GalaxyReviewRouteQueryDto query) => session.FindRoutePlan(query);
        public StrategicRouteSnapshot FindRoute(string originNodeId, string destinationNodeId) => session.FindRoute(originNodeId, destinationNodeId);
    }
}
