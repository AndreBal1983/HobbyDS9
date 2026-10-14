#nullable enable
using UnityEngine.UIElements;
namespace HobbyDS9.UnityClient
{
    // Presentation-only bridge. No ClientEntry, campaign factory or simulation commands.
    public static class IntegratedAudioPanelBridge
    {
        public static void Present(VisualElement root)
        {
            var music=AudioDirector.TryGetOrCreate();var sfx=SfxDirector.TryGetOrCreate();
            root.Add(new Label("Sound aboard the console"){name="audio-heading"});
            root.Add(new Label("Audio settings are device preferences. They do not change your campaign or saved orders."));
            if(music==null){root.Add(new Label("Music is currently unavailable. You can continue playing."));}
            else
            {
                root.Add(new Label(music.Status){name="audio-status"});
                var master=new Slider("Master volume",0,1){name="audio-master",value=music.Preferences.MasterVolume};
                var volume=new Slider("Music volume",0,1){name="audio-music",value=music.Preferences.MusicVolume};
                var mute=new Toggle("Mute music"){name="audio-music-mute",value=music.Preferences.Muted};
                master.RegisterValueChangedCallback(e=>music.SetMasterVolume(e.newValue));volume.RegisterValueChangedCallback(e=>music.SetMusicVolume(e.newValue));mute.RegisterValueChangedCallback(e=>music.SetMuted(e.newValue));
                root.Add(master);root.Add(volume);root.Add(mute);
            }
            if(sfx!=null)ClientEntry.AddSfxControls(root,sfx);else root.Add(new Label("Button sounds are currently unavailable."));
        }
    }
}
