#nullable enable
using System;
using HobbyDS9.Contracts;

namespace HobbyDS9.UnityReview
{
    public sealed class ReviewLaunchOptions
    {
        public const bool DefaultEnabled=false;
        public string Profile { get; }
        public string Actor { get; }
        private ReviewLaunchOptions(string profile,string actor) { Profile=profile; Actor=actor; }
        public static bool TryParse(string[] args,out ReviewLaunchOptions? options)
        {
            options=null;
            if(Array.IndexOf(args,"--hobbyds9-physical-review")<0) return false;
            var profile=Read(args,"--hobbyds9-review-profile"); var actor=Read(args,"--hobbyds9-review-actor");
            if((profile!="faster" && profile!="slower") || !EntityId.TryParse(actor,out _)) return false;
            options=new ReviewLaunchOptions(profile!,actor!); return true;
        }
        private static string? Read(string[] args,string key)
        { var i=Array.IndexOf(args,key); return i>=0 && i+1<args.Length ? args[i+1] : null; }
    }
}
