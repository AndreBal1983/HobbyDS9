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
    public static partial class PublishedTacticalSupportCompiler
    {
        // Private exact typed context: no public/generic skip list can weaken compiler v2.
        private sealed class ActivityCompilation
        {
            internal readonly Dictionary<string,ActivityModuleSupportDto> Modules;
            internal readonly string Hash;
            internal readonly string CatalogueId;internal readonly int Revision;
            internal ActivityCompilation(Dictionary<string,ActivityModuleSupportDto> modules,string hash,string catalogueId,int revision)
            {Modules=modules;Hash=hash;CatalogueId=catalogueId;Revision=revision;}
        }
        public static ActivitySupportResult EvaluateActivities(ShipDesignCatalogueSnapshot catalogue,PublishedShipBlueprint publication,
            ShipDesignAccessDto access,ActivityTacticalSupportCatalogueDto support)
        {
            var issues=new List<TacticalSupportIssue>();
            void Error(string path)=>issues.Add(new TacticalSupportIssue(TacticalSupportIssueCode.InvalidMetadata,path));
            ActivitySupportResult Reject()=>new ActivitySupportResult(null,issues);
            try
            {
                if(catalogue==null||publication==null||publication.Fit==null||publication.Fit.Installations==null||publication.Fit.Profiles==null
                    ||publication.Components==null||publication.Performance==null||publication.Performance.Profiles==null
                    ||support==null||support.FormatVersion!=ActivityTacticalSupportIdentity.Format||support.SchemaId!=ActivityTacticalSupportIdentity.Schema
                    ||support.CompilerId!=ActivityTacticalSupportIdentity.Compiler||!DefinitionId.TryParse(support.CatalogueId,out _)
                    ||support.Revision<1||support.Tactical==null||support.Modules==null||support.Modules.Count>32)
                {Error("$.ActivitySupport");return Reject();}
                // The same actual metadata validator as v2, without admitting activity-only kinds to v2.
                var innerHash=ValidateMetadata(catalogue,support.Tactical);
                var modules=new Dictionary<string,ActivityModuleSupportDto>(StringComparer.Ordinal);
                foreach(var m in support.Modules)
                {
                    if(m==null||!DefinitionId.TryParse(m.ComponentVersionId,out _)||modules.ContainsKey(m.ComponentVersionId)
                        ||support.Tactical.Components!.Any(c=>c.ComponentVersionId==m.ComponentVersionId)
                        ||m.SurveyPower<=0||m.SurveyPower>10000||m.StudyWork<0||m.StudyWork>100||m.ContactCapacity<0||m.ContactCapacity>100
                        ||(m.Kind==ActivityModuleKind.Laboratory?m.StudyWork==0||m.ContactCapacity!=0
                            :m.Kind==ActivityModuleKind.DiplomaticSuite?m.ContactCapacity==0||m.StudyWork!=0:true))
                    {Error("$.ActivitySupport.Modules");continue;}
                    var source=catalogue.Components.SingleOrDefault(c=>c.VersionId==m.ComponentVersionId);
                    if(source==null||source.Kind!=(m.Kind==ActivityModuleKind.Laboratory?ComponentKind.Science:ComponentKind.Support)
                        ||source.PowerSupply!=0||source.Modes.Count!=3||source.Modes.Any(p=>p.Profile==OperatingProfileKind.Survey
                            ?p.FullPower!=m.SurveyPower||p.MinimumPower!=m.SurveyPower
                            :p.FullPower!=0||p.MinimumPower!=0||p.Outputs.Any(o=>o.Amount!=0)))
                    {Error("$.ActivitySupport.Modules."+m.ComponentVersionId);continue;}
                    modules.Add(m.ComponentVersionId,new ActivityModuleSupportDto{ComponentVersionId=m.ComponentVersionId,Kind=m.Kind,
                        SurveyPower=m.SurveyPower,StudyWork=m.StudyWork,ContactCapacity=m.ContactCapacity});
                }
                if(issues.Count>0)return Reject();
                int work=0,contact=0;var used=new HashSet<ActivityModuleKind>();
                foreach(var installation in publication.Fit.Installations)
                {
                    if(!modules.TryGetValue(installation.ComponentVersionId,out var m))continue;
                    if(installation.Quantity!=1||!used.Add(m.Kind)){Error("$.Publication.ActivityInstallations");continue;}
                    foreach(var kind in new[]{OperatingProfileKind.Cruise,OperatingProfileKind.Combat,OperatingProfileKind.Survey})
                    {
                        var profile=publication.Fit.Profiles.SingleOrDefault(p=>p.Kind==kind);
                        var allocation=profile?.Allocations.SingleOrDefault(a=>a.InstallationId==installation.InstallationId);
                        var expected=kind==OperatingProfileKind.Survey?m.SurveyPower:0;
                        if(allocation==null||allocation.RequestedPower!=expected||allocation.AllocatedPower!=expected)
                            Error("$.Publication.ActivityPower."+installation.InstallationId+"."+kind);
                    }
                    work=m.Kind==ActivityModuleKind.Laboratory?m.StudyWork:work;contact=m.Kind==ActivityModuleKind.DiplomaticSuite?m.ContactCapacity:contact;
                }
                if(issues.Count>0)return Reject();
                var hash=ActivityHash(support,innerHash,modules.Values);
                // Recompile the COMPLETE ordinary publication, source facts and costs before any successful result.
                var tactical=EvaluateCore(catalogue,publication,access,support.Tactical,new ActivityCompilation(modules,hash,support.CatalogueId,support.Revision),out var canonical);
                if(!tactical.IsSupportedByCompiler)return new ActivitySupportResult(null,tactical.Issues);
                // Inspect independently compiled Survey power too; input allocations alone are not output evidence.
                var survey=canonical!.Performance.Profiles.Single(p=>p.Kind==OperatingProfileKind.Survey);
                foreach(var i in publication.Fit.Installations.Where(i=>modules.ContainsKey(i.ComponentVersionId)))
                {var m=modules[i.ComponentVersionId];var allocation=survey.Installations.Single(a=>a.InstallationId==i.InstallationId);
                 if(allocation.AllocatedPower!=m.SurveyPower||allocation.FullPower!=m.SurveyPower||survey.AllocatedPower>survey.AvailablePower)
                     Error("$.Publication.CompiledSurveyPower."+i.InstallationId);}
                if(issues.Count>0)return Reject();
                return new ActivitySupportResult(new ActivitySupportedShip(tactical.Fit!,work,contact,support),issues);
            }
            catch(ArgumentException){Error("$.ActivitySupport.InvalidInput");return Reject();}
            catch(InvalidOperationException){Error("$.ActivitySupport.InvalidShape");return Reject();}
            catch(OverflowException){Error("$.ActivitySupport.Arithmetic");return Reject();}
        }
        private static string ActivityHash(ActivityTacticalSupportCatalogueDto s,string tacticalHash,IEnumerable<ActivityModuleSupportDto> modules)
        {
            using(var bytes=new MemoryStream())
            using(var writer=new BinaryWriter(bytes,Encoding.UTF8,true))
            {
                void Text(string text){if(text==null||text.Length>256)throw new ArgumentException("Activity identity length.");var data=Encoding.UTF8.GetBytes(text);writer.Write(data.Length);writer.Write(data);}
                Text(ActivityTacticalSupportIdentity.Compiler);writer.Write(s.FormatVersion);Text(s.SchemaId);Text(s.CompilerId);Text(s.CatalogueId);writer.Write(s.Revision);Text(tacticalHash);
                var ordered=modules.OrderBy(m=>m.ComponentVersionId,StringComparer.Ordinal).ToList();writer.Write(ordered.Count);
                foreach(var m in ordered){Text(m.ComponentVersionId);writer.Write((int)m.Kind);writer.Write(m.SurveyPower);writer.Write(m.StudyWork);writer.Write(m.ContactCapacity);}
                writer.Flush();if(bytes.Length>65536)throw new ArgumentException("Activity encoding budget.");
                using(var sha=SHA256.Create())return string.Concat(sha.ComputeHash(bytes.ToArray()).Select(b=>b.ToString("x2",CultureInfo.InvariantCulture)));
            }
        }
    }
}
