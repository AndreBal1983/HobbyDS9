using System;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    // Closed Format11 preflight, generated OFFLINE from the fixed DTO surface.
    // No reflection, serializer, copying, hashing or caller-defined visitor runs here.
    // All scalar/null/container visits are charged; JSON byte accounting deliberately
    // overestimates punctuation, integer widths and escaped UTF16 string content.
    // This bounds work/allocation before replay; it never certifies semantic validity.
    internal static class ColonisationRootBudget
    {
        internal const string AlgorithmIdentity="colonisation-root-preflight-v1";
        internal const string SchemaFingerprint="eefec29cac2be2889afae7277e955587af184e186337303559001fbd9b852e32";
        internal static void Validate(CampaignColonisationSaveDto root,ColonisationPolicyDto policy)
            =>ValidateCore(root,policy,0,0);
        // Typed outer graph accounting. Old Format11 limits/schema/visitor remain unchanged.
        internal static void ValidateExpansion(CampaignColonisationExpansionSaveDto root,ColonisationPolicyDto policy)
        {
            if(root==null||root.FormatVersion!=CampaignColonisationExpansionIdentity.Format||root.ProfileId!=CampaignColonisationExpansionIdentity.Profile
                ||root.BaseCampaign==null||root.AiPlans==null||root.AiPlans.Count>4)Fail("Complete bounded expansion root required.");
            // Reserve the full bounded future plan from genesis, not only today's
            // empty receipts/targets. This allowance cannot grow after payment.
            long visits=9,bytes=1024+2+6*512;
            void Text(string? value){if(value!=null&&value.Length>512)Fail("Expansion plan string bound.");}
            Text(root.PackageHash);var actors=new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);
            foreach(var p in root.AiPlans)
            {
                if(p==null||p.ActorId==null||!actors.Add(p.ActorId)||p.GoalId!="establish-first-frontier-colony-v1"||!System.Enum.IsDefined(typeof(ColonisationAiStage),p.Stage)
                    ||p.OwnAttemptedTargets==null||p.OwnAttemptedTargets.Count>8||p.LatestActions==null||p.LatestActions.Count>8)Fail("Invalid bounded expansion plan.");
                // 64 fixed visits +16 text fields +8 targets +8*(16+1) receipt visits.
                visits=checked(visits+224);
                // Fixed keys/scalars plus maximum escaped UTF16 text at every slot.
                bytes=checked(bytes+8192+16*(2+6*512)+8*(2+6*512)+8*(1024+2+6*128));
                Text(p.ActorId);Text(p.GoalId);Text(p.HomeColonyId);Text(p.YardColonyId);Text(p.SourceCohortId);Text(p.ScoutShipId);Text(p.TransportShipId);
                Text(p.BlueprintId);Text(p.TargetPlanetId);Text(p.TargetNodeId);Text(p.KitQueueId);Text(p.TransportQueueId);Text(p.ManifestId);Text(p.CampId);Text(p.EstablishedColonyId);Text(p.HoldReason);
                foreach(var id in p.OwnAttemptedTargets){if(id==null)Fail("Null attempted target.");Text(id);}
                foreach(var a in p.LatestActions)
                {
                    if(a==null||a.CommandId==null||a.CommandId.Length>128||!System.Enum.IsDefined(typeof(ColonisationAiAction),a.Action)
                        ||!System.Enum.IsDefined(typeof(CampaignTacticalError),a.Error))Fail("Invalid bounded AI action receipt.");
                    Text(a.CommandId);
                }
            }
            ValidateCore(root.BaseCampaign,policy,visits,bytes);
        }
        private static void ValidateCore(CampaignColonisationSaveDto root,ColonisationPolicyDto policy,long extraVisits,long extraBytes)
        {
            if(root==null||policy==null||policy.MaximumValidationVisits<=0||policy.MaximumValidationVisits>2000000
                ||policy.MaximumCanonicalRootBytes<=0||policy.MaximumCanonicalRootBytes>67108864)
                Fail("Invalid colonisation preflight policy.");
            if(root.FormatVersion!=CampaignColonisationIdentity.Format||root.ProfileId!=CampaignColonisationIdentity.Profile||root.PolicyId!=CampaignColonisationIdentity.Policy)
                Fail("Different closed colonisation root.");
            if(root.Journal==null||root.Journal.Count>policy.JournalBudget)Fail("Colonisation journal limit.");
            if(root.FrozenPlanets==null||root.FrozenPlanets.Definitions==null||root.FrozenPlanets.InitialPlanets==null||root.FrozenPlanets.Bodies==null||root.FrozenPlanets.GenesisControls==null
                ||root.FrozenPlanets.Definitions.Count>policy.MaximumPlanets||root.FrozenPlanets.InitialPlanets.Count>policy.MaximumPlanets
                ||root.FrozenPlanets.Bodies.Count>policy.MaximumPlanets||root.FrozenPlanets.GenesisControls.Count>policy.MaximumPlanets)
                Fail("Complete bounded frozen planet inventory required.");
            if(extraVisits>=policy.MaximumValidationVisits||extraBytes>=policy.MaximumCanonicalRootBytes)Fail("Expansion root work or byte limit.");
            new Counter(policy.MaximumValidationVisits-extraVisits,policy.MaximumCanonicalRootBytes-extraBytes).Root(root);
        }
        [System.Diagnostics.CodeAnalysis.DoesNotReturn] private static void Fail(string message)=>throw new ArgumentException(message);
        private static void Need(object? value){if(value==null)Fail("Null required colonisation root member or collection item.");}
        private static int Present(object? value)=>value==null?0:1;
        private static void Shape(ColonisationOperationDto x){if(Present(x.Colonisation)+Present(x.Existing)!=1)Fail("Closed colonisation journal carrier required.");}
        private static void Shape(CampaignActivityOperationDto x){if(Present(x.Activity)+Present(x.Common)!=1)Fail("Closed inherited activity carrier required.");}
        private static void Shape(CampaignPlayableOperationDto x){if(Present(x.Common)+Present(x.Organization)!=1)Fail("Closed inherited playable carrier required.");}
        private static void Shape(ColonisationCommandDto x)
        {
            if(Present(x.StartSurvey)+Present(x.CancelSurvey)+Present(x.Embark)+Present(x.Unload)+Present(x.Deploy)+Present(x.Withdraw)+Present(x.Evacuate)+Present(x.Deliver)+Present(x.EnqueueKit)+Present(x.LoadSupplies)!=1)
                Fail("Exactly one colonisation command payload required.");
            bool matches=x.Action switch{
                ColonisationAction.StartPlanetSurvey=>x.StartSurvey!=null,ColonisationAction.CancelPlanetSurvey=>x.CancelSurvey!=null,
                ColonisationAction.EmbarkSettlers=>x.Embark!=null,ColonisationAction.UnloadTransport=>x.Unload!=null,
                ColonisationAction.RequestDeployment=>x.Deploy!=null,ColonisationAction.WithdrawDeployment=>x.Withdraw!=null,
                ColonisationAction.EvacuateColony=>x.Evacuate!=null,ColonisationAction.DeliverCarriedSupplies=>x.Deliver!=null,
                ColonisationAction.EnqueueKit=>x.EnqueueKit!=null,ColonisationAction.LoadCarriedSupplies=>x.LoadSupplies!=null,_=>false};
            if(!matches)Fail("Mismatched colonisation command payload.");
        }
        private static void Shape(CampaignFrontierOperationDto x)
        {
            bool payloadFree=x.Travel==null&&x.Strategic==null&&x.Round==null;
            bool metadataFree=x.ActorId==null&&x.CommandId==null&&x.FleetId==null&&x.Enabled==null&&x.Turn==null&&x.Revision==null;
            bool matches=x.Kind switch{
                FrontierOperationKind.Travel=>metadataFree&&x.Travel!=null&&x.Strategic==null&&x.Round==null,
                FrontierOperationKind.Strategic=>metadataFree&&x.Strategic!=null&&x.Travel==null&&x.Round==null,
                FrontierOperationKind.Round=>metadataFree&&x.Round!=null&&x.Travel==null&&x.Strategic==null,
                FrontierOperationKind.Cloak=>payloadFree&&x.ActorId!=null&&x.CommandId!=null&&x.FleetId!=null&&x.Enabled.HasValue&&x.Turn.HasValue&&x.Revision.HasValue,
                FrontierOperationKind.Advance=>payloadFree&&x.ActorId==null&&x.CommandId==null&&x.FleetId==null&&x.Enabled==null&&x.Turn.HasValue&&x.Revision.HasValue,
                FrontierOperationKind.Finalize=>payloadFree&&x.ActorId==null&&x.CommandId==null&&x.FleetId==null&&x.Enabled==null&&x.Turn.HasValue&&x.Revision.HasValue,_=>false};
            if(!matches)Fail("Malformed closed inherited operation.");
        }
        private sealed class Counter
        {
            private long remainingVisits,remainingBytes;
            internal Counter(long visits,long bytes){remainingVisits=visits;remainingBytes=bytes;}
            internal void Root(CampaignColonisationSaveDto root)=>V(root);
            private void Count(long bytes)
            {
                if(remainingVisits<=0||bytes<0||bytes>remainingBytes)Fail("Colonisation root work or byte limit.");
                remainingVisits--;remainingBytes-=bytes;
            }
            private void Null()=>Count(4);
            private void V(global::HobbyDS9.Contracts.ActivityBodyDto? value)
            {
                if(value==null){Null();return;}
                Count(60);
                Need(value.BodyId);
                V(value.BodyId);
                Need(value.SystemId);
                V(value.SystemId);
                Need(value.DisplayName);
                V(value.DisplayName);
                V(value.GovernmentActorId);
            }
            private void V(global::HobbyDS9.Contracts.ActivityFindingDto? value)
            {
                if(value==null){Null();return;}
                Count(262);
                Need(value.ActorId);
                V(value.ActorId);
                Need(value.SiteId);
                V(value.SiteId);
                Need(value.ChoiceId);
                V(value.ChoiceId);
                Need(value.FindingId);
                V(value.FindingId);
                V(value.FindingBudget);
                Need(value.ShipId);
                V(value.ShipId);
                Need(value.FleetId);
                V(value.FleetId);
                V(value.Stage);
                V(value.WorkDone);
                V(value.RemainingAdvances);
                V(value.StartedTurn);
                V(value.ResolvedTurn);
                V(value.PaidCredits);
                V(value.InsightApplied);
                V(value.InsightUnlearnable);
                V(value.ResearchReceipts);
                Need(value.Report);
                V(value.Report);
                Need(value.ContactOutcome);
                V(value.ContactOutcome);
            }
            private void V(global::HobbyDS9.Contracts.ActivityMinorRelation value){Count(11);}
            private void V(global::HobbyDS9.Contracts.ActivityMinorRelationDto? value)
            {
                if(value==null){Null();return;}
                Count(56);
                Need(value.ActorId);
                V(value.ActorId);
                Need(value.MinorActorId);
                V(value.MinorActorId);
                V(value.Relation);
                V(value.ChangedTurn);
            }
            private void V(global::HobbyDS9.Contracts.ActivityMinorStateDto? value)
            {
                if(value==null){Null();return;}
                Count(66);
                Need(value.ActorId);
                V(value.ActorId);
                Need(value.DefinitionId);
                V(value.DefinitionId);
                Need(value.BodyId);
                V(value.BodyId);
                V(value.Population);
                Need(value.SpeciesId);
                V(value.SpeciesId);
            }
            private void V(global::HobbyDS9.Contracts.ActivityProductionItemDto? value)
            {
                if(value==null){Null();return;}
                Count(46);
                Need(value.ColonyId);
                V(value.ColonyId);
                V(value.Kind);
                Need(value.DefinitionId);
                V(value.DefinitionId);
                V(value.Paid);
            }
            private void V(global::HobbyDS9.Contracts.ActivityProductionKind value){Count(11);}
            private void V(global::HobbyDS9.Contracts.ActivityProgressStage value){Count(11);}
            private void V(global::HobbyDS9.Contracts.ActivitySiteObservationDto? value)
            {
                if(value==null){Null();return;}
                Count(44);
                Need(value.ActorId);
                V(value.ActorId);
                Need(value.SiteId);
                V(value.SiteId);
                V(value.FirstObservedTurn);
            }
            private void V(global::HobbyDS9.Contracts.ActivitySiteStateDto? value)
            {
                if(value==null){Null();return;}
                Count(76);
                Need(value.SiteId);
                V(value.SiteId);
                Need(value.DefinitionId);
                V(value.DefinitionId);
                Need(value.NodeId);
                V(value.NodeId);
                Need(value.SystemId);
                V(value.SystemId);
                Need(value.BodyId);
                V(value.BodyId);
                V(value.MinorActorId);
            }
            private void V(global::HobbyDS9.Contracts.ActivityStructureReceiptDto? value)
            {
                if(value==null){Null();return;}
                Count(73);
                Need(value.QueueItemId);
                V(value.QueueItemId);
                Need(value.ColonyId);
                V(value.ColonyId);
                Need(value.DefinitionId);
                V(value.DefinitionId);
                Need(value.OwnerId);
                V(value.OwnerId);
                V(value.CompletedTurn);
            }
            private void V(global::HobbyDS9.Contracts.AuthoredTacticalAmmoStockDto? value)
            {
                if(value==null){Null();return;}
                Count(49);
                V(value.WeaponId);
                V(value.AmmunitionDefinitionId);
                V(value.Stock);
            }
            private void V(global::HobbyDS9.Contracts.AuthoredTacticalShieldStockDto? value)
            {
                if(value==null){Null();return;}
                Count(23);
                V(value.Sector);
                V(value.Current);
            }
            private void V(global::HobbyDS9.Contracts.BlueprintReferenceDto? value)
            {
                if(value==null){Null();return;}
                Count(29);
                Need(value.BlueprintId);
                V(value.BlueprintId);
                V(value.Revision);
            }
            private void V(global::HobbyDS9.Contracts.BlueprintRevisionDto? value)
            {
                if(value==null){Null();return;}
                Count(160);
                Need(value.OwnerCivilizationId);
                V(value.OwnerCivilizationId);
                Need(value.BlueprintId);
                V(value.BlueprintId);
                V(value.Revision);
                Need(value.CompilerVersion);
                V(value.CompilerVersion);
                Need(value.RulesVersion);
                V(value.RulesVersion);
                Need(value.SourceContentHash);
                V(value.SourceContentHash);
                V(value.FrozenCatalogue);
                V(value.Fit);
                V(value.CompiledDesign);
                V(value.Cost);
            }
            private void V(global::HobbyDS9.Contracts.CampaignActivityCommandDto? value)
            {
                if(value==null){Null();return;}
                Count(129);
                Need(value.ActorId);
                V(value.ActorId);
                Need(value.CommandId);
                V(value.CommandId);
                V(value.ExpectedTurn);
                V(value.ExpectedRevision);
                V(value.Kind);
                Need(value.TargetId);
                V(value.TargetId);
                Need(value.DefinitionId);
                V(value.DefinitionId);
                V(value.BlueprintRevision);
                Need(value.ShipId);
                V(value.ShipId);
            }
            private void V(global::HobbyDS9.Contracts.CampaignActivityCommandKind value){Count(11);}
            private void V(global::HobbyDS9.Contracts.CampaignActivityOperationDto? value)
            {
                if(value==null){Null();return;}
                Shape(value);
                Count(24);
                V(value.Common);
                V(value.Activity);
            }
            private void V(global::HobbyDS9.Contracts.CampaignActivityStateDto? value)
            {
                if(value==null){Null();return;}
                Count(100);
                V(value.Production);
                V(value.Structures);
                V(value.Sites);
                V(value.Minors);
                V(value.Findings);
                V(value.Bodies);
                V(value.Relations);
                V(value.Observations);
            }
            private void V(global::HobbyDS9.Contracts.CampaignColonisationSaveDto? value)
            {
                if(value==null){Null();return;}
                Count(291);
                V(value.FormatVersion);
                Need(value.ProfileId);
                V(value.ProfileId);
                Need(value.PolicyId);
                V(value.PolicyId);
                Need(value.TemplatePackageHash);
                V(value.TemplatePackageHash);
                Need(value.PackageHash);
                V(value.PackageHash);
                Need(value.SupportHash);
                V(value.SupportHash);
                Need(value.Map);
                V(value.Map);
                Need(value.Campaign);
                V(value.Campaign);
                Need(value.FrozenPlanets);
                V(value.FrozenPlanets);
                Need(value.Discoveries);
                V(value.Discoveries);
                Need(value.PublishedBuiltShips);
                V(value.PublishedBuiltShips);
                Need(value.Activities);
                V(value.Activities);
                Need(value.Colonisation);
                V(value.Colonisation);
                Need(value.Production);
                V(value.Production);
                Need(value.ProductionHistory);
                V(value.ProductionHistory);
                V(value.MovementOwnership);
                V(value.LastSettlementProof);
                Need(value.Journal);
                V(value.Journal);
            }
            private void V(global::HobbyDS9.Contracts.CampaignColonisationStateDto? value)
            {
                if(value==null){Null();return;}
                Count(250);
                Need(value.PlanetControls);
                V(value.PlanetControls);
                Need(value.Bodies);
                V(value.Bodies);
                Need(value.Surveys);
                V(value.Surveys);
                Need(value.ManifestRecords);
                V(value.ManifestRecords);
                Need(value.ManifestOrigins);
                V(value.ManifestOrigins);
                Need(value.ManifestResolutions);
                V(value.ManifestResolutions);
                Need(value.ColonyOrigins);
                V(value.ColonyOrigins);
                Need(value.LandedCohortLineages);
                V(value.LandedCohortLineages);
                Need(value.CampSources);
                V(value.CampSources);
                Need(value.CampTerminals);
                V(value.CampTerminals);
                Need(value.DeploymentIntents);
                V(value.DeploymentIntents);
                V(value.LastSettlementTurn);
                V(value.FoodProduced);
                V(value.FoodConsumed);
            }
            private void V(global::HobbyDS9.Contracts.CampaignContactKnowledgeDto? value)
            {
                if(value==null){Null();return;}
                Count(77);
                Need(value.ActorId);
                V(value.ActorId);
                Need(value.FleetId);
                V(value.FleetId);
                Need(value.LastSeenNodeId);
                V(value.LastSeenNodeId);
                V(value.LastSeenTurn);
                Need(value.TrackingNodeIds);
                V(value.TrackingNodeIds);
            }
            private void V(global::HobbyDS9.Contracts.CampaignFleetManeuver value){Count(11);}
            private void V(global::HobbyDS9.Contracts.CampaignFleetNameDto? value)
            {
                if(value==null){Null();return;}
                Count(28);
                Need(value.FleetId);
                V(value.FleetId);
                Need(value.DisplayName);
                V(value.DisplayName);
            }
            private void V(global::HobbyDS9.Contracts.CampaignFleetOrganizationKind value){Count(11);}
            private void V(global::HobbyDS9.Contracts.CampaignFleetTravelReceiptDto? value)
            {
                if(value==null){Null();return;}
                Count(219);
                Need(value.FleetId);
                V(value.FleetId);
                Need(value.OwnerId);
                V(value.OwnerId);
                V(value.Turn);
                Need(value.EndNodeId);
                V(value.EndNodeId);
                V(value.TransitDestinationId);
                V(value.TransitTotalCost);
                V(value.TransitProgress);
                V(value.CostPaid);
                V(value.EdgesCompleted);
                V(value.StoppedByEncounter);
                Need(value.StopTimeNumerator);
                V(value.StopTimeNumerator);
                Need(value.StopTimeDenominator);
                V(value.StopTimeDenominator);
                V(value.EncounterId);
            }
            private void V(global::HobbyDS9.Contracts.CampaignFrontierOperationDto? value)
            {
                if(value==null){Null();return;}
                Shape(value);
                Count(108);
                V(value.Kind);
                V(value.Travel);
                V(value.Strategic);
                V(value.Round);
                V(value.ActorId);
                V(value.CommandId);
                V(value.FleetId);
                V(value.Enabled);
                V(value.Turn);
                V(value.Revision);
            }
            private void V(global::HobbyDS9.Contracts.CampaignFrontierReceiptDto? value)
            {
                if(value==null){Null();return;}
                Count(85);
                Need(value.ActorId);
                V(value.ActorId);
                Need(value.FleetId);
                V(value.FleetId);
                Need(value.ArrivalNodeId);
                V(value.ArrivalNodeId);
                V(value.Turn);
                Need(value.ChartedNodeIds);
                V(value.ChartedNodeIds);
                Need(value.ChartedLaneIds);
                V(value.ChartedLaneIds);
            }
            private void V(global::HobbyDS9.Contracts.CampaignIntegratedBuiltShipDto? value)
            {
                if(value==null){Null();return;}
                Count(139);
                V(value.ShipId);
                V(value.ShipDesignDefinitionId);
                V(value.SourceProvisionShipId);
                V(value.OwnerCivilizationId);
                V(value.FleetIdAtConstruction);
                V(value.QueueItemId);
                V(value.TurnBuilt);
            }
            private void V(global::HobbyDS9.Contracts.CampaignIntegratedPlaytestSaveDto? value)
            {
                if(value==null){Null();return;}
                Count(73);
                V(value.FormatVersion);
                Need(value.ProfileId);
                V(value.ProfileId);
                Need(value.PackageHash);
                V(value.PackageHash);
                V(value.Physical);
                V(value.BuiltShips);
            }
            private void V(global::HobbyDS9.Contracts.CampaignPhysicalMovementProofDto? value)
            {
                if(value==null){Null();return;}
                Count(33);
                Need(value.Start);
                V(value.Start);
                Need(value.Orders);
                V(value.Orders);
                Need(value.Contacts);
                V(value.Contacts);
            }
            private void V(global::HobbyDS9.Contracts.CampaignPlayableFleetSaveDto? value)
            {
                if(value==null){Null();return;}
                Count(84);
                V(value.FormatVersion);
                Need(value.ProfileId);
                V(value.ProfileId);
                Need(value.PackageHash);
                V(value.PackageHash);
                Need(value.Integrated);
                V(value.Integrated);
                Need(value.Names);
                V(value.Names);
                Need(value.Transcript);
                V(value.Transcript);
            }
            private void V(global::HobbyDS9.Contracts.CampaignPlayableOperationDto? value)
            {
                if(value==null){Null();return;}
                Shape(value);
                Count(28);
                V(value.Common);
                V(value.Organization);
            }
            private void V(global::HobbyDS9.Contracts.CampaignPublishedBuiltShipDto? value)
            {
                if(value==null){Null();return;}
                Count(316);
                V(value.ShipId);
                V(value.BlueprintId);
                V(value.BlueprintRevision);
                V(value.OwnerCivilizationId);
                V(value.FleetIdAtConstruction);
                V(value.QueueItemId);
                V(value.TurnBuilt);
                V(value.SourceCompilerId);
                V(value.SourceRulesVersion);
                V(value.SourceContentHash);
                V(value.PublicationHash);
                V(value.SupportCatalogueId);
                V(value.SupportCatalogueRevision);
                V(value.SupportHash);
                V(value.SupportCompilerId);
                V(value.FitId);
                V(value.FitRevision);
            }
            private void V(global::HobbyDS9.Contracts.CampaignTacticalActorReportDto? value)
            {
                if(value==null){Null();return;}
                Count(122);
                Need(value.ActorId);
                V(value.ActorId);
                V(value.Sequence);
                V(value.Round);
                Need(value.Kind);
                V(value.Kind);
                V(value.ShipId);
                V(value.TargetShipId);
                V(value.OwnWeaponId);
                V(value.Amount);
                V(value.Sector);
                V(value.Range);
                Need(value.Detail);
                V(value.Detail);
            }
            private void V(global::HobbyDS9.Contracts.CampaignTacticalBattleCheckpointDto? value)
            {
                if(value==null){Null();return;}
                Count(92);
                Need(value.State);
                V(value.State);
                V(value.BoundaryRevision);
                Need(value.HiddenShipIds);
                V(value.HiddenShipIds);
                Need(value.Commitments);
                V(value.Commitments);
                Need(value.Reports);
                V(value.Reports);
                Need(value.ResolvedRounds);
                V(value.ResolvedRounds);
            }
            private void V(global::HobbyDS9.Contracts.CampaignTacticalCommitmentDto? value)
            {
                if(value==null){Null();return;}
                Count(36);
                Need(value.ActorId);
                V(value.ActorId);
                Need(value.CommandId);
                V(value.CommandId);
                Need(value.Orders);
                V(value.Orders);
            }
            private void V(global::HobbyDS9.Contracts.CampaignTacticalFleetOrderDto? value)
            {
                if(value==null){Null();return;}
                Count(41);
                Need(value.FleetId);
                V(value.FleetId);
                V(value.Maneuver);
                V(value.FocusFleetId);
            }
            private void V(global::HobbyDS9.Contracts.CampaignTacticalFleetSettingDto? value)
            {
                if(value==null){Null();return;}
                Count(30);
                Need(value.FleetId);
                V(value.FleetId);
                V(value.ApproachCloak);
            }
            private void V(global::HobbyDS9.Contracts.CampaignTacticalPoolDto? value)
            {
                if(value==null){Null();return;}
                Count(77);
                Need(value.ShipId);
                V(value.ShipId);
                Need(value.FitId);
                V(value.FitId);
                V(value.FitRevision);
                Need(value.ProvenanceId);
                V(value.ProvenanceId);
                Need(value.Shields);
                V(value.Shields);
                Need(value.Ammunition);
                V(value.Ammunition);
            }
            private void V(global::HobbyDS9.Contracts.CampaignTacticalResolvedRoundDto? value)
            {
                if(value==null){Null();return;}
                Count(26);
                V(value.Round);
                Need(value.Commitments);
                V(value.Commitments);
            }
            private void V(global::HobbyDS9.Contracts.CampaignTacticalReviewSaveDto? value)
            {
                if(value==null){Null();return;}
                Count(154);
                V(value.FormatVersion);
                Need(value.ProfileId);
                V(value.ProfileId);
                Need(value.PackageHash);
                V(value.PackageHash);
                Need(value.ResolverId);
                V(value.ResolverId);
                V(value.ResolverVersion);
                Need(value.Campaign);
                V(value.Campaign);
                Need(value.Pools);
                V(value.Pools);
                Need(value.FleetSettings);
                V(value.FleetSettings);
                Need(value.Battles);
                V(value.Battles);
                Need(value.AcceptedRoundCommands);
                V(value.AcceptedRoundCommands);
            }
            private void V(global::HobbyDS9.Contracts.CampaignTacticalTravelSaveDto? value)
            {
                if(value==null){Null();return;}
                Count(131);
                V(value.FormatVersion);
                Need(value.ProfileId);
                V(value.ProfileId);
                Need(value.PackageHash);
                V(value.PackageHash);
                Need(value.Core);
                V(value.Core);
                V(value.Phase);
                Need(value.Requests);
                V(value.Requests);
                Need(value.Orders);
                V(value.Orders);
                Need(value.Contacts);
                V(value.Contacts);
                Need(value.LastMovement);
                V(value.LastMovement);
                V(value.MovementProof);
            }
            private void V(global::HobbyDS9.Contracts.CampaignTravelIntent value){Count(11);}
            private void V(global::HobbyDS9.Contracts.CampaignTravelOrderDto? value)
            {
                if(value==null){Null();return;}
                Count(84);
                Need(value.FleetId);
                V(value.FleetId);
                V(value.Intent);
                V(value.ContactFleetId);
                V(value.ExpectedContactSeenTurn);
                Need(value.RouteNodeIds);
                V(value.RouteNodeIds);
            }
            private void V(global::HobbyDS9.Contracts.CancelPlanetSurveyDto? value)
            {
                if(value==null){Null();return;}
                Count(14);
                Need(value.SurveyId);
                V(value.SurveyId);
            }
            private void V(global::HobbyDS9.Contracts.CancelProductionPayload? value)
            {
                if(value==null){Null();return;}
                Count(29);
                Need(value.ColonyId);
                V(value.ColonyId);
                Need(value.QueueItemId);
                V(value.QueueItemId);
            }
            private void V(global::HobbyDS9.Contracts.CivilizationNavigationKnowledgeDto? value)
            {
                if(value==null){Null();return;}
                Count(44);
                Need(value.CivilizationId);
                V(value.CivilizationId);
                V(value.Nodes);
                V(value.Connections);
            }
            private void V(global::HobbyDS9.Contracts.CivilizationStateDto? value)
            {
                if(value==null){Null();return;}
                Count(333);
                Need(value.CivilizationId);
                V(value.CivilizationId);
                Need(value.DefinitionId);
                V(value.DefinitionId);
                V(value.ControllerKind);
                V(value.Credits);
                V(value.ResearchDiscoverySeed);
                V(value.ScienceAllocations);
                V(value.EngineeringParameters);
                V(value.ScienceAllocationUnits);
                V(value.ScienceReserve);
                V(value.EngineeringReserve);
                V(value.Disciplines);
                V(value.EngineeringTargetDefinitionId);
                V(value.EngineeringProgress);
                V(value.MatureEngineeringApplicationDefinitionIds);
                V(value.Statistics);
            }
            private void V(global::HobbyDS9.Contracts.CohortPoliticalReportDto? value)
            {
                if(value==null){Null();return;}
                Count(167);
                Need(value.CohortId);
                V(value.CohortId);
                V(value.PreviousStatus);
                V(value.ResultStatus);
                V(value.TransitionProgress);
                V(value.SatisfactionChange);
                V(value.IntegrationChange);
                V(value.AlignmentChange);
                V(value.RadicalisationChange);
                V(value.Reasons);
            }
            private void V(global::HobbyDS9.Contracts.ColonisationAction value){Count(11);}
            private void V(global::HobbyDS9.Contracts.ColonisationColonyStageDto? value)
            {
                if(value==null){Null();return;}
                Count(23);
                Need(value.ColonyId);
                V(value.ColonyId);
                V(value.Stage);
            }
            private void V(global::HobbyDS9.Contracts.ColonisationCommandDto? value)
            {
                if(value==null){Null();return;}
                Shape(value);
                Count(182);
                Need(value.CommandId);
                V(value.CommandId);
                Need(value.ActorId);
                V(value.ActorId);
                V(value.Turn);
                V(value.Revision);
                V(value.Action);
                V(value.StartSurvey);
                V(value.CancelSurvey);
                V(value.Embark);
                V(value.Unload);
                V(value.Deploy);
                V(value.Withdraw);
                V(value.Evacuate);
                V(value.Deliver);
                V(value.EnqueueKit);
                V(value.LoadSupplies);
            }
            private void V(global::HobbyDS9.Contracts.ColonisationDeploymentReceiptDto? value)
            {
                if(value==null){Null();return;}
                Count(170);
                V(value.Intent);
                V(value.ManifestBefore);
                V(value.Outcome);
                V(value.ColonyId);
                V(value.CampId);
                V(value.LandedCohortId);
                V(value.PopulationTransferred);
                V(value.FoodTransferred);
                V(value.EquipmentConsumed);
                V(value.MaterialsTransferred);
            }
            private void V(global::HobbyDS9.Contracts.ColonisationMovementOwnershipDto? value)
            {
                if(value==null){Null();return;}
                Count(56);
                V(value.Turn);
                V(value.Revision);
                Need(value.PlanetControls);
                V(value.PlanetControls);
                Need(value.ColonyStages);
                V(value.ColonyStages);
            }
            private void V(global::HobbyDS9.Contracts.ColonisationOperationDto? value)
            {
                if(value==null){Null();return;}
                Shape(value);
                Count(30);
                V(value.Colonisation);
                V(value.Existing);
            }
            private void V(global::HobbyDS9.Contracts.ColonisationProductionItemDto? value)
            {
                if(value==null){Null();return;}
                Count(160);
                Need(value.QueueItemId);
                V(value.QueueItemId);
                Need(value.ColonyId);
                V(value.ColonyId);
                Need(value.OwnerId);
                V(value.OwnerId);
                V(value.Kind);
                Need(value.DefinitionId);
                V(value.DefinitionId);
                V(value.Blueprint);
                V(value.RequiredIndustry);
                V(value.CompletedIndustry);
                Need(value.RequiredResources);
                V(value.RequiredResources);
                Need(value.ConsumedResources);
                V(value.ConsumedResources);
            }
            private void V(global::HobbyDS9.Contracts.ColonisationProductionKind value){Count(11);}
            private void V(global::HobbyDS9.Contracts.ColonisationProductionReceiptDto? value)
            {
                if(value==null){Null();return;}
                Count(50);
                V(value.Order);
                V(value.Completed);
                V(value.ResolvedTurn);
                V(value.ShipId);
            }
            private void V(global::HobbyDS9.Contracts.ColonisationSettlementProofDto? value)
            {
                if(value==null){Null();return;}
                Count(102);
                V(value.Turn);
                V(value.BeforeGameState);
                V(value.AfterGameState);
                V(value.BeforeColonisation);
                V(value.AfterColonisation);
                Need(value.Receipts);
                V(value.Receipts);
            }
            private void V(global::HobbyDS9.Contracts.ColonyEstablishmentStage value){Count(11);}
            private void V(global::HobbyDS9.Contracts.ColonyOriginDto? value)
            {
                if(value==null){Null();return;}
                Count(48);
                Need(value.ColonyId);
                V(value.ColonyId);
                Need(value.PlanetId);
                V(value.PlanetId);
                Need(value.ManifestId);
                V(value.ManifestId);
                V(value.Turn);
            }
            private void V(global::HobbyDS9.Contracts.ColonyStateDto? value)
            {
                if(value==null){Null();return;}
                Count(245);
                V(value.Politics);
                Need(value.ColonyId);
                V(value.ColonyId);
                Need(value.SystemId);
                V(value.SystemId);
                V(value.OwnerCivilizationId);
                Need(value.PlanetId);
                V(value.PlanetId);
                V(value.Cohorts);
                V(value.Infrastructure);
                V(value.Stockpiles);
                V(value.DesiredStockpiles);
                V(value.Stability);
                V(value.Prosperity);
                V(value.Automation);
                V(value.FoodShortageTurns);
                V(value.AcceptsRefugees);
                V(value.Food);
                V(value.ProductionQueue);
            }
            private void V(global::HobbyDS9.Contracts.CombatEventDto? value)
            {
                if(value==null){Null();return;}
                Count(95);
                V(value.Sequence);
                V(value.Kind);
                V(value.Phase);
                V(value.SourceFleetId);
                V(value.SourceShipId);
                V(value.Amount);
                V(value.FleetId);
                V(value.ShipId);
            }
            private void V(global::HobbyDS9.Contracts.CombatEventKind value){Count(11);}
            private void V(global::HobbyDS9.Contracts.CombatParticipantDto? value)
            {
                if(value==null){Null();return;}
                Count(40);
                Need(value.CivilizationId);
                V(value.CivilizationId);
                Need(value.FleetId);
                V(value.FleetId);
                Need(value.Ships);
                V(value.Ships);
            }
            private void V(global::HobbyDS9.Contracts.CombatPhaseKind value){Count(11);}
            private void V(global::HobbyDS9.Contracts.CombatRequestDto? value)
            {
                if(value==null){Null();return;}
                Count(165);
                Need(value.EncounterId);
                V(value.EncounterId);
                V(value.TurnNumber);
                Need(value.LocationNodeId);
                V(value.LocationNodeId);
                Need(value.RulesVersion);
                V(value.RulesVersion);
                Need(value.ContentHash);
                V(value.ContentHash);
                Need(value.ResolverId);
                V(value.ResolverId);
                V(value.ResolverVersion);
                Need(value.RandomAlgorithmId);
                V(value.RandomAlgorithmId);
                V(value.RandomState);
                Need(value.Participants);
                V(value.Participants);
            }
            private void V(global::HobbyDS9.Contracts.CombatResolutionStatus value){Count(11);}
            private void V(global::HobbyDS9.Contracts.CombatResultDto? value)
            {
                if(value==null){Null();return;}
                Count(121);
                Need(value.EncounterId);
                V(value.EncounterId);
                Need(value.ResolverId);
                V(value.ResolverId);
                V(value.ResolverVersion);
                V(value.Status);
                V(value.FinalRandomState);
                Need(value.ShipChanges);
                V(value.ShipChanges);
                Need(value.FleetChanges);
                V(value.FleetChanges);
                Need(value.Events);
                V(value.Events);
            }
            private void V(global::HobbyDS9.Contracts.CombatShipDto? value)
            {
                if(value==null){Null();return;}
                Count(60);
                Need(value.ShipId);
                V(value.ShipId);
                Need(value.DesignId);
                V(value.DesignId);
                V(value.Hull);
                V(value.MaxHull);
                V(value.CombatProfile);
            }
            private void V(global::HobbyDS9.Contracts.CommandEnvelope? value)
            {
                if(value==null){Null();return;}
                Count(297);
                Need(value.CommandId);
                V(value.CommandId);
                Need(value.ActorCivilizationId);
                V(value.ActorCivilizationId);
                V(value.ExpectedTurn);
                V(value.ExpectedRevision);
                V(value.Kind);
                V(value.PublishShipBlueprint);
                V(value.EnqueueBlueprintConstruction);
                V(value.EnqueueProduction);
                V(value.CancelProduction);
                V(value.SetResearchAllocation);
                V(value.SetScienceAllocation);
                V(value.SetEngineeringTarget);
                V(value.SetWorldGovernancePolicy);
                V(value.SetFleetRoute);
            }
            private void V(global::HobbyDS9.Contracts.CommandKind value){Count(11);}
            private void V(global::HobbyDS9.Contracts.CommitCampaignTacticalRoundDto? value)
            {
                if(value==null){Null();return;}
                Count(118);
                Need(value.ActorId);
                V(value.ActorId);
                Need(value.CommandId);
                V(value.CommandId);
                Need(value.EncounterId);
                V(value.EncounterId);
                V(value.ExpectedTurn);
                V(value.ExpectedRound);
                V(value.ExpectedBoundaryRevision);
                V(value.Replacements);
            }
            private void V(global::HobbyDS9.Contracts.ControllerKind value){Count(11);}
            private void V(global::HobbyDS9.Contracts.DeliverCarriedSuppliesDto? value)
            {
                if(value==null){Null();return;}
                Count(49);
                Need(value.ManifestId);
                V(value.ManifestId);
                Need(value.ColonyId);
                V(value.ColonyId);
                V(value.Food);
                V(value.Materials);
            }
            private void V(global::HobbyDS9.Contracts.DeploySettlersDto? value)
            {
                if(value==null){Null();return;}
                Count(28);
                Need(value.ManifestId);
                V(value.ManifestId);
                Need(value.PlanetId);
                V(value.PlanetId);
            }
            private void V(global::HobbyDS9.Contracts.DeploymentBoundaryOutcome value){Count(11);}
            private void V(global::HobbyDS9.Contracts.DisplacementDto? value)
            {
                if(value==null){Null();return;}
                Count(96);
                Need(value.DisplacementId);
                V(value.DisplacementId);
                Need(value.SourcePlanetId);
                V(value.SourcePlanetId);
                Need(value.CohortId);
                V(value.CohortId);
                V(value.Population);
                Need(value.Cause);
                V(value.Cause);
                V(value.ResponsibleEntityId);
            }
            private void V(global::HobbyDS9.Contracts.EmbarkSettlersDto? value)
            {
                if(value==null){Null();return;}
                Count(71);
                Need(value.ShipId);
                V(value.ShipId);
                Need(value.ColonyId);
                V(value.ColonyId);
                Need(value.CohortId);
                V(value.CohortId);
                V(value.Population);
                V(value.Food);
                V(value.Materials);
            }
            private void V(global::HobbyDS9.Contracts.EngineeringParameterDto? value)
            {
                if(value==null){Null();return;}
                Count(27);
                Need(value.DefinitionId);
                V(value.DefinitionId);
                V(value.Value);
            }
            private void V(global::HobbyDS9.Contracts.EngineeringProgressDto? value)
            {
                if(value==null){Null();return;}
                Count(30);
                Need(value.DefinitionId);
                V(value.DefinitionId);
                V(value.Progress);
            }
            private void V(global::HobbyDS9.Contracts.EnqueueBlueprintConstructionPayload? value)
            {
                if(value==null){Null();return;}
                Count(41);
                Need(value.ColonyId);
                V(value.ColonyId);
                Need(value.BlueprintId);
                V(value.BlueprintId);
                V(value.Revision);
            }
            private void V(global::HobbyDS9.Contracts.EnqueueProductionPayload? value)
            {
                if(value==null){Null();return;}
                Count(30);
                Need(value.ColonyId);
                V(value.ColonyId);
                Need(value.DefinitionId);
                V(value.DefinitionId);
            }
            private void V(global::HobbyDS9.Contracts.EnqueueSettlementKitDto? value)
            {
                if(value==null){Null();return;}
                Count(14);
                Need(value.ColonyId);
                V(value.ColonyId);
            }
            private void V(global::HobbyDS9.Contracts.EntityIdentityDto? value)
            {
                if(value==null){Null();return;}
                Count(30);
                Need(value.EntityId);
                V(value.EntityId);
                V(value.DefinitionId);
            }
            private void V(global::HobbyDS9.Contracts.EnvironmentGalaxyContributionDto? value)
            {
                if(value==null){Null();return;}
                Count(25);
                Need(value.FeatureId);
                V(value.FeatureId);
                V(value.Amount);
            }
            private void V(global::HobbyDS9.Contracts.EnvironmentGalaxyFeatureDto? value)
            {
                if(value==null){Null();return;}
                Count(111);
                Need(value.FeatureId);
                V(value.FeatureId);
                V(value.Kind);
                Need(value.DisplayName);
                V(value.DisplayName);
                Need(value.Position);
                V(value.Position);
                V(value.RadiusX);
                V(value.RadiusY);
                V(value.DensityWeight);
                V(value.SystemId);
                V(value.NodeId);
            }
            private void V(global::HobbyDS9.Contracts.EnvironmentGalaxyLayoutDto? value)
            {
                if(value==null){Null();return;}
                Count(76);
                V(value.Width);
                V(value.Height);
                Need(value.Regions);
                V(value.Regions);
                Need(value.Systems);
                V(value.Systems);
                Need(value.Connections);
                V(value.Connections);
                Need(value.HomeCandidates);
                V(value.HomeCandidates);
            }
            private void V(global::HobbyDS9.Contracts.EnvironmentGalaxyRegionInfluenceDto? value)
            {
                if(value==null){Null();return;}
                Count(61);
                Need(value.NodeId);
                V(value.NodeId);
                V(value.BaseDensity);
                V(value.TargetDensity);
                Need(value.Contributions);
                V(value.Contributions);
            }
            private void V(global::HobbyDS9.Contracts.EnvironmentGalaxyTuningDto? value)
            {
                if(value==null){Null();return;}
                Count(181);
                V(value.BaseDensity);
                V(value.MaximumDensity);
                V(value.MinimumSiteSeparation);
                V(value.MinimumStarSeparation);
                V(value.OuterMargin);
                V(value.BlackHoleCount);
                V(value.NebulaCount);
                V(value.CandidateDrawsPerRegion);
                V(value.PlacementAttempts);
            }
            private void V(global::HobbyDS9.Contracts.EvacuateFoundingColonyDto? value)
            {
                if(value==null){Null();return;}
                Count(22);
                Need(value.CampId);
                V(value.CampId);
                Need(value.ShipId);
                V(value.ShipId);
            }
            private void V(global::HobbyDS9.Contracts.ExternalAffinityDto? value)
            {
                if(value==null){Null();return;}
                Count(30);
                Need(value.CivilizationId);
                V(value.CivilizationId);
                V(value.Rating);
            }
            private void V(global::HobbyDS9.Contracts.FleetCombatChangeDto? value)
            {
                if(value==null){Null();return;}
                Count(45);
                Need(value.FleetId);
                V(value.FleetId);
                V(value.Disposition);
                V(value.RetreatNodeId);
            }
            private void V(global::HobbyDS9.Contracts.FleetCombatDisposition value){Count(11);}
            private void V(global::HobbyDS9.Contracts.FleetStateDto? value)
            {
                if(value==null){Null();return;}
                Count(120);
                V(value.TransitTotalCost);
                V(value.TransitProgress);
                Need(value.FleetId);
                V(value.FleetId);
                Need(value.OwnerCivilizationId);
                V(value.OwnerCivilizationId);
                Need(value.LocationNodeId);
                V(value.LocationNodeId);
                V(value.ShipIds);
                V(value.RouteNodeIds);
            }
            private void V(global::HobbyDS9.Contracts.FrontierOperationKind value){Count(11);}
            private void V(global::HobbyDS9.Contracts.FrozenColonisationPlanetsDto? value)
            {
                if(value==null){Null();return;}
                Count(91);
                Need(value.PolicyId);
                V(value.PolicyId);
                Need(value.ContentHash);
                V(value.ContentHash);
                Need(value.Definitions);
                V(value.Definitions);
                Need(value.InitialPlanets);
                V(value.InitialPlanets);
                Need(value.Bodies);
                V(value.Bodies);
                Need(value.GenesisControls);
                V(value.GenesisControls);
            }
            private void V(global::HobbyDS9.Contracts.GalaxyEdgeKind value){Count(11);}
            private void V(global::HobbyDS9.Contracts.GalaxyEnvironmentFeatureKind value){Count(11);}
            private void V(global::HobbyDS9.Contracts.GameStateDto? value)
            {
                if(value==null){Null();return;}
                Count(415);
                V(value.Phase);
                V(value.ResearchPaceFactor);
                Need(value.LastResearchReceipts);
                V(value.LastResearchReceipts);
                V(value.TurnNumber);
                V(value.StateRevision);
                V(value.NextEntitySequence);
                Need(value.RngState);
                V(value.RngState);
                Need(value.EntityIdentities);
                V(value.EntityIdentities);
                Need(value.AcceptedCommandIds);
                V(value.AcceptedCommandIds);
                Need(value.Civilizations);
                V(value.Civilizations);
                Need(value.StarSystems);
                V(value.StarSystems);
                Need(value.NavigationKnowledge);
                V(value.NavigationKnowledge);
                Need(value.TraversalEffects);
                V(value.TraversalEffects);
                V(value.NavigationStatistics);
                Need(value.Planets);
                V(value.Planets);
                Need(value.Displacements);
                V(value.Displacements);
                Need(value.LastDeliveries);
                V(value.LastDeliveries);
                V(value.PopulationStatistics);
                Need(value.Colonies);
                V(value.Colonies);
                Need(value.Blueprints);
                V(value.Blueprints);
                Need(value.Ships);
                V(value.Ships);
                Need(value.Fleets);
                V(value.Fleets);
                Need(value.PendingCombats);
                V(value.PendingCombats);
                Need(value.ResolvedCombats);
                V(value.ResolvedCombats);
            }
            private void V(global::HobbyDS9.Contracts.GeneratedGalaxyHomeCandidateDto? value)
            {
                if(value==null){Null();return;}
                Count(52);
                Need(value.CivilizationDefinitionId);
                V(value.CivilizationDefinitionId);
                Need(value.SystemId);
                V(value.SystemId);
                Need(value.NodeId);
                V(value.NodeId);
            }
            private void V(global::HobbyDS9.Contracts.GeneratedGalaxyPointDto? value)
            {
                if(value==null){Null();return;}
                Count(12);
                V(value.X);
                V(value.Y);
            }
            private void V(global::HobbyDS9.Contracts.GeneratedGalaxyRegionDto? value)
            {
                if(value==null){Null();return;}
                Count(61);
                Need(value.NodeId);
                V(value.NodeId);
                Need(value.DisplayName);
                V(value.DisplayName);
                V(value.SystemId);
                Need(value.Center);
                V(value.Center);
                Need(value.Boundary);
                V(value.Boundary);
            }
            private void V(global::HobbyDS9.Contracts.GeneratedGalaxySystemDto? value)
            {
                if(value==null){Null();return;}
                Count(39);
                Need(value.SystemId);
                V(value.SystemId);
                Need(value.DisplayName);
                V(value.DisplayName);
                Need(value.NodeId);
                V(value.NodeId);
            }
            private void V(global::HobbyDS9.Contracts.HabitatDefinitionDto? value)
            {
                if(value==null){Null();return;}
                Count(41);
                V(value.Kind);
                V(value.ShareBps);
                V(value.NaturalCapacity);
            }
            private void V(global::HobbyDS9.Contracts.HabitatKind value){Count(11);}
            private void V(global::HobbyDS9.Contracts.InfrastructureStateDto? value)
            {
                if(value==null){Null();return;}
                Count(27);
                Need(value.DefinitionId);
                V(value.DefinitionId);
                V(value.Count);
            }
            private void V(global::HobbyDS9.Contracts.LandedCohortLineageDto? value)
            {
                if(value==null){Null();return;}
                Count(62);
                Need(value.ManifestId);
                V(value.ManifestId);
                Need(value.ColonyId);
                V(value.ColonyId);
                Need(value.CohortId);
                V(value.CohortId);
                V(value.Population);
                V(value.Turn);
            }
            private void V(global::HobbyDS9.Contracts.LegalStatus value){Count(11);}
            private void V(global::HobbyDS9.Contracts.LoadCarriedSuppliesDto? value)
            {
                if(value==null){Null();return;}
                Count(51);
                Need(value.ShipId);
                V(value.ShipId);
                Need(value.SourceColonyId);
                V(value.SourceColonyId);
                V(value.Food);
                V(value.Materials);
            }
            private void V(global::HobbyDS9.Contracts.LogisticsDeliveryDto? value)
            {
                if(value==null){Null();return;}
                Count(184);
                Need(value.OriginPlanetId);
                V(value.OriginPlanetId);
                Need(value.DestinationPlanetId);
                V(value.DestinationPlanetId);
                Need(value.ResourceDefinitionId);
                V(value.ResourceDefinitionId);
                V(value.RouteNodeIds);
                V(value.ImportNeed);
                V(value.ReachableSurplus);
                V(value.RouteCapacity);
                V(value.StabilityFactor);
                V(value.RouteIntegrity);
                V(value.Delivered);
            }
            private void V(global::HobbyDS9.Contracts.ManifestOriginKind value){Count(11);}
            private void V(global::HobbyDS9.Contracts.ManifestOriginLineageDto? value)
            {
                if(value==null){Null();return;}
                Count(143);
                Need(value.ManifestId);
                V(value.ManifestId);
                Need(value.SourceColonyId);
                V(value.SourceColonyId);
                V(value.SourceCohortId);
                V(value.EmbarkedCohortId);
                V(value.Kind);
                V(value.EmbarkedPopulation);
                V(value.Turn);
                V(value.LoadedFood);
                V(value.LoadedMaterials);
            }
            private void V(global::HobbyDS9.Contracts.ManifestResolutionDto? value)
            {
                if(value==null){Null();return;}
                Count(163);
                Need(value.ManifestId);
                V(value.ManifestId);
                V(value.Stage);
                V(value.Turn);
                V(value.DestinationPlanetId);
                V(value.PopulationTransferred);
                V(value.PopulationLost);
                V(value.FoodTransferred);
                V(value.FoodLost);
                V(value.CargoTransferred);
                V(value.CargoLost);
            }
            private void V(global::HobbyDS9.Contracts.NaturalGalaxyDocumentDto? value)
            {
                if(value==null){Null();return;}
                Count(162);
                V(value.FormatVersion);
                Need(value.ProfileId);
                V(value.ProfileId);
                Need(value.GeneratorVersion);
                V(value.GeneratorVersion);
                Need(value.TuningVersion);
                V(value.TuningVersion);
                Need(value.ContentHash);
                V(value.ContentHash);
                Need(value.Settings);
                V(value.Settings);
                Need(value.Tuning);
                V(value.Tuning);
                Need(value.Features);
                V(value.Features);
                Need(value.StarGroups);
                V(value.StarGroups);
                Need(value.Layout);
                V(value.Layout);
                Need(value.RegionInfluences);
                V(value.RegionInfluences);
            }
            private void V(global::HobbyDS9.Contracts.NaturalGalaxySettingsDto? value)
            {
                if(value==null){Null();return;}
                Count(67);
                Need(value.Map);
                V(value.Map);
                V(value.IsolatedStarSharePermille);
                V(value.AssociationSpreadPermille);
            }
            private void V(global::HobbyDS9.Contracts.NaturalGalaxyTuningDto? value)
            {
                if(value==null){Null();return;}
                Count(303);
                Need(value.Environment);
                V(value.Environment);
                V(value.IsolatedStarSeparation);
                V(value.MinimumAssociationSize);
                V(value.MaximumAssociationSize);
                V(value.MinimumAssociationRadius);
                V(value.MaximumAssociationRadius);
                V(value.MinimumAssociationSeparation);
                V(value.MaximumAssociationSeparation);
                V(value.ExtraSiteSeparation);
                V(value.StellarPlacementPressurePermille);
                V(value.QuietDisturbanceThreshold);
            }
            private void V(global::HobbyDS9.Contracts.NaturalStarGroupDto? value)
            {
                if(value==null){Null();return;}
                Count(71);
                Need(value.GroupId);
                V(value.GroupId);
                V(value.Kind);
                Need(value.Center);
                V(value.Center);
                V(value.RadiusX);
                V(value.RadiusY);
                Need(value.StarFeatureIds);
                V(value.StarFeatureIds);
            }
            private void V(global::HobbyDS9.Contracts.NaturalStarGroupKind value){Count(11);}
            private void V(global::HobbyDS9.Contracts.NavigationConnectionDto? value)
            {
                if(value==null){Null();return;}
                Count(73);
                Need(value.ConnectionId);
                V(value.ConnectionId);
                Need(value.EndpointA);
                V(value.EndpointA);
                Need(value.EndpointB);
                V(value.EndpointB);
                V(value.Kind);
                V(value.BaseTraversalCost);
            }
            private void V(global::HobbyDS9.Contracts.NavigationKnowledgeEntryDto? value)
            {
                if(value==null){Null();return;}
                Count(17);
                Need(value.Id);
                V(value.Id);
                V(value.Level);
            }
            private void V(global::HobbyDS9.Contracts.NavigationKnowledgeLevel value){Count(11);}
            private void V(global::HobbyDS9.Contracts.NavigationStatisticsDto? value)
            {
                if(value==null){Null();return;}
                Count(66);
                V(value.TraversalCostPaid);
                V(value.EdgesCompleted);
                V(value.PartialTraversalTurns);
            }
            private void V(global::HobbyDS9.Contracts.PendingCombatDto? value)
            {
                if(value==null){Null();return;}
                Count(13);
                Need(value.Request);
                V(value.Request);
            }
            private void V(global::HobbyDS9.Contracts.PlanetBodyBindingDto? value)
            {
                if(value==null){Null();return;}
                Count(52);
                Need(value.PlanetId);
                V(value.PlanetId);
                Need(value.BodyId);
                V(value.BodyId);
                Need(value.SystemId);
                V(value.SystemId);
                V(value.MinorActorId);
            }
            private void V(global::HobbyDS9.Contracts.PlanetControlDto? value)
            {
                if(value==null){Null();return;}
                Count(33);
                Need(value.PlanetId);
                V(value.PlanetId);
                V(value.Kind);
                V(value.ActorId);
            }
            private void V(global::HobbyDS9.Contracts.PlanetControllerKind value){Count(11);}
            private void V(global::HobbyDS9.Contracts.PlanetDefinitionDto? value)
            {
                if(value==null){Null();return;}
                Count(144);
                Need(value.DefinitionId);
                V(value.DefinitionId);
                Need(value.DisplayName);
                V(value.DisplayName);
                Need(value.SystemDefinitionId);
                V(value.SystemDefinitionId);
                Need(value.PlanetClass);
                V(value.PlanetClass);
                V(value.Gravity);
                V(value.Pressure);
                V(value.Radiation);
                Need(value.Atmosphere);
                V(value.Atmosphere);
                V(value.Habitats);
                V(value.Deposits);
            }
            private void V(global::HobbyDS9.Contracts.PlanetStateDto? value)
            {
                if(value==null){Null();return;}
                Count(63);
                Need(value.PlanetId);
                V(value.PlanetId);
                Need(value.DefinitionId);
                V(value.DefinitionId);
                Need(value.SystemId);
                V(value.SystemId);
                V(value.RemainingDeposits);
            }
            private void V(global::HobbyDS9.Contracts.PlanetSurveyDto? value)
            {
                if(value==null){Null();return;}
                Count(151);
                Need(value.SurveyId);
                V(value.SurveyId);
                Need(value.ActorId);
                V(value.ActorId);
                Need(value.PlanetId);
                V(value.PlanetId);
                Need(value.ShipId);
                V(value.ShipId);
                V(value.Stage);
                V(value.CompletedWeeks);
                V(value.StartedTurn);
                V(value.ResolvedTurn);
                V(value.LastObservedControl);
                V(value.ControlObservedTurn);
            }
            private void V(global::HobbyDS9.Contracts.PlanetSurveyOrderDto? value)
            {
                if(value==null){Null();return;}
                Count(24);
                Need(value.ShipId);
                V(value.ShipId);
                Need(value.PlanetId);
                V(value.PlanetId);
            }
            private void V(global::HobbyDS9.Contracts.PlanetSurveyStage value){Count(11);}
            private void V(global::HobbyDS9.Contracts.PoliticalReasonDto? value)
            {
                if(value==null){Null();return;}
                Count(114);
                Need(value.Code);
                V(value.Code);
                V(value.SourceEntityId);
                V(value.SatisfactionChange);
                V(value.IntegrationChange);
                V(value.AlignmentChange);
                V(value.RadicalisationChange);
            }
            private void V(global::HobbyDS9.Contracts.PopulationCohortDto? value)
            {
                if(value==null){Null();return;}
                Count(219);
                Need(value.CohortId);
                V(value.CohortId);
                Need(value.SpeciesDefinitionId);
                V(value.SpeciesDefinitionId);
                V(value.Population);
                V(value.LegalStatusEligibleTurns);
                V(value.LegalStatus);
                V(value.OriginCivilizationId);
                V(value.Satisfaction);
                V(value.Integration);
                V(value.CivicAlignment);
                V(value.Radicalisation);
                V(value.ExternalAffinities);
                V(value.Memories);
            }
            private void V(global::HobbyDS9.Contracts.PopulationMemoryDto? value)
            {
                if(value==null){Null();return;}
                Count(84);
                Need(value.Kind);
                V(value.Kind);
                V(value.Magnitude);
                V(value.SourceEntityId);
                V(value.StartTurn);
                V(value.ExpiryTurn);
                V(value.DecayPerTurn);
            }
            private void V(global::HobbyDS9.Contracts.PopulationStatisticsDto? value)
            {
                if(value==null){Null();return;}
                Count(153);
                V(value.Births);
                V(value.Deaths);
                V(value.Migrated);
                V(value.RefugeesMoved);
                V(value.FoodDelivered);
                V(value.ResourceExtracted);
                V(value.ResourceDelivered);
                V(value.ResourceConsumed);
                V(value.ProductionCompleted);
            }
            private void V(global::HobbyDS9.Contracts.ProductionQueueItemDto? value)
            {
                if(value==null){Null();return;}
                Count(129);
                V(value.Blueprint);
                Need(value.QueueItemId);
                V(value.QueueItemId);
                Need(value.ShipDesignDefinitionId);
                V(value.ShipDesignDefinitionId);
                V(value.RequiredIndustry);
                V(value.CompletedIndustry);
                V(value.PaidResources);
                V(value.PaidCredits);
            }
            private void V(global::HobbyDS9.Contracts.PublishShipBlueprintPayload? value)
            {
                if(value==null){Null();return;}
                Count(38);
                Need(value.BlueprintId);
                V(value.BlueprintId);
                V(value.Revision);
                V(value.Draft);
            }
            private void V(global::HobbyDS9.Contracts.ResearchReceiptDto? value)
            {
                if(value==null){Null();return;}
                Count(128);
                Need(value.CivilizationId);
                V(value.CivilizationId);
                Need(value.Category);
                V(value.Category);
                V(value.Discipline);
                V(value.IntervalStartLevel);
                V(value.BreakthroughOrdinal);
                V(value.ApplicationDefinitionId);
                V(value.Amount);
            }
            private void V(global::HobbyDS9.Contracts.ResearchStatisticsDto? value)
            {
                if(value==null){Null();return;}
                Count(220);
                V(value.TotalResearchGenerated);
                V(value.TotalScienceAllocated);
                V(value.TotalEngineeringAllocated);
                V(value.TotalScienceSpent);
                V(value.TotalEngineeringSpent);
                V(value.TotalBreakthroughs);
                V(value.TotalScienceLevelsCompleted);
                V(value.TotalEngineeringApplicationsMatured);
            }
            private void V(global::HobbyDS9.Contracts.ResistanceState value){Count(11);}
            private void V(global::HobbyDS9.Contracts.ResolvedCombatDto? value)
            {
                if(value==null){Null();return;}
                Count(23);
                Need(value.Request);
                V(value.Request);
                Need(value.Result);
                V(value.Result);
            }
            private void V(global::HobbyDS9.Contracts.ResourceAmountDto? value)
            {
                if(value==null){Null();return;}
                Count(36);
                Need(value.ResourceDefinitionId);
                V(value.ResourceDefinitionId);
                V(value.Amount);
            }
            private void V(global::HobbyDS9.Contracts.RngStateDto? value)
            {
                if(value==null){Null();return;}
                Count(46);
                Need(value.AlgorithmId);
                V(value.AlgorithmId);
                V(value.AlgorithmVersion);
                V(value.State);
            }
            private void V(global::HobbyDS9.Contracts.SaveEnvelopeDto? value)
            {
                if(value==null){Null();return;}
                Count(81);
                V(value.SaveFormatVersion);
                Need(value.RulesVersion);
                V(value.RulesVersion);
                Need(value.ScenarioId);
                V(value.ScenarioId);
                Need(value.ContentHash);
                V(value.ContentHash);
                Need(value.GameState);
                V(value.GameState);
            }
            private void V(global::HobbyDS9.Contracts.ScaledGalaxySettingsDto? value)
            {
                if(value==null){Null();return;}
                Count(266);
                Need(value.Seed);
                V(value.Seed);
                V(value.SystemCount);
                V(value.EmptyRegionCount);
                V(value.Width);
                V(value.Height);
                V(value.QuietEmptyAreaTargetPermille);
                V(value.BlackHolesPerBaselineArea);
                V(value.NebulaePerBaselineArea);
                Need(value.HomeCivilizationDefinitionIds);
                V(value.HomeCivilizationDefinitionIds);
                V(value.StellarInfluenceEnabled);
                V(value.BlackHoleInfluenceEnabled);
                V(value.NebulaInfluenceEnabled);
            }
            private void V(global::HobbyDS9.Contracts.ScienceAllocationDto? value)
            {
                if(value==null){Null();return;}
                Count(26);
                V(value.Discipline);
                V(value.Weight);
            }
            private void V(global::HobbyDS9.Contracts.ScienceDiscipline value){Count(11);}
            private void V(global::HobbyDS9.Contracts.ScienceDisciplineStateDto? value)
            {
                if(value==null){Null();return;}
                Count(66);
                V(value.Discipline);
                V(value.Level);
                V(value.ProgressTowardNextLevel);
                V(value.TotalSpent);
            }
            private void V(global::HobbyDS9.Contracts.SessionPhase value){Count(11);}
            private void V(global::HobbyDS9.Contracts.SetEngineeringTargetPayload? value)
            {
                if(value==null){Null();return;}
                Count(24);
                V(value.TargetDefinitionId);
            }
            private void V(global::HobbyDS9.Contracts.SetFleetRoutePayload? value)
            {
                if(value==null){Null();return;}
                Count(29);
                Need(value.FleetId);
                V(value.FleetId);
                V(value.RouteNodeIds);
            }
            private void V(global::HobbyDS9.Contracts.SetResearchAllocationPayload? value)
            {
                if(value==null){Null();return;}
                Count(28);
                V(value.ScienceAllocationUnits);
            }
            private void V(global::HobbyDS9.Contracts.SetScienceAllocationPayload? value)
            {
                if(value==null){Null();return;}
                Count(17);
                V(value.Allocations);
            }
            private void V(global::HobbyDS9.Contracts.SetWorldGovernancePolicyPayload? value)
            {
                if(value==null){Null();return;}
                Count(36);
                Need(value.ColonyId);
                V(value.ColonyId);
                Need(value.PolicyDefinitionId);
                V(value.PolicyDefinitionId);
            }
            private void V(global::HobbyDS9.Contracts.SettlementCampDto? value)
            {
                if(value==null){Null();return;}
                Count(149);
                Need(value.CampId);
                V(value.CampId);
                Need(value.ColonyId);
                V(value.ColonyId);
                Need(value.ManifestId);
                V(value.ManifestId);
                Need(value.OwnerId);
                V(value.OwnerId);
                V(value.Stage);
                V(value.DeployedTurn);
                V(value.FedWeeks);
                V(value.CompletedIndustry);
                V(value.FoodConsumed);
                V(value.Deaths);
                Need(value.LatestReport);
                V(value.LatestReport);
            }
            private void V(global::HobbyDS9.Contracts.SettlementCampTerminalDto? value)
            {
                if(value==null){Null();return;}
                Count(48);
                Need(value.CampId);
                V(value.CampId);
                V(value.Kind);
                V(value.Turn);
                V(value.ReturnManifestId);
            }
            private void V(global::HobbyDS9.Contracts.SettlementDeploymentIntentDto? value)
            {
                if(value==null){Null();return;}
                Count(63);
                Need(value.IntentId);
                V(value.IntentId);
                Need(value.ManifestId);
                V(value.ManifestId);
                Need(value.PlanetId);
                V(value.PlanetId);
                Need(value.ActorId);
                V(value.ActorId);
                V(value.Disputed);
            }
            private void V(global::HobbyDS9.Contracts.SettlementTerminalKind value){Count(11);}
            private void V(global::HobbyDS9.Contracts.SettlerManifestDto? value)
            {
                if(value==null){Null();return;}
                Count(328);
                Need(value.ManifestId);
                V(value.ManifestId);
                Need(value.OwnerId);
                V(value.OwnerId);
                Need(value.ShipId);
                V(value.ShipId);
                V(value.Kind);
                V(value.Stage);
                V(value.Blueprint);
                Need(value.FitId);
                V(value.FitId);
                Need(value.SupportHash);
                V(value.SupportHash);
                V(value.Passengers);
                V(value.Food);
                V(value.FoodShortageWeeks);
                V(value.Cargo);
                V(value.FoodConsumed);
                V(value.FoodDelivered);
                V(value.FoodLost);
                V(value.PassengerDeaths);
                V(value.PopulationTransferred);
                V(value.EquipmentConsumed);
                V(value.MaterialsDelivered);
                V(value.MaterialsLost);
                V(value.LastSurvivalTurn);
                Need(value.LatestReport);
                V(value.LatestReport);
            }
            private void V(global::HobbyDS9.Contracts.SettlerManifestKind value){Count(11);}
            private void V(global::HobbyDS9.Contracts.SettlerManifestStage value){Count(11);}
            private void V(global::HobbyDS9.Contracts.ShipCombatChangeDto? value)
            {
                if(value==null){Null();return;}
                Count(29);
                Need(value.ShipId);
                V(value.ShipId);
                V(value.RemainingHull);
            }
            private void V(global::HobbyDS9.Contracts.ShipCombatProfileDto? value)
            {
                if(value==null){Null();return;}
                Count(109);
                V(value.ShieldCapacity);
                V(value.LongRangeAttack);
                V(value.MediumRangeAttack);
                V(value.CloseRangeAttack);
                V(value.AccuracyBps);
                V(value.EvasionBps);
            }
            private void V(global::HobbyDS9.Contracts.ShipDesign.ComponentKind value){Count(11);}
            private void V(global::HobbyDS9.Contracts.ShipDesign.ComponentOperatingModeDto? value)
            {
                if(value==null){Null();return;}
                Count(77);
                V(value.Profile);
                V(value.FullPower);
                V(value.MinimumPower);
                V(value.Scaling);
                V(value.Essential);
                V(value.Outputs);
            }
            private void V(global::HobbyDS9.Contracts.ShipDesign.ComponentVersionDto? value)
            {
                if(value==null){Null();return;}
                Count(286);
                Need(value.VersionId);
                V(value.VersionId);
                Need(value.FamilyId);
                V(value.FamilyId);
                V(value.VersionNumber);
                V(value.ParentVersionId);
                Need(value.DisplayName);
                V(value.DisplayName);
                V(value.Kind);
                V(value.Capacity);
                V(value.CrewRequired);
                V(value.ComputerDemand);
                V(value.PowerSupply);
                V(value.EnvelopeSize);
                V(value.CompatibleHullFamilyIds);
                V(value.RequiredHullTags);
                V(value.Tags);
                V(value.RequiredComponentTags);
                V(value.Mounts);
                V(value.Dependencies);
                V(value.Modes);
                V(value.Cost);
            }
            private void V(global::HobbyDS9.Contracts.ShipDesign.HullFamilyDto? value)
            {
                if(value==null){Null();return;}
                Count(29);
                Need(value.FamilyId);
                V(value.FamilyId);
                Need(value.DisplayName);
                V(value.DisplayName);
            }
            private void V(global::HobbyDS9.Contracts.ShipDesign.HullVersionDto? value)
            {
                if(value==null){Null();return;}
                Count(265);
                Need(value.VersionId);
                V(value.VersionId);
                Need(value.FamilyId);
                V(value.FamilyId);
                V(value.VersionNumber);
                V(value.ParentVersionId);
                Need(value.DisplayName);
                V(value.DisplayName);
                V(value.Capacity);
                V(value.MaxHull);
                V(value.DesignLoad);
                V(value.MinimumCrew);
                V(value.PowerDistributionLimit);
                V(value.MaxReactors);
                V(value.MaxPropulsionPackages);
                V(value.ReactorSizeLimit);
                V(value.PropulsionSizeLimit);
                V(value.Tags);
                V(value.Mounts);
                V(value.Cost);
            }
            private void V(global::HobbyDS9.Contracts.ShipDesign.MountKind value){Count(11);}
            private void V(global::HobbyDS9.Contracts.ShipDesign.MountLimitDto? value)
            {
                if(value==null){Null();return;}
                Count(54);
                Need(value.Id);
                V(value.Id);
                V(value.Kind);
                V(value.SizeRating);
                V(value.Count);
                Need(value.CoverageTag);
                V(value.CoverageTag);
            }
            private void V(global::HobbyDS9.Contracts.ShipDesign.MountRequirementDto? value)
            {
                if(value==null){Null();return;}
                Count(48);
                V(value.Kind);
                V(value.SizeRating);
                V(value.Count);
                Need(value.CoverageTag);
                V(value.CoverageTag);
            }
            private void V(global::HobbyDS9.Contracts.ShipDesign.OperatingProfileKind value){Count(11);}
            private void V(global::HobbyDS9.Contracts.ShipDesign.PowerScaling value){Count(11);}
            private void V(global::HobbyDS9.Contracts.ShipDesign.ProfilePowerAllocationDto? value)
            {
                if(value==null){Null();return;}
                Count(56);
                Need(value.InstallationId);
                V(value.InstallationId);
                V(value.RequestedPower);
                V(value.AllocatedPower);
            }
            private void V(global::HobbyDS9.Contracts.ShipDesign.RoleRecommendationDto? value)
            {
                if(value==null){Null();return;}
                Count(34);
                V(value.Profile);
                V(value.Output);
                V(value.Minimum);
            }
            private void V(global::HobbyDS9.Contracts.ShipDesign.ShipClassDraftDto? value)
            {
                if(value==null){Null();return;}
                Count(100);
                Need(value.ClassId);
                V(value.ClassId);
                Need(value.DisplayName);
                V(value.DisplayName);
                Need(value.RoleId);
                V(value.RoleId);
                Need(value.HullVersionId);
                V(value.HullVersionId);
                V(value.EmbarkedCrew);
                V(value.Installations);
                V(value.Profiles);
            }
            private void V(global::HobbyDS9.Contracts.ShipDesign.ShipDesignCatalogueDto? value)
            {
                if(value==null){Null();return;}
                Count(67);
                V(value.FormatVersion);
                V(value.HullFamilies);
                V(value.Hulls);
                V(value.Components);
                V(value.Roles);
            }
            private void V(global::HobbyDS9.Contracts.ShipDesign.ShipDesignCostDto? value)
            {
                if(value==null){Null();return;}
                Count(53);
                V(value.Credits);
                V(value.Industry);
                V(value.Maintenance);
                V(value.Resources);
            }
            private void V(global::HobbyDS9.Contracts.ShipDesign.ShipInstallationDto? value)
            {
                if(value==null){Null();return;}
                Count(54);
                Need(value.InstallationId);
                V(value.InstallationId);
                Need(value.ComponentVersionId);
                V(value.ComponentVersionId);
                V(value.Quantity);
            }
            private void V(global::HobbyDS9.Contracts.ShipDesign.ShipOperatingProfileDto? value)
            {
                if(value==null){Null();return;}
                Count(25);
                V(value.Kind);
                V(value.Allocations);
            }
            private void V(global::HobbyDS9.Contracts.ShipDesign.ShipOutputDto? value)
            {
                if(value==null){Null();return;}
                Count(20);
                V(value.Kind);
                V(value.Amount);
            }
            private void V(global::HobbyDS9.Contracts.ShipDesign.ShipOutputKind value){Count(11);}
            private void V(global::HobbyDS9.Contracts.ShipDesign.ShipRoleDto? value)
            {
                if(value==null){Null();return;}
                Count(46);
                Need(value.RoleId);
                V(value.RoleId);
                Need(value.DisplayName);
                V(value.DisplayName);
                V(value.Recommendations);
            }
            private void V(global::HobbyDS9.Contracts.ShipDesignDefinitionDto? value)
            {
                if(value==null){Null();return;}
                Count(173);
                Need(value.DefinitionId);
                V(value.DefinitionId);
                Need(value.DisplayName);
                V(value.DisplayName);
                V(value.MovementBudget);
                V(value.IndustryCost);
                V(value.ResourceCosts);
                V(value.CreditCost);
                V(value.MaxHull);
                V(value.CombatProfile);
                V(value.RequiredEngineeringApplicationDefinitionIds);
            }
            private void V(global::HobbyDS9.Contracts.ShipStateDto? value)
            {
                if(value==null){Null();return;}
                Count(100);
                V(value.Blueprint);
                Need(value.ShipId);
                V(value.ShipId);
                Need(value.ShipDesignDefinitionId);
                V(value.ShipDesignDefinitionId);
                Need(value.OwnerCivilizationId);
                V(value.OwnerCivilizationId);
                Need(value.FleetId);
                V(value.FleetId);
                V(value.CurrentHull);
            }
            private void V(global::HobbyDS9.Contracts.StarSystemStateDto? value)
            {
                if(value==null){Null();return;}
                Count(53);
                Need(value.SystemId);
                V(value.SystemId);
                Need(value.DefinitionId);
                V(value.DefinitionId);
                V(value.OwnerCivilizationId);
            }
            private void V(global::HobbyDS9.Contracts.SubmitCampaignFleetOrganizationDto? value)
            {
                if(value==null){Null();return;}
                Count(113);
                Need(value.ActorId);
                V(value.ActorId);
                Need(value.CommandId);
                V(value.CommandId);
                V(value.ExpectedTurn);
                V(value.ExpectedRevision);
                V(value.Kind);
                V(value.ShipIds);
                V(value.TargetFleetId);
                V(value.DisplayName);
            }
            private void V(global::HobbyDS9.Contracts.SubmitCampaignTravelOrderDto? value)
            {
                if(value==null){Null();return;}
                Count(71);
                Need(value.ActorId);
                V(value.ActorId);
                Need(value.CommandId);
                V(value.CommandId);
                V(value.ExpectedTurn);
                V(value.ExpectedRevision);
                V(value.Order);
            }
            private void V(global::HobbyDS9.Contracts.TacticalArc value){Count(11);}
            private void V(global::HobbyDS9.Contracts.TacticalCompletion value){Count(11);}
            private void V(global::HobbyDS9.Contracts.TacticalEncounterV2Dto? value)
            {
                if(value==null){Null();return;}
                Count(176);
                V(value.FormatVersion);
                Need(value.ResolverId);
                V(value.ResolverId);
                V(value.ResolverVersion);
                Need(value.RulesId);
                V(value.RulesId);
                Need(value.RandomAlgorithmId);
                V(value.RandomAlgorithmId);
                Need(value.EncounterId);
                V(value.EncounterId);
                V(value.RandomState);
                V(value.NextRound);
                V(value.Completion);
                Need(value.Rules);
                V(value.Rules);
                Need(value.Formations);
                V(value.Formations);
                Need(value.Geometry);
                V(value.Geometry);
            }
            private void V(global::HobbyDS9.Contracts.TacticalFormationStatus value){Count(11);}
            private void V(global::HobbyDS9.Contracts.TacticalFormationV2Dto? value)
            {
                if(value==null){Null();return;}
                Count(200);
                Need(value.FormationId);
                V(value.FormationId);
                Need(value.SideId);
                V(value.SideId);
                V(value.Speed);
                V(value.Agility);
                V(value.Sensors);
                V(value.Command);
                V(value.Cohesion);
                V(value.Status);
                Need(value.StandingOrder);
                V(value.StandingOrder);
                V(value.Objective);
                V(value.ObjectiveRequiredRounds);
                V(value.ObjectiveProgress);
                V(value.ObjectiveCompleted);
                Need(value.Ships);
                V(value.Ships);
            }
            private void V(global::HobbyDS9.Contracts.TacticalGeometryV2Dto? value)
            {
                if(value==null){Null();return;}
                Count(61);
                Need(value.FormationA);
                V(value.FormationA);
                Need(value.FormationB);
                V(value.FormationB);
                V(value.Range);
                V(value.FacingA);
                V(value.FacingB);
            }
            private void V(global::HobbyDS9.Contracts.TacticalManeuver value){Count(11);}
            private void V(global::HobbyDS9.Contracts.TacticalMountV2Dto? value)
            {
                if(value==null){Null();return;}
                Count(49);
                Need(value.MountId);
                V(value.MountId);
                V(value.WeaponClass);
                V(value.Coverage);
                V(value.Slots);
            }
            private void V(global::HobbyDS9.Contracts.TacticalObjective value){Count(11);}
            private void V(global::HobbyDS9.Contracts.TacticalOrderV2Dto? value)
            {
                if(value==null){Null();return;}
                Count(110);
                Need(value.FormationId);
                V(value.FormationId);
                V(value.Maneuver);
                V(value.TargetPriority);
                V(value.PreferredRange);
                V(value.TargetFormationId);
                V(value.ProtectedFormationId);
            }
            private void V(global::HobbyDS9.Contracts.TacticalRange value){Count(11);}
            private void V(global::HobbyDS9.Contracts.TacticalRulesV2Dto? value)
            {
                if(value==null){Null();return;}
                Count(343);
                V(value.SpeedWeight);
                V(value.AgilityWeight);
                V(value.SensorsWeight);
                V(value.CommandWeight);
                V(value.CohesionWeight);
                V(value.ManeuverTieMargin);
                V(value.ArmorNumerator);
                V(value.ArmorDenominator);
                V(value.DistantTorpedoHitPenaltyBps);
                V(value.DistantInterceptionBonusBps);
                V(value.InterceptionBaseBps);
                V(value.ScreenDefenseBps);
                V(value.WithdrawalDefenseBps);
                V(value.HoldDefenseBps);
                V(value.ChargeDefenseBps);
                V(value.SuppressRechargeOnHitSector);
            }
            private void V(global::HobbyDS9.Contracts.TacticalSector value){Count(11);}
            private void V(global::HobbyDS9.Contracts.TacticalShieldV2Dto? value)
            {
                if(value==null){Null();return;}
                Count(34);
                V(value.Sector);
                V(value.Maximum);
                V(value.Current);
            }
            private void V(global::HobbyDS9.Contracts.TacticalShipRole value){Count(11);}
            private void V(global::HobbyDS9.Contracts.TacticalShipV2Dto? value)
            {
                if(value==null){Null();return;}
                Count(182);
                Need(value.ShipId);
                V(value.ShipId);
                V(value.Role);
                V(value.Hull);
                V(value.MaxHull);
                V(value.Armor);
                V(value.EvasionBps);
                V(value.PowerBudget);
                V(value.ShieldPowerRequired);
                V(value.ShieldPowerAllocated);
                V(value.ShieldRechargePerRound);
                Need(value.Shields);
                V(value.Shields);
                Need(value.Mounts);
                V(value.Mounts);
                Need(value.Weapons);
                V(value.Weapons);
            }
            private void V(global::HobbyDS9.Contracts.TacticalTargetPriority value){Count(11);}
            private void V(global::HobbyDS9.Contracts.TacticalWeaponClass value){Count(11);}
            private void V(global::HobbyDS9.Contracts.TacticalWeaponV2Dto? value)
            {
                if(value==null){Null();return;}
                Count(226);
                Need(value.WeaponId);
                V(value.WeaponId);
                Need(value.MountId);
                V(value.MountId);
                V(value.Class);
                V(value.Coverage);
                V(value.Equipped);
                V(value.RequiredPower);
                V(value.AllocatedPower);
                V(value.Damage);
                V(value.AccuracyBps);
                V(value.TrackingBps);
                V(value.OpportunitiesPerRound);
                V(value.CadenceRounds);
                V(value.NextFireRound);
                V(value.MagazineCapacity);
                V(value.Ammunition);
            }
            private void V(global::HobbyDS9.Contracts.TraversalEffectDto? value)
            {
                if(value==null){Null();return;}
                Count(114);
                Need(value.EffectId);
                V(value.EffectId);
                V(value.NodeId);
                V(value.ConnectionId);
                V(value.TraversalCostIncrease);
                V(value.MovementBudgetPenalty);
                V(value.StartTurn);
                V(value.EndTurn);
            }
            private void V(global::HobbyDS9.Contracts.UnloadSettlersDto? value)
            {
                if(value==null){Null();return;}
                Count(28);
                Need(value.ManifestId);
                V(value.ManifestId);
                Need(value.ColonyId);
                V(value.ColonyId);
            }
            private void V(global::HobbyDS9.Contracts.WithdrawDeploymentDto? value)
            {
                if(value==null){Null();return;}
                Count(14);
                Need(value.IntentId);
                V(value.IntentId);
            }
            private void V(global::HobbyDS9.Contracts.WorldPoliticalStateDto? value)
            {
                if(value==null){Null();return;}
                Count(222);
                Need(value.GovernancePolicyDefinitionId);
                V(value.GovernancePolicyDefinitionId);
                V(value.Resistance);
                V(value.Pressure);
                V(value.PreviousPressure);
                V(value.EscalationTurns);
                V(value.RecoveryTurns);
                V(value.OrganizedTurns);
                V(value.DisruptionRemainingTurns);
                V(value.Disruptions);
                V(value.LastUpdateTurn);
                V(value.CohortReports);
                V(value.Events);
            }
            private void V(bool value){Count(5);}
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.ActivityBodyDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.ActivityFindingDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.ActivityMinorRelationDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.ActivityMinorStateDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.ActivityProductionItemDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.ActivitySiteObservationDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.ActivitySiteStateDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.ActivityStructureReceiptDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.AuthoredTacticalAmmoStockDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.AuthoredTacticalShieldStockDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.BlueprintRevisionDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.CampaignContactKnowledgeDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.CampaignFleetNameDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.CampaignFleetTravelReceiptDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.CampaignFrontierReceiptDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.CampaignIntegratedBuiltShipDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.CampaignPlayableOperationDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.CampaignPublishedBuiltShipDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.CampaignTacticalActorReportDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.CampaignTacticalBattleCheckpointDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.CampaignTacticalCommitmentDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.CampaignTacticalFleetOrderDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.CampaignTacticalFleetSettingDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.CampaignTacticalPoolDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.CampaignTacticalResolvedRoundDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.CampaignTravelOrderDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.CivilizationNavigationKnowledgeDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.CivilizationStateDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.CohortPoliticalReportDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.ColonisationColonyStageDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.ColonisationDeploymentReceiptDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.ColonisationOperationDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.ColonisationProductionItemDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.ColonisationProductionReceiptDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.ColonyOriginDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.ColonyStateDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.CombatEventDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.CombatParticipantDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.CombatRequestDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.CombatShipDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.DisplacementDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.EngineeringParameterDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.EngineeringProgressDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.EntityIdentityDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.EnvironmentGalaxyContributionDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.EnvironmentGalaxyFeatureDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.EnvironmentGalaxyRegionInfluenceDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.ExternalAffinityDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.FleetCombatChangeDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.FleetStateDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.GeneratedGalaxyHomeCandidateDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.GeneratedGalaxyPointDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.GeneratedGalaxyRegionDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.GeneratedGalaxySystemDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.HabitatDefinitionDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.InfrastructureStateDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.LandedCohortLineageDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.LogisticsDeliveryDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.ManifestOriginLineageDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.ManifestResolutionDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.NaturalStarGroupDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.NavigationConnectionDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.NavigationKnowledgeEntryDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.PendingCombatDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.PlanetBodyBindingDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.PlanetControlDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.PlanetDefinitionDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.PlanetStateDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.PlanetSurveyDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.PoliticalReasonDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.PopulationCohortDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.PopulationMemoryDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.ProductionQueueItemDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.ResearchReceiptDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.ResolvedCombatDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.ResourceAmountDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.ScienceAllocationDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.ScienceDisciplineStateDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.SettlementCampDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.SettlementCampTerminalDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.SettlementDeploymentIntentDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.SettlerManifestDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.ShipCombatChangeDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.ShipDesign.ComponentOperatingModeDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.ShipDesign.ComponentVersionDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.ShipDesign.HullFamilyDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.ShipDesign.HullVersionDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.ShipDesign.MountLimitDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.ShipDesign.MountRequirementDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.ShipDesign.ProfilePowerAllocationDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.ShipDesign.RoleRecommendationDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.ShipDesign.ShipInstallationDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.ShipDesign.ShipOperatingProfileDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.ShipDesign.ShipOutputDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.ShipDesign.ShipRoleDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.ShipStateDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.StarSystemStateDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.TacticalFormationV2Dto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.TacticalGeometryV2Dto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.TacticalMountV2Dto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.TacticalShieldV2Dto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.TacticalShipV2Dto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.TacticalWeaponV2Dto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::HobbyDS9.Contracts.TraversalEffectDto>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(System.Collections.Generic.List<global::System.String>? value)
            {
                if(value==null){Null();return;}
                Count(2L+System.Math.Max(0,value.Count-1));
                if(value.Count>remainingVisits)Fail("Colonisation root work limit.");
                foreach(var item in value){Need(item);V(item);}
            }
            private void V(global::System.Int32 value){Count(11);}
            private void V(global::System.Int64 value){Count(20);}
            private void V(global::HobbyDS9.Contracts.CombatPhaseKind? value){if(value.HasValue)V(value.Value);else Null();}
            private void V(global::HobbyDS9.Contracts.ControllerKind? value){if(value.HasValue)V(value.Value);else Null();}
            private void V(global::HobbyDS9.Contracts.ScienceDiscipline? value){if(value.HasValue)V(value.Value);else Null();}
            private void V(global::HobbyDS9.Contracts.SessionPhase? value){if(value.HasValue)V(value.Value);else Null();}
            private void V(global::HobbyDS9.Contracts.TacticalRange? value){if(value.HasValue)V(value.Value);else Null();}
            private void V(global::HobbyDS9.Contracts.TacticalSector? value){if(value.HasValue)V(value.Value);else Null();}
            private void V(global::System.Boolean? value){if(value.HasValue)V(value.Value);else Null();}
            private void V(global::System.Int32? value){if(value.HasValue)V(value.Value);else Null();}
            private void V(global::System.Int64? value){if(value.HasValue)V(value.Value);else Null();}
            private void V(global::System.UInt64? value){if(value.HasValue)V(value.Value);else Null();}
            private void V(string? value){Count(value==null?4L:2L+6L*value.Length);}
            private void V(global::System.UInt64 value){Count(20);}
        }
    }
}


