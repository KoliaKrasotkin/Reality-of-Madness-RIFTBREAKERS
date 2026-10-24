using System;
using MultiFPS;
using UnityEngine;
using UnityEngine.UI;
using ROM.UI.Gameplay;
namespace ROM.UI.Sprint01 {
// Cheap sliced surfaces: no full-screen blur or extra render pass for WebGL.
public static class RiftModernWidgets {
 static Sprite rounded;public static readonly Color Accent=new Color(.65f,.45f,.94f),Text=new Color(.91f,.92f,.96f);
 static Sprite Rounded {get{if(rounded)return rounded;const int size=48;var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);texture.wrapMode=TextureWrapMode.Clamp;for(int y=0;y<size;y++)for(int x=0;x<size;x++){float dx=Mathf.Max(8-x,x-39),dy=Mathf.Max(8-y,y-39);float distance=Mathf.Sqrt(Mathf.Max(0,dx)*Mathf.Max(0,dx)+Mathf.Max(0,dy)*Mathf.Max(0,dy));texture.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(8.5f-distance)));}texture.Apply();rounded=Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(12,12,12,12));return rounded;}}
 public static RectTransform Panel(Transform p,string name,float x,float y,float w,float h){var rect=RiftWidgets.Rect(p,name,x,y,w,h);Surface(rect,new Color(.006f,.008f,.013f,.8f));return rect;}
 public static void Surface(RectTransform rect,Color fill){for(int i=rect.childCount-1;i>=0;i--){var c=rect.GetChild(i);if(c.name=="Top"||c.name=="Bottom"||c.name=="Left"||c.name=="Right"||c.name=="Accent")UnityEngine.Object.Destroy(c.gameObject);}var image=rect.GetComponent<Image>();if(!image)image=rect.gameObject.AddComponent<Image>();image.sprite=Rounded;image.type=Image.Type.Sliced;image.color=fill;var edge=rect.GetComponent<Outline>();if(!edge)edge=rect.gameObject.AddComponent<Outline>();edge.effectColor=new Color(.34f,.35f,.43f,.4f);edge.effectDistance=new Vector2(1,-1);}
 public static Button Button(Transform p,string title,float x,float y,float w,float h,Action clicked){var rect=RiftWidgets.Rect(p,title,x,y,w,h);Surface(rect,new Color(.008f,.01f,.018f,.82f));var image=rect.GetComponent<Image>();image.raycastTarget=true;var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;Palette(button);var label=RiftWidgets.Label(rect,title,10,0,w-20,h,18,Text);var scope=p.GetComponentInParent<RiftInventoryFontScope>();label.font=scope?scope.Font:Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");label.alignment=TextAnchor.MiddleCenter;button.onClick.AddListener(()=>clicked());return button;}
 public static void Palette(Button button){var colors=button.colors;colors.normalColor=new Color(.065f,.072f,.095f,.48f);colors.highlightedColor=new Color(.12f,.1f,.18f,.66f);colors.pressedColor=new Color(.13f,.09f,.2f,.74f);colors.selectedColor=new Color(.10f,.075f,.16f,.62f);button.colors=colors;button.targetGraphic.color=Color.white;}
 public static void Tab(Button button,bool selected){var edge=button.transform.Find("Selected underline");if(!edge)edge=RiftWidgets.Image(button.transform,"Selected underline",12,((RectTransform)button.transform).rect.height-2,((RectTransform)button.transform).rect.width-24,2,Accent).transform;edge.gameObject.SetActive(selected);var colors=button.colors;colors.normalColor=selected?new Color(.08f,.055f,.13f,.44f):new Color(.04f,.045f,.065f,.18f);button.colors=colors;button.GetComponent<Image>().color=Color.white;}
}
public sealed class RiftModernKeycap:MonoBehaviour {
 public string Action;public UnityEngine.UI.Text Label;
 void OnEnable(){RiftControls.Changed+=Refresh;}void OnDisable(){RiftControls.Changed-=Refresh;}
 public void Refresh(){if(Label&&!string.IsNullOrEmpty(Action))Label.text=RiftControls.ActionLabel(Action);}
}
public sealed partial class RiftHub {
 RectTransform modernInventoryHeader,modernMoreMenu;
 void BuildModernInventoryHeader(){modernInventoryHeader=RiftModernWidgets.Panel(Design,"Modern inventory navigation",896,35,988,72);modernInventoryHeader.gameObject.AddComponent<RiftInventoryFontScope>().Font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
  // Character statistics/equipment remain available through their existing supported routes.
  var inventory=ModernBound("INVENTARIO","Inventory",12,10,360,52,()=>TogglePanel("Inventory",HubPage.Inventory),HUDShape.Bag);RiftModernWidgets.Tab(inventory,true);AddInventoryActiveGlow(inventory);
  ModernBound("TALENTOS","Talents",586,10,246,52,ShowTalents,HUDShape.Skill);
  RiftModernWidgets.Button(modernInventoryHeader,"MÁS",840,10,64,52,()=>modernMoreMenu.gameObject.SetActive(!modernMoreMenu.gameObject.activeSelf));
  var close=RiftModernWidgets.Button(modernInventoryHeader,"×",912,10,64,52,()=>ResumeRequested?.Invoke());close.GetComponentInChildren<UnityEngine.UI.Text>().fontSize=26;var closeTip=close.gameObject.AddComponent<RiftTooltipTrigger>();closeTip.Title="Cerrar";closeTip.Description="Volver al juego";closeTip.ActionId="Pause";
  modernMoreMenu=RiftModernWidgets.Panel(modernInventoryHeader,"Secondary actions",656,78,320,150);
  ModernBound("HABILIDADES","Abilities",8,6,304,42,()=>{modernMoreMenu.gameObject.SetActive(false);ShowSkills();},HUDShape.Skill,modernMoreMenu);
  ModernBound("DIARIO","Journal",8,54,304,42,()=>{modernMoreMenu.gameObject.SetActive(false);ShowGameplayPage(HubPage.Missions);},HUDShape.Target,modernMoreMenu);
  ModernBound("AJUSTES","Settings",8,102,304,42,()=>{modernMoreMenu.gameObject.SetActive(false);ShowGameSettings();},HUDShape.Gear,modernMoreMenu);
  modernMoreMenu.gameObject.SetActive(false);modernInventoryHeader.gameObject.SetActive(false);
 }
 // Local to the selected Inventory navigation tab; other tabs and shared styles are untouched.
 static Sprite inventoryActiveGlow;
 static void AddInventoryActiveGlow(Button tab){
  if(!inventoryActiveGlow){const int w=64,h=16;var texture=new Texture2D(w,h,TextureFormat.RGBA32,false);texture.name="Inventory tab violet glow";texture.wrapMode=TextureWrapMode.Clamp;texture.filterMode=FilterMode.Bilinear;for(int y=0;y<h;y++)for(int x=0;x<w;x++){float edge=Mathf.Pow(Mathf.Max(0,Mathf.Sin(Mathf.PI*x/(w-1))),.45f);float vertical=(y/(float)(h-1)-.5f)*2;float alpha=edge*Mathf.Exp(-vertical*vertical*5);texture.SetPixel(x,y,new Color(1,1,1,alpha));}texture.Apply(false,true);inventoryActiveGlow=Sprite.Create(texture,new Rect(0,0,w,h),new Vector2(.5f,.5f),100);}
  var rect=(RectTransform)tab.transform;var glow=RiftWidgets.Rect(tab.transform,"Inventory active violet glow",0,rect.rect.height-10,rect.rect.width,20);glow.SetAsFirstSibling();var image=glow.gameObject.AddComponent<Image>();image.sprite=inventoryActiveGlow;image.color=new Color(.62f,.36f,.96f,.28f);image.raycastTarget=false;
 }
 Button ModernBound(string label,string action,float x,float y,float w,float h,Action clicked,HUDShape shape,Transform parent=null){var b=RiftModernWidgets.Button(parent?parent:modernInventoryHeader,label,x,y,w,h,clicked);var text=b.GetComponentInChildren<UnityEngine.UI.Text>();text.rectTransform.anchoredPosition=new Vector2(46,0);text.rectTransform.sizeDelta=new Vector2(w-122,h);text.alignment=TextAnchor.MiddleLeft;text.fontSize=16;
  var icon=RiftWidgets.Rect(b.transform,"Navigation icon",12,(h-24)/2,24,24).gameObject.AddComponent<RiftHUDGraphic>();icon.Shape=shape;icon.color=new Color(.73f,.72f,.82f);icon.Border=new Color(.73f,.72f,.82f);icon.raycastTarget=false;
  var cap=RiftModernWidgets.Panel(b.transform,"Binding",w-66,(h-26)/2,56,26);cap.GetComponent<Image>().raycastTarget=false;var key=RiftWidgets.Label(cap,"",3,0,50,26,12,RiftWidgets.Muted);key.alignment=TextAnchor.MiddleCenter;key.resizeTextForBestFit=key.font&&key.font.dynamic;key.resizeTextMinSize=9;key.resizeTextMaxSize=12;var binding=b.gameObject.AddComponent<RiftModernKeycap>();binding.Action=action;binding.Label=key;binding.Refresh();
  var tip=b.gameObject.AddComponent<RiftTooltipTrigger>();tip.Title=label;tip.Description="Clic o atajo. Reasignable en Ajustes > Controles.";tip.ActionId=action;return b;
 }
}

}

