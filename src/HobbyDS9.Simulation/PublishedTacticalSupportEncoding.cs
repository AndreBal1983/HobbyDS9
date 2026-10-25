using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;
namespace HobbyDS9.Simulation
{
    // Explicit schema v2. New properties are NOT included automatically: revise this schema deliberately.
    internal static class PublishedTacticalSupportEncoding
    {
        internal const int MaximumBytes=128*1024,MaximumWork=100000,MaximumListCount=8192;
        private sealed class LimitException : ArgumentException { public LimitException():base("Tactical support encoding budget exceeded."){} }
        internal sealed class Budget
        {
            private int bytes,work;
            internal void Work(int amount=1){if(amount<0||amount>MaximumWork-work)throw new LimitException();work+=amount;}
            internal void Bytes(int amount){if(amount<0||amount>MaximumBytes-bytes)throw new LimitException();bytes+=amount;}
        }
        private sealed class Writer : IDisposable
        {
            private readonly MemoryStream stream=new MemoryStream();
            private readonly BinaryWriter binary;
            private readonly Budget budget;
            internal Writer(string domain,Budget budget){this.budget=budget;binary=new BinaryWriter(stream,new UTF8Encoding(false,true),true);Text(domain);}
            private void Work(int amount=1)=>budget.Work(amount);
            private void Bytes(int amount)=>budget.Bytes(amount);
            internal void Bool(bool value){Work();Bytes(1);binary.Write(value);}
            internal bool Object(bool present){Bool(present);return present;}
            internal void Number(long value){Work();Bytes(8);binary.Write(value);}
            internal void Optional(long? value){Bool(value.HasValue);if(value.HasValue)Number(value.Value);}
            internal void Text(string? value)
            {
                Work();if(value==null){Bytes(4);binary.Write(-1);return;}
                Work(value.Length);var size=new UTF8Encoding(false,true).GetByteCount(value);Bytes(checked(4+size));
                binary.Write(size);binary.Write(new UTF8Encoding(false,true).GetBytes(value));
            }
            internal void Items<T>(IReadOnlyList<T>? values,Comparison<T> compare,Action<T> emit)
            {
                Work();if(values==null){Number(-1);return;}
                if(values.Count>MaximumListCount)throw new LimitException();Work(values.Count);Number(values.Count);
                var sorted=new T[values.Count];for(var i=0;i<sorted.Length;i++)sorted[i]=values[i];
                try{Array.Sort(sorted,(a,b)=>{Work();return a is null?(b is null?0:-1):b is null?1:compare(a,b);});}
                catch(InvalidOperationException e) when(e.InnerException is LimitException){throw new LimitException();}
                foreach(var value in sorted)emit(value);
            }
            internal int TextOrder(string? a,string? b){var cost=(long)(a?.Length??0)+(b?.Length??0)+1;if(cost>MaximumWork)throw new LimitException();Work((int)cost);return StringComparer.Ordinal.Compare(a,b);}
            internal string Finish(){binary.Flush();using(var sha=SHA256.Create())return string.Concat(sha.ComputeHash(stream.ToArray()).Select(b=>b.ToString("x2",CultureInfo.InvariantCulture)));}
            public void Dispose(){binary.Dispose();stream.Dispose();}
        }
        private static int OutputOrder(ShipOutputSnapshot a,ShipOutputSnapshot b){var c=a.Kind.CompareTo(b.Kind);return c!=0?c:a.Amount.CompareTo(b.Amount);}
        private static void Strings(Writer w,IReadOnlyList<string>? values)=>w.Items(values,(a,b)=>w.TextOrder(a,b),w.Text);
        private static void Outputs(Writer w,IReadOnlyList<ShipOutputSnapshot>? values)=>w.Items(values,OutputOrder,v=>{if(!w.Object(v!=null))return;w.Number((int)v!.Kind);w.Number(v.Amount);});
        private static void Capabilities(Writer w,AuthoredTacticalCapabilitiesDto? c)
        {if(!w.Object(c!=null))return;w.Optional(c!.Speed);w.Optional(c.Agility);w.Optional(c.Sensors);w.Optional(c.Command);w.Optional(c.Cohesion);}
        internal static string Support(TacticalSupportCatalogueDto s,Budget budget)
        {
            using(var w=new Writer("published-tactical-support-metadata-encoding-v2",budget))
            {
                w.Optional(s.FormatVersion);w.Text(s.SchemaId);w.Text(s.CatalogueId);w.Optional(s.Revision);w.Text(s.SourceCompilerId);w.Text(s.BalanceStatus);w.Text(s.CloakPolicyId);
                w.Items(s.Hulls,(a,b)=>w.TextOrder(a.HullVersionId,b.HullVersionId),h=>{
                    if(!w.Object(h!=null))return;w.Text(h!.HullVersionId);Strings(w,h.SupportedRoleIds);w.Optional(h.Armor);w.Optional(h.TacticalRole.HasValue?(long?)h.TacticalRole.Value:null);Capabilities(w,h.Capabilities);
                    w.Items(h.Mounts,(a,b)=>w.TextOrder(a.SourceMountId,b.SourceMountId),m=>{if(!w.Object(m!=null))return;w.Text(m!.SourceMountId);w.Text(m.SourceCoverageTag);w.Optional(m.Coverage.HasValue?(long?)m.Coverage.Value:null);w.Optional(m.Slots);});});
                w.Items(s.Components,(a,b)=>w.TextOrder(a.ComponentVersionId,b.ComponentVersionId),c=>{
                    if(!w.Object(c!=null))return;w.Text(c!.ComponentVersionId);w.Optional(c.Kind.HasValue?(long?)c.Kind.Value:null);w.Optional(c.TacticalSensorRating);
                    var b=c.Beam;if(!w.Object(b!=null))return;w.Text(b!.SourceMountId);w.Optional(b.SlotsRequired);w.Optional(b.Coverage.HasValue?(long?)b.Coverage.Value:null);
                    w.Optional(b.Damage);w.Optional(b.AccuracyBps);w.Optional(b.TrackingBps);w.Optional(b.OpportunitiesPerRound);w.Optional(b.CadenceRounds);w.Optional(b.InitialNextFireRound);});
                return w.Finish();
            }
        }
        private static void Cost(Writer w,ShipDesignCostSnapshot? c)
        {
            if(!w.Object(c!=null))return;w.Number(c!.Credits);w.Number(c.Industry);w.Number(c.Maintenance);
            w.Items(c.Resources,(a,b)=>w.TextOrder(a.ResourceDefinitionId,b.ResourceDefinitionId),r=>{if(!w.Object(r!=null))return;w.Text(r!.ResourceDefinitionId);w.Number(r.Amount);});
        }
        // New explicit catalogue identity for the activity profile; old v2 domains are unchanged.
        internal static string ActivityCatalogue(ShipDesignCatalogueSnapshot catalogue)
        {
            using(var w=new Writer("activity-catalogue-encoding-v1",new Budget()))
            {
                w.Optional(catalogue.FormatVersion);
                w.Items(catalogue.HullFamilies,(a,b)=>w.TextOrder(a.FamilyId,b.FamilyId),f=>{w.Text(f.FamilyId);w.Text(f.DisplayName);});
                w.Items(catalogue.Hulls,(a,b)=>w.TextOrder(a.VersionId,b.VersionId),h=>Hull(w,h));
                w.Items(catalogue.Components,(a,b)=>w.TextOrder(a.VersionId,b.VersionId),c=>Component(w,c));
                w.Items(catalogue.Roles,(a,b)=>w.TextOrder(a.RoleId,b.RoleId),r=>{w.Text(r.RoleId);w.Text(r.DisplayName);
                    w.Items(r.Recommendations,(a,b)=>{var order=a.Profile.CompareTo(b.Profile);return order!=0?order:a.Output.CompareTo(b.Output);},
                        x=>{w.Number((int)x.Profile);w.Number((int)x.Output);w.Number(x.Minimum);});});
                return w.Finish();
            }
        }
        private static void Hull(Writer w,HullVersionSnapshot? h)
        {
            if(!w.Object(h!=null))return;w.Text(h!.VersionId);w.Text(h.FamilyId);w.Number(h.VersionNumber);w.Text(h.ParentVersionId);w.Text(h.DisplayName);
            w.Number(h.Capacity);w.Number(h.MaxHull);w.Number(h.DesignLoad);w.Number(h.MinimumCrew);w.Number(h.PowerDistributionLimit);w.Number(h.MaxReactors);w.Number(h.MaxPropulsionPackages);
            w.Number(h.ReactorSizeLimit);w.Number(h.PropulsionSizeLimit);Strings(w,h.Tags);
            w.Items(h.Mounts,(a,b)=>w.TextOrder(a.Id,b.Id),m=>{if(!w.Object(m!=null))return;w.Text(m!.Id);w.Number((int)m.Kind);w.Number(m.SizeRating);w.Number(m.Count);w.Text(m.CoverageTag);});Cost(w,h.Cost);
        }
        private static int MountRequirementOrder(Writer w,MountRequirementSnapshot a,MountRequirementSnapshot b)
        {
            var c=a.Kind.CompareTo(b.Kind);if(c!=0)return c;c=a.SizeRating.CompareTo(b.SizeRating);if(c!=0)return c;c=w.TextOrder(a.CoverageTag,b.CoverageTag);return c!=0?c:a.Count.CompareTo(b.Count);
        }
        private static void Component(Writer w,ComponentVersionSnapshot? c)
        {
            if(!w.Object(c!=null))return;w.Text(c!.VersionId);w.Text(c.FamilyId);w.Number(c.VersionNumber);w.Text(c.ParentVersionId);w.Text(c.DisplayName);w.Number((int)c.Kind);
            w.Number(c.Capacity);w.Number(c.CrewRequired);w.Number(c.ComputerDemand);w.Number(c.PowerSupply);w.Number(c.EnvelopeSize);
            Strings(w,c.CompatibleHullFamilyIds);Strings(w,c.RequiredHullTags);Strings(w,c.Tags);Strings(w,c.RequiredComponentTags);
            w.Items(c.Mounts,(a,b)=>MountRequirementOrder(w,a,b),m=>{if(!w.Object(m!=null))return;w.Number((int)m!.Kind);w.Number(m.SizeRating);w.Number(m.Count);w.Text(m.CoverageTag);});
            Outputs(w,c.Dependencies);w.Items(c.Modes,(a,b)=>a.Profile.CompareTo(b.Profile),m=>{if(!w.Object(m!=null))return;w.Number((int)m!.Profile);w.Number(m.FullPower);w.Number(m.MinimumPower);w.Number((int)m.Scaling);w.Bool(m.Essential);Outputs(w,m.Outputs);});Cost(w,c.Cost);
        }
        private static void Draft(Writer w,ShipClassDraftSnapshot? d)
        {
            if(!w.Object(d!=null))return;w.Text(d!.ClassId);w.Text(d.DisplayName);w.Text(d.RoleId);w.Text(d.HullVersionId);w.Number(d.EmbarkedCrew);
            w.Items(d.Installations,(a,b)=>w.TextOrder(a.InstallationId,b.InstallationId),i=>{if(!w.Object(i!=null))return;w.Text(i!.InstallationId);w.Text(i.ComponentVersionId);w.Number(i.Quantity);});
            w.Items(d.Profiles,(a,b)=>a.Kind.CompareTo(b.Kind),p=>{if(!w.Object(p!=null))return;w.Number((int)p!.Kind);
                w.Items(p.Allocations,(a,b)=>w.TextOrder(a.InstallationId,b.InstallationId),a=>{if(!w.Object(a!=null))return;w.Text(a!.InstallationId);w.Number(a.RequestedPower);w.Number(a.AllocatedPower);});});
        }
        private static void Performance(Writer w,ShipDesignPreview? p)
        {
            if(!w.Object(p!=null))return;w.Bool(p!.IsValid);w.Optional(p.CapacityUsed);w.Optional(p.CapacityLimit);w.Optional(p.RequiredCrew);w.Optional(p.ReactorPower);w.Optional(p.DistributionLimit);
            w.Items(p.Issues,(a,b)=>{var c=w.TextOrder(a.Path,b.Path);if(c!=0)return c;c=a.Code.CompareTo(b.Code);if(c!=0)return c;c=a.Severity.CompareTo(b.Severity);return c!=0?c:w.TextOrder(a.Message,b.Message);},i=>{
                if(!w.Object(i!=null))return;w.Number((int)i!.Code);w.Number((int)i.Severity);w.Text(i.Path);w.Text(i.Message);});
            w.Items(p.Mounts,(a,b)=>w.TextOrder(a.MountId,b.MountId),m=>{if(!w.Object(m!=null))return;w.Text(m!.MountId);w.Number((int)m.Kind);w.Number(m.SizeRating);w.Text(m.CoverageTag);w.Number(m.Used);w.Number(m.Limit);});
            w.Items(p.Envelopes,(a,b)=>a.Kind.CompareTo(b.Kind),e=>{if(!w.Object(e!=null))return;w.Number((int)e!.Kind);w.Number(e.PackagesUsed);w.Number(e.PackageLimit);w.Number(e.LargestSizeUsed);w.Number(e.SizeLimit);});
            Cost(w,p.Cost);
            w.Items(p.Profiles,(a,b)=>a.Kind.CompareTo(b.Kind),q=>{
                if(!w.Object(q!=null))return;w.Number((int)q!.Kind);w.Number(q.AvailablePower);w.Number(q.InstalledPeakPower);w.Number(q.RequestedPower);w.Number(q.AllocatedPower);
                w.Items(q.Installations,(a,b)=>w.TextOrder(a.InstallationId,b.InstallationId),i=>{if(!w.Object(i!=null))return;w.Text(i!.InstallationId);w.Number(i.FullPower);w.Number(i.MinimumPower);w.Number(i.RequestedPower);w.Number(i.AllocatedPower);w.Bool(i.Essential);Outputs(w,i.Outputs);});Outputs(w,q.Outputs);});
            var compiled=p.Compiled;if(w.Object(compiled!=null)){w.Number(compiled!.MaxHull);w.Number(compiled.MovementBudget);var c=compiled.Combat;
                if(w.Object(c!=null)){w.Number(c!.ShieldCapacity);w.Number(c.LongRangeAttack);w.Number(c.MediumRangeAttack);w.Number(c.CloseRangeAttack);w.Number(c.AccuracyBps);w.Number(c.EvasionBps);}}
        }
        internal static string Publication(PublishedShipBlueprint p,Budget budget)
        {
            using(var w=new Writer("published-tactical-publication-source-encoding-v2",budget))
            {w.Text(p.BlueprintId);w.Number(p.Revision);w.Text(p.RulesIdentity);Draft(w,p.Fit);Hull(w,p.Hull);w.Items(p.Components,(a,b)=>w.TextOrder(a.VersionId,b.VersionId),c=>Component(w,c));Performance(w,p.Performance);return w.Finish();}
        }
    }
}
