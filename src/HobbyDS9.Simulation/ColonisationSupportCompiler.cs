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
        // Constructed only after exact v4 metadata and every installed allocation are checked.
        private sealed class ColonisationCompilation
        {
            internal readonly ColonisationSupportCatalogueDto Support;
            internal readonly SettlerModuleSupportDto? Installed;
            internal ColonisationCompilation(ColonisationSupportCatalogueDto support,SettlerModuleSupportDto? installed)
            {Support=support;Installed=installed;}
        }
        public static ColonisationSupportResult EvaluateColonisation(ShipDesignCatalogueSnapshot catalogue,PublishedShipBlueprint publication,
            ShipDesignAccessDto access,ColonisationSupportCatalogueDto support)
        {
            var issues=new List<TacticalSupportIssue>();
            ColonisationSupportResult Reject(string path){issues.Add(new TacticalSupportIssue(TacticalSupportIssueCode.InvalidMetadata,path));return new ColonisationSupportResult(null,issues);}
            try
            {
                if(catalogue==null||publication?.Fit?.Installations==null||publication.Fit.Profiles==null||access==null||support==null
                    ||support.FormatVersion!=ColonisationSupportIdentity.Format||support.SchemaId!=ColonisationSupportIdentity.Schema
                    ||support.CompilerId!=ColonisationSupportIdentity.Compiler||!DefinitionId.TryParse(support.CatalogueId,out _)||support.Revision<1
                    ||support.Activity?.Tactical?.Components==null||support.Activity.Modules==null||support.SettlerModules==null||support.SettlerModules.Count!=1)
                    return Reject("$.ColonisationSupport");
                if(publication.Fit.Installations.Any(i=>i==null)||publication.Fit.Profiles.Any(p=>p==null||p.Allocations==null||p.Allocations.Any(a=>a==null))
                    ||catalogue.Components.Any(c=>c==null)||support.Activity.Tactical.Components.Any(c=>c==null)||support.Activity.Modules.Any(m=>m==null))
                    return Reject("$.ColonisationSupport.NullRow");
                var m=support.SettlerModules[0];
                if(m==null||!DefinitionId.TryParse(m.ComponentVersionId,out _)||m.RequiredPower<1||m.RequiredPower>10000
                    ||m.Passengers<1||m.Passengers>10000||m.Food<1||m.Food>10000||m.Equipment<1||m.Equipment>100||m.Materials<0||m.Materials>10000
                    ||support.Activity.Modules.Any(x=>x==null||x.ComponentVersionId==m.ComponentVersionId)
                    ||support.Activity.Tactical.Components.Any(x=>x==null||x.ComponentVersionId==m.ComponentVersionId))return Reject("$.ColonisationSupport.SettlerModule");
                var source=catalogue.Components.SingleOrDefault(x=>x.VersionId==m.ComponentVersionId);
                if(source==null||source.Kind!=ComponentKind.Support||source.PowerSupply!=0||source.Modes.Count!=3||source.Capacity<=0
                    ||!source.RequiredComponentTags.Contains("system-tag.life-support",StringComparer.Ordinal)
                    ||source.Modes.Any(x=>x==null||x.Outputs==null||x.Outputs.Any(o=>o==null)||x.FullPower!=m.RequiredPower||x.MinimumPower!=m.RequiredPower||!x.Essential||x.Outputs.Any(o=>o.Amount!=0)))
                    return Reject("$.ColonisationSupport.SettlerSource");
                var installed=publication.Fit.Installations.Where(i=>i.ComponentVersionId==m.ComponentVersionId).ToList();
                if(installed.Count>1||installed.Any(i=>i.Quantity!=1))return Reject("$.Publication.SettlerInstallations");
                if(installed.Count==1)
                foreach(var kind in new[]{OperatingProfileKind.Cruise,OperatingProfileKind.Combat,OperatingProfileKind.Survey})
                {
                    var allocation=publication.Fit.Profiles.SingleOrDefault(p=>p.Kind==kind)?.Allocations.SingleOrDefault(a=>a.InstallationId==installed[0].InstallationId);
                    if(allocation==null||allocation.RequestedPower!=m.RequiredPower||allocation.AllocatedPower!=m.RequiredPower)return Reject("$.Publication.SettlerPower."+kind);
                }
                // Complete ordinary recompile, source identity, v3 modules, tactical validation and access remain shared.
                var inner=EvaluateActivitiesCore(catalogue,publication,access,support.Activity,new ColonisationCompilation(support,installed.Count==1?m:null));
                if(!inner.Supported)return new ColonisationSupportResult(null,inner.Issues);
                return new ColonisationSupportResult(new ColonisationSupportedShip(inner.Ship!.Tactical,inner.Ship.StudyWork,inner.Ship.ContactCapacity,installed.Count==1?m:null,support),issues);
            }
            catch(ArgumentException){return Reject("$.ColonisationSupport.InvalidInput");}
            catch(InvalidOperationException){return Reject("$.ColonisationSupport.InvalidShape");}
            catch(OverflowException){return Reject("$.ColonisationSupport.Arithmetic");}
        }
        private static string ColonisationHash(ColonisationSupportCatalogueDto s,string completeActivityHash)
        {
            using(var bytes=new MemoryStream())using(var writer=new BinaryWriter(bytes,Encoding.UTF8,true))
            {
                void Text(string value){if(value==null||value.Length>256)throw new ArgumentException("Colonisation identity string.");var b=Encoding.UTF8.GetBytes(value);writer.Write(b.Length);writer.Write(b);}
                Text(ColonisationSupportIdentity.Compiler);writer.Write(s.FormatVersion);Text(s.SchemaId);Text(s.CompilerId);Text(s.CatalogueId);writer.Write(s.Revision);Text(completeActivityHash);
                writer.Write(s.SettlerModules!.Count);
                foreach(var m in s.SettlerModules.OrderBy(x=>x.ComponentVersionId,StringComparer.Ordinal))
                {Text(m.ComponentVersionId);writer.Write(m.RequiredPower);writer.Write(m.Passengers);writer.Write(m.Food);writer.Write(m.Equipment);writer.Write(m.Materials);}
                writer.Flush();if(bytes.Length>65536)throw new ArgumentException("Colonisation identity budget.");
                using(var sha=SHA256.Create())return string.Concat(sha.ComputeHash(bytes.ToArray()).Select(b=>b.ToString("x2",CultureInfo.InvariantCulture)));
            }
        }
    }
}
