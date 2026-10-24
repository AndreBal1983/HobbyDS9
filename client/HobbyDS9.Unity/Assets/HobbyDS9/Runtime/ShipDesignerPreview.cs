#nullable enable
using System;
using HobbyDS9.ArtDemo;
using UnityEngine;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityClient
{
    // Presentation reference only: this is not the actual ship identity resolver.
    public sealed class ShipDesignerPreview : VisualElement, IDisposable
    {
        private readonly string? visualKey;
        private readonly Image image = new Image { name="designer-hull-reference-image", scaleMode=ScaleMode.ScaleToFit, pickingMode=PickingMode.Ignore };
        private readonly Label caption = new Label { name="designer-hull-reference-caption", pickingMode=PickingMode.Ignore };
        private readonly VisualElement schematic = new VisualElement();
        private ShipPreviewStage? stage;
        private bool disposed;

        public static string? ReferenceKey(string? hullVersionId) =>
            string.Equals(hullVersionId,"hull.utility.v1",StringComparison.Ordinal) ? ShipVisualCatalog.ExplorationCruiser : null;

        public ShipDesignerPreview(string? hullVersionId)
        {
            name="designer-hull-reference";pickingMode=PickingMode.Ignore; AddToClassList("sd-hull-reference");
            visualKey=ReferenceKey(hullVersionId);
            schematic.AddToClassList("sd-reference-schematic");
            foreach(var text in new[]{"MISSION / SENSORS / SCIENCE","CORE / POWER / PROPULSION","DEFENCE / SUPPORT"})
                schematic.Add(new Label(text){pickingMode=PickingMode.Ignore});
            image.AddToClassList("sd-hull-reference-image");
            Add(image); Add(schematic); Add(caption);
            Fallback(visualKey==null ? "Hull schematic · no mapped model reference" : "Illustrative hull reference · equipment shown schematically");
            RegisterCallback<AttachToPanelEvent>(_=>Open());
            RegisterCallback<DetachFromPanelEvent>(_=>Release());
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
                stage.ShowArtReference(visualKey);
                if(stage.Availability!=ShipPreviewAvailability.Model)
                {
                    Fallback("Hull schematic · "+stage.Label); Release(); return;
                }
                image.image=stage.Texture; image.style.display=DisplayStyle.Flex;
                schematic.style.display=DisplayStyle.None; caption.text=stage.Label;
            }
            catch(Exception exception)
            {
                Release(); Fallback("Hull schematic · model preview unavailable");
                Debug.LogWarning("Designer reference preview unavailable: "+exception.GetType().Name);
            }
        }

        private void Release()
        {
            image.image=null;stage?.Dispose();stage=null;
        }
        public void Dispose() { if(disposed)return; disposed=true;Release(); }
    }
}
