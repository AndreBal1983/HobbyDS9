using HobbyDS9.Contracts.ShipDesign;
using HobbyDS9.Simulation;

namespace HobbyDS9.Application
{
    public static class PublishedActivitySupportApplication
    {
        public static ActivitySupportResult Evaluate(ShipDesignCatalogueSnapshot catalogue,PublishedShipBlueprint publication,
            ShipDesignAccessDto access,ActivityTacticalSupportCatalogueDto support)
            => PublishedTacticalSupportCompiler.EvaluateActivities(catalogue,publication,access,support);
    }
}
