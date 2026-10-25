using MultiFPS.Gameplay;
using MultiFPS.Gameplay.Gamemodes;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
namespace MultiFPS.UI.HUD {
// Presentation only: the server retains ownership of the actual respawn timer.
public class UIRespawnCooldown:CharacterHud {
 [SerializeField]Image _bar;[SerializeField]Text _msg;Coroutine countdown;bool built;
 public float Remaining{get;private set;}
 protected override void Awake(){base.Awake();gameObject.SetActive(false);}
 protected override void AssignOwnedCharacterForUI(CharacterInstance actor){HideUI();actor.Health.Client_OnHealthDepleted+=OnOwnedCharacterDied;}
 protected override void DeassignCurrentlyOwnedCharacterFromUI(CharacterInstance actor){actor.Health.Client_OnHealthDepleted-=OnOwnedCharacterDied;HideUI();}
 void OnOwnedCharacterDied(CharacterPart part,byte attacker){if(GameManager.Gamemode.LetPlayersSpawnOnTheirOwn)StartUI(RoomSetup.Properties.P_RespawnCooldown);}
 RectTransform Rect(Transform parent,string name,Vector2 position,Vector2 size){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.anchoredPosition=position;r.sizeDelta=size;return r;}
 Image Image(Transform parent,string name,Vector2 position,Vector2 size,Color color){var image=Rect(parent,name,position,size).gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;return image;}
 Text Label(string name,string value,Font font,int size,Vector2 position,Vector2 dimensions){var t=Rect(transform,name,position,dimensions).gameObject.AddComponent<Text>();t.font=font;t.fontSize=size;t.text=value;t.alignment=TextAnchor.MiddleCenter;t.color=new Color(.95f,.93f,1);t.raycastTarget=false;t.supportRichText=false;return t;}
 void Build(){if(built)return;built=true;Font font=_msg&&_msg.font?_msg.font:Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");foreach(Transform child in transform)child.gameObject.SetActive(false);var root=(RectTransform)transform;var canvas=GetComponentInParent<Canvas>();if(canvas)root.SetParent(canvas.transform,false);root.anchorMin=root.anchorMax=root.pivot=Vector2.one*.5f;root.anchoredPosition=Vector2.zero;root.sizeDelta=new Vector2(420,144);root.localScale=Vector3.one;
  var surface=GetComponent<Image>();if(!surface)surface=gameObject.AddComponent<Image>();surface.color=new Color(.025f,.028f,.045f,.94f);surface.raycastTarget=false;var border=new Color(.54f,.39f,.75f,.9f);Image(transform,"Respawn top border",new Vector2(0,71.5f),new Vector2(420,1),border);Image(transform,"Respawn bottom border",new Vector2(0,-71.5f),new Vector2(420,1),border);Image(transform,"Respawn left border",new Vector2(-209.5f,0),new Vector2(1,144),border);Image(transform,"Respawn right border",new Vector2(209.5f,0),new Vector2(1,144),border);
  Label("Respawn title","Has caído",font,26,new Vector2(0,34),new Vector2(380,38)).fontStyle=FontStyle.Bold;_msg=Label("Respawn countdown","",font,19,new Vector2(0,-4),new Vector2(380,32));var track=Image(transform,"Respawn indicator track",new Vector2(0,-43),new Vector2(336,6),new Color(.1f,.085f,.15f));_bar=Image(track.transform,"Respawn indicator",Vector2.zero,new Vector2(336,6),new Color(.67f,.4f,.92f));_bar.rectTransform.anchorMin=Vector2.zero;_bar.rectTransform.anchorMax=Vector2.one;_bar.rectTransform.offsetMin=_bar.rectTransform.offsetMax=Vector2.zero;var group=GetComponent<CanvasGroup>();if(!group)group=gameObject.AddComponent<CanvasGroup>();group.alpha=1;group.blocksRaycasts=group.interactable=false;
 }
 void Draw(float duration){_msg.text="Reaparecés en "+Mathf.CeilToInt(Remaining)+" s";float ratio=duration>0?Mathf.Clamp01(Remaining/duration):0;_bar.fillAmount=ratio;_bar.rectTransform.anchorMax=new Vector2(ratio,1);_bar.gameObject.SetActive(ratio>0);}
 public void StartUI(float cooldown){HideUI();if(float.IsNaN(cooldown)||float.IsInfinity(cooldown)||cooldown<=0)return;Build();Remaining=cooldown;gameObject.SetActive(true);Draw(cooldown);countdown=StartCoroutine(Count(cooldown));}
 IEnumerator Count(float duration){float started=Time.time;while((Remaining=Mathf.Max(0,duration-(Time.time-started)))>0){Draw(duration);yield return null;}Remaining=0;Draw(duration);countdown=null;gameObject.SetActive(false);}
 public void HideUI(){if(countdown!=null){StopCoroutine(countdown);countdown=null;}Remaining=0;if(_bar)_bar.gameObject.SetActive(false);gameObject.SetActive(false);}
}
}
