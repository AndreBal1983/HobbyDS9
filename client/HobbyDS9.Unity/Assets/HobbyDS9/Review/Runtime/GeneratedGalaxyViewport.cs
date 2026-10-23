#nullable enable
using System;

namespace HobbyDS9.UnityReview
{
    // Presentation transform only. Region geometry and route costs belong to the generator.
    public sealed class GeneratedGalaxyViewport
    {
        private double worldWidth=1,worldHeight=1,width=1,height=1;
        private double offsetX,offsetY;
        private const double Margin=30;
        public double Zoom { get; private set; }=1;
        public double MaximumZoom { get; set; }=8;
        public double Scale=>Math.Max(.0000001,Math.Min(Math.Max(1,width-Margin*2)/worldWidth,
            Math.Max(1,height-Margin*2)/worldHeight))*Zoom;
        public double X(double worldX)=>width/2+(worldX-worldWidth/2)*Scale+offsetX;
        public double Y(double worldY)=>height/2+(worldY-worldHeight/2)*Scale+offsetY;
        public double WorldX(double screenX)=>(screenX-width/2-offsetX)/Scale+worldWidth/2;
        public double WorldY(double screenY)=>(screenY-height/2-offsetY)/Scale+worldHeight/2;
        public void SetMap(double mapWidth,double mapHeight)
        {
            if(!Positive(mapWidth)||!Positive(mapHeight))throw new ArgumentOutOfRangeException(nameof(mapWidth));
            worldWidth=mapWidth;worldHeight=mapHeight;Fit();
        }
        public void Resize(double viewportWidth,double viewportHeight)
        {
            if(!Positive(viewportWidth)||!Positive(viewportHeight))return;
            var centerX=WorldX(width/2);var centerY=WorldY(height/2);
            width=viewportWidth;height=viewportHeight;
            offsetX=(worldWidth/2-centerX)*Scale;offsetY=(worldHeight/2-centerY)*Scale;Clamp();
        }
        public void Fit(){Zoom=1;offsetX=0;offsetY=0;}
        public void ZoomAt(double factor,double screenX,double screenY)
        {
            if(!Positive(factor)||!Finite(screenX)||!Finite(screenY))return;
            var anchorX=WorldX(screenX);var anchorY=WorldY(screenY);
            Zoom=Math.Max(1,Math.Min(MaximumZoom,Zoom*factor));
            offsetX+=screenX-X(anchorX);offsetY+=screenY-Y(anchorY);Clamp();
        }
        public void Pan(double x,double y)
        {
            if(!Finite(x)||!Finite(y))return;
            offsetX+=x;offsetY+=y;Clamp();
        }
        private void Clamp()
        {
            // Keep at least half the viewport over the map, including narrow maps.
            var limitX=worldWidth*Scale/2;var limitY=worldHeight*Scale/2;
            offsetX=Math.Max(-limitX,Math.Min(limitX,offsetX));
            offsetY=Math.Max(-limitY,Math.Min(limitY,offsetY));
        }
        private static bool Positive(double value)=>Finite(value)&&value>0;
        private static bool Finite(double value)=>!double.IsNaN(value)&&!double.IsInfinity(value);
    }
}
