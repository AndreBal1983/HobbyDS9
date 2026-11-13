#nullable enable
using System;
using HobbyDS9.Contracts;
using UnityEngine;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityReview
{
    // One disclosed body, fixed square framing, no overview/orbit crop or idle loop.
    internal sealed class ReviewPlanetPortrait : VisualElement,IDisposable
    {
        private readonly ColonisationPlanetSnapshot body;
        private readonly Image image;
        private readonly Label unavailable;
        private ReviewSolStage? stage;
        private bool disposed;
        internal ReviewPlanetPortrait(ColonisationPlanetSnapshot body)
        {
            this.body=body;name="planet-detail-visual";AddToClassList("planet-detail-visual");
            image=new Image{name="planet-detail-portrait",scaleMode=ScaleMode.ScaleToFit,pickingMode=PickingMode.Ignore};
            image.AddToClassList("planet-detail-portrait");Add(image);
            unavailable=new Label("Planet preview unavailable");unavailable.style.display=DisplayStyle.None;Add(unavailable);
            var caption=new Label(PlanetVisualProfile.From(body).Caption);caption.AddToClassList("console-copy");Add(caption);
            var credits=new Foldout{text="Surface map credits",value=false};
            credits.Add(new Label("Earth/Mars surface maps: Solar System Scope · CC BY 4.0. Other terrain is decorative."));Add(credits);
            RegisterCallback<AttachToPanelEvent>(_=>schedule.Execute(Render));
            RegisterCallback<DetachFromPanelEvent>(e=>{if(ReferenceEquals(e.target,this))Dispose();});
        }
        private void Render()
        {
            if(disposed||panel==null)return;
            try
            {
                var shader=Resources.Load<Shader>("SystemBodyArt/ProceduralSurface");
                if(shader==null||!shader.isSupported||SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)
                    throw new InvalidOperationException();
                stage??=new ReviewSolStage(shader,new[]{body});
                if(!stage.RenderPortrait(body.PlanetId))throw new InvalidOperationException();
                image.image=stage.PortraitTexture;
            }
            catch(Exception){image.image=null;stage?.Dispose();stage=null;unavailable.style.display=DisplayStyle.Flex;}
        }
        public void Dispose(){if(disposed)return;disposed=true;image.image=null;stage?.Dispose();stage=null;}
    }
}
