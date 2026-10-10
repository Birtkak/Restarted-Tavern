using UnityEditor;

namespace RestartedTavern.Client.Editor
{
    /// <summary>
    /// Tavern Guide screenshots (Assets/Resources/Guide, made by Tools/GuideShots): sprites at full size (no power-of-two
    /// rescale), no mipmaps, high-quality compression, so the text in them stays readable.
    /// </summary>
    public sealed class GuideImageImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').Contains("/Resources/Guide/")) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.mipmapEnabled = false;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            ti.maxTextureSize = 2048;
        }
    }
}
