#nullable enable
using System;
using System.Linq;
namespace HobbyDS9.UnityReview
{
    public sealed class SingleplayerDemoLaunchOptions
    {
        public const bool DefaultEnabled=false;
        public const string Flag="--hobbyds9-singleplayer-demo";
        private SingleplayerDemoLaunchOptions(){}
        public static bool TryParse(string[] args,out SingleplayerDemoLaunchOptions? options)
        {
            options=null;
            if(args.Count(a=>a==Flag)!=1||args.Any(a=>a=="--hobbyds9-physical-review"||a=="--hobbyds9-integrated-playtest"
                ||a=="--hobbyds9-review-actor"||a=="--hobbyds9-integrated-actor"))return false;
            options=new SingleplayerDemoLaunchOptions();return true;
        }
    }
}
