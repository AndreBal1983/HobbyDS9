using System.Text.Json;
using System.Text.Json.Serialization;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;

namespace HobbyDS9.TacticalReviewStorage;

public interface IPhysicalReviewFileCodec : IPhysicalReviewHostCodec
{
    byte[] EncodePrivateSave(CampaignTacticalTravelSaveDto root);
    CampaignTacticalTravelSaveDto DecodePrivateSave(byte[] bytes);
}
public sealed class JsonPhysicalReviewCodec : IPhysicalReviewFileCodec
{
    private static readonly JsonSerializerOptions Options=new JsonSerializerOptions {
        UnmappedMemberHandling=JsonUnmappedMemberHandling.Disallow,
        PropertyNameCaseInsensitive=false,NumberHandling=JsonNumberHandling.Strict
    };
    public CampaignTacticalTravelPackageDto CopyPackage(CampaignTacticalTravelPackageDto p)
        => JsonSerializer.Deserialize<CampaignTacticalTravelPackageDto>(JsonSerializer.SerializeToUtf8Bytes(p,Options),Options)!;
    public CampaignTacticalTravelSaveDto CopySave(CampaignTacticalTravelSaveDto root)
        => DecodePrivateSave(EncodePrivateSave(root));
    public byte[] EncodePrivateSave(CampaignTacticalTravelSaveDto root)
        => JsonSerializer.SerializeToUtf8Bytes(root,Options);
    public CampaignTacticalTravelSaveDto DecodePrivateSave(byte[] bytes)
        => JsonSerializer.Deserialize<CampaignTacticalTravelSaveDto>(bytes,Options)
            ?? throw new InvalidDataException("Missing complete physical review root.");
}
