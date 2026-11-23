#nullable enable
using System;
namespace HobbyDS9.UnityReview
{
    // Presentation pose only; no campaign time, entity, random or command dependency.
    public sealed class PlanetPortraitMotion
    {
        public const double AutoDegreesPerSecond=4,MaximumPitch=65,MaximumIdleStepSeconds=.25;
        public double Yaw {get;private set;} public double Pitch {get;private set;}
        public bool ReducedMotion {get;set;} public int? PointerId {get;private set;}
        public bool Dragging=>PointerId.HasValue;
        private double lastX,lastY;
        public PlanetPortraitMotion(bool reducedMotion=false){ReducedMotion=reducedMotion;}
        private static bool Finite(double value)=>!double.IsNaN(value)&&!double.IsInfinity(value);
        private static double Wrap(double value){value%=360;return value>180?value-360:value< -180?value+360:value;}
        public bool BeginDrag(int pointerId,double x,double y)
        {
            if(Dragging||pointerId<0||!Finite(x)||!Finite(y))return false;
            PointerId=pointerId;lastX=x;lastY=y;return true;
        }
        public bool MoveDrag(int pointerId,double x,double y)
        {
            if(PointerId!=pointerId||!Finite(x)||!Finite(y))return false;
            var yaw=-(x-lastX)*.35;var pitch=-(y-lastY)*.25;
            if(!Finite(yaw)||!Finite(pitch))return false;
            lastX=x;lastY=y;return Nudge(yaw,pitch);
        }
        public bool EndDrag(int pointerId){if(PointerId!=pointerId)return false;PointerId=null;return true;}
        public void CancelDrag(){PointerId=null;}
        public bool Advance(double seconds,bool active)
        {
            if(!active||Dragging||ReducedMotion||!Finite(seconds)||seconds<=0)return false;
            return Nudge(Math.Min(seconds,MaximumIdleStepSeconds)*AutoDegreesPerSecond,0);
        }
        public bool Nudge(double yaw,double pitch)
        {
            if(!Finite(yaw)||!Finite(pitch))return false;
            var nextYaw=Wrap(Yaw+yaw);var nextPitch=Math.Max(-MaximumPitch,Math.Min(MaximumPitch,Pitch+pitch));
            var changed=nextYaw!=Yaw||nextPitch!=Pitch;Yaw=nextYaw;Pitch=nextPitch;return changed;
        }
        public bool Reset(){var changed=Yaw!=0||Pitch!=0;Yaw=Pitch=0;return changed;}
    }
}
