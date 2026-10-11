#nullable enable
using System;
using UnityEngine;
namespace HobbyDS9.UnityClient
{
    public enum LcarsSfx { ButtonPress, Success, Error, Processing, ScreenOpen, Search }
    [Serializable] public sealed class LcarsSfxEntry
    {
        public LcarsSfx effect;
        public string semanticId = "";
        public string intendedFilename = "";
        public AudioClip? clip;
    }
    [CreateAssetMenu(menuName = "HobbyDS9/LCARS SFX catalog")]
    public sealed class LcarsSfxCatalog : ScriptableObject
    {
        public LcarsSfxEntry[] entries = Array.Empty<LcarsSfxEntry>();
        public AudioClip? Find(LcarsSfx effect)
        {
            foreach (var entry in entries) if (entry != null && entry.effect == effect) return entry.clip;
            return null;
        }
    }
}
