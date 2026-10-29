using System;using UnityEngine;
namespace ROM.Rendering {
 [CreateAssetMenu(menuName="Rift/Visual/Atmosphere pilot settings")]
 public sealed class RiftAtmospherePilotSettings:ScriptableObject {
  [Header("Opt-in scope, no scene changes")][Tooltip("Only this scene and this radius around its starting checkpoint.")]public string SceneName="map_rom_horde";[Min(20)]public float Radius=130;
  [Header("Nocturnal palette, readable foreground")][Range(0,2)]public float BaseExposure=.50f;[Range(0,2)]public float SkyExposure=.58f;[Range(0,3)]public float AmbientIntensity=1.5f;
  public Color AmbientSky=new Color(.48f,.55f,.82f),AmbientHorizon=new Color(.36f,.40f,.65f),AmbientGround=new Color(.23f,.27f,.43f);
  [Range(0,3)]public float KeyIntensity=1.08f;public Color KeyColor=new Color(.50f,.59f,1);[Range(0,1)]public float ShadowStrength=.65f;
  [Header("World-space sky bounce, no camera-attached light")][Range(0,1)]public float FillIntensity=.34f;public Color FillColor=new Color(.63f,.69f,.90f);
  [Header("Three readable depth planes")][Min(0)]public float FogStart=35;[Min(50)]public float FogEnd=210;public Color FogColor=new Color(.15f,.14f,.27f);
  [Header("Local crystal, motivated spill")][ColorUsage(false,true)]public Color CrystalEnergy=new Color(.90f,.14f,2.4f);[ColorUsage(false,true)]public Color CrystalEdge=new Color(.36f,.10f,.75f);
  public Shader CrystalShader;public Color SpillColor=new Color(.62f,.26f,1);[Min(0)]public float SpillIntensity=14;[Min(1)]public float SpillRange=27;
  [Header("Postprocess, composed with player preferences")][Range(0,1)]public float BloomIntensity=.18f;[Min(0)]public float BloomThreshold=1.25f;[Range(-20,20)]public float Contrast=6,Saturation=2;[Range(0,.3f)]public float Vignette=.09f;
  public Color ShadowTone=new Color(.46f,.47f,.54f),HighlightTone=new Color(.53f,.51f,.48f);
  [Serializable]public sealed class MaterialRule {public Material Source;[Range(0,1)]public float Metallic,Smoothness=.18f;}
  [Header("Only explicit material slots; never whole-body metallic") ]public MaterialRule[] MaterialRules=Array.Empty<MaterialRule>();
  public int Revision{get;private set;}
  void OnValidate(){FogEnd=Mathf.Max(FogStart+30,FogEnd);Radius=Mathf.Max(20,Radius);Revision++;}
 }
}
