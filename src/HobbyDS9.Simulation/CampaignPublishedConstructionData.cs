using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;
using static HobbyDS9.Simulation.CampaignTacticalReviewData;

namespace HobbyDS9.Simulation
{
    internal static class CampaignPublishedConstructionData
    {
        // Explicit versioned, length-prefixed identity. No serializer/reflection or
        // metadata from a saved root is trusted as an external content package.
        internal static string Identity(string domain,params string[] fields)
        {
            using(var stream=new MemoryStream())using(var writer=new BinaryWriter(stream,new UTF8Encoding(false,true),true))
            {
                void Text(string value) { var b=Encoding.UTF8.GetBytes(value);Require(b.Length<=4096,"Identity field bound.");writer.Write(b.Length);writer.Write(b); }
                Text("published-construction-package-encoding-v1");Text(CampaignPublishedConstructionIdentity.Policy);Text(domain);
                writer.Write(fields.Length);foreach(var f in fields)Text(f);writer.Flush();
                using(var sha=SHA256.Create())return string.Concat(sha.ComputeHash(stream.ToArray()).Select(b=>b.ToString("x2",System.Globalization.CultureInfo.InvariantCulture)));
            }
        }
        internal static TacticalSupportCatalogueDto CopySupport(TacticalSupportCatalogueDto s)=>new TacticalSupportCatalogueDto {
            FormatVersion=s.FormatVersion,SchemaId=s.SchemaId,CatalogueId=s.CatalogueId,Revision=s.Revision,SourceCompilerId=s.SourceCompilerId,
            BalanceStatus=s.BalanceStatus,CloakPolicyId=s.CloakPolicyId,
            Hulls=s.Hulls!.Select(h=>new TacticalHullSupportDto{HullVersionId=h.HullVersionId,SupportedRoleIds=h.SupportedRoleIds!.ToList(),Armor=h.Armor,TacticalRole=h.TacticalRole,
                Capabilities=new AuthoredTacticalCapabilitiesDto{Speed=h.Capabilities!.Speed,Agility=h.Capabilities.Agility,Sensors=h.Capabilities.Sensors,Command=h.Capabilities.Command,Cohesion=h.Capabilities.Cohesion},
                Mounts=h.Mounts!.Select(m=>new TacticalMountSupportDto{SourceMountId=m.SourceMountId,SourceCoverageTag=m.SourceCoverageTag,Coverage=m.Coverage,Slots=m.Slots}).ToList()}).ToList(),
            Components=s.Components!.Select(c=>new TacticalComponentSupportDto{ComponentVersionId=c.ComponentVersionId,Kind=c.Kind,TacticalSensorRating=c.TacticalSensorRating,
                Beam=c.Beam==null?null:new TacticalBeamSupportDto{SourceMountId=c.Beam.SourceMountId,SlotsRequired=c.Beam.SlotsRequired,Coverage=c.Beam.Coverage,Damage=c.Beam.Damage,
                    AccuracyBps=c.Beam.AccuracyBps,TrackingBps=c.Beam.TrackingBps,OpportunitiesPerRound=c.Beam.OpportunitiesPerRound,CadenceRounds=c.Beam.CadenceRounds,InitialNextFireRound=c.Beam.InitialNextFireRound}}).ToList()
        };
        internal static CampaignPublishedSupportIdentityDto SupportIdentity(TacticalSupportCatalogueDto s,string hash)=>new CampaignPublishedSupportIdentityDto {
            SchemaId=s.SchemaId,CatalogueId=s.CatalogueId,Revision=s.Revision,SupportHash=hash,SupportCompilerId=PublishedTacticalSupportIdentity.Compiler,
            SourceCompilerId=s.SourceCompilerId,CanonicalEncodingId="published-tactical-support-metadata-encoding-v2"
        };
        internal static bool Same(CampaignPublishedSupportIdentityDto? a,CampaignPublishedSupportIdentityDto? b)=>a!=null&&b!=null
            &&(a.SchemaId,a.CatalogueId,a.Revision,a.SupportHash,a.SupportCompilerId,a.SourceCompilerId,a.CanonicalEncodingId)
                ==(b.SchemaId,b.CatalogueId,b.Revision,b.SupportHash,b.SupportCompilerId,b.SourceCompilerId,b.CanonicalEncodingId);
        internal static string WrapperHash(CampaignPlayableFleetPackageDto template,TacticalSupportCatalogueDto support,int records)
        {
            CampaignTacticalReviewSession.StartPlayable(template);
            Require(records>0&&records<=65536,"Explicit supported retained-record budget required.");
            var catalogue=new ShipDesignCatalogueSnapshot(template.Integrated!.Physical!.CorePackage!.Campaign!.Definitions.ShipDesignCatalogue!);
            string hash=PublishedTacticalSupportCompiler.ValidateMetadata(catalogue,support);
            return Identity("wrapper",template.PackageHash!,hash,records.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ConstructionJournalFacts.ClosedRules,PublishedTacticalSupportIdentity.Compiler,"single-entry-operations-v1");
        }
    }
}
