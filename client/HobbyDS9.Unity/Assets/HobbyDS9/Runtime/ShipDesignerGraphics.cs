using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityClient
{
    [CreateAssetMenu(menuName="HobbyDS9/Ship designer graphics")]
    public sealed class ShipDesignerGraphics : ScriptableObject
    {
        [Serializable] public sealed class Slot { public string SystemId; public Texture2D Image; }
        public List<Slot> Slots=new List<Slot>();
        public Texture2D Find(string systemId) => Slots.Find(s=>s.SystemId==systemId)?.Image;
    }
    public sealed class ShipSystemGraphic : VisualElement
    {
        public Image Art { get; }
        public bool HasImage => Art.image!=null;
        public ShipSystemGraphic(string systemId,string caption,ShipDesignerGraphics catalogue)
        {
            name="designer-graphic"; AddToClassList("sd-graphic"); pickingMode=PickingMode.Ignore;
            Art=new Image {name="designer-image",image=catalogue==null?null:catalogue.Find(systemId),scaleMode=ScaleMode.ScaleToFit,pickingMode=PickingMode.Ignore};
            Art.AddToClassList("sd-image"); Add(Art);
            if(!HasImage)
            {
                Art.style.display=DisplayStyle.None;
                var title=new Label(caption.ToUpperInvariant()) {name="designer-graphic-placeholder",pickingMode=PickingMode.Ignore};
                title.AddToClassList("sd-placeholder-title"); Add(title);
                Add(new Label("SYSTEM VIEW / ART PENDING") {pickingMode=PickingMode.Ignore});
            }
        }
    }
}
