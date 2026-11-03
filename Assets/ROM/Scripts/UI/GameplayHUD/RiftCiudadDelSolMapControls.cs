using UnityEngine;

namespace ROM.UI.Gameplay {
// Runtime-only additions to the existing M map. No prefab or shared theme changes.
[DefaultExecutionOrder(1250)]
public sealed class RiftCiudadDelSolMapControls : MonoBehaviour {
 RiftFieldUI map;
 RectTransform panel;
 UnityEngine.UI.Button[] buttons;
 readonly string[] labels={"Automático", "Galerías · Y=-16", "Intermedio · Y=-24", "Terrazas · Y=-30", "Profundo · Y=-34 / -38"};
 UnityEngine.CanvasGroup markerOpacity;
 float originalOpacity;
 bool ownsOpacity, wasCity;

 void Awake(){map=GetComponent<RiftFieldUI>();}
 void Build(){
  var parent=map.MapPanel.Find("Map navigation");
  panel=new GameObject("Ciudad del Sol floor selector",typeof(RectTransform)).GetComponent<RectTransform>();
  panel.SetParent(parent?parent:map.MapPanel,false);
  panel.anchorMin=panel.anchorMax=panel.pivot=new Vector2(0,1);
  panel.anchoredPosition=parent?new Vector2(0,-140):new Vector2(24,-140);
  panel.sizeDelta=new Vector2(300,200);
  var background=panel.gameObject.AddComponent<UnityEngine.UI.Image>();
  background.color=new Color(.035f,.038f,.06f,1);background.raycastTarget=true;
  buttons=new UnityEngine.UI.Button[5];
  for(int i=0;i<buttons.Length;i++){
   int selection=i-1;
   var row=new GameObject(labels[i],typeof(RectTransform)).GetComponent<RectTransform>();
   row.SetParent(panel,false);row.anchorMin=row.anchorMax=row.pivot=new Vector2(0,1);
   row.anchoredPosition=new Vector2(5,-5-i*38);row.sizeDelta=new Vector2(290,34);
   var image=row.gameObject.AddComponent<UnityEngine.UI.Image>();image.color=new Color(.15f,.12f,.22f,1);
   var button=row.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=image;
   button.navigation=new UnityEngine.UI.Navigation{mode=UnityEngine.UI.Navigation.Mode.None};
   button.onClick.AddListener(()=>RiftCiudadDelSolCartography.SelectFloor(selection));buttons[i]=button;
   var textRect=new GameObject("Floor label",typeof(RectTransform)).GetComponent<RectTransform>();
   textRect.SetParent(row,false);textRect.anchorMin=Vector2.zero;textRect.anchorMax=Vector2.one;
   textRect.offsetMin=textRect.offsetMax=Vector2.zero;
   var text=textRect.gameObject.AddComponent<UnityEngine.UI.Text>();text.font=map.Theme.BodyFont;
   text.text=labels[i];text.fontSize=16;text.color=Color.white;text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;
  }
 }
 void LateUpdate(){
  if(!map)return;
  bool city=map.CityCartography;
  if(city&&!panel)Build();
  if(panel)panel.gameObject.SetActive(city&&map.MapOpen);
  if(!city){if(wasCity){RestoreMarker();map.MapInfo.text="Mapa general · Rueda: zoom · Arrastrar: mover.";}wasCity=false;return;}
  wasCity=true;
  if(!map.MapOpen)return;
  for(int i=0;i<buttons.Length;i++){
   bool selected=RiftCiudadDelSolCartography.ManualFloor==i-1;
   buttons[i].targetGraphic.color=selected?new Color(.43f,.28f,.64f,1):new Color(.15f,.12f,.22f,1);
   buttons[i].GetComponentInChildren<UnityEngine.UI.Text>().text=(selected?"● ":"")+labels[i];
  }
  bool other=RiftCiudadDelSolCartography.ManualFloor>=0 && RiftCiudadDelSolCartography.DisplayFloor!=RiftCiudadDelSolCartography.AutomaticFloor;
  if(!markerOpacity){markerOpacity=map.PlayerMarker.GetComponent<UnityEngine.CanvasGroup>();ownsOpacity=!markerOpacity;if(ownsOpacity)markerOpacity=map.PlayerMarker.gameObject.AddComponent<UnityEngine.CanvasGroup>();originalOpacity=markerOpacity.alpha;}
  markerOpacity.alpha=other?.35f:originalOpacity;
  map.MapInfo.text="CIUDAD DEL SOL · "+(RiftCiudadDelSolCartography.ManualFloor<0?"AUTO":"MANUAL")+"\n"+
   RiftCiudadDelSolCartography.DisplayName+"\n"+(other?"Estás en: "+RiftCiudadDelSolCartography.CurrentName:"Naranja: escaleras · Flecha: subida");
 }
 void RestoreMarker(){if(!markerOpacity)return;markerOpacity.alpha=originalOpacity;if(ownsOpacity)Destroy(markerOpacity);markerOpacity=null;ownsOpacity=false;}
 void OnDisable(){RestoreMarker();if(panel)panel.gameObject.SetActive(false);}
 void OnDestroy(){RestoreMarker();if(panel)Destroy(panel.gameObject);}
}
}
