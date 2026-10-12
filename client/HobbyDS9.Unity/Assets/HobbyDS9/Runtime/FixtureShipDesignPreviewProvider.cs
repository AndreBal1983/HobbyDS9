#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace HobbyDS9.UnityClient
{
    // Authored DEMO examples, not a validator/compiler. Exact-fit lookup only;
    // arbitrary edits deliberately have no fabricated performance or eligibility.
    public sealed class FixtureShipDesignPreviewProvider : IShipDesignPreviewProvider
    {
        public string StatusLabel => "DEMO / AUTHORED PREVIEW FIXTURES • No backend validation • No construction";
        public bool FixtureResults => true;
        public IReadOnlyList<DesignerHull> Hulls { get; }
        public IReadOnlyList<DesignerComponent> Catalogue { get; }
        public IReadOnlyList<DesignerTemplate> Templates { get; }
        private readonly Dictionary<string, DesignerPreview> previews = new Dictionary<string, DesignerPreview>(StringComparer.Ordinal);
        public FixtureShipDesignPreviewProvider()
        {
            Hulls = new[] {new DesignerHull("demo.pathfinder-hull-v1","Pathfinder / v1 (demo)","100 capacity • 2 bank mounts / 1 launcher mount • 60 power distribution. One shared illustrative hull; not canon balance.")}.ToList().AsReadOnly();
            Catalogue = new[]
            {
                new DesignerComponent("demo.reactor", "Reactor Mk I", "Core", "Supplies the shared power bus. Reactor and hull distribution limits belong to backend validation.", "18 capacity • 60 power supply • 80 credits / 40 industry"),
                new DesignerComponent("demo.drive", "Warp / impulse drive", "Core", "Cruise and combat use different allocations. A mobile ship needs compatible propulsion.", "16 capacity • propulsion envelope A • 60 credits / 35 industry"),
                new DesignerComponent("demo.crew", "Crew support", "Core", "Accommodation and life support for the embarked crew. Removing this package demonstrates an authored hard error.", "12 capacity • 80 crew support • 30 credits / 20 industry"),
                new DesignerComponent("demo.command", "Command computer", "Core", "Coordinates onboard systems. Computing dependencies will be resolved by the Application preview.", "8 capacity • computing rating 3 • 35 credits / 20 industry"),
                new DesignerComponent("demo.sensors", "Sensor array", "Mission", "Provides survey observation and combat targeting. Installed output is not the same as powered output.", "8 capacity • sensor rating 4 • 35 credits / 15 industry"),
                new DesignerComponent("demo.labs", "Survey laboratories", "Mission", "Dedicated scientific facilities. Quantity 2 on the surveyor demonstrates an authored capacity error.", "18 capacity each • survey package • 70 credits / 30 industry"),
                new DesignerComponent("demo.shield", "Shield package", "Defence", "Encounter shielding powered by the selected profile; persistent damage rules remain unchanged.", "6 capacity each • shield package • 40 credits / 20 industry"),
                new DesignerComponent("demo.bank", "Energy bank", "Defence", "One broad-coverage energy mount per package. Quantity 3 on the escort demonstrates an authored mount/capacity error.", "8 capacity each • 1 energy mount • 45 credits / 25 industry"),
                new DesignerComponent("demo.launcher", "Launcher package", "Defence", "One launcher mount. This demo introduces no ammunition or new combat rules.", "8 capacity each • 1 launcher mount • 50 credits / 25 industry")
            }.ToList().AsReadOnly();
            var survey = Fit("Pathfinder", "Surveyor", new Dictionary<string,int> { ["demo.labs"] = 1, ["demo.shield"] = 1 });
            var escort = Fit("Sentinel", "Escort", new Dictionary<string,int> { ["demo.shield"] = 2, ["demo.bank"] = 2, ["demo.launcher"] = 1 });
            Templates = new[] { new DesignerTemplate("demo.surveyor", "Surveyor / Pathfinder", survey), new DesignerTemplate("demo.escort", "Escort / Sentinel", escort) }.ToList().AsReadOnly();
            foreach (DesignerProfile mode in Enum.GetValues(typeof(DesignerProfile)))
            {
                previews.Add(Key(survey,mode), Survey(mode)); previews.Add(Key(escort,mode), Escort(mode));
                foreach (var core in new[] { "demo.reactor", "demo.drive", "demo.crew", "demo.command" })
                {
                    var label = Catalogue.Single(c => c.Id == core).Name;
                    AddVariant(survey,mode,core,0,Error(core,label + " is missing. Restore this package in the catalogue; this is an authored example, not a live validator."));
                    AddVariant(escort,mode,core,0,Error(core,label + " is missing. Restore this package in the catalogue; this is an authored example, not a live validator."));
                }
                AddVariant(survey,mode,"demo.labs",2, new DesignerPreview(DesignerStatus.HardError,"Authored over-capacity example",104,100,-4,
                    issues:new[] { new DesignerIssue("demo.labs",DesignerStatus.HardError,"Two laboratories exceed this hull's demo capacity by 4. Reduce the laboratory quantity to 1.") }));
                AddVariant(escort,mode,"demo.bank",3,new DesignerPreview(DesignerStatus.HardError,"Authored mount/capacity example",106,100,-6,
                    issues:new[] { new DesignerIssue("demo.bank",DesignerStatus.HardError,"Three bank packages use 3 energy mounts; the demo hull has 2. Reduce to 2 banks. Demo capacity is also exceeded by 6.") }));
            }
        }
        private static DesignerFit Fit(string name,string role,Dictionary<string,int> mission)
        {
            var items = new Dictionary<string,int> { ["demo.reactor"]=1,["demo.drive"]=1,["demo.crew"]=1,["demo.command"]=1,["demo.sensors"]=1 };
            foreach (var pair in mission) items.Add(pair.Key,pair.Value);
            return new DesignerFit(name,"demo.pathfinder-hull-v1",role,items);
        }
        private static string Key(DesignerFit fit,DesignerProfile mode) => fit.HullId + "|" + fit.Role + "|" + mode + "|"
            + string.Join(";",fit.Installed.OrderBy(p=>p.Key,StringComparer.Ordinal).Select(p=>p.Key+"="+p.Value));
        private void AddVariant(DesignerFit original,DesignerProfile mode,string id,int amount,DesignerPreview result)
        {
            var items = original.Installed.ToDictionary(p=>p.Key,p=>p.Value,StringComparer.Ordinal);
            if(amount==0) items.Remove(id); else items[id]=amount;
            previews.Add(Key(new DesignerFit(original.Name,original.HullId,original.Role,items),mode),result);
        }
        private static DesignerPreview Error(string id,string explanation) => new DesignerPreview(DesignerStatus.HardError,"Authored missing-system example",
            issues:new[] {new DesignerIssue(id,DesignerStatus.HardError,explanation)});
        private static DesignerMetric M(string caption,string value) => new DesignerMetric(caption,value);
        private static DesignerPreview Survey(DesignerProfile mode)
        {
            var power = mode == DesignerProfile.Survey ? "52" : mode == DesignerProfile.Combat ? "38" : "44";
            return new DesignerPreview(mode==DesignerProfile.Combat?DesignerStatus.Warning:DesignerStatus.Valid,"Authored surveyor preview • illustrative values",86,100,14,
                new[] {M("Available / requested","60 / "+power),M("Actual allocation",power),M("Power source","60 reactor / 60 distribution")},
                new[] {M("Cruise speed",mode==DesignerProfile.Cruise?"Warp 5 (demo)":"Not powered for warp cruise"),M("Weapons","Unarmed"),M("Science",mode==DesignerProfile.Survey?"Survey rating 8 (demo)":"Labs not powered in this profile"),M("Crew / mounts","80 supported • 0/2 banks • 0/1 launcher")},
                new[] {M("Construction","350 credits / 180 industry"),M("Delivery / upkeep","2 Dilithium / 4 credits per turn")},
                mode==DesignerProfile.Combat ? new[] {new DesignerIssue("demo.bank",DesignerStatus.Warning,"Unarmed surveyor: avoid hostile engagements. This role warning is not a hard requirement to install weapons.")} : null);
        }
        private static DesignerPreview Escort(DesignerProfile mode)
        {
            var combat = mode==DesignerProfile.Combat; var survey = mode==DesignerProfile.Survey;
            return new DesignerPreview(combat||survey?DesignerStatus.Warning:DesignerStatus.Valid,"Authored escort preview • illustrative values",98,100,2,
                new[] {M("Available / requested",combat?"60 / 66":survey?"60 / 28":"60 / 44"),M("Actual allocation",combat?"60":survey?"28":"44"),M("Power source","60 reactor / 60 distribution")},
                new[] {M("Cruise speed",mode==DesignerProfile.Cruise?"Warp 5 (demo)":"Not powered for warp cruise"),M("Weapons",combat?"18/24 allocated • 75% requested output":"Weapons not powered"),M("Science","Basic observation only • no laboratories"),M("Crew / mounts","80 supported • 2/2 banks • 1/1 launcher")},
                new[] {M("Construction","480 credits / 245 industry"),M("Delivery / upkeep","4 Dilithium / 6 credits per turn")},
                combat ? new[] {new DesignerIssue("demo.bank",DesignerStatus.Warning,"Shield-priority Combat uses 60 of 66 requested power. Weapon output is reduced; full output is not claimed.")}
                    : survey ? new[] {new DesignerIssue("demo.labs",DesignerStatus.Warning,"No dedicated laboratories: this escort offers observation rather than full survey capability.")} : null);
        }
        public DesignerPreview Preview(DesignerFit fit,DesignerProfile profile) => previews.TryGetValue(Key(fit,profile),out var result) ? result
            : new DesignerPreview(DesignerStatus.NotEvaluated,"No authored preview for this edited fit. Power, capabilities, total costs and legality are NOT evaluated. Reset to a template to restore a known preview.");
    }
}
