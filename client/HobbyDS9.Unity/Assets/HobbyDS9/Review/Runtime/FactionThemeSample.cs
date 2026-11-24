#nullable enable
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityReview
{
    // Disconnected reference widget. The caller supplies validated tokens and explicit
    // lawful identity, or deliberately requests a proposed-profile preview. No global styles.
    public sealed class FactionThemeSample:VisualElement
    {
        public FactionThemeSelection Selection {get;}
        public bool ReducedMotion {get;}
        public int TransitionMilliseconds {get;}
        public static FactionThemeSample ForIdentity(FactionThemeTokens tokens,FactionThemeIdentity identity,bool reducedMotion=true)
            =>new FactionThemeSample(tokens,tokens.Resolve(identity),reducedMotion);
        public static FactionThemeSample ForProfilePreview(FactionThemeTokens tokens,string? profileId,bool reducedMotion=true)
            =>new FactionThemeSample(tokens,tokens.PreviewProfile(profileId),reducedMotion);
        private FactionThemeSample(FactionThemeTokens tokens,FactionThemeSelection selection,bool reducedMotion)
        {
            Selection=selection;ReducedMotion=reducedMotion;TransitionMilliseconds=reducedMotion?0:tokens.TransitionMs;
            name="faction-theme-reference-sample";AddToClassList("faction-theme-reference-sample");
            var profile=selection.Profile;var family=profile.Family;
            style.backgroundColor=ColorOf(profile.Colors["background"]);style.color=ColorOf(tokens.SharedColors["textPrimary"]);
            style.paddingLeft=style.paddingRight=16;style.paddingTop=style.paddingBottom=16;style.minWidth=0;style.flexGrow=1;
            style.borderTopLeftRadius=style.borderBottomRightRadius=family.CornerRadius;
            Label(this,"PROPOSED REFERENCE SAMPLE",tokens.SecondaryPx,"faction-theme-proposal-label");
            var heading=Label(this,(selection.IsProfilePreview?"Manual profile preview · ":"Lawful catalogue binding · ")+profile.DisplayName,tokens.HeadingPx,"faction-theme-profile-label");
            heading.style.unityFontStyleAndWeight=family.HeadingWeight>=700?FontStyle.Bold:FontStyle.Normal;
            Label(this,selection.ArtifactStatus,tokens.SecondaryPx,"faction-theme-artifact-status");
            var chrome=new Chrome(family,profile.Colors["brandPrimary"],profile.Colors["brandSecondary"]){name="faction-theme-family-chrome"};
            chrome.tooltip=family.Frame+" · "+family.SectionMotif+" · proposed geometry";chrome.style.height=96;chrome.style.overflow=Overflow.Hidden;chrome.style.marginTop=12;chrome.style.marginBottom=12;Add(chrome);
            var layout=new VisualElement{name="faction-theme-reference-layout"};layout.style.flexDirection=FlexDirection.Row;layout.style.flexWrap=Wrap.Wrap;Add(layout);
            var nav=new VisualElement{name="faction-theme-reference-navigation"};nav.style.width=tokens.NavigationWidthPx;nav.style.marginRight=16;layout.Add(nav);
            foreach(var title in new[]{"System reference","Worlds reference","Research reference"})
            {
                var control=new Button{text=title,tooltip="Reference control only; no navigation or order is connected."};
                control.style.minHeight=tokens.MinimumControlPx;control.style.fontSize=tokens.BodyPx;control.style.whiteSpace=WhiteSpace.Normal;
                control.style.color=ColorOf(tokens.SharedColors["textPrimary"]);control.style.backgroundColor=ColorOf(profile.Colors["raised"]);
                control.style.borderLeftWidth=control.style.borderRightWidth=control.style.borderTopWidth=control.style.borderBottomWidth=2;
                void Border(bool focused){var c=ColorOf(tokens.SharedColors[focused?"focus":"controlBorder"]);control.style.borderLeftColor=control.style.borderRightColor=control.style.borderTopColor=control.style.borderBottomColor=c;}
                Border(false);control.RegisterCallback<FocusInEvent>(_=>Border(true));control.RegisterCallback<FocusOutEvent>(_=>Border(false));
                control.style.borderTopLeftRadius=control.style.borderBottomRightRadius=family.CornerRadius;control.style.marginBottom=8;nav.Add(control);
            }
            var content=new VisualElement{name="faction-theme-reference-states"};content.style.flexGrow=1;content.style.minWidth=240;content.style.backgroundColor=ColorOf(profile.Colors["surface"]);layout.Add(content);
            Label(content,"Shared states · identical meaning across all families",tokens.BodyPx,"faction-theme-state-heading");
            foreach(var key in new[]{"available","active","completed","warning","error","information","unknown","unavailable"})
            {
                var state=tokens.States[key];var row=new VisualElement{name="faction-theme-state-"+key};row.style.flexDirection=FlexDirection.Row;row.style.alignItems=Align.Center;row.style.minHeight=tokens.CompactRowPx;
                row.tooltip=state.Label+" · "+state.Shape+" · shared semantic token";content.Add(row);
                var mark=new StateMark(state.Shape,ColorOf(state.Color)){name="faction-theme-shape-"+key};mark.style.width=28;mark.style.height=28;mark.style.marginRight=12;row.Add(mark);
                var label=Label(row,state.Label,tokens.BodyPx,"faction-theme-label-"+key);label.style.color=ColorOf(state.Color);
            }
            Label(this,"Reference only · controls submit nothing · "+(reducedMotion?"reduced motion":"static sample; no animation"),tokens.SecondaryPx,"faction-theme-reference-scope");
            Label(this,profile.CatalogueStatus+"\n"+profile.SourceStatus,tokens.SecondaryPx,"faction-theme-source-status");
        }
        private static Label Label(VisualElement parent,string text,int size,string name)
        {var label=new Label(text){name=name};label.style.fontSize=size;label.style.whiteSpace=WhiteSpace.Normal;label.style.marginBottom=8;parent.Add(label);return label;}
        private static Color ColorOf(string hex){ColorUtility.TryParseHtmlString(hex,out var color);return color;}
        private sealed class Chrome:VisualElement
        {
            private readonly FactionThemeFamily family;private readonly Color primary,secondary;
            internal Chrome(FactionThemeFamily family,string primary,string secondary)
            {this.family=family;this.primary=ColorOf(primary);this.secondary=ColorOf(secondary);pickingMode=PickingMode.Ignore;generateVisualContent+=Draw;}
            private static void Path(Painter2D p,Color color,bool fill,params Vector2[] points)
            {p.strokeColor=p.fillColor=color;p.lineWidth=3;p.BeginPath();p.MoveTo(points[0]);for(var i=1;i<points.Length;i++)p.LineTo(points[i]);if(fill){p.ClosePath();p.Fill();}else p.Stroke();}
            private void Draw(MeshGenerationContext context)
            {
                var p=context.painter2D;var w=contentRect.width;var h=contentRect.height;if(w<80||h<40)return;
                var cut=family.CutSize;var radius=family.CornerRadius;
                switch(family.Frame)
                {
                    case "asymmetric-elbow-rail":
                        Path(p,primary,true,new Vector2(0,0),new Vector2(w*.66f,0),new Vector2(w*.66f,16),new Vector2(radius,16),new Vector2(radius,h),new Vector2(0,h));
                        for(var i=0;i<3;i++)Path(p,secondary,true,new Vector2(40+i*56,h-14),new Vector2(88+i*56,h-14),new Vector2(88+i*56,h),new Vector2(40+i*56,h));break;
                    case "stepped-angular-brackets":
                        Path(p,primary,false,new Vector2(w*.42f,8),new Vector2(cut+8,8),new Vector2(cut+8,28),new Vector2(8,28),new Vector2(8,h-8),new Vector2(w*.26f,h-8));
                        Path(p,secondary,false,new Vector2(w*.58f,h-8),new Vector2(w-cut-8,h-8),new Vector2(w-cut-8,h-28),new Vector2(w-8,h-28),new Vector2(w-8,8),new Vector2(w*.74f,8));break;
                    case "layered-curved-junctions":
                        for(var layer=0;layer<3;layer++)
                        {
                            var points=new Vector2[25];for(var i=0;i<points.Length;i++){var a=(float)(Math.PI*.2+Math.PI*1.6*i/24);points[i]=new Vector2(w*.5f+(w*.44f-layer*12)*Mathf.Cos(a),h*.5f+(radius+10-layer*8)*Mathf.Sin(a));}
                            Path(p,layer%2==0?primary:secondary,false,points);
                        }break;
                    case "swept-open-junctions":
                        Path(p,primary,false,new Vector2(8,h-8),new Vector2(w*.35f,h*.5f),new Vector2(w*.14f,8),new Vector2(w*.7f,8));
                        Path(p,secondary,false,new Vector2(w-8,8),new Vector2(w*.65f,h*.5f+cut),new Vector2(w*.86f,h-8),new Vector2(w*.3f,h-8));break;
                    case "interlocked-ribs":
                        for(var i=0;i<3;i++)
                        {
                            var x=w*(.16f+i*.25f);Path(p,i%2==0?primary:secondary,false,new Vector2(x,8),new Vector2(x+cut,h*.5f),new Vector2(x,h-8),new Vector2(x+w*.18f,h-8),new Vector2(x+w*.18f-cut,h*.5f),new Vector2(x+w*.18f,8));
                        }break;
                    default:
                        Path(p,primary,false,new Vector2(4,12),new Vector2(w-4,12));Path(p,secondary,false,new Vector2(4,h-12),new Vector2(w-4,h-12));break;
                }
            }
        }
        private sealed class StateMark:VisualElement
        {
            private readonly string shape;private readonly Color color;
            internal StateMark(string shape,Color color){this.shape=shape;this.color=color;pickingMode=PickingMode.Ignore;generateVisualContent+=Draw;}
            private void Draw(MeshGenerationContext context)
            {
                var p=context.painter2D;p.strokeColor=color;p.fillColor=color;p.lineWidth=2.5f;
                void Line(params Vector2[] points){p.BeginPath();p.MoveTo(points[0]);for(var i=1;i<points.Length;i++)p.LineTo(points[i]);p.Stroke();}
                void Circle(){p.BeginPath();for(var i=0;i<=24;i++){var a=i*Mathf.PI/12;var v=new Vector2(14+10*Mathf.Cos(a),14+10*Mathf.Sin(a));if(i==0)p.MoveTo(v);else p.LineTo(v);}p.Stroke();}
                if(shape=="check"||shape=="double-check")
                {Line(new Vector2(3,14),new Vector2(9,21),new Vector2(22,6));if(shape=="double-check")Line(new Vector2(11,21),new Vector2(26,6));}
                else if(shape=="triangle")
                {Line(new Vector2(14,3),new Vector2(26,25),new Vector2(2,25),new Vector2(14,3));Line(new Vector2(14,10),new Vector2(14,17));Line(new Vector2(14,20),new Vector2(14,22));}
                else if(shape=="cross")
                {Line(new Vector2(5,5),new Vector2(23,23));Line(new Vector2(23,5),new Vector2(5,23));}
                else if(shape=="clock"){Circle();Line(new Vector2(14,6),new Vector2(14,14),new Vector2(20,17));}
                else if(shape=="info"){Circle();Line(new Vector2(14,12),new Vector2(14,21));Line(new Vector2(14,7),new Vector2(14,9));}
                else if(shape=="bar"){p.BeginPath();p.MoveTo(new Vector2(4,11));p.LineTo(new Vector2(24,11));p.LineTo(new Vector2(24,17));p.LineTo(new Vector2(4,17));p.ClosePath();p.Fill();}
                else Line(new Vector2(5,14),new Vector2(23,14));
            }
        }
    }
}
