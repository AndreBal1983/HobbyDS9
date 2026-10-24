#nullable enable
using UnityEngine.UIElements;
namespace HobbyDS9.UnityClient
{
    // Presentation-only bridge. No ClientEntry, campaign factory or simulation commands.
    public static class IntegratedAudioPanelBridge
    {
        public static void PresentPlayer(VisualElement root)
        {
            var music=AudioDirector.TryGetOrCreate();
            if(music==null)return;
            var panel=new VisualElement{name="menu-music-player"};root.Add(panel);
            panel.style.marginTop=10;panel.style.marginBottom=10;
            var title=new Label("Music"){name="music-now-playing"};panel.Add(title);
            var status=new Label(music.Status){name="audio-status"};panel.Add(status);
            var tracks=new System.Collections.Generic.List<string>();
            foreach(var track in music.Tracks)tracks.Add(track.Name);
            var selection=new DropdownField("Track",tracks,-1){name="music-track-select"};panel.Add(selection);
            selection.RegisterValueChangedCallback(_=>{if(selection.index>=0&&selection.index<music.Tracks.Count)music.PlayTrack(music.Tracks[selection.index].Id);});
            var row=new VisualElement();row.style.flexDirection=FlexDirection.Row;row.style.flexWrap=Wrap.Wrap;panel.Add(row);
            var previous=new Button(()=>music.StepTrack(-1)){text="Previous",name="music-previous"};row.Add(previous);
            var pause=new Button(()=>music.TogglePause()){text="Pause",name="music-pause"};row.Add(pause);
            var next=new Button(()=>music.StepTrack(1)){text="Next",name="music-next"};row.Add(next);
            var automatic=new Button(()=>music.UseAutomaticPlaylist()){text="Automatic playlist",name="music-automatic"};row.Add(automatic);
            foreach(var button in new[]{previous,pause,next,automatic}){button.style.minHeight=36;button.style.flexGrow=1;}
            var volume=new Slider("Music volume",0,1){name="audio-music",value=music.Preferences.MusicVolume};
            volume.RegisterValueChangedCallback(e=>music.SetMusicVolume(e.newValue));panel.Add(volume);
            void Refresh()
            {
                if(music==null)return;
                var position=System.TimeSpan.FromSeconds(music.PositionSeconds);var duration=System.TimeSpan.FromSeconds(music.DurationSeconds);
                title.text=music.CurrentTrackName+"  ·  "+position.ToString(@"m\:ss")+" / "+duration.ToString(@"m\:ss");
                pause.text=music.IsPaused?"Resume":"Pause";
                status.text=music.Status;
                automatic.SetEnabled(!music.AutomaticPlaylist);
                var index=-1;for(var i=0;i<music.Tracks.Count;i++)if(music.Tracks[i].Id==music.CurrentTrackId){index=i;break;}
                if(index>=0)selection.SetValueWithoutNotify(tracks[index]);
                volume.SetValueWithoutNotify(music.Preferences.MusicVolume);
            }
            Refresh();panel.schedule.Execute(Refresh).Every(250);
        }
        public static void Present(VisualElement root)
        {
            var music=AudioDirector.TryGetOrCreate();var sfx=SfxDirector.TryGetOrCreate();
            root.Add(new Label("Sound aboard the console"){name="audio-heading"});
            root.Add(new Label("Audio settings are device preferences. They do not change your campaign or saved orders."));
            if(music==null){root.Add(new Label("Music is currently unavailable. You can continue playing."));}
            else
            {
                PresentPlayer(root);
                var master=new Slider("Master volume",0,1){name="audio-master",value=music.Preferences.MasterVolume};
                var mute=new Toggle("Mute music"){name="audio-music-mute",value=music.Preferences.Muted};
                master.RegisterValueChangedCallback(e=>music.SetMasterVolume(e.newValue));mute.RegisterValueChangedCallback(e=>music.SetMuted(e.newValue));
                root.Add(master);root.Add(mute);
            }
            if(sfx!=null)ClientEntry.AddSfxControls(root,sfx);else root.Add(new Label("Button sounds are currently unavailable."));
        }
    }
}
