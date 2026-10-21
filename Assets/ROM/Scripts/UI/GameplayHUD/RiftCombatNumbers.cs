using UnityEngine;
using UnityEngine.UI;
using MultiFPS;
using MultiFPS.UI;
using MultiFPS.Gameplay;
using MultiFPS.Gameplay.RiftCombat;
namespace ROM.UI.Gameplay {
// Bounded pool: particles/text never determine damage. Only owner-targeted server results appear.
public sealed class RiftCombatNumbers:MonoBehaviour {
 const int Capacity=48;sealed class Entry {public Text Label;public RectTransform Rect;public Vector3 World;public float Start,Side;public bool Critical;public bool Active;}
 readonly Entry[] entries=new Entry[Capacity];Canvas canvas;RectTransform root;int cursor;
 public int ShownCount {get;private set;}public int LastAmount {get;private set;}public bool LastCritical {get;private set;}
 void Awake(){canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=135;root=(RectTransform)transform;var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);var theme=Resources.Load<RiftHUDTheme>("RiftHUDTheme");var font=theme&&theme.BodyFont?theme.BodyFont:Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");for(int i=0;i<Capacity;i++){var go=new GameObject("Damage number "+i,typeof(RectTransform),typeof(Text),typeof(Outline));go.transform.SetParent(transform,false);var rect=(RectTransform)go.transform;rect.sizeDelta=new Vector2(260,85);var text=go.GetComponent<Text>();text.font=font;text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;text.horizontalOverflow=HorizontalWrapMode.Overflow;text.verticalOverflow=VerticalWrapMode.Overflow;go.GetComponent<Outline>().effectColor=new Color(0,0,0,.9f);go.GetComponent<Outline>().effectDistance=new Vector2(1.5f,-1.5f);entries[i]=new Entry{Label=text,Rect=rect};go.SetActive(false);}}
 void OnEnable(){AbilityController.DamageNumber+=Show;}void OnDisable(){AbilityController.DamageNumber-=Show;}
 void Show(Vector3 world,int amount,bool critical){if(amount<=0)return;var entry=entries[cursor++%Capacity];entry.World=world;entry.Start=Time.unscaledTime;entry.Side=Random.Range(-34f,34f);entry.Critical=critical;entry.Active=true;entry.Label.text=amount.ToString("N0")+(critical?"!":"");entry.Label.fontSize=critical?52:32;entry.Label.fontStyle=critical?FontStyle.Bold:FontStyle.Normal;entry.Label.color=critical?new Color(1,.75f,.05f):new Color(1,.94f,.48f);entry.Label.gameObject.SetActive(true);ShownCount++;LastAmount=amount;LastCritical=critical;}
 void LateUpdate(){bool visible=ClientFrontend.OwnedCharacterInstance&&!ClientFrontend.Pause&&!ClientFrontend.Hub;canvas.enabled=visible;var camera=GameplayCamera._instance?GameplayCamera._instance.GetComponent<Camera>():Camera.main;foreach(var e in entries){if(!e.Active)continue;float t=(Time.unscaledTime-e.Start)/1.3f;if(t>=1){e.Active=false;e.Label.gameObject.SetActive(false);continue;}var point=camera?camera.WorldToScreenPoint(e.World):new Vector3(0,0,-1);e.Label.enabled=point.z>0;if(point.z<=0)continue;RectTransformUtility.ScreenPointToLocalPointInRectangle(root,point,null,out var local);e.Rect.anchoredPosition=local+new Vector2(e.Side*(.4f+t),t*90);e.Rect.localScale=Vector3.one*(e.Critical?1+Mathf.Max(0,1-t*5)*.3f:1);var color=e.Label.color;color.a=1-Mathf.Clamp01((t-.55f)/.45f);e.Label.color=color;}}
}}
