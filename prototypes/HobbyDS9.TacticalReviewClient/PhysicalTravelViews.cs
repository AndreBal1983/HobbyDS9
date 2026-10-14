using System;
using System.Collections.Generic;
using HobbyDS9.Contracts;

namespace HobbyDS9.TacticalReviewClient
{
    public sealed class PhysicalTravelView
    {
        public IReadOnlyList<CampaignOwnTravelOrder> OwnOrders { get; }
        public IReadOnlyList<CampaignOwnTravelReceipt> OwnReceipts { get; }
        public IReadOnlyList<CampaignKnownContact> KnownContacts { get; }
        internal PhysicalTravelView(IReadOnlyList<CampaignOwnTravelOrder> orders,IReadOnlyList<CampaignOwnTravelReceipt> receipts,
            IReadOnlyList<CampaignKnownContact> contacts)
        { OwnOrders=orders; OwnReceipts=receipts; KnownContacts=contacts; }
    }
    public sealed class KnownRouteOffer
    {
        internal Guid HostId { get; }
        internal long Generation { get; }
        public string Actor { get; }
        public string Fleet { get; }
        public string Origin { get; }
        public string Destination { get; }
        public int Turn { get; }
        public long CampaignRevision { get; }
        public StrategicRouteSnapshot Route { get; }
        internal KnownRouteOffer(Guid host,long generation,string actor,string fleet,string origin,string destination,
            CampaignTacticalReviewStatus status,StrategicRouteSnapshot route)
        { HostId=host; Generation=generation; Actor=actor; Fleet=fleet; Origin=origin; Destination=destination;
            Turn=status.Turn; CampaignRevision=status.CampaignRevision; Route=route; }
    }
    public sealed class TravelEdit
    {
        public string Fleet { get; }
        public CampaignTravelIntent Intent { get; }
        public string? ContactFleet { get; }
        public int? ContactSeenTurn { get; }
        public IReadOnlyList<string> Route { get; }
        internal TravelEdit(string fleet,CampaignTravelIntent intent,string? contact,int? seen,IEnumerable<string> route)
        { Fleet=fleet; Intent=intent; ContactFleet=contact; ContactSeenTurn=seen; Route=new List<string>(route).AsReadOnly(); }
    }
}
