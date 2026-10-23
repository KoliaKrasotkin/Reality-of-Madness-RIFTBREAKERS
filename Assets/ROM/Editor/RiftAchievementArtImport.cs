using UnityEditor;
using UnityEngine;
namespace ROM.EditorTools {
public sealed class RiftAchievementArtImport:AssetPostprocessor {
 void OnPreprocessTexture(){if(assetPath!="Assets/ROM/Resources/RiftAchievements/AchievementIcons.png")return;var importer=(TextureImporter)assetImporter;importer.textureType=TextureImporterType.Default;importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;}
}}
