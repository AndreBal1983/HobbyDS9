using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using HobbyDS9.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace HobbyDS9.FoundationRepositoryHost
{
    internal static class StrictFoundationJson
    {
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);
        internal static JsonSerializer Serializer() => JsonSerializer.Create(new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Error, TypeNameHandling = TypeNameHandling.None,
            MaxDepth = 128, DateParseHandling = DateParseHandling.None,
            ContractResolver = new AuthoritativeContractResolver()
        });

        internal static T Decode<T>(byte[] bytes) where T : class
        {
            try
            {
                var text = Utf8.GetString(bytes);
                new JsonGrammar(text).Validate();
                using var input = new StringReader(text);
                using var reader = new JsonTextReader(input) { MaxDepth = 128, DateParseHandling = DateParseHandling.None };
                var token = JToken.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
                var serializer = Serializer();
                // Authored scenarios retain their existing optional authoring fields.
                // Saved envelopes must explicitly carry every emitted DTO member,
                // including nulls and empty lists; DTO initializers are not evidence.
                Wire(token, typeof(T), serializer, typeof(T) == typeof(SaveEnvelopeDto));
                return token.ToObject<T>(serializer) ?? throw new ArgumentException("Empty foundation JSON.");
            }
            catch (Exception e) when (e is JsonException || e is DecoderFallbackException || e is EncoderFallbackException || e is OverflowException)
            { throw new ArgumentException("Invalid foundation JSON.", nameof(bytes), e); }
        }

        private static void Wire(JToken token, Type declared, JsonSerializer serializer, bool completeMembers)
        {
            var nullable = Nullable.GetUnderlyingType(declared);
            if (token.Type == JTokenType.Null)
            {
                if (declared.IsValueType && nullable == null) throw new ArgumentException("Null scalar.");
                return;
            }
            var type = nullable ?? declared;
            if (type == typeof(string))
            {
                if (token.Type != JTokenType.String) throw new ArgumentException("Expected JSON string.");
                Utf8.GetByteCount(token.Value<string>()!); // Reject lone UTF-16 surrogates, including escaped ones.
                return;
            }
            if (type == typeof(bool))
            {
                if (token.Type != JTokenType.Boolean) throw new ArgumentException("Expected JSON boolean.");
                return;
            }
            if (type.IsEnum || type == typeof(int) || type == typeof(long) || type == typeof(ulong))
            {
                // Forbid string/float coercion, including lossy UInt64 -> double -> UInt64.
                if (token.Type != JTokenType.Integer) throw new ArgumentException("Expected exact JSON integer.");
                token.ToObject(type, serializer); // Performs checked integral range validation.
                return;
            }
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                if (!(token is JArray array)) throw new ArgumentException("Expected JSON array.");
                foreach (var item in array) Wire(item, type.GetGenericArguments()[0], serializer, completeMembers);
                return;
            }
            if (!(token is JObject obj) || !(serializer.ContractResolver.ResolveContract(type) is JsonObjectContract contract))
                throw new ArgumentException("Unsupported foundation wire type.");
            foreach (var property in obj.Properties())
            {
                // Newtonsoft normally accepts case-insensitive aliases; this codec does not.
                var member = contract.Properties.GetProperty(property.Name, StringComparison.Ordinal);
                if (member == null || member.Ignored || !member.Writable || member.PropertyType == null)
                    throw new ArgumentException("Unknown foundation member: " + property.Name);
                Wire(property.Value, member.PropertyType, serializer, completeMembers);
            }
            foreach (var member in contract.Properties)
                if (!member.Ignored && member.Readable && (completeMembers || member.Required == Required.Always)
                    && obj.Property(member.PropertyName!, StringComparison.Ordinal) == null)
                    throw new ArgumentException("Missing foundation member: " + member.PropertyName);
        }

        private sealed class AuthoritativeContractResolver : DefaultContractResolver
        {
            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization serialization)
            {
                var property = base.CreateProperty(member, serialization);
                if (member.DeclaringType?.Assembly == typeof(GameStateDto).Assembly && property.PropertyType != null
                    && property.PropertyType.IsValueType && Nullable.GetUnderlyingType(property.PropertyType) == null)
                    property.Required = Required.Always;
                return property;
            }
        }
    }
}
