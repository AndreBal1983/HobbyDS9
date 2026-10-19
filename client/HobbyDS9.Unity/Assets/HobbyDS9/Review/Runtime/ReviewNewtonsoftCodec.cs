#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace HobbyDS9.UnityReview
{
    // Actual Unity-compatible codec. Complete physical package/root only; no engine API or net10 dependency.
    public sealed partial class ReviewNewtonsoftCodec : ISingleplayerDemoHostCodec
    {
        private static readonly UTF8Encoding Utf8=new UTF8Encoding(false,true);
        private static JsonSerializerSettings Settings() => new JsonSerializerSettings {
            MissingMemberHandling=MissingMemberHandling.Error,TypeNameHandling=TypeNameHandling.None,
            MetadataPropertyHandling=MetadataPropertyHandling.Ignore,DateParseHandling=DateParseHandling.None,
            FloatParseHandling=FloatParseHandling.Decimal,Culture=CultureInfo.InvariantCulture,
            MaxDepth=128,ContractResolver=new RequiredScalarResolver()
        };
        public CampaignTacticalTravelPackageDto CopyPackage(CampaignTacticalTravelPackageDto package)
            => Decode<CampaignTacticalTravelPackageDto>(Encode(package));
        public CampaignTacticalTravelSaveDto CopySave(CampaignTacticalTravelSaveDto root)
            => Decode<CampaignTacticalTravelSaveDto>(Encode(root));
        public byte[] EncodeRoot(CampaignTacticalTravelSaveDto root) => Encode(root);
        public CampaignTacticalTravelSaveDto DecodeRoot(byte[] bytes) => Decode<CampaignTacticalTravelSaveDto>(bytes);
        public CampaignTacticalTravelPackageDto DecodeProfile(byte[] bytes) => Decode<CampaignTacticalTravelPackageDto>(bytes);
        public CampaignTacticalReviewPackageDto DecodeCoreProfile(byte[] bytes) => Decode<CampaignTacticalReviewPackageDto>(bytes);
        public ScenarioDto DecodeScenario(byte[] bytes) => Decode<ScenarioDto>(bytes);
        public CampaignIntegratedPlaytestPackageDto CopyPackage(CampaignIntegratedPlaytestPackageDto package)=>Decode<CampaignIntegratedPlaytestPackageDto>(Encode(package));
        public CampaignIntegratedPlaytestSaveDto CopySave(CampaignIntegratedPlaytestSaveDto save)=>Decode<CampaignIntegratedPlaytestSaveDto>(Encode(save));
        public CampaignIntegratedPlaytestPackageDto DecodeIntegratedProfile(byte[] bytes)=>Decode<CampaignIntegratedPlaytestPackageDto>(bytes);
        public byte[] EncodeIntegratedRoot(CampaignIntegratedPlaytestSaveDto save)=>Encode(save);
        public CampaignIntegratedPlaytestSaveDto DecodeIntegratedRoot(byte[] bytes)=>Decode<CampaignIntegratedPlaytestSaveDto>(bytes);
        public CampaignSingleplayerDemoPackageDto CopyPackage(CampaignSingleplayerDemoPackageDto package)=>Decode<CampaignSingleplayerDemoPackageDto>(Encode(package));
        public CampaignSingleplayerDemoSaveDto CopySave(CampaignSingleplayerDemoSaveDto save)=>DecodeDemoRoot(EncodeDemoRoot(save));
        public CampaignSingleplayerDemoPackageDto DecodeDemoProfile(byte[] bytes)=>Decode<CampaignSingleplayerDemoPackageDto>(bytes);
        public byte[] EncodeDemoRoot(CampaignSingleplayerDemoSaveDto save)
        {CheckDemoCarrier(save);return Encode(save);}
        public CampaignSingleplayerDemoSaveDto DecodeDemoRoot(byte[] bytes)
        {var save=Decode<CampaignSingleplayerDemoSaveDto>(bytes);CheckDemoCarrier(save);return save;}
        private static void CheckDemoCarrier(CampaignSingleplayerDemoSaveDto save)
        {if(save.FormatVersion!=CampaignSingleplayerDemoIdentity.Format||save.ProfileId!=CampaignSingleplayerDemoIdentity.Profile||save.Integrated==null||save.OpponentPlan==null)throw Invalid();}
        private static byte[] Encode<T>(T value)
        {
            try { return Utf8.GetBytes(JsonConvert.SerializeObject(value,Formatting.None,Settings())); }
            catch(Exception) { throw Invalid(); }
        }
        private static T Decode<T>(byte[] bytes)
        {
            try
            {
                using(var text=new StringReader(Utf8.GetString(bytes)))
                using(var reader=new JsonTextReader(text) { DateParseHandling=DateParseHandling.None,FloatParseHandling=FloatParseHandling.Decimal,MaxDepth=128 })
                {
                    var token=JToken.Load(reader,new JsonLoadSettings { DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error,LineInfoHandling=LineInfoHandling.Ignore });
                    if(reader.Read()) throw Invalid();
                    ValidateShape(token,typeof(T));
                    var value=token.ToObject<T>(JsonSerializer.Create(Settings()));
                    if(value == null) throw Invalid();
                    return value;
                }
            }
            catch(Exception) { throw Invalid(); } // No content/path/raw Newtonsoft diagnostics.
        }
        private static InvalidDataException Invalid() => new InvalidDataException("Invalid physical review data.");
        private static void ValidateShape(JToken token,Type declared)
        {
            var nullable=Nullable.GetUnderlyingType(declared);
            var type=nullable ?? declared;
            if(token.Type == JTokenType.Null)
            { if(declared.IsValueType && nullable == null) throw Invalid(); return; }
            if(type == typeof(string)) { if(token.Type != JTokenType.String) throw Invalid(); return; }
            if(type == typeof(bool)) { if(token.Type != JTokenType.Boolean) throw Invalid(); return; }
            if(type.IsEnum || type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort)
                || type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong))
            { if(token.Type != JTokenType.Integer) throw Invalid(); return; }
            if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                if(!(token is JArray array)) throw Invalid();
                foreach(var item in array) ValidateShape(item,type.GetGenericArguments()[0]);
                return;
            }
            if(type.Assembly != typeof(GameStateDto).Assembly || !(token is JObject obj)) throw Invalid();
            var properties=type.GetProperties(BindingFlags.Public|BindingFlags.Instance).Where(p=>p.CanRead && p.CanWrite)
                .ToDictionary(p=>p.Name,StringComparer.Ordinal);
            foreach(var property in obj.Properties())
            {
                if(!properties.TryGetValue(property.Name,out var contract)) throw Invalid();
                ValidateShape(property.Value,contract.PropertyType);
            }
            foreach(var contract in properties.Values)
                if(contract.PropertyType.IsValueType && Nullable.GetUnderlyingType(contract.PropertyType) == null && obj.Property(contract.Name,StringComparison.Ordinal) == null)
                    throw Invalid();
        }
        private sealed class RequiredScalarResolver : DefaultContractResolver
        {
            protected override JsonProperty CreateProperty(MemberInfo member,MemberSerialization serialization)
            {
                var property=base.CreateProperty(member,serialization);
                if(member.DeclaringType?.Assembly == typeof(GameStateDto).Assembly && property.PropertyType != null
                    && property.PropertyType.IsValueType && Nullable.GetUnderlyingType(property.PropertyType) == null)
                    property.Required=Required.Always;
                return property;
            }
        }
    }
}
