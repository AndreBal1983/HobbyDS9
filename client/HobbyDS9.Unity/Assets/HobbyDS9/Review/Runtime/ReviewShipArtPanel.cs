#nullable enable
using HobbyDS9.ArtDemo;
using UnityEngine.UIElements;
namespace HobbyDS9.UnityReview
{
    // Only the caller's selected owned, already-filtered identities enter this panel.
    // No session/save/enemy roster access or identity inference.
    public sealed class ReviewShipArtPanel : VisualElement
    {
        private ShipPreviewStage? stage;
        private readonly Image image;
        public ReviewShipArtPanel(string civilizationDefinition,string shipDesignDefinition)
        {
            name="owned-ship-art";stage=new ShipPreviewStage(512,320);
            stage.Show(new ShipPreviewSelection(civilizationDefinition,shipDesignDefinition,ShipPreviewDisclosure.Owned));
            image=new Image{name="owned-ship-model",image=stage.Texture,scaleMode=UnityEngine.ScaleMode.ScaleToFit};image.style.height=180;Add(image);
            var label=new Label(stage.Label){name="owned-ship-art-label"};label.style.whiteSpace=WhiteSpace.Normal;Add(label);
            RegisterCallback<DetachFromPanelEvent>(_=>{image.image=null;stage?.Dispose();stage=null;});
        }
    }
}
