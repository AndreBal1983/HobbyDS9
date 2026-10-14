# Opt-in physical review Unity binding — active checkpoint

14 October 2026. Base: PR #29, `372de4ca8d74f43dc723238461036e9aa6d7e605`. Branch: `codex/tactical-unity-review-binding`. This is the current implementation assignment; the historical UX reconciliation at `455bf10` is a different completed task.

In progress: actual Unity-compatible Newtonsoft complete-root codec, exact-byte package loader, disjoint atomic review slots, portable UI host model and explicitly selected UIElements shell. Keep all existing uncommitted work. Default v1 remains the normal path; review requires explicit flags. No backend or default activation changes are authorized.

Ownership: this branch owns new Review Runtime/Editor/Tests/Scenes source, asmdefs and their metadata, plus the minimal existing ClientEntry bootstrap selector and link.xml additions. Primary owns PrepareUnityReview.proj, its checker/workflow, generated portable DLL/meta and Review/Resources/ReviewPackages exact-byte assets and parent metadata. Do not import unfinished Primary work or edit those paths. Expected resource names: `ReviewPackages/{faster,slower}/{package,core,scenario}`.

Authorized validation: portable actual-source serializer/controller/slot tests and static compile/source checks. **Unity Editor, Unity EditMode execution, native builds/player/helpers and desktop input are prohibited during this assignment.** Authored Editor tests, linker settings, scene/build helpers and layout policies do not prove Editor/AOT/native/visual acceptance. The three planned sizes are 1280×720, 1920×1080 and 3440×1440.

Complete physical format-2 roots must reach actual Application load validation before live replacement. Never import net10 storage/shell, extract nested Core as a save, use JsonUtility, reserialize source for hashing, expose private roots or invent gameplay calculations. Preserve failed-load/live-draft and failed-overwrite/prior-slot behavior, explicit hotseat actor consent, busy/stale callback guards and lawful views.

Next: compile/test the six actual non-engine Runtime source files through a netstandard2.1 adapter project; add complete-root/resume/negative/atomic/actor tests, Unity asmdefs/metas and authored EditMode checks. Finish reviewable thin entry and report exact executed checks versus unperformed runtime gates. Draft PR only; no merges. Deadline: 08:30 Amsterdam on 14 October.
