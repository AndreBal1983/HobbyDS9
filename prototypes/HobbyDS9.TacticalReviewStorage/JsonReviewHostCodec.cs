using System.Text.Json;
using System.Text.Json.Serialization;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;

namespace HobbyDS9.TacticalReviewStorage;

// Trusted headless .NET host, not a Unity dependency or portable gameplay contract.
public interface IReviewFileCodec : IReviewHostCodec
{
    byte[] EncodePrivateSave(CampaignTacticalReviewSaveDto save);
    CampaignTacticalReviewSaveDto DecodePrivateSave(byte[] bytes);
}
public sealed class JsonReviewHostCodec : IReviewFileCodec
{
    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions {
        UnmappedMemberHandling=JsonUnmappedMemberHandling.Disallow,
        PropertyNameCaseInsensitive=false,
        NumberHandling=JsonNumberHandling.Strict
    };
    public CampaignTacticalReviewPackageDto CopyPackage(CampaignTacticalReviewPackageDto package)
        => JsonSerializer.Deserialize<CampaignTacticalReviewPackageDto>(JsonSerializer.SerializeToUtf8Bytes(package,Options),Options)!;
    public CampaignTacticalReviewSaveDto CopySave(CampaignTacticalReviewSaveDto save)
        => DecodePrivateSave(EncodePrivateSave(save));
    public byte[] EncodePrivateSave(CampaignTacticalReviewSaveDto save)
        => JsonSerializer.SerializeToUtf8Bytes(save,Options);
    public CampaignTacticalReviewSaveDto DecodePrivateSave(byte[] bytes)
        => JsonSerializer.Deserialize<CampaignTacticalReviewSaveDto>(bytes,Options)
            ?? throw new InvalidDataException("Missing review checkpoint.");
}
