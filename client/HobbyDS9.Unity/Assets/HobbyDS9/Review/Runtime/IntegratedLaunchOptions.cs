#nullable enable
using System;
using HobbyDS9.Contracts;
namespace HobbyDS9.UnityReview
{
    public sealed class IntegratedLaunchOptions
    {
        public const bool DefaultEnabled=false;
        public string Actor { get; }
        private IntegratedLaunchOptions(string actor){Actor=actor;}
        public static bool TryParse(string[] args,out IntegratedLaunchOptions? options)
        {
            options=null;if(Array.IndexOf(args,"--hobbyds9-integrated-playtest")<0||Array.IndexOf(args,"--hobbyds9-physical-review")>=0)return false;
            var i=Array.IndexOf(args,"--hobbyds9-integrated-actor");
            if(i<0||i+1>=args.Length||!EntityId.TryParse(args[i+1],out _))return false;
            options=new IntegratedLaunchOptions(args[i+1]);return true;
        }
    }
}
