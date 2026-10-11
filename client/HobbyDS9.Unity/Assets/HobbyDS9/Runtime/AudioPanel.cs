#nullable enable
using UnityEngine.UIElements;

namespace HobbyDS9.UnityClient
{
    public sealed partial class ClientEntry
    {
        private void ShowAudio()
        {
            Text("Audio", true);
            var director = AudioDirector.TryGetOrCreate();
            if (director == null) { Text("Audio unavailable. The campaign remains playable."); return; }
            Text(director.Status);
            Text("Strategic music continues through selection, turns, save/load and combat. Audio settings are client preferences, separate from campaign saves.");
            var master = new Slider("Master volume", 0, 1) { value = director.Preferences.MasterVolume };
            var music = new Slider("Music volume", 0, 1) { value = director.Preferences.MusicVolume };
            var mute = new Toggle("Mute music") { value = director.Preferences.Muted };
            master.AddToClassList("combat-plan-field"); music.AddToClassList("combat-plan-field");
            master.RegisterValueChangedCallback(e => director.SetMasterVolume(e.newValue));
            music.RegisterValueChangedCallback(e => director.SetMusicVolume(e.newValue));
            mute.RegisterValueChangedCallback(e => director.SetMuted(e.newValue));
            details.Add(master); details.Add(music); details.Add(mute);
            var sfx = SfxDirector.TryGetOrCreate();
            if (sfx != null) AddSfxControls(details, sfx);
        }
        public static void AddSfxControls(VisualElement container, SfxDirector sfx)
        {
            var volume = new Slider("SFX volume", 0, 1) { name = "sfx-volume", value = sfx.Preferences.Volume };
            var muted = new Toggle("Mute SFX") { name = "sfx-mute", value = sfx.Preferences.Muted };
            volume.AddToClassList("combat-plan-field");
            volume.RegisterValueChangedCallback(e => sfx.SetVolume(e.newValue));
            muted.RegisterValueChangedCallback(e => sfx.SetMuted(e.newValue));
            container.Add(volume); container.Add(muted);
        }
    }
}
