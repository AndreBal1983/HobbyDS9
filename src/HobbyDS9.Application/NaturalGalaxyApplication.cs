using System.Collections.Generic;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;

namespace HobbyDS9.Application
{
    public static class NaturalGalaxyApplication
    {
        public static NaturalGalaxyReview Generate(NaturalGalaxySettingsDto settings, IEnumerable<CivilizationDefinitionDto> catalog)
            => new NaturalGalaxyReview(NaturalGalaxyReviewSession.Generate(settings, catalog));
        public static NaturalGalaxyReview Open(NaturalGalaxyDocumentDto document, IEnumerable<CivilizationDefinitionDto> catalog)
            => new NaturalGalaxyReview(NaturalGalaxyReviewSession.Open(document, catalog));
    }
    public sealed class NaturalGalaxyReview
    {
        private readonly NaturalGalaxyReviewSession session;
        internal NaturalGalaxyReview(NaturalGalaxyReviewSession session) { this.session = session; }
        public NaturalGalaxyDocumentDto Export() => session.Export();
        public StrategicRouteSnapshot FindRoute(string originNodeId, string destinationNodeId) => session.FindRoute(originNodeId, destinationNodeId);
        public GalaxyReviewRoutePlanSnapshot FindRoutePlan(GalaxyReviewRouteQueryDto query) => session.FindRoutePlan(query);
    }
}
