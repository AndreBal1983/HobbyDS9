using System;
using UnityEditor;
using UnityEngine;

namespace HobbyDS9.UnityReview.Editor
{
    // Runs ONLY during a separately authorised Editor import. Original PNG bytes
    // are numerical linear RGBA; A is dust optical depth, never transparency.
    public sealed class GeneratedGalaxyV3Import : AssetPostprocessor
    {
        public const string AtlasPath="Assets/HobbyDS9/Review/Resources/GalaxyPresentationV3/galaxy.png";
        private void OnPreprocessTexture()
        {
            if(!string.Equals(assetPath,AtlasPath,StringComparison.Ordinal))return;
            var importer=(TextureImporter)assetImporter;
            importer.textureType=TextureImporterType.Default;importer.textureShape=TextureImporterShape.Texture2D;
            importer.sRGBTexture=false;importer.mipmapEnabled=true;importer.isReadable=false;
            importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=false;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.crunchedCompression=false;
            importer.maxTextureSize=2048;importer.npotScale=TextureImporterNPOTScale.None;
            importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=1;
            var platform=importer.GetPlatformTextureSettings("Standalone");platform.overridden=true;
            platform.maxTextureSize=2048;platform.format=TextureImporterFormat.RGBA32;
            platform.textureCompression=TextureImporterCompression.Uncompressed;platform.crunchedCompression=false;
            importer.SetPlatformTextureSettings(platform);
        }
    }
}
