#nullable enable
using System;

namespace HobbyDS9.UnityReview
{
    // UI-thread permission, not campaign authority. Each opaque request is terminal once;
    // invalidate on view/lifetime replacement. Consume before invoking any external code.
    public sealed class ReviewModalPermission
    {
        private ReviewModalCallbacks? active;
        public bool IsOpen => active!=null;
        public ReviewModalCallbacks? TryOpen(Action confirm,Action cancel)
        {
            if(active!=null)return null;
            if(confirm==null)throw new ArgumentNullException(nameof(confirm));
            if(cancel==null)throw new ArgumentNullException(nameof(cancel));
            active=new ReviewModalCallbacks(this,confirm,cancel);
            return active;
        }
        public void Invalidate(){active=null;}
        internal void Invoke(ReviewModalCallbacks request,Action action)
        {
            if(!ReferenceEquals(active,request))return;
            active=null;
            action();
        }
    }
    public sealed class ReviewModalCallbacks
    {
        private readonly Action confirm,cancel;
        private readonly ReviewModalPermission owner;
        internal ReviewModalCallbacks(ReviewModalPermission owner,Action confirm,Action cancel)
        {this.owner=owner;this.confirm=confirm;this.cancel=cancel;}
        public void Confirm(){owner.Invoke(this,confirm);}
        public void Cancel(){owner.Invoke(this,cancel);}
    }
}
