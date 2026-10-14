"""Integrated source/resource boundaries only; no Editor, AOT, native or render proof."""
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
EXPECTED = "88a919f6f51d244f79bb638ba933ff3c17305f25231a6b793aa6d53fca5f688b"
def check(root):
    errors=[]
    def need(value,message):
        if not value: errors.append(message)
    assets=root/"client/HobbyDS9.Unity/Assets/HobbyDS9"
    raw=root/"scenarios/integrated-playtest-v1"
    resources=assets/"Review/Resources/IntegratedPackages/v1"
    hashes={}
    for name in ("package","physical","core","scenario"):
        source=(raw/(name+".json")).read_bytes();target=resources/(name+".json")
        need(target.exists() and target.read_bytes()==source,"Integrated raw resource bytes differ: "+name)
        need(Path(str(target)+".meta").is_file(),"Missing integrated resource metadata: "+name)
        hashes[name]=hashlib.sha256(source).hexdigest()
    def digest(text):return hashlib.sha256(text.encode("ascii")).hexdigest()
    core=digest("campaign-tactical-review-package-v1\n"+hashes["core"]+"\n"+hashes["scenario"]+"\n")
    physical=digest("campaign-physical-review-package-v2\n"+hashes["physical"]+"\n"+core+"\n")
    outer=digest("campaign-integrated-playtest-package-v3\n"+hashes["package"]+"\n"+physical+"\n")
    need(outer==EXPECTED,"Integrated package pin differs")
    info=json.loads((raw/"package.json").read_text())
    need(info.get("FormatVersion")==3 and info.get("DefaultActivationAllowed") is False,"Integrated source must remain format3 opt-in")
    default=(assets/"Runtime/ClientEntry.cs").read_text(encoding="utf-8-sig")
    need('Array.IndexOf(reviewArgs,"--hobbyds9-integrated-playtest")>=0' in default and 'HobbyDS9.UnityReview.IntegratedReviewLaunch, HobbyDS9.UnityReview' in default,"Missing explicit integrated selector/bridge")
    need('Array.IndexOf(reviewArgs,"--hobbyds9-physical-review")>=0' in default and 'HobbyDS9.UnityReview.ReviewLaunch, HobbyDS9.UnityReview' in default,"Physical selector changed")
    need('QualitySettings.vSyncCount = 1;' in default and 'UnityEngine.Application.targetFrameRate = -1;' in default,"VSync changed")
    runtime=assets/"Review/Runtime"
    options=(runtime/"IntegratedLaunchOptions.cs").read_text()
    need("DefaultEnabled=false" in options and '"--hobbyds9-integrated-actor"' in options,"Integrated launch must remain explicit")
    entry=(runtime/"ReviewEntry.cs").read_text()
    need('"IntegratedPackages/v1/"+name' in entry and 'Bytes("physical")' in entry and 'return asset.bytes' in entry,"Integrated entry must consume all exact inputs")
    need('HobbyDS9IntegratedPlaytestOnly-v3' in entry and 'HobbyDS9PhysicalReviewOnly-v2' in entry,"Slot directories must remain separate")
    host=(root/"prototypes/HobbyDS9.TacticalReviewClient/PhysicalReviewHost.Integrated.cs").read_text()
    need('.ExportIntegratedSave()' in host and '.ExportSave()' not in host,"Integrated trusted carrier must be the full format3 root")
    view=(runtime/"ReviewStrategyView.cs").read_text()
    need('Frame.Strategy' in view and not any(x in view for x in ('ExportSave','ExportIntegratedSave','GameStateDto','CampaignSnapshot','new ReviewUiModel','CampaignApplication.Start')),"Strategy view must use scoped projections, never a parallel/private campaign")
    store=(runtime/"ReviewIntegratedSlotStore.cs").read_text()
    need('unity-integrated-playtest-' in store and 'EncodeIntegratedRoot' in store and 'DecodeIntegratedRoot' in store,"Wrong integrated store carrier/prefix")
    build=(assets/"Review/Editor/IntegratedBuild.cs").read_text()
    need('artifacts/unity-integrated/Windows/HobbyDS9IntegratedPlaytest.exe' in build and 'Review/Scenes/IntegratedPlaytest.unity' in build and 'Launch-IntegratedPlaytest.cmd' in build,"Integrated builder must have separate output/scene and explicit launcher")
    need('artifacts/unity-review/Windows/HobbyDS9Review.exe' not in build,"Integrated builder overwrites old review output")
    return errors
if __name__=="__main__":
    failures=check(ROOT)
    for error in failures:print("ERROR:",error)
    if failures:raise SystemExit(1)
    print("Integrated four-byte-source/hash/explicit launch/scoped view/full-root/separate output checks passed; Editor/native NOT executed.")
