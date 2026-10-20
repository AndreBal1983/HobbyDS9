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
            if(Environment!=null)return Environment.Features.Single(f=>f.Kind==GalaxyEnvironmentFeatureKind.Star&&f.SystemId==systemId).Position;
            return Regions.Single(r=>r.NodeId==Systems.Single(s=>s.SystemId==systemId).NodeId).Center;
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
        public GeneratedGalaxyMapData? Document { get; private set; }
        public bool Unexported { get; private set; }
        public string Notice { get; private set; }="Choose a seed and size, then generate a map.";
        public string Seed { get; set; }="20261020";
        public int Size { get; set; }=40;
        public int Slot { get; set; }=1;
        public string Location=>store.Location;
        public IReadOnlyDictionary<string,string> CivilizationNames { get; }
        public GeneratedGalaxyWorkspace(IEnumerable<CivilizationDefinitionDto> catalog,GeneratedGalaxyMapStore mapStore)
        {
            civilizations=catalog.Select(c=>new CivilizationDefinitionDto{DefinitionId=c.DefinitionId,DisplayName=c.DisplayName}).ToList();
            CivilizationNames=civilizations.ToDictionary(c=>c.DefinitionId,c=>c.DisplayName,StringComparer.Ordinal);
            store=mapStore;
        }
        public bool Generate()
        {
            try
            {
                var settings=new GeneratedGalaxySettingsDto{Seed=Seed,SystemCount=Size,
                    HomeCivilizationDefinitionIds=new List<string>{"civilization.federation","civilization.cardassian"}};
                var next=EnvironmentGalaxyApplication.Generate(new EnvironmentGalaxySettingsDto{Map=settings,
                    StellarInfluenceEnabled=true,BlackHoleInfluenceEnabled=true,NebulaInfluenceEnabled=true},civilizations);
                var document=new GeneratedGalaxyMapData(next.Export());
                environmentReview=next;review=null;Document=document;Unexported=true;
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
            if(review==null&&environmentReview==null){Notice="Generate or open a map first.";return false;}
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
                var bytes=store.Read(Slot);GeneratedGalaxyReview? next=null;EnvironmentGalaxyReview? nextEnvironment=null;
                GeneratedGalaxyMapData document;
                switch(codec.GeneratedGalaxyFormat(bytes))
                {
                    case 1:
                        next=GeneratedGalaxyApplication.Open(codec.DecodeGeneratedGalaxy(bytes),civilizations);
                        document=new GeneratedGalaxyMapData(next.Export());break;
                    case 2:
                        nextEnvironment=EnvironmentGalaxyApplication.Open(codec.DecodeEnvironmentGalaxy(bytes),civilizations);
                        document=new GeneratedGalaxyMapData(nextEnvironment.Export());break;
                    default:throw new InvalidDataException("Unsupported generated-map format.");
                }
                // Commit the replacement only after decoding and portable validation both succeed.
                review=next;environmentReview=nextEnvironment;Document=document;Seed=document.Settings.Seed;Size=document.Settings.SystemCount;Unexported=false;
                Notice="Opened map slot "+Slot+" · seed "+Seed+" · "+Size+" systems.";return true;
            }
            catch(FileNotFoundException){Notice="Map slot "+Slot+" is empty. Current map is kept.";return false;}
            catch(DirectoryNotFoundException){Notice="Map slot "+Slot+" is empty. Current map is kept.";return false;}
            catch(Exception error) when(error is IOException||error is InvalidDataException||error is UnauthorizedAccessException||error is ArgumentException||error is InvalidOperationException)
            {Notice="Map could not be opened: invalid, incompatible or unavailable file. Current map is kept.";return false;}
        }
        public StrategicRouteSnapshot FindRoute(string origin,string destination)
        {
            if(environmentReview!=null)return environmentReview.FindRoute(origin,destination);
            if(review==null)throw new InvalidOperationException("No generated map is open.");
            return review.FindRoute(origin,destination);
        }
        public byte[] ExportBytes()
        {
            if(environmentReview!=null)return codec.EncodeEnvironmentGalaxy(environmentReview.Export());
            if(review!=null)return codec.EncodeGeneratedGalaxy(review.Export());
            throw new InvalidOperationException("No generated map is open.");
        }
    }
}
