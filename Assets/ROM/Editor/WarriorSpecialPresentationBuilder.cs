using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using MultiFPS.Gameplay.RiftCombat;
namespace ROM.EditorTools {
public static class WarriorSpecialPresentationBuilder {
 const string Root="Assets/ROM/Characters/Warrior/AbilityIcons";
 static float Segment(Vector2 p,Vector2 a,Vector2 b){var d=b-a;return Vector2.Distance(p,a+d*Mathf.Clamp01(Vector2.Dot(p-a,d)/d.sqrMagnitude));}
 static Color Paint(Vector2 p,int kind){
  float radius=(p-new Vector2(.5f,.5f)).magnitude;var color=Color.Lerp(new Color(.075f,.10f,.15f),new Color(.008f,.013f,.027f),Mathf.Clamp01(radius*1.8f));var gold=new Color(.9f,.68f,.29f);var ivory=new Color(1,.93f,.7f);var fire=new Color(1,.37f,.12f);var ice=new Color(.25f,.8f,1);
  float edge=Mathf.Min(Mathf.Min(p.x,p.y),Mathf.Min(1-p.x,1-p.y));if(edge<.025f)return new Color(.08f,.07f,.065f);if(edge<.035f)return gold*.65f;
  if(kind==0){
   if(Segment(p,new Vector2(.5f,.22f),new Vector2(.5f,.79f))<.04f||Segment(p,new Vector2(.5f,.79f),new Vector2(.3f,.59f))<.045f||Segment(p,new Vector2(.5f,.79f),new Vector2(.7f,.59f))<.045f)color=ivory;
   if(Segment(p,new Vector2(.18f,.2f),new Vector2(.82f,.2f))<.018f)color=gold;
   if(Segment(p,new Vector2(.5f,.2f),new Vector2(.42f,.13f))<.015f||Segment(p,new Vector2(.42f,.13f),new Vector2(.54f,.08f))<.015f)color=fire;
   if(Segment(p,new Vector2(.21f,.32f),new Vector2(.33f,.47f))<.02f||Segment(p,new Vector2(.79f,.32f),new Vector2(.67f,.47f))<.02f)color=fire;
  }else if(kind==1){
   var delta=p-new Vector2(.5f,.5f);float angle=Mathf.Atan2(delta.y,delta.x);float r=delta.magnitude;
   if(Mathf.Abs(r-.29f)<.035f&&Mathf.Sin(angle*2+1)>.1f||Mathf.Abs(r-.40f)<.015f&&Mathf.Sin(angle*2-1)>.1f)color=ice;
   if(Segment(p,new Vector2(.35f,.28f),new Vector2(.61f,.72f))<.023f)color=gold;
   if(p.y>.53f&&p.y<.75f&&p.x>.42f&&p.x<.73f&&p.x+p.y<1.4f)color=ivory;
  }else{
   float y=p.y;float half=y>.35f?.28f:.28f*Mathf.Clamp01((y-.13f)/.22f);bool shield=y>.13f&&y<.8f&&Mathf.Abs(p.x-.5f)<half;
   if(shield){color=new Color(.10f,.24f,.35f);if(Mathf.Abs(p.x-.5f)>half-.035f||y>.76f||y<.19f)color=ivory;}
   if(shield&&(Segment(p,new Vector2(.5f,.31f),new Vector2(.5f,.66f))<.025f||Segment(p,new Vector2(.39f,.56f),new Vector2(.61f,.56f))<.025f))color=gold;
   if(Segment(p,new Vector2(.13f,.5f),new Vector2(.2f,.5f))<.017f||Segment(p,new Vector2(.8f,.5f),new Vector2(.87f,.5f))<.017f||Segment(p,new Vector2(.5f,.85f),new Vector2(.5f,.92f))<.017f)color=gold;
  }
  return color;
 }
 static Sprite Icon(string name,int kind){
  string path=Root+"/"+name+".png";var texture=new Texture2D(256,256,TextureFormat.RGBA32,false);var pixels=new Color[256*256];for(int y=0;y<256;y++)for(int x=0;x<256;x++){Color c=Color.clear;for(int sy=0;sy<2;sy++)for(int sx=0;sx<2;sx++)c+=Paint(new Vector2((x+(sx+.5f)/2)/256f,(y+(sy+.5f)/2)/256f),kind)*.25f;pixels[y*256+x]=c;}texture.SetPixels(pixels);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.mipmapEnabled=false;importer.alphaIsTransparency=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);
 }
 public static void Build(){Directory.CreateDirectory(Root);AssetDatabase.Refresh();var names=new[]{"WarbornLeap","WhirlingExecution","LastStand"};for(int i=0;i<3;i++){var ability=AssetDatabase.LoadAssetAtPath<AbilityDefinition>("Assets/ROM/Combat/"+names[i]+".asset");if(!ability)throw new Exception("Missing "+names[i]);Icon(names[i],i); /* Retained draft art only. Never replace restored production icons. */}AssetDatabase.SaveAssets();}
 public static void AuditRuntimeContent(){
  var lines=new System.Collections.Generic.List<string>();
  Action<bool,string> check=(ok,message)=>{if(!ok)throw new Exception(message);lines.Add("PASS "+message);};
  var scenes=EditorBuildSettings.scenes;var paths=System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(System.Linq.Enumerable.Where(scenes,s=>s.enabled),s=>s.path));
  check(Array.IndexOf(paths,"Assets/ROM/Scenes/MainMenu.unity")>=0&&Array.IndexOf(paths,"Assets/ROM/Levels/map_rom_horde.unity")>=0,"Main menu and El Rift are enabled player scenes");
  var deps=new System.Collections.Generic.HashSet<string>(AssetDatabase.GetDependencies(paths,true));
  foreach(var name in new[]{"WarbornLeap","WhirlingExecution","LastStand"}){var ability=AssetDatabase.LoadAssetAtPath<AbilityDefinition>("Assets/ROM/Combat/"+name+".asset");check(ability&&ability.Icon&&deps.Contains(AssetDatabase.GetAssetPath(ability.Icon)),name+" icon is referenced by enabled-scene dependency graph");}
  foreach(var name in new[]{"WarbornLeap","WhirlingExecution","LastStand"})check(!deps.Contains(Root+"/"+name+".png"),"Unused draft icon is not referenced by enabled scenes: "+name);
  check(Resources.Load<Shader>("RiftQuestHighlight"),"Quest shader is a runtime Resources asset, included independently of Editor fixtures");
  const string hud="Assets/ROM/UI/GameplayHUD/Resources/RiftGameplayHUD.prefab";
  check(AssetDatabase.LoadAssetAtPath<GameObject>(hud),"Runtime HUD prefab resides in Resources");
  check(Array.IndexOf(AssetDatabase.GetDependencies(hud,true),"Assets/ROM/Scripts/UI/GameplayHUD/RiftJournalHUD.cs")>=0,"Runtime HUD references journal component that installs quest guidance");
  foreach(var path in new[]{"Assets/ROM/Scripts/UI/GameplayHUD/RiftQuestGuidance.cs","Assets/ROM/Scripts/Progression/RiftQuestCollectible.cs","Assets/ROM/Scripts/Progression/RiftQuestCollection.cs","Assets/MultiFPS/Scripts/Gameplay/RiftCombat/WarriorSpecialMotion.cs"})check(!path.Contains("/Editor/")&&File.Exists(path),"Runtime implementation outside Editor: "+path);
  lines.Add("NOT RUN Standalone/mobile build and separate remote client. This audit verifies inclusion paths and dependencies, not an executed player build.");
  var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-riftReviewOutput");var output=index>=0&&index+1<args.Length?args[index+1]:Path.GetFullPath("Logs/BagReview");Directory.CreateDirectory(output);File.WriteAllLines(Path.Combine(output,"rift-runtime-content.txt"),lines);
 }
 public static void RunRuntimeAudit(){try{AuditRuntimeContent();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 public static void Run(){try{Build();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
}
