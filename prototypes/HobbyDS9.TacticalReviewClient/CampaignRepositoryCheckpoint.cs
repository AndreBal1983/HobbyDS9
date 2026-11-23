using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using HobbyDS9.Contracts;

namespace HobbyDS9.TacticalReviewClient
{
    // Host-private full images, never actor projections or network replication.
    public enum CampaignRepositoryError { None, Unavailable, Busy, Stale, InvalidPayload, Incompatible, Cancelled }

    public sealed class CampaignRepositoryEdition
    {
        public const int MaximumIdentityCharacters = 16384;
        public IReadOnlyList<KeyValuePair<string,string>> Fields { get; }
        public string Identity { get; }
        internal CampaignRepositoryEdition(IEnumerable<KeyValuePair<string,string>> fields)
        {
            var rows=fields.ToList();
            if(rows.Count==0||rows.Select(x=>x.Key).Distinct(StringComparer.Ordinal).Count()!=rows.Count)
                throw new ArgumentException("Unique edition fields required.");
            var text=new StringBuilder("campaign-repository-edition-v1|");
            foreach(var row in rows)
            {
                if(string.IsNullOrEmpty(row.Key)||string.IsNullOrEmpty(row.Value))throw new ArgumentException("Complete edition identity required.");
                if(row.Key.IndexOf('\0')>=0||row.Value.IndexOf('\0')>=0)throw new ArgumentException("Invalid edition text.");
                var utf8=new UTF8Encoding(false,true);utf8.GetByteCount(row.Key);utf8.GetByteCount(row.Value);
                text.Append(row.Key.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(row.Key);
                text.Append(row.Value.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(row.Value);
                if(text.Length>MaximumIdentityCharacters)throw new ArgumentException("Edition identity is too large.");
            }
            Fields=rows.AsReadOnly();Identity=text.ToString();
        }
    }

    public sealed class CampaignRepositoryImage
    {
        public const int MaximumBytes=64*1024*1024;
        private readonly byte[] payload;
        public CampaignRepositoryEdition Edition { get; }
        public int Turn { get; }
        public long Revision { get; }
        public SessionPhase Phase { get; }
        public int Length=>payload.Length;
        public string PayloadHash { get; }
        internal CampaignRepositoryImage(byte[] bytes,CampaignRepositoryEdition edition,int turn,long revision,SessionPhase phase)
        {
            if(bytes.Length==0||bytes.Length>MaximumBytes)throw new ArgumentException("Bounded complete payload required.");
            payload=(byte[])bytes.Clone();Edition=edition;Turn=turn;Revision=revision;Phase=phase;
            using(var sha=SHA256.Create())PayloadHash=BitConverter.ToString(sha.ComputeHash(payload)).Replace("-","").ToLowerInvariant();
        }
        public byte[] ReadPayload()=>(byte[])payload.Clone();
    }

    public sealed class CampaignRepositoryStamp
    {
        internal Guid HostId { get; }
        public long SessionEpoch { get; }
        public string EpochIdentity=>HostId.ToString("N")+":"+SessionEpoch.ToString(CultureInfo.InvariantCulture);
        public int Turn { get; }
        public long Revision { get; }
        public SessionPhase Phase { get; }
        internal string Actor {get;}
        internal string CombatBoundary {get;}
        public bool SameState(CampaignRepositoryStamp? other)=>other!=null&&HostId==other.HostId&&SessionEpoch==other.SessionEpoch
            &&Turn==other.Turn&&Revision==other.Revision&&Phase==other.Phase&&Actor==other.Actor&&CombatBoundary==other.CombatBoundary;
        internal CampaignRepositoryStamp(Guid host,long epoch,int turn,long revision,SessionPhase phase,string actor,string combatBoundary)
        {HostId=host;SessionEpoch=epoch;Turn=turn;Revision=revision;Phase=phase;Actor=actor;CombatBoundary=combatBoundary;}
    }

    // Must implement this edition's bounded strict wire decoder. A DTO serializer
    // that fills omitted properties with defaults is not an interchangeable codec.
    public interface ICampaignRepositoryCodec
    {
        string Version { get; }
        byte[] Encode(CampaignColonisationCycleSaveDto root);
        CampaignColonisationCycleSaveDto Decode(byte[] bytes);
    }

    public sealed class DelegatedCampaignRepositoryCodec:ICampaignRepositoryCodec
    {
        private readonly Func<CampaignColonisationCycleSaveDto,byte[]> encode;
        private readonly Func<byte[],CampaignColonisationCycleSaveDto> decode;
        public string Version {get;}
        public DelegatedCampaignRepositoryCodec(string version,Func<CampaignColonisationCycleSaveDto,byte[]> encode,Func<byte[],CampaignColonisationCycleSaveDto> decode)
        {
            if(string.IsNullOrWhiteSpace(version)||version.Length>256)throw new ArgumentException("Codec identity required.",nameof(version));
            Version=version;this.encode=encode??throw new ArgumentNullException(nameof(encode));this.decode=decode??throw new ArgumentNullException(nameof(decode));
        }
        public byte[] Encode(CampaignColonisationCycleSaveDto root)=>encode(root);
        public CampaignColonisationCycleSaveDto Decode(byte[] bytes)=>decode(bytes);
    }

    public sealed class CampaignRepositoryImageResult
    {
        public CampaignRepositoryError Error {get;}
        public bool Succeeded=>Error==CampaignRepositoryError.None;
        public CampaignRepositoryImage? Image {get;}
        internal CampaignRepositoryImageResult(CampaignRepositoryError error,CampaignRepositoryImage? image=null){Error=error;Image=image;}
    }
    // A storage adapter supplies owned complete bytes and its indexed metadata.
    // The host independently validates every field before making a replacement.
    public sealed class CampaignRepositoryStoredPayload
    {
        private readonly byte[] payload;
        public string EditionIdentity {get;}
        public int Turn {get;}
        public long Revision {get;}
        public string Phase {get;}
        public string PayloadHash {get;}
        public int Length=>payload.Length;
        public CampaignRepositoryStoredPayload(byte[] payload,string editionIdentity,int turn,long revision,string phase,string payloadHash)
        {
            if(payload==null||payload.Length==0||payload.Length>CampaignRepositoryImage.MaximumBytes)throw new ArgumentException("Bounded payload required.");
            if(string.IsNullOrEmpty(editionIdentity)||editionIdentity.Length>CampaignRepositoryEdition.MaximumIdentityCharacters||turn<1||revision<0
                ||string.IsNullOrEmpty(phase)||phase.Length>64||string.IsNullOrEmpty(payloadHash)||payloadHash.Length!=64)throw new ArgumentException("Complete storage metadata required.");
            this.payload=(byte[])payload.Clone();EditionIdentity=editionIdentity;Turn=turn;Revision=revision;Phase=phase;PayloadHash=payloadHash;
        }
        public byte[] ReadPayload()=>(byte[])payload.Clone();
        internal bool Matches(CampaignRepositoryImage image)=>EditionIdentity==image.Edition.Identity&&Turn==image.Turn&&Revision==image.Revision
            &&Phase==image.Phase.ToString()&&PayloadHash==image.PayloadHash&&Length==image.Length;
    }
}
