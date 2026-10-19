"""Observer contract checks only; not Unity, callback or native-input execution."""
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
REL = "client/HobbyDS9.Unity/Assets/HobbyDS9/Diagnostics/NativeInputEvidence/NativeInputEvidence.cs"


def check(root):
    source = (root / REL).read_text(encoding="utf-8-sig")
    errors = []
    def need(condition, message):
        if not condition:
            errors.append(message)
    need('"--hobbyds9-native-input-evidence") < 0) return;' in source,
         "Observer must stay behind the explicit opt-in flag")
    need('MaxRecords = 512, MaxRoots = 16, MaxKeyboards = 4' in source and
         'DurationSeconds = 1800' in source and 'records >= MaxRecords-1' in source,
         "Observer duration/record/root/device limits must remain bounded")
    for event, handler in (("KeyDownEvent", "PanelKey"), ("KeyUpEvent", "PanelKeyUp"),
                           ("NavigationSubmitEvent", "PanelSubmit"), ("NavigationMoveEvent", "PanelMove"),
                           ("FocusInEvent", "PanelFocus")):
        need(f'panel.RegisterCallback<{event}>({handler},TrickleDown.TrickleDown);' in source and
             f'panel.UnregisterCallback<{event}>({handler},TrickleDown.TrickleDown);' in source,
             "Observer panel callbacks must unsubscribe: " + event)
    need('e.destinationPanel.visualTree' in source and 'ReferenceEquals(e.target,binding.Root)' in source and
         'UnbindDocument(binding)' in source and 'panels.Remove(panel)' in source and
         'inputEvent.type != StateEvent.Type && inputEvent.type != DeltaStateEvent.Type' in source,
         "Observer must follow document/current-panel lifecycle and named state events")
    need(all(x in source for x in ('keyboard.enterKey', 'keyboard.tabKey', 'keyboard.numpadEnterKey', 'keyboard.spaceKey')) and
         '.allKeys' not in source and '.character' not in source and '.compositionString' not in source,
         "Observer must inspect only named controls, never typed text or all keys")
    need('type.GetField("returnHeld",InstanceFields)' in source and 'type.GetField("spaceHeld",InstanceFields)' in source and
         'type.FullName!="HobbyDS9.UnityReview.ReviewEntry"' in source and
         not re.search(r'GetField\("(?!returnHeld"|spaceHeld")[^"]+",InstanceFields\)', source),
         "New component reflection must be restricted to the two own latch flags")
    need(not re.search(r'\.(?:StopPropagation|StopImmediatePropagation|PreventDefault|SendEvent|Focus|Enable|Disable|SetValue|SetValueWithoutNotify|Invoke|SetValueDirect)\s*\(', source) and
         'InputSystem.Update(' not in source and 'InputSystem.AddDevice(' not in source and
         'Keyboard.current.' not in source and 'InputSystem.settings' not in source and '.text' not in source,
         "Observer must never consume, dispatch, focus, enable or mutate input/UI state")
    return errors


if __name__ == "__main__":
    failures = check(ROOT)
    for failure in failures:
        print("ERROR:", failure)
    if failures:
        raise SystemExit(1)
    print("Bounded opt-in named-input/current-panel/readonly observer checks passed; Unity/native NOT executed.")
