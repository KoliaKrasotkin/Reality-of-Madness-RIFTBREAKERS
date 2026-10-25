using ROM.UI.Gameplay;
using UnityEngine;
namespace ROM.UI.Sprint01 {
[CreateAssetMenu(menuName="Rift/UI/Main Menu Layout",fileName="MainMenuLayout")]
public sealed class RiftMainMenuLayout:ScriptableObject {
 [Header("1920 x 1080 design coordinates; x/y from top left")]
 public Rect NavigationGlass=new Rect(36,48,500,984);
 public Rect Logo=new Rect(70,115,430,143);
 public Rect PrimaryButtons=new Rect(68,330,436,88);
 [Min(0)]public float PrimarySpacing=112;
 public Rect SecondaryActions=new Rect(68,826,436,100);
 public Rect Identity=new Rect(1540,864,344,160);
 public Rect Version=new Rect(650,1034,1200,26);
 [Header("Glass and active accents")]
 public Color GlassColor=new Color(.037f,.04f,.065f,.83f);
 public Color BorderColor=new Color(.48f,.47f,.6f,.8f);
 public Color ActiveColor=new Color(.66f,.4f,1);
 [Range(0,1)]public float BackgroundOpacity=.05f;
 static RiftMainMenuLayout fallback;
 public static RiftMainMenuLayout Load(){var asset=Resources.Load<RiftMainMenuLayout>("RiftMenu/MainMenuLayout");if(asset)return asset;if(!fallback){fallback=CreateInstance<RiftMainMenuLayout>();fallback.hideFlags=HideFlags.HideAndDontSave;}return fallback;}
 public void Apply(RectTransform root){
  Set(root,"Main navigation glass",NavigationGlass);Set(root,"Approved ROM logo",Logo);Set(root,"Secondary existing actions",SecondaryActions);Set(root,"Selected skin identity",Identity);
  string[] actions={"Jugar","Personaje","Ajustes","Salir"};for(int i=0;i<actions.Length;i++){
   var rect=new Rect(PrimaryButtons.x,PrimaryButtons.y+i*PrimarySpacing,Mathf.Max(240,PrimaryButtons.width),Mathf.Max(56,PrimaryButtons.height));Set(root,"Main menu "+actions[i],rect);
   var button=root.Find("Main menu "+actions[i]);if(!button)continue;var surface=button.GetComponent<RiftMapSurface>();if(surface)surface.Border=i==0?ActiveColor:BorderColor;
   foreach(var text in button.GetComponentsInChildren<UnityEngine.UI.Text>()){var t=text.rectTransform;if(text.text==">"){t.anchoredPosition=new Vector2(rect.width-38,0);t.sizeDelta=new Vector2(28,rect.height);}else{t.sizeDelta=new Vector2(rect.width-130,rect.height);}}
   var icon=button.Find("Action icon") as RectTransform;if(icon)icon.anchoredPosition=new Vector2(24,(rect.height-icon.sizeDelta.y)*-.5f);
  }
  var glass=root.Find("Main navigation glass");if(glass){var s=glass.GetComponent<RiftMapSurface>();if(s){s.Fill=GlassColor;s.Border=BorderColor;}}
  foreach(var text in root.GetComponentsInChildren<UnityEngine.UI.Text>())if(text.name=="Rift release version"||text.text.StartsWith("Versión "))ApplyRect(text.rectTransform,Version);
 }
 static void Set(Transform root,string name,Rect value){var rect=root.Find(name) as RectTransform;if(rect)ApplyRect(rect,value);}
 static void ApplyRect(RectTransform rect,Rect value){rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(value.x,-value.y);rect.sizeDelta=value.size;}
}
}
