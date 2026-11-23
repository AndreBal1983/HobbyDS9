using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HobbyDS9.Application;
using HobbyDS9.Contracts;

namespace HobbyDS9.TacticalReviewClient
{
    // No filesystem, SQLite, serializer dependency, storage identity or mutation of
    // a live session. Composition maps this result to the repository's own DTOs.
    public sealed class CycleCampaignRepositoryAdapter
    {
        private readonly CampaignColonisationCyclePackageDto package;
        private readonly ICampaignRepositoryCodec codec;
        public CycleCampaignRepositoryAdapter(CampaignColonisationCyclePackageDto package,IColonisationCycleReviewHostCodec copyCodec,ICampaignRepositoryCodec codec)
        {
            this.package=(copyCodec??throw new ArgumentNullException(nameof(copyCodec))).CopyCyclePackage(package??throw new ArgumentNullException(nameof(package)));
            this.codec=codec??throw new ArgumentNullException(nameof(codec));
        }
        public CampaignRepositoryImageResult Validate(byte[] payload,string? expectedEditionIdentity=null)
            =>Load(payload,expectedEditionIdentity,out _);

        internal CampaignRepositoryImageResult Load(byte[] payload,string? expectedEditionIdentity,out ICampaignColonisationCycleSession? candidate)
        {
            candidate=null;
            if(payload==null||payload.Length==0||payload.Length>CampaignRepositoryImage.MaximumBytes)
                return new CampaignRepositoryImageResult(CampaignRepositoryError.InvalidPayload);
            var owned=(byte[])payload.Clone();
            CampaignColonisationCycleSaveDto root;
            try{root=codec.Decode(owned);}catch(Exception){return new CampaignRepositoryImageResult(CampaignRepositoryError.InvalidPayload);}
            var format=package is CampaignBeltBodiesPackageDto?18:package is CampaignEngineeringRefinementPackageDto?17:
                package is CampaignAuthoredSolPackageDto?16:package is CampaignSystemRichnessPackageDto?15:14;
            if(root==null)return new CampaignRepositoryImageResult(CampaignRepositoryError.InvalidPayload);
            if(root.FormatVersion!=format||root.PackageHash!=package.PackageHash)
                return new CampaignRepositoryImageResult(CampaignRepositoryError.Incompatible);
            try
            {
                var game=root.BaseCampaign?.Campaign?.Integrated?.Physical?.Core?.Campaign?.GameState;
                if(game!=null&&(game.TurnNumber<=0||game.StateRevision<0))
                {
                    // FoundationValidator rejects these absolute counter values.
                    // A failed or ambiguous decode/replay cannot provide this proof.
                    var exactEdition=expectedEditionIdentity!=null&&
                        string.Equals(expectedEditionIdentity,Describe(root).Identity,StringComparison.Ordinal);
                    return new CampaignRepositoryImageResult(CampaignRepositoryError.InvalidPayload,definiteCorruption:exactEdition);
                }
                var loaded=CampaignColonisationCycleApplication.LoadCycleSave(package,root);
                // Equality of the entire edition carrier, including all histories
                // and private sidecars, not just the visible GameState or its hash.
                if(!codec.Encode(root).SequenceEqual(codec.Encode(loaded.ExportCycleSave())))
                    return new CampaignRepositoryImageResult(CampaignRepositoryError.InvalidPayload);
                var edition=Describe(root);
                if(expectedEditionIdentity!=null&&!string.Equals(expectedEditionIdentity,edition.Identity,StringComparison.Ordinal))
                    return new CampaignRepositoryImageResult(CampaignRepositoryError.Incompatible);
                var status=loaded.CaptureStatus();
                var image=new CampaignRepositoryImage(owned,edition,status.Turn,status.CampaignRevision,status.Phase);
                candidate=loaded;
                return new CampaignRepositoryImageResult(CampaignRepositoryError.None,image);
            }
            catch(Exception){return new CampaignRepositoryImageResult(CampaignRepositoryError.InvalidPayload);}
        }

        private CampaignRepositoryEdition Describe(CampaignColonisationCycleSaveDto root)
        {
            var colony=root.BaseCampaign!;var playable=colony.Campaign!;var integrated=playable.Integrated!;
            var physical=integrated.Physical!;var core=physical.Core;var inner=core.Campaign;var rng=inner.GameState.RngState!;
            var fields=new List<KeyValuePair<string,string>>();
            void Add(string name,object value)=>fields.Add(new KeyValuePair<string,string>(name,Convert.ToString(value,CultureInfo.InvariantCulture)??throw new ArgumentException("Missing identity.")));
            Add("carrier","campaign-colonisation-cycle");Add("root-format",root.FormatVersion);Add("root-profile",root.ProfileId);
            Add("package-format",package.FormatVersion);Add("package-profile",package.ProfileId);Add("package-hash",package.PackageHash);
            Add("colony-format",colony.FormatVersion);Add("colony-profile",colony.ProfileId);Add("colony-package",colony.PackageHash);
            Add("colony-template",colony.TemplatePackageHash);Add("colony-policy",colony.PolicyId);Add("support-hash",colony.SupportHash);
            Add("frozen-planets",colony.FrozenPlanets!.ContentHash);
            Add("playable-format",playable.FormatVersion);Add("playable-profile",playable.ProfileId);Add("playable-package",playable.PackageHash);
            Add("integrated-format",integrated.FormatVersion);Add("integrated-profile",integrated.ProfileId);Add("integrated-package",integrated.PackageHash);
            Add("physical-format",physical.FormatVersion);Add("physical-profile",physical.ProfileId);Add("physical-package",physical.PackageHash);
            Add("review-format",core.FormatVersion);Add("review-profile",core.ProfileId);Add("review-package",core.PackageHash);
            Add("inner-save-format",inner.SaveFormatVersion);Add("scenario",inner.ScenarioId);Add("content",inner.ContentHash);Add("rules",inner.RulesVersion);
            Add("resolver",core.ResolverId);Add("resolver-version",core.ResolverVersion);Add("rng",rng.AlgorithmId);Add("rng-version",rng.AlgorithmVersion);
            Add("map-format",colony.Map!.FormatVersion);Add("map-generator",colony.Map.GeneratorVersion);
            Add("generation",root is CampaignBeltBodiesSaveDto b?b.GenerationVersion:root is CampaignEngineeringRefinementSaveDto e?e.GenerationVersion:
                root is CampaignAuthoredSolSaveDto a?a.GenerationVersion:root is CampaignSystemRichnessSaveDto r?r.GenerationVersion:"none");
            Add("refinement-policy",root is CampaignBeltBodiesSaveDto bb?bb.Refinement!.PolicyId:root is CampaignEngineeringRefinementSaveDto ee?ee.Refinement!.PolicyId:"none");
            Add("belt-policy",root is CampaignBeltBodiesSaveDto belt?belt.BeltBodies!.PolicyId:"none");Add("codec",codec.Version);
            return new CampaignRepositoryEdition(fields);
        }
    }
}
