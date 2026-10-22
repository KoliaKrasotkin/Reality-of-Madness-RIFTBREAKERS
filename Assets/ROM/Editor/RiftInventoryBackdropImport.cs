using UnityEditor;
namespace ROM.EditorTools {
// Scope texture settings to the approved preview backdrop; no scene/global rendering changes.
public sealed class RiftInventoryBackdropImport:AssetPostprocessor {
 void OnPreprocessTexture(){if(assetPath!="Assets/ROM/Resources/RiftInventoryBackdrop.png")return;var t=(TextureImporter)assetImporter;t.textureType=TextureImporterType.Default;t.alphaIsTransparency=true;t.mipmapEnabled=false;t.maxTextureSize=1024;t.textureCompression=TextureImporterCompression.Compressed;t.npotScale=TextureImporterNPOTScale.None;t.wrapMode=UnityEngine.TextureWrapMode.Clamp;}
}
}
