#nullable enable
using System.IO;

namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewUiModel
    {
        public bool CombatPlaytest=>demoPackage?.Integrated?.Physical?.CorePackage?.Campaign?.ScenarioId=="scenario.combat-playtest-v1";
        // Shared New/Continue composition: exact authored bytes, existing host and
        // a separate store. The caller supplies its own persistent-data root.
        public static ReviewUiModel StartCombatPlaytest(byte[][] inputs,string persistentDataRoot)
        {
            var codec=new ReviewNewtonsoftCodec();
            var package=CombatPlaytestOptions.CreatePackage(inputs,codec);
            return new ReviewUiModel(package,codec,new ReviewDemoSlotStore(Path.Combine(persistentDataRoot,CombatPlaytestOptions.SaveDirectory),codec));
        }
    }
}
