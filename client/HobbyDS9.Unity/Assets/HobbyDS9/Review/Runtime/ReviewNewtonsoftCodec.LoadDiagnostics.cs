#nullable enable
using HobbyDS9.TacticalReviewClient;
namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewNewtonsoftCodec
    {
        private readonly ReviewLoadDiagnostic? loadDiagnostic;
        public ReviewNewtonsoftCodec() { }
        public ReviewNewtonsoftCodec(ReviewLoadDiagnostic? diagnostic) {loadDiagnostic=diagnostic;}
    }
}
