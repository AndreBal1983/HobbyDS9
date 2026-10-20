#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HobbyDS9.Application;
using HobbyDS9.Contracts;

namespace HobbyDS9.UnityReview
{
    // Separate presentation workspace; never holds or advances a campaign session.
    public sealed class GeneratedGalaxyWorkspace
    {
        private readonly List<CivilizationDefinitionDto> civilizations;
        private readonly ReviewNewtonsoftCodec codec=new ReviewNewtonsoftCodec();
        private readonly GeneratedGalaxyMapStore store;
        private GeneratedGalaxyReview? review;
        public GeneratedGalaxyDocumentDto? Document { get; private set; }
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
                var next=GeneratedGalaxyApplication.Generate(settings,civilizations);
                var document=next.Export();
                review=next;Document=document;Unexported=true;
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
            if(review==null){Notice="Generate or open a map first.";return false;}
            try
            {
                store.Write(Slot,codec.EncodeGeneratedGalaxy(review.Export()),overwrite);
                Unexported=false;Notice="Exported to map slot "+Slot+". Campaign saves are separate.";return true;
            }
            catch(Exception error) when(error is IOException||error is UnauthorizedAccessException||error is ArgumentException)
            {Notice="Export failed. Current map and any previous export are kept.";return false;}
        }
        public bool Open()
        {
            try
            {
                var next=GeneratedGalaxyApplication.Open(codec.DecodeGeneratedGalaxy(store.Read(Slot)),civilizations);
                var document=next.Export();
                // Commit the replacement only after decoding and portable validation both succeed.
                review=next;Document=document;Seed=document.Settings.Seed;Size=document.Settings.SystemCount;Unexported=false;
                Notice="Opened map slot "+Slot+" · seed "+Seed+" · "+Size+" systems.";return true;
            }
            catch(FileNotFoundException){Notice="Map slot "+Slot+" is empty. Current map is kept.";return false;}
            catch(DirectoryNotFoundException){Notice="Map slot "+Slot+" is empty. Current map is kept.";return false;}
            catch(Exception error) when(error is IOException||error is UnauthorizedAccessException||error is ArgumentException||error is InvalidOperationException)
            {Notice="Map could not be opened: invalid, incompatible or unavailable file. Current map is kept.";return false;}
        }
        public StrategicRouteSnapshot FindRoute(string origin,string destination)
        {
            if(review==null)throw new InvalidOperationException("No generated map is open.");
            return review.FindRoute(origin,destination);
        }
    }
}
