using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ROM.UI.Sprint01;
namespace ROM.UI {
public partial class ROMLoadingScreen {
 bool transitionTimedOut;
 RectTransform modernRoot;
 Text modernStage;
 Button dismissTransition;
 void BuildModernPresentation(){
  if(!_group)return;
  var parent=_group.transform;
  foreach(Transform child in parent)child.gameObject.SetActive(false);
  modernRoot=new GameObject("Modern Rift loading",typeof(RectTransform)).GetComponent<RectTransform>();modernRoot.SetParent(parent,false);modernRoot.anchorMin=Vector2.zero;modernRoot.anchorMax=Vector2.one;modernRoot.sizeDelta=Vector2.zero;
  var backdrop=Stretch("Approved Rift environment",modernRoot).gameObject.AddComponent<RawImage>();backdrop.texture=Resources.Load<Texture2D>("RiftMenu/RiftBackground");backdrop.raycastTarget=false;backdrop.color=Color.white;
  if(backdrop.texture){var fit=backdrop.gameObject.AddComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;fit.aspectRatio=(float)backdrop.texture.width/backdrop.texture.height;}
  var shade=Stretch("Local background readability",modernRoot).gameObject.AddComponent<Image>();shade.color=new Color(.013f,.012f,.026f,.65f);shade.raycastTarget=true;
  var safe=new GameObject("Loading safe content",typeof(RectTransform)).GetComponent<RectTransform>();safe.SetParent(modernRoot,false);safe.anchorMin=new Vector2(.08f,.12f);safe.anchorMax=new Vector2(.92f,.86f);safe.offsetMin=safe.offsetMax=Vector2.zero;
  var logo=Rect("ROM logo",safe,new Vector2(0,1),new Vector2(0,1),new Vector2(0,-12),new Vector2(360,120)).gameObject.AddComponent<RawImage>();logo.texture=Resources.Load<Texture2D>("RiftMenu/Logo");logo.raycastTarget=false;if(logo.texture)logo.rectTransform.sizeDelta=new Vector2(360,360f*logo.texture.height/logo.texture.width);
  _title=Label("Destination",safe,new Vector2(0,.36f),new Vector2(0,.36f),Vector2.zero,new Vector2(1000,68),44,new Color(.96f,.94f,1));
  _subtitle=Label("Subtitle",safe,new Vector2(0,.36f),new Vector2(0,.36f),new Vector2(2,-74),new Vector2(1000,40),22,new Color(.75f,.71f,.84f));
  modernStage=Label("Actual transition stage",safe,new Vector2(0,0),new Vector2(0,0),new Vector2(0,94),new Vector2(900,34),20,new Color(.88f,.85f,.96f));
  _percent=Label("Measured progress",safe,new Vector2(1,0),new Vector2(1,0),new Vector2(-150,94),new Vector2(150,34),20,new Color(.86f,.79f,1));_percent.alignment=TextAnchor.MiddleRight;
  var track=Rect("Progress track",safe,new Vector2(0,0),new Vector2(0,0),new Vector2(0,62),new Vector2(900,4));track.anchorMax=new Vector2(1,0);track.sizeDelta=new Vector2(0,4);var trackImage=track.gameObject.AddComponent<Image>();trackImage.color=new Color(.6f,.57f,.72f,.25f);trackImage.raycastTarget=false;
  _bar=Rect("Measured progress bar",track,new Vector2(0,.5f),new Vector2(0,.5f),Vector2.zero,new Vector2(900,4));var fill=_bar.gameObject.AddComponent<Image>();fill.color=new Color(.64f,.39f,1);fill.raycastTarget=false;
  _tip=Label("Honest transition help",safe,new Vector2(0,0),new Vector2(0,0),new Vector2(0,0),new Vector2(1100,42),16,new Color(.7f,.67f,.78f));_tip.text="";
  var exitRect=Rect("Dismiss transition notice",safe,new Vector2(1,0),new Vector2(1,0),new Vector2(0,155),new Vector2(300,52));var surface=exitRect.gameObject.AddComponent<ROM.UI.Gameplay.RiftMapSurface>();surface.Fill=new Color(.07f,.05f,.12f,.9f);surface.Border=new Color(.6f,.42f,.86f);surface.Radius=7;surface.raycastTarget=true;dismissTransition=exitRect.gameObject.AddComponent<Button>();dismissTransition.targetGraphic=surface;Label("Dismiss label",exitRect,Vector2.zero,Vector2.zero,Vector2.zero,new Vector2(300,52),17,new Color(.94f,.91f,1)).alignment=TextAnchor.MiddleCenter;exitRect.GetComponentInChildren<Text>().text="CERRAR AVISO";dismissTransition.onClick.AddListener(()=>{_active=false;transitionTimedOut=false;});exitRect.gameObject.SetActive(false);
  _spinner=null;
 }
 static RectTransform Stretch(string name,Transform parent){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;return r;}
 static RectTransform Rect(string name,Transform parent,Vector2 anchor,Vector2 pivot,Vector2 position,Vector2 size){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=anchor;r.pivot=pivot;r.anchoredPosition=position;r.sizeDelta=size;return r;}
 static Text Label(string name,Transform parent,Vector2 anchor,Vector2 pivot,Vector2 pos,Vector2 size,int fontSize,Color color){var text=Rect(name,parent,anchor,pivot,pos,size).gameObject.AddComponent<Text>();text.font=RiftWidgets.Body?RiftWidgets.Body:Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=fontSize;text.color=color;text.raycastTarget=false;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;return text;}
 void UpdateModernStatus(bool loading){
  if(!modernRoot)return;
  if(modernStage)modernStage.text=transitionTimedOut?"La carga todavía no comenzó":loading?"Cargando escena":_sceneChangedAt>=0?"Escena lista":"Esperando el inicio de la carga";
  if(_percent)_percent.text=loading?Mathf.RoundToInt(_shownProgress*100f)+"%":_sceneChangedAt>=0?"100%":"";
  if(_bar){_bar.gameObject.SetActive(loading||_sceneChangedAt>=0);var track=(RectTransform)_bar.parent;var size=_bar.sizeDelta;size.x=track.rect.width*_shownProgress;_bar.sizeDelta=size;}
  if(_tip)_tip.text=transitionTimedOut?"Este aviso no cancela la conexión. Cerralo para revisar el estado de la partida.":"El progreso aparece cuando Unity inicia la carga de la escena.";
  if(dismissTransition){bool changed=dismissTransition.gameObject.activeSelf!=transitionTimedOut;dismissTransition.gameObject.SetActive(transitionTimedOut);if(changed&&transitionTimedOut&&EventSystem.current)EventSystem.current.SetSelectedGameObject(dismissTransition.gameObject);}
 }
}
}
