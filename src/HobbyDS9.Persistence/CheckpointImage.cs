using System;
using System.Security.Cryptography;
using System.Text;

namespace HobbyDS9.Persistence
{
    // Trusted host data. This is not an actor projection or a game-validity token.
    public sealed class CheckpointImage
    {
        public const int MaximumPayloadBytes = 64 * 1024 * 1024;
        private readonly byte[] payload;
        public string EditionIdentity { get; }
        public int Turn { get; }
        public long Revision { get; }
        public string Phase { get; }
        public string PayloadHash { get; }
        public int PayloadLength => payload.Length;
        public CheckpointImage(byte[] payload, string editionIdentity, int turn, long revision, string phase)
        {
            if (payload == null || payload.Length == 0 || payload.Length > MaximumPayloadBytes) throw new ArgumentException("Payload size.", nameof(payload));
            if (string.IsNullOrWhiteSpace(editionIdentity) || editionIdentity.Length > 16384 || editionIdentity.IndexOf('\0') >= 0) throw new ArgumentException("Complete edition identity required.", nameof(editionIdentity));
            if (turn < 1 || revision < 0 || string.IsNullOrWhiteSpace(phase) || phase.Length > 64 || phase.IndexOf('\0') >= 0) throw new ArgumentException("Invalid source stamp.");
            var strict = new UTF8Encoding(false, true);
            try { strict.GetByteCount(editionIdentity); strict.GetByteCount(phase); }
            catch (EncoderFallbackException e) { throw new ArgumentException("Invalid Unicode.", e); }
            this.payload = (byte[])payload.Clone(); EditionIdentity = editionIdentity; Turn = turn; Revision = revision; Phase = phase;
            PayloadHash = Hash(this.payload);
        }
        public byte[] ReadPayload() => (byte[])payload.Clone();
        public static string Hash(byte[] bytes)
        {
            using var sha = SHA256.Create();
            var result = new StringBuilder(64);
            foreach (var b in sha.ComputeHash(bytes)) result.Append(b.ToString("x2", System.Globalization.CultureInfo.InvariantCulture));
            return result.ToString();
        }
    }
    // Implementation must strictly decode, use the matching complete game loader,
    // verify continuation and extract metadata. Preserve exact input bytes.
    public interface ICheckpointValidator
    {
        CheckpointImage Validate(byte[] ownedPayload);
    }
}
