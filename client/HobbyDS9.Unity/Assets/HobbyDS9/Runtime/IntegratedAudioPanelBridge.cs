#nullable enable
using UnityEngine;
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
            panel.AddToClassList("console-panel");panel.style.width=760;panel.style.maxWidth=new Length(100,LengthUnit.Percent);
            panel.style.marginTop=10;panel.style.marginBottom=10;panel.style.paddingLeft=12;panel.style.paddingRight=12;
            var title=new Label("Music"){name="music-now-playing"};panel.Add(title);
            var status=new Label(music.Status){name="audio-status"};panel.Add(status);
            var tracks=new System.Collections.Generic.List<string>();
            foreach(var track in music.Tracks)tracks.Add(track.Name);
            var selection=new DropdownField("Track",tracks,-1){name="music-track-select"};panel.Add(selection);
            selection.RegisterValueChangedCallback(_=>{if(selection.index>=0&&selection.index<music.Tracks.Count)music.PlayTrack(music.Tracks[selection.index].Id);});
            var seek=new Slider(0,1){name="music-seek",tooltip="Seek within the current song"};panel.Add(seek);
            var scrubbing=false;
            seek.RegisterCallback<PointerDownEvent>(e=>{if(e.button==0)scrubbing=true;},TrickleDown.TrickleDown);
            void CommitSeek(){if(scrubbing){scrubbing=false;music.Seek(seek.value);}}
            seek.RegisterCallback<PointerUpEvent>(_=>CommitSeek(),TrickleDown.TrickleDown);
            seek.RegisterCallback<PointerCaptureOutEvent>(_=>CommitSeek());
            seek.RegisterValueChangedCallback(e=>{if(!scrubbing)music.Seek(e.newValue);});
            var row=new VisualElement();row.style.flexDirection=FlexDirection.Row;row.style.flexWrap=Wrap.Wrap;panel.Add(row);
            Transport(row,"music-previous","Previous",()=>music.StepTrack(-1),()=>"previous");
            var pause=Transport(row,"music-pause","Pause",()=>music.TogglePause(),()=>music.IsPaused?"play":"pause");
            Transport(row,"music-stop","Stop",()=>music.StopMusic(0),()=>"stop");
            Transport(row,"music-next","Next",()=>music.StepTrack(1),()=>"next");
            var time=new Label("0:00 / 0:00"){name="music-time"};time.style.alignSelf=Align.Center;time.style.marginLeft=8;time.style.marginRight=12;row.Add(time);
            var automatic=new Button(()=>music.UseAutomaticPlaylist()){text="Auto",tooltip="Return to the automatic playlist",name="music-automatic"};automatic.AddToClassList("review-action");row.Add(automatic);
            var volume=new Slider("Volume",0,1){name="audio-music",value=music.Preferences.MusicVolume};volume.style.minWidth=150;volume.style.flexGrow=1;volume.style.alignSelf=Align.Center;
            volume.RegisterValueChangedCallback(e=>music.SetMusicVolume(e.newValue));row.Add(volume);
            void Refresh()
            {
                if(music==null)return;
                var position=System.TimeSpan.FromSeconds(music.PositionSeconds);var duration=System.TimeSpan.FromSeconds(music.DurationSeconds);
                title.text="Music";
                time.text=position.ToString(@"m\:ss")+" / "+duration.ToString(@"m\:ss");
                pause.tooltip=music.IsPaused?"Play":"Pause";pause.MarkDirtyRepaint();foreach(var icon in pause.Children())icon.MarkDirtyRepaint();
                status.text=music.Status;
                status.style.display=music.Status.Contains("unavailable")||music.Status.StartsWith("No playable")?DisplayStyle.Flex:DisplayStyle.None;
                automatic.SetEnabled(!music.AutomaticPlaylist);
                seek.SetEnabled(music.DurationSeconds>0);if(!scrubbing)seek.SetValueWithoutNotify(music.DurationSeconds>0?music.PositionSeconds/music.DurationSeconds:0);
                var index=-1;for(var i=0;i<music.Tracks.Count;i++)if(music.Tracks[i].Id==music.CurrentTrackId){index=i;break;}
                if(index>=0)selection.SetValueWithoutNotify(tracks[index]);
                volume.SetValueWithoutNotify(music.Preferences.MusicVolume);
            }
            Refresh();panel.schedule.Execute(Refresh).Every(250);
        }
        private static Button Transport(VisualElement row,string name,string tooltip,System.Action action,System.Func<string> symbol)
        {
            var button=new Button(action){name=name,tooltip=tooltip};button.AddToClassList("review-action");button.style.width=44;button.style.height=44;button.style.flexShrink=0;
            var icon=new VisualElement{pickingMode=PickingMode.Ignore};icon.style.width=18;icon.style.height=18;icon.style.alignSelf=Align.Center;
            icon.generateVisualContent+=context=>{
                var p=context.painter2D;p.fillColor=new Color(.92f,.92f,.95f);var kind=symbol();
                void Polygon(params Vector2[] points){p.BeginPath();p.MoveTo(points[0]);for(var i=1;i<points.Length;i++)p.LineTo(points[i]);p.ClosePath();p.Fill();}
                void Rect(float x,float width)=>Polygon(new Vector2(x,2),new Vector2(x+width,2),new Vector2(x+width,16),new Vector2(x,16));
                if(kind=="pause"){Rect(3,4);Rect(11,4);}else if(kind=="stop"){Rect(2,14);}
                else if(kind=="previous"){Rect(2,3);Polygon(new Vector2(15,2),new Vector2(5,9),new Vector2(15,16));}
                else {if(kind=="next")Rect(14,3);Polygon(new Vector2(3,2),new Vector2(kind=="next"?13:16,9),new Vector2(3,16));}
            };
            button.Add(icon);row.Add(button);return button;
        }
        public static void Present(VisualElement root)
        {
            var music=AudioDirector.TryGetOrCreate();var sfx=SfxDirector.TryGetOrCreate();
            root.Add(new Label("Sound aboard the console"){name="audio-heading"});
            root.Add(new Label("Audio settings are device preferences. They do not change your campaign or saved orders."));
            if(music==null){root.Add(new Label("Music is currently unavailable. You can continue playing."));}
            else
            {
                var layout=new VisualElement();layout.AddToClassList("audio-preference-layout");root.Add(layout);
                var player=new VisualElement();player.AddToClassList("audio-player-group");layout.Add(player);PresentPlayer(player);
                var levels=new VisualElement();levels.AddToClassList("audio-levels-group");layout.Add(levels);
                var heading=new Label("Output levels");heading.AddToClassList("console-section");levels.Add(heading);
                var master=new Slider("Master volume",0,1){name="audio-master",value=music.Preferences.MasterVolume};
                var mute=new Toggle("Mute music"){name="audio-music-mute",value=music.Preferences.Muted};
                master.RegisterValueChangedCallback(e=>music.SetMasterVolume(e.newValue));mute.RegisterValueChangedCallback(e=>music.SetMuted(e.newValue));
                levels.Add(master);levels.Add(mute);
                if(sfx!=null)ClientEntry.AddSfxControls(levels,sfx);else levels.Add(new Label("Button sounds are currently unavailable."));
            }
            if(music==null){if(sfx!=null)ClientEntry.AddSfxControls(root,sfx);else root.Add(new Label("Button sounds are currently unavailable."));}
        }
    }
}
