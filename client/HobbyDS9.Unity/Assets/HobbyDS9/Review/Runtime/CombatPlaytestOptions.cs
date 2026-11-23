#nullable enable
using System;
using HobbyDS9.Contracts;

namespace HobbyDS9.UnityReview
{
    // Explicit host composition seam. No normal campaign selection or gameplay rules change.
    public static class CombatPlaytestOptions
    {
        public const string ResourceRoot="CombatPlaytestV1/";
        public const string SaveDirectory="HobbyDS9CombatPlaytestOnly-v1";
        public static string[] Slots()=>new[]{"combat-1","combat-2","combat-3"};
        public const string DisplayName="Close-contact combat test (authored fixture)";
        public const string Description="Fixed Bajor military-loop fixture, not a balanced generated campaign. Existing fleets have ordinary initial travel orders. End turn to meet the opponent within 1–3 turns; use the normal planner, explicit rounds and aftermath. Separate test saves.";
        public static string[] InputNames()=>new[]{"demo","package","physical","core","scenario"};
        public static CampaignSingleplayerDemoPackageDto CreatePackage(byte[][] bytes,ReviewNewtonsoftCodec codec)
        {
            if(bytes==null||bytes.Length!=5)throw new ArgumentException("Five combat-test resources required.",nameof(bytes));
            var package=ReviewExactPackageLoader.LoadDemo(bytes[0],bytes[1],bytes[2],bytes[3],bytes[4],codec);
            if(package.Integrated!.Physical!.CorePackage!.Campaign!.ScenarioId!="scenario.combat-playtest-v1")
                throw new ArgumentException("Explicit combat-test scenario required; no normal campaign fallback.",nameof(bytes));
            return package;
        }
    }
}
