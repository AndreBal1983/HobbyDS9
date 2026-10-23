#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HobbyDS9.Application;
using HobbyDS9.Contracts;

namespace HobbyDS9.UnityReview
{
    // Detached presentation projection only. Never serialize this as a legacy map or feed it to Open.
    public sealed class GeneratedGalaxyMapData
    {
        public int FormatVersion { get; }
        public string GeneratorVersion { get; }
        public string ContentHash { get; }
        public GeneratedGalaxySettingsDto Settings { get; }
        public int Width { get; }
        public int Height { get; }
        public IReadOnlyList<GeneratedGalaxyRegionDto> Regions { get; }
        public IReadOnlyList<GeneratedGalaxySystemDto> Systems { get; }
        public IReadOnlyList<NavigationConnectionDto> Connections { get; }
        public IReadOnlyList<GeneratedGalaxyHomeCandidateDto> HomeCandidates { get; }
        public EnvironmentGalaxyDocumentDto? Environment { get; }
        public ScaledGalaxyDocumentDto? Scaled { get; }
        public NaturalGalaxyDocumentDto? Natural { get; }
        public bool IsLarge=>Scaled!=null||Natural!=null;
        public ScaledGalaxyPresentationSummary? ScaleSummary { get; }
        public bool HasEnvironment=>Environment!=null||IsLarge;
        public IReadOnlyList<EnvironmentGalaxyFeatureDto> Features=>Natural?.Features??Scaled?.Features??Environment?.Features??(IReadOnlyList<EnvironmentGalaxyFeatureDto>)Array.Empty<EnvironmentGalaxyFeatureDto>();
        public IReadOnlyList<EnvironmentGalaxyRegionInfluenceDto> RegionInfluences=>Natural?.RegionInfluences??Scaled?.RegionInfluences??Environment?.RegionInfluences??(IReadOnlyList<EnvironmentGalaxyRegionInfluenceDto>)Array.Empty<EnvironmentGalaxyRegionInfluenceDto>();
        public int MaximumDensity=>Natural?.Tuning.Environment.MaximumDensity??Scaled?.Tuning.Environment.MaximumDensity??Environment?.Tuning.MaximumDensity??0;
        public GeneratedGalaxyMapData(GeneratedGalaxyDocumentDto map)
        {
            FormatVersion=map.FormatVersion;GeneratorVersion=map.GeneratorVersion;ContentHash=map.ContentHash;Settings=map.Settings;
            Width=map.Width;Height=map.Height;Regions=map.Regions;Systems=map.Systems;Connections=map.Connections;HomeCandidates=map.HomeCandidates;
        }
        public GeneratedGalaxyMapData(EnvironmentGalaxyDocumentDto map)
        {
            Environment=map;FormatVersion=map.FormatVersion;GeneratorVersion=map.GeneratorVersion;ContentHash=map.ContentHash;Settings=map.Settings.Map;
            Width=map.Layout.Width;Height=map.Layout.Height;Regions=map.Layout.Regions;Systems=map.Layout.Systems;
            Connections=map.Layout.Connections;HomeCandidates=map.Layout.HomeCandidates;
        }
        public GeneratedGalaxyPointDto SystemPosition(string systemId)
        {
            if(HasEnvironment)return Features.Single(f=>f.Kind==GalaxyEnvironmentFeatureKind.Star&&f.SystemId==systemId).Position;
            return Regions.Single(r=>r.NodeId==Systems.Single(s=>s.SystemId==systemId).NodeId).Center;
        }
        public GeneratedGalaxyMapData(ScaledGalaxyDocumentDto map)
        {
            Scaled=map;FormatVersion=map.FormatVersion;GeneratorVersion=map.GeneratorVersion;ContentHash=map.ContentHash;
            ScaleSummary=new ScaledGalaxyPresentationSummary(map);
            Settings=new GeneratedGalaxySettingsDto{Seed=map.Settings.Seed,SystemCount=map.Settings.SystemCount,
                HomeCivilizationDefinitionIds=new List<string>(map.Settings.HomeCivilizationDefinitionIds)};
            Width=map.Layout.Width;Height=map.Layout.Height;Regions=map.Layout.Regions;Systems=map.Layout.Systems;
            Connections=map.Layout.Connections;HomeCandidates=map.Layout.HomeCandidates;
        }
        public GeneratedGalaxyMapData(NaturalGalaxyDocumentDto map)
        {
            Natural=map;FormatVersion=map.FormatVersion;GeneratorVersion=map.GeneratorVersion;ContentHash=map.ContentHash;
            ScaleSummary=new ScaledGalaxyPresentationSummary(map);
            Settings=new GeneratedGalaxySettingsDto{Seed=map.Settings.Map.Seed,SystemCount=map.Settings.Map.SystemCount,
                HomeCivilizationDefinitionIds=new List<string>(map.Settings.Map.HomeCivilizationDefinitionIds)};
            Width=map.Layout.Width;Height=map.Layout.Height;Regions=map.Layout.Regions;Systems=map.Layout.Systems;
            Connections=map.Layout.Connections;HomeCandidates=map.Layout.HomeCandidates;
        }
    }
    // Separate presentation workspace; never holds or advances a campaign session.
    public sealed class GeneratedGalaxyWorkspace
    {
        private readonly List<CivilizationDefinitionDto> civilizations;
        private readonly ReviewNewtonsoftCodec codec=new ReviewNewtonsoftCodec();
        private readonly GeneratedGalaxyMapStore store;
        private GeneratedGalaxyReview? review;
        private EnvironmentGalaxyReview? environmentReview;
        private ScaledGalaxyReview? scaledReview;
        private NaturalGalaxyReview? naturalReview;
        public GeneratedGalaxyMapData? Document { get; private set; }
        public bool Unexported { get; private set; }
        public string Notice { get; private set; }="Choose a seed and size, then generate a map.";
        public string Seed { get; set; }="20261020";
        public int Size { get; set; }=40;
        public int QuietEmptyAreaTargetPermille { get; set; }=2000;
        public bool NaturalGeneration { get; set; }
        public int Slot { get; set; }=1;
        public string Location=>store.Location;
        public IReadOnlyDictionary<string,string> CivilizationNames { get; }
        public GeneratedGalaxyWorkspace(IEnumerable<CivilizationDefinitionDto> catalog,GeneratedGalaxyMapStore mapStore)
        {
            civilizations=catalog.Select(c=>new CivilizationDefinitionDto{DefinitionId=c.DefinitionId,DisplayName=c.DisplayName}).ToList();
            CivilizationNames=civilizations.ToDictionary(c=>c.DefinitionId,c=>c.DisplayName,StringComparer.Ordinal);
            store=mapStore;
        }
        // Background work uses a detached operation workspace. The live review is published by the UI
        // only after completion and its epoch/document checks; failures carry a notice, never authority.
        public GeneratedGalaxyWorkspace CaptureOperation()=>new GeneratedGalaxyWorkspace(civilizations,store)
            {Seed=Seed,Size=Size,Slot=Slot,QuietEmptyAreaTargetPermille=QuietEmptyAreaTargetPermille,NaturalGeneration=NaturalGeneration};
        public bool PublishOperation(GeneratedGalaxyWorkspace completed,GeneratedGalaxyMapData? expectedCurrent)
        {
            if(!ReferenceEquals(Document,expectedCurrent))return false;
            Notice=completed.Notice;if(completed.Document==null)return false;
            review=completed.review;environmentReview=completed.environmentReview;scaledReview=completed.scaledReview;naturalReview=completed.naturalReview;NaturalGeneration=completed.NaturalGeneration;
            Document=completed.Document;Seed=completed.Seed;Size=completed.Size;QuietEmptyAreaTargetPermille=completed.QuietEmptyAreaTargetPermille;
            Unexported=completed.Unexported;return true;
        }
        public bool Generate()
        {
            try
            {
                if(Size==250||Size==1000)
                {
                    var scale=Size==250?1:2;
                    var mapSettings=new ScaledGalaxySettingsDto{Seed=Seed,SystemCount=Size,EmptyRegionCount=Size,
                        Width=2500000*scale,Height=2000000*scale,QuietEmptyAreaTargetPermille=QuietEmptyAreaTargetPermille,
                        HomeCivilizationDefinitionIds=new List<string>{"civilization.federation","civilization.cardassian"}};
                    if(NaturalGeneration)
                    {
                        var nextNatural=NaturalGalaxyApplication.Generate(new NaturalGalaxySettingsDto{Map=mapSettings},civilizations);
                        var nextDocument=new GeneratedGalaxyMapData(nextNatural.Export());naturalReview=nextNatural;
                        scaledReview=null;environmentReview=null;review=null;Document=nextDocument;Unexported=true;
                        Notice="Generated natural "+Size+" systems · format4 · not exported.";return true;
                    }
                    var nextScaled=ScaledGalaxyApplication.Generate(mapSettings,civilizations);
                    var scaledDocument=new GeneratedGalaxyMapData(nextScaled.Export());
                    scaledReview=nextScaled;naturalReview=null;environmentReview=null;review=null;Document=scaledDocument;Unexported=true;
                    Notice="Generated "+Size+" systems · "+scaledDocument.Regions.Count+" regions · not exported.";return true;
                }
                var settings=new GeneratedGalaxySettingsDto{Seed=Seed,SystemCount=Size,
                    HomeCivilizationDefinitionIds=new List<string>{"civilization.federation","civilization.cardassian"}};
                var next=EnvironmentGalaxyApplication.Generate(new EnvironmentGalaxySettingsDto{Map=settings,
                    StellarInfluenceEnabled=true,BlackHoleInfluenceEnabled=true,NebulaInfluenceEnabled=true},civilizations);
                var document=new GeneratedGalaxyMapData(next.Export());
                environmentReview=next;scaledReview=null;naturalReview=null;review=null;Document=document;Unexported=true;
                Notice="Generated "+document.Systems.Count+" systems · "+document.Regions.Count+" regions · not exported.";
                return true;
            }
            catch(ArgumentException){Notice="Cannot generate: use an unsigned whole-number seed and a supported size. Current map is kept.";return false;}
            catch(InvalidOperationException){Notice="Generation unavailable. Current map is kept.";return false;}
        }
        public bool SlotOccupied()
        {
            try{return store.Exists(Slot);}
            catch(IOException){Notice="Map slot cannot be inspected. Current map is kept.";return true;}
            catch(UnauthorizedAccessException){Notice="Map slot is unavailable. Current map is kept.";return true;}
        }
        public bool Export(bool overwrite)
        {
            if(review==null&&environmentReview==null&&scaledReview==null&&naturalReview==null){Notice="Generate or open a map first.";return false;}
            try
            {
                store.Write(Slot,ExportBytes(),overwrite);
                Unexported=false;Notice="Exported to map slot "+Slot+". Campaign saves are separate.";return true;
            }
            catch(Exception error) when(error is IOException||error is InvalidDataException||error is UnauthorizedAccessException||error is ArgumentException)
            {Notice="Export failed. Current map and any previous export are kept.";return false;}
        }
        public bool Open()
        {
            try
            {
                var bytes=store.Read(Slot);GeneratedGalaxyReview? next=null;EnvironmentGalaxyReview? nextEnvironment=null;ScaledGalaxyReview? nextScaled=null;NaturalGalaxyReview? nextNatural=null;
                GeneratedGalaxyMapData document;
                switch(codec.GeneratedGalaxyFormat(bytes))
                {
                    case 1:
                        next=GeneratedGalaxyApplication.Open(codec.DecodeGeneratedGalaxy(bytes),civilizations);
                        document=new GeneratedGalaxyMapData(next.Export());break;
                    case 2:
                        nextEnvironment=EnvironmentGalaxyApplication.Open(codec.DecodeEnvironmentGalaxy(bytes),civilizations);
                        document=new GeneratedGalaxyMapData(nextEnvironment.Export());break;
                    case 3:
                        nextScaled=ScaledGalaxyApplication.Open(codec.DecodeScaledGalaxy(bytes),civilizations);
                        document=new GeneratedGalaxyMapData(nextScaled.Export());break;
                    case 4:
                        nextNatural=NaturalGalaxyApplication.Open(codec.DecodeNaturalGalaxy(bytes),civilizations);
                        document=new GeneratedGalaxyMapData(nextNatural.Export());break;
                    default:throw new InvalidDataException("Unsupported generated-map format.");
                }
                // Commit the replacement only after decoding and portable validation both succeed.
                review=next;environmentReview=nextEnvironment;scaledReview=nextScaled;naturalReview=nextNatural;Document=document;Seed=document.Settings.Seed;Size=document.Settings.SystemCount;Unexported=false;
                if(document.Scaled!=null)QuietEmptyAreaTargetPermille=document.Scaled.Settings.QuietEmptyAreaTargetPermille;
                if(document.Natural!=null)QuietEmptyAreaTargetPermille=document.Natural.Settings.Map.QuietEmptyAreaTargetPermille;
                Notice="Opened map slot "+Slot+" · seed "+Seed+" · "+Size+" systems.";return true;
            }
            catch(FileNotFoundException){Notice="Map slot "+Slot+" is empty. Current map is kept.";return false;}
            catch(DirectoryNotFoundException){Notice="Map slot "+Slot+" is empty. Current map is kept.";return false;}
            catch(Exception error) when(error is IOException||error is InvalidDataException||error is UnauthorizedAccessException||error is ArgumentException||error is InvalidOperationException)
            {Notice="Map could not be opened: invalid, incompatible or unavailable file. Current map is kept.";return false;}
        }
        public StrategicRouteSnapshot FindRoute(string origin,string destination)
        {
            if(naturalReview!=null)return naturalReview.FindRoute(origin,destination);
            if(scaledReview!=null)return scaledReview.FindRoute(origin,destination);
            if(environmentReview!=null)return environmentReview.FindRoute(origin,destination);
            if(review==null)throw new InvalidOperationException("No generated map is open.");
            return review.FindRoute(origin,destination);
        }
        public GalaxyReviewRoutePlanSnapshot FindRoutePlan(GalaxyReviewRouteQueryDto query)
        {
            if(naturalReview!=null)return naturalReview.FindRoutePlan(query);
            if(scaledReview!=null)return scaledReview.FindRoutePlan(query);
            if(environmentReview!=null)return environmentReview.FindRoutePlan(query);
            if(review!=null)return review.FindRoutePlan(query);
            throw new InvalidOperationException("No generated map is open.");
        }
        public byte[] ExportBytes()
        {
            if(naturalReview!=null)return codec.EncodeNaturalGalaxy(naturalReview.Export());
            if(scaledReview!=null)return codec.EncodeScaledGalaxy(scaledReview.Export());
            if(environmentReview!=null)return codec.EncodeEnvironmentGalaxy(environmentReview.Export());
            if(review!=null)return codec.EncodeGeneratedGalaxy(review.Export());
            throw new InvalidOperationException("No generated map is open.");
        }
    }
}
