using UnityEngine;
namespace ROM.UI.Sprint01 {
// Explicit owned-asset references for normal MainMenu startup; no Editor/test dependency.
public sealed class RiftLivingMenuAssets : ScriptableObject {
 public GameObject PortalBase,Column;
 public Material Smoke,Sparks;
 public Shader EnergyShader,EmberShader;
 public static void Attach(RiftModelView view){
  if(!view||!view.PreviewWorld||view.PreviewWorld.GetComponentInChildren<RiftLivingMenuStage>())return;
  var assets=Resources.Load<RiftLivingMenuAssets>("RiftMenu/LivingMenuAssets");
  if(!assets||!assets.PortalBase||!assets.Column||!assets.Smoke||!assets.Sparks){Debug.LogWarning("Rift menu: presentation assets unavailable; using the ordinary character preview.");return;}
  view.EnableWideMenuPreview();
  RiftLivingMenuStage.Create(view.PreviewWorld,assets.PortalBase,assets.Smoke,assets.Sparks,assets.Column);
 }
}
}
