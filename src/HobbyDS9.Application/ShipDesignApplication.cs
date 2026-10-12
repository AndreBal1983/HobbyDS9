using System.Collections.Generic;
using HobbyDS9.Contracts.ShipDesign;
using HobbyDS9.Simulation;
namespace HobbyDS9.Application
{
    public sealed class ShipDesignerCreationResult
    {
        public IReadOnlyList<DesignIssue> Issues
        { get; }
        public ShipDesigner? Designer
        { get; }
        internal ShipDesignerCreationResult(IReadOnlyList<DesignIssue> issues, ShipDesigner? designer)
        {
            Issues = issues;
            Designer = designer;
        }
    }
    public static class ShipDesignApplication
    {
        public static ShipDesignerCreationResult Create(ShipDesignCatalogueDto? catalogue)
        {
            var issues = ShipDesignCatalogueValidation.Validate(catalogue);
            return new ShipDesignerCreationResult(issues, issues.Count == 0 ? new ShipDesigner(new ShipDesignCatalogueSnapshot(catalogue!)) : null);
        }
    }
    // Independent design workspace; no privileged campaign/session reference or mutation path.
    public sealed class ShipDesigner
    {
        private readonly ShipDesignCompiler compiler;
        public ShipDesignCatalogueSnapshot Catalogue => compiler.Catalogue;
        internal ShipDesigner(ShipDesignCatalogueSnapshot catalogue)
        {
            compiler = new ShipDesignCompiler(catalogue);
        }
        public ShipDesignPreview Preview(ShipClassDraftDto? draft, ShipDesignAccessDto? access) => compiler.Preview(draft, access);
        public ShipBlueprintPublicationResult Publish(ShipClassDraftDto? draft, ShipDesignAccessDto? access, string blueprintId, int revision) => compiler.Publish(draft, access, blueprintId, revision);
    }
}
