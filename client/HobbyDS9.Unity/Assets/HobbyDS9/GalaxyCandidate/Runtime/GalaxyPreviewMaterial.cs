using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace HobbyDS9.GalaxyCandidate
{
    // Opt-in adapter only. Existing ArtDemo catalog and preview stage do not call it.
    // Caller owns the returned material; referenced source textures remain borrowed.
    public static class GalaxyPreviewMaterial
    {
        public static Material Create(Material source, Shader shader)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (shader == null || shader.name != "HobbyDS9/GalaxyCandidate/TexturedStudio" || !shader.isSupported)
                throw new ArgumentException("A supported candidate studio shader is required.", nameof(shader));
            foreach (var property in new[] { "_BaseMap", "_BumpMap", "_MetallicGlossMap", "_EmissionMap" })
                if (!source.HasProperty(property) || source.GetTexture(property) == null)
                    throw new ArgumentException("Missing required candidate texture: " + property, nameof(source));

            var target = new Material(shader) { name = source.name + " candidate preview", hideFlags = HideFlags.DontSave };
            foreach (var property in new[] { "_BaseMap", "_BumpMap", "_MetallicGlossMap", "_EmissionMap" })
            {
                target.SetTexture(property, source.GetTexture(property));
                target.SetTextureScale(property, source.GetTextureScale(property));
                target.SetTextureOffset(property, source.GetTextureOffset(property));
            }
            target.SetColor("_BaseColor", source.GetColor("_BaseColor"));
            target.SetColor("_EmissionColor", source.GetColor("_EmissionColor"));
            target.SetFloat("_BumpScale", source.GetFloat("_BumpScale"));
            target.SetFloat("_Smoothness", source.GetFloat("_Smoothness"));
            // URP's metallic-map R replaces the scalar metallic value. Smoothness
            // multiplies map A. Source glTF factors are one, set by the builder.
            target.SetFloat("_Cull", (float)CullMode.Off);
            return target;
        }
    }
}
