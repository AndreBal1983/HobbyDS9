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
        public ReviewShipArtPanel(string civilizationDefinition,string shipDesignDefinition,bool fillViewport=false)
            :this(civilizationDefinition,shipDesignDefinition,null,fillViewport) { }
        public ReviewShipArtPanel(string civilizationDefinition,string shipDesignDefinition,string? exactHullVersionId,bool fillViewport=false)
        {
            name="owned-ship-art";stage=new ShipPreviewStage(512,320);
            if(!string.IsNullOrWhiteSpace(exactHullVersionId))stage.ShowActivityHull(exactHullVersionId);
            else stage.Show(new ShipPreviewSelection(civilizationDefinition,shipDesignDefinition,ShipPreviewDisclosure.Owned));
            image=new Image{name="owned-ship-model",image=stage.Texture,scaleMode=UnityEngine.ScaleMode.ScaleToFit,pickingMode=PickingMode.Ignore};image.style.height=220;Add(image);
            if(fillViewport){style.flexGrow=1;style.minHeight=0;image.style.height=StyleKeyword.Auto;image.style.flexGrow=1;image.style.flexBasis=0;image.style.minHeight=180;}
            var label=new Label(stage.Label){name="owned-ship-art-label"};label.style.whiteSpace=WhiteSpace.Normal;Add(label);
            RegisterCallback<DetachFromPanelEvent>(_=>{image.image=null;stage?.Dispose();stage=null;});
        }
    }
}
