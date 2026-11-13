using HobbyDS9.Contracts.ShipDesign;
using HobbyDS9.Simulation;
namespace HobbyDS9.Application
{
    // Detached eligibility projection only. No live profile, queue or publication mutation.
    public static class PublishedTacticalSupportApplication
    {
        public static PublishedTacticalSupportResult Evaluate(ShipDesignCatalogueSnapshot frozenCatalogue,PublishedShipBlueprint publication,
            ShipDesignAccessDto currentAccess,TacticalSupportCatalogueDto support)=>PublishedTacticalSupportCompiler.Evaluate(frozenCatalogue,publication,currentAccess,support);
    }
}
