using Mirror;
using MultiFPS;
using MultiFPS.Gameplay;
using MultiFPS.UI;
using UnityEngine;
using UnityEngine.UI;
using W=ROM.UI.Sprint01.RiftWidgets;
namespace ROM.UI.Sprint01 {
public sealed class RiftClassHUD : MonoBehaviour {
    static RiftClassHUD instance;Text title,first,second,help;RectTransform panel;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install(){if(instance)return;var go=new GameObject("ROM - Habilidades de clase",typeof(Canvas),typeof(CanvasScaler));DontDestroyOnLoad(go);go.AddComponent<RiftClassHUD>();}
    void Awake(){if(instance&&instance!=this){Destroy(gameObject);return;}instance=this;var canvas=GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=120;var scaler=GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;}
    void Start(){
        if(!W.Body)W.Body=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        panel=W.Panel(transform,"ClassAbilities",0,0,610,155);panel.anchorMin=panel.anchorMax=new Vector2(.5f,0);panel.pivot=new Vector2(.5f,0);panel.anchoredPosition=new Vector2(0,20);
        title=W.Label(panel,"CLASE",15,4,580,32,22,W.Gold);first=W.Label(panel,"F",15,44,580,30,22);second=W.Label(panel,"Z",15,79,580,30,22);help=W.Label(panel,"",15,119,580,27,16,W.Muted);
    }
    void Update(){
        if(!panel)return;var p=ClientFrontend.ClientPlayerInstance;var def=p?p.ClassDefinition:null;bool visible=def&&p.MyCharacter&&p.MyCharacter.Health.CurrentHealth>0&&!ClientFrontend.Pause&&!ClientFrontend.Hub;
        panel.gameObject.SetActive(visible);if(!visible)return;
        double now=NetworkTime.time;string state=p.HiddenUntil>now?" · OCULTO":p.EmpoweredUntil>now?" · POTENCIADO":"";
        title.text=def.Name.ToUpperInvariant()+state;title.color=def.Color;
        bool available=GameManager.Gamemode.CanUseClassAbilities(p.MyCharacter.transform.position);
        first.text="[F] "+def.FirstAbility+" · "+(available?Ready(p.FirstReadyAt,now):"REFUGIO");second.text="[Z] "+def.SecondAbility+" · "+(available?Ready(p.SecondReadyAt,now):"REFUGIO");
        help.text=available?"Apuntá a un objetivo · F / Z para usar · Esc → Clase":"Acercate al muñeco para entrenar · Esc → Clase";
    }
    void OnDestroy(){if(instance==this)instance=null;}
    static string Ready(double time,double now)=>time>now?Mathf.CeilToInt((float)(time-now))+" s":"LISTO";
}
}
