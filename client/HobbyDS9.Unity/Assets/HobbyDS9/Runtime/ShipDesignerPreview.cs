#nullable enable
using System;
using System.Collections.Generic;
using HobbyDS9.ArtDemo;
using UnityEngine;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityClient
{
    // Presentation reference only: this is not the actual ship identity resolver.
    public sealed class ShipDesignerPreview : VisualElement, IDisposable
    {
        private string? visualKey;
        private string? hullId;
        private readonly Dictionary<string,int> installed=new Dictionary<string,int>(StringComparer.Ordinal);
        private bool pendingPreview;
        private float yaw=37.57f,pitch=26,zoom=1;
        private int dragPointer=-1;
        private Vector2 dragPosition;
        private readonly VisualElement nacelleStudies=new VisualElement(),deflectorStudies=new VisualElement();
        private const string ArtCredit="Galaxy source: Chris Kuhn / Kuhn Industries, Big D, CC BY-NC-SA 3.0. Noncommercial fan-art derivative. Original procedural modules from the HobbyDS9 art study.";
        private readonly Image image = new Image { name="designer-hull-reference-image", scaleMode=ScaleMode.ScaleToFit, pickingMode=PickingMode.Position };
        private readonly Label caption = new Label { name="designer-hull-reference-caption", pickingMode=PickingMode.Ignore };
        private readonly VisualElement schematic = new VisualElement();
        private ShipPreviewStage? stage;
        private VisualElement? viewport;
        private bool disposed;

        public static string? ReferenceKey(string? hullVersionId) =>
            ShipPreviewStage.DesignerReferenceKey(hullVersionId!);

        public ShipDesignerPreview(string? hullVersionId)
        {
            name="designer-hull-reference";pickingMode=PickingMode.Ignore; AddToClassList("sd-hull-reference");
            hullId=hullVersionId;visualKey=ReferenceKey(hullVersionId);
            schematic.AddToClassList("sd-reference-schematic");
            foreach(var text in new[]{"MISSION / SENSORS / SCIENCE","CORE / POWER / PROPULSION","DEFENCE / SUPPORT"})
                schematic.Add(new Label(text){pickingMode=PickingMode.Ignore});
            image.AddToClassList("sd-hull-reference-image");
            image.tooltip="Drag with left mouse to orbit · wheel to zoom the illustrative model";
            image.RegisterCallback<PointerDownEvent>(e=>
            {
                if(e.button!=0 || stage?.IsDesignerReference!=true || dragPointer!=-1)return;
                dragPointer=e.pointerId;dragPosition=new Vector2(e.position.x,e.position.y);image.CapturePointer(e.pointerId);e.StopPropagation();
            });
            image.RegisterCallback<PointerMoveEvent>(e=>
            {
                if(dragPointer!=e.pointerId || !image.HasPointerCapture(e.pointerId))return;
                var position=new Vector2(e.position.x,e.position.y);var delta=position-dragPosition;dragPosition=position;
                yaw+=Mathf.Clamp(delta.x,-80,80)*.6f;pitch=Mathf.Clamp(pitch-Mathf.Clamp(delta.y,-80,80)*.5f,-75,75);
                stage?.OrbitReference(yaw,pitch);e.StopPropagation();
            });
            image.RegisterCallback<PointerUpEvent>(e=>{if(e.pointerId==dragPointer){ReleaseDrag();e.StopPropagation();}});
            image.RegisterCallback<PointerCancelEvent>(e=>{if(e.pointerId==dragPointer)ReleaseDrag();});
            image.RegisterCallback<PointerCaptureOutEvent>(e=>{if(e.pointerId==dragPointer)dragPointer=-1;});
            image.RegisterCallback<WheelEvent>(e=>
            {
                if(stage?.IsDesignerReference!=true)return;
                zoom=Mathf.Clamp(zoom*Mathf.Exp(-Mathf.Clamp(e.delta.y,-10,10)*.12f),.6f,2.2f);stage.ZoomReference(zoom);e.StopPropagation();
            });
            var controls=new VisualElement {name="designer-reference-controls"};controls.style.flexDirection=FlexDirection.Row;controls.style.flexWrap=Wrap.Wrap;
            foreach(var view in new[]{DesignerReferenceView.Hull,DesignerReferenceView.Nacelles,DesignerReferenceView.Deflector,DesignerReferenceView.CoreCutaway})
            {
                var choice=view;var button=new Button(()=>{stage?.SetReferenceView(choice);RefreshCaption();})
                    {name="designer-view-"+view.ToString().ToLowerInvariant(),text=view==DesignerReferenceView.CoreCutaway?"Internal core":view.ToString()};
                CompactControl(button);controls.Add(button);
            }
            foreach(var step in new[]{("◀",-20f,0f),("▶",20f,0f),("↑",0f,15f),("↓",0f,-15f)})
            {
                var turn=step;var button=new Button(()=>{yaw+=turn.Item2;pitch=Mathf.Clamp(pitch+turn.Item3,-75,75);stage?.OrbitReference(yaw,pitch);RefreshCaption();})
                    {name="designer-orbit-"+(turn.Item2<0?"left":turn.Item2>0?"right":turn.Item3>0?"up":"down"),text=turn.Item1,tooltip="Rotate illustrative reference"};
                CompactControl(button);controls.Add(button);
            }
            Add(controls);
            nacelleStudies.name="designer-nacelle-studies";deflectorStudies.name="designer-deflector-studies";
            foreach(var group in new[]{nacelleStudies,deflectorStudies}){group.style.flexDirection=FlexDirection.Row;group.style.flexWrap=Wrap.Wrap;group.style.display=DisplayStyle.None;Add(group);}
            foreach(var choice in new[]{("Classic",DesignerStudyVariant.ClassicNacelles),("Sovereign-style",DesignerStudyVariant.SovereignNacelles),("Blue",DesignerStudyVariant.BlueDeflector),("Amber",DesignerStudyVariant.AmberDeflector),("Survey",DesignerStudyVariant.SurveyDeflector)})
            {
                var selected=choice;var button=new Button(()=>{stage?.SetStudyVariant(selected.Item2);RefreshCaption();})
                    {name="designer-study-"+choice.Item2.ToString().ToLowerInvariant(),text=choice.Item1+" · art study",tooltip="Changes reference artwork only; does not fit equipment or change technology."};
                CompactControl(button);
                (choice.Item2==DesignerStudyVariant.ClassicNacelles || choice.Item2==DesignerStudyVariant.SovereignNacelles?nacelleStudies:deflectorStudies).Add(button);
            }
            Add(image); Add(schematic); Add(caption);
            caption.tooltip=ArtCredit;
            Fallback(visualKey==null ? "Hull schematic · no mapped model reference" : "Illustrative hull reference · equipment shown schematically");
            RegisterCallback<AttachToPanelEvent>(_=>Open());
            RegisterCallback<DetachFromPanelEvent>(_=>Release());
            image.RegisterCallback<GeometryChangedEvent>(FitViewport);
            caption.RegisterCallback<GeometryChangedEvent>(FitViewport);
            controls.RegisterCallback<GeometryChangedEvent>(FitViewport);
            RegisterCallback<GeometryChangedEvent>(FitViewport);
        }

        private static void CompactControl(Button button)
        {
            button.AddToClassList("lcars-pill");button.style.height=28;button.style.minHeight=28;
            button.style.fontSize=12;button.style.paddingTop=2;button.style.paddingBottom=2;
            button.style.paddingLeft=7;button.style.paddingRight=7;button.style.marginTop=1;button.style.marginBottom=1;
        }

        public void SetFit(string hullVersionId,IReadOnlyDictionary<string,int> quantities,bool isPendingPreview)
        {
            if(disposed)return;
            if(quantities==null || quantities.Count>256)throw new ArgumentException("Invalid visual fit.",nameof(quantities));
            foreach(var pair in quantities)if(pair.Key==null || pair.Value<0)throw new ArgumentException("Invalid visual quantity.",nameof(quantities));
            bool hullChanged=!string.Equals(hullId,hullVersionId,StringComparison.Ordinal);
            hullId=hullVersionId;visualKey=ReferenceKey(hullVersionId);pendingPreview=isPendingPreview;
            installed.Clear();foreach(var pair in quantities)installed.Add(pair.Key,pair.Value);
            if(hullChanged){Release();Fallback(visualKey==null?"Hull schematic · no mapped Galaxy reference":"Galaxy reference awaiting renderer");if(panel!=null)Open();}
            else if(stage!=null){stage.ShowDesignerReference(hullId!,installed);RefreshCaption();}
        }

        public void UpdateFacts(string text) { caption.tooltip=text+"\n"+ArtCredit; }
        public DesignerArtSnapshot? CaptureArtDiagnostics()=>stage?.CaptureArtDiagnostics();
        private void RefreshCaption()
        {
            if(stage!=null)
            {
                var text=stage.Label;
                if(stage.IsDesignerReference && stage.ReferenceView==DesignerReferenceView.Hull)
                    text="Illustrative Galaxy · propulsion "+ShipPreviewStage.DesignerModuleQuantity(installed,DesignerReferenceView.Nacelles)
                        +" / reactor "+ShipPreviewStage.DesignerModuleQuantity(installed,DesignerReferenceView.CoreCutaway)+" · art only";
                else if(stage.IsDesignerReference && stage.ReferenceView==DesignerReferenceView.Deflector)text="Uninstalled deflector art study";
                if(stage.Label.Contains("missing art:"))text+=" · some fitting art unavailable";
                caption.text=(pendingPreview?"PREVIEW · ":"DRAFT FIT · ")+text;
                caption.tooltip=stage.Label+"\n"+ArtCredit;
            }
            nacelleStudies.style.display=stage?.ReferenceView==DesignerReferenceView.Nacelles?DisplayStyle.Flex:DisplayStyle.None;
            deflectorStudies.style.display=stage?.ReferenceView==DesignerReferenceView.Deflector?DisplayStyle.Flex:DisplayStyle.None;
        }

        public void BindViewport(VisualElement value)
        {
            if(viewport!=null)viewport.UnregisterCallback<GeometryChangedEvent>(FitViewport);
            viewport=value;viewport.RegisterCallback<GeometryChangedEvent>(FitViewport);
        }

        private void FitViewport(GeometryChangedEvent change)
        {
            if(disposed || viewport==null || panel==null)return;
            var availableWidth=viewport.worldBound.xMax-worldBound.xMin-2;
            if(!float.IsNaN(availableWidth) && !float.IsInfinity(availableWidth) && availableWidth>0)
            {
                // A vertical ScrollView does not constrain an Image's intrinsic
                // texture width. Explicitly break that auto/percent size cycle.
                if(Mathf.Abs(resolvedStyle.width-availableWidth)>.5f){style.width=availableWidth;style.maxWidth=availableWidth;}
                var imageWidth=Mathf.Max(1,viewport.worldBound.xMax-image.worldBound.xMin-2);
                if(Mathf.Abs(image.resolvedStyle.width-imageWidth)>.5f){image.style.width=imageWidth;image.style.maxWidth=imageWidth;}
            }
            var available=viewport.worldBound.yMax-image.worldBound.yMin-caption.resolvedStyle.height-12;
            if(float.IsNaN(available) || float.IsInfinity(available))return;
            var maximum=GetFirstAncestorOfType<ShipDesignerView>()?.ClassListContains("sd-wide")==true?440:360;
            var height=Mathf.Min(maximum,Mathf.Max(1,available));
            if(Mathf.Abs(image.resolvedStyle.height-height)<.5f)return;
            image.style.minHeight=1;image.style.height=height;
        }

        private void Fallback(string text)
        {
            image.style.display=DisplayStyle.None; schematic.style.display=DisplayStyle.Flex; caption.text=text;
        }

        private void Open()
        {
            if(disposed || stage!=null || visualKey==null)return;
            try
            {
                stage=new ShipPreviewStage(768,480);
                stage.ShowDesignerReference(hullId!,installed);
                if(stage.Availability!=ShipPreviewAvailability.Model)
                {
                    Fallback("Hull schematic · "+stage.Label); Release(); return;
                }
                image.image=stage.Texture; image.style.display=DisplayStyle.Flex;
                schematic.style.display=DisplayStyle.None;stage.OrbitReference(yaw,pitch);stage.ZoomReference(zoom);RefreshCaption();
            }
            catch(Exception exception)
            {
                Release(); Fallback("Hull schematic · model preview unavailable");
                Debug.LogWarning("Designer reference preview unavailable: "+exception.GetType().Name);
            }
        }

        private void Release()
        {
            ReleaseDrag();image.image=null;stage?.Dispose();stage=null;
        }
        private void ReleaseDrag()
        {
            var pointer=dragPointer;dragPointer=-1;
            if(pointer>=0 && image.HasPointerCapture(pointer))image.ReleasePointer(pointer);
        }
        public void Dispose()
        {
            if(disposed)return; disposed=true;
            if(viewport!=null)viewport.UnregisterCallback<GeometryChangedEvent>(FitViewport);
            viewport=null;Release();
        }
    }
}
