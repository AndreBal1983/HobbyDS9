#nullable enable
using System;

namespace HobbyDS9.UnityReview
{
    // Initial extraction preserves the existing clear-then-action callback semantics.
    // The focused failing regression establishes the gap before adding permission checks.
    public sealed class ReviewModalPermission
    {
        private ReviewModalCallbacks? active;
        public bool IsOpen => active!=null;
        public ReviewModalCallbacks? TryOpen(Action confirm,Action cancel)
        {
            if(active!=null)return null;
            active=new ReviewModalCallbacks(
                ()=>{active=null;confirm();},
                ()=>{active=null;cancel();});
            return active;
        }
        public void Invalidate(){active=null;}
    }
    public sealed class ReviewModalCallbacks
    {
        private readonly Action confirm,cancel;
        internal ReviewModalCallbacks(Action confirm,Action cancel){this.confirm=confirm;this.cancel=cancel;}
        public void Confirm(){confirm();}
        public void Cancel(){cancel();}
    }
}
