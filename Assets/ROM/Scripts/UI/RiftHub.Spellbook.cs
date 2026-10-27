using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MultiFPS;
using MultiFPS.Gameplay;
using MultiFPS.Gameplay.RiftCombat;
using ROM.UI.Gameplay;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace ROM.UI.Sprint01 {
// Local visual primitives for skills/talents only. Inventory theme is intentionally untouched.
public static class RiftSkillStyle {
 public static readonly Color Violet=new Color(.64f,.42f,1), White=new Color(.95f,.94f,.98f), Muted=new Color(.69f,.69f,.77f);
 public static RectTransform Panel(Transform p,string name,float x,float y,float w,float h,float alpha=.72f){var r=RiftWidgets.Rect(p,name,x,y,w,h);var g=r.gameObject.AddComponent<RiftMapSurface>();g.Fill=new Color(.033f,.036f,.055f,alpha);g.Border=new Color(.35f,.35f,.44f,.8f);g.Radius=8;g.raycastTarget=true;return r;}
 public static Button Button(Transform p,string text,float x,float y,float w,float h,Action action,bool active=false){var r=Panel(p,text,x,y,w,h,.6f);var g=r.GetComponent<RiftMapSurface>();g.Glow=active?4:0;g.Border=active?Violet:new Color(.35f,.35f,.44f,.8f);g.Fill=active?new Color(.18f,.1f,.29f,.8f):new Color(.04f,.044f,.065f,.7f);var b=r.gameObject.AddComponent<Button>();b.targetGraphic=g;var c=b.colors;c.highlightedColor=new Color(1.2f,1.12f,1.4f);c.selectedColor=c.highlightedColor;b.colors=c;b.onClick.AddListener(()=>r.GetComponent<RiftSkillAction>().Dispatch(action));r.gameObject.AddComponent<RiftSkillAction>();var label=RiftWidgets.Label(r,text,10,0,w-20,h,18,White);label.alignment=TextAnchor.MiddleCenter;return b;}
}
public sealed class RiftSkillAction:MonoBehaviour {public void Dispatch(Action action){StartCoroutine(Delayed(action));}IEnumerator Delayed(Action action){yield return null;action?.Invoke();}}
public sealed class RiftSpellFocus:MonoBehaviour,ISelectHandler {public int Ability;public RiftSpellbookView Book;public void OnSelect(BaseEventData e){Book.SelectAbility(Ability);Book.Reveal(transform);}}
public sealed class RiftSpellbookView:MonoBehaviour {
 AbilityController combat;RiftHub hub;int selected=1,selectedSlot,filter;float next;Text detail,status,learned,detailTitle,detailState;Image detailIcon;Button assign;RectTransform list;readonly List<int> rowIds=new List<int>();readonly List<Text> names=new List<Text>(),facts=new List<Text>();readonly List<Image> rowImages=new List<Image>(),icons=new List<Image>();readonly List<RiftMapSurface> surfaces=new List<RiftMapSurface>();readonly List<Text> keys=new List<Text>();readonly List<Button> filters=new List<Button>();
 public int SelectedAbility=>selected;public int SelectedSlot=>selectedSlot;
 public void Build(PlayerInstance player,AbilityController abilities,RiftHub owner){
 combat=abilities;hub=owner;var body=(RectTransform)transform;
 RiftWidgets.Label(body,"LIBRO DE HABILIDADES",0,0,800,44,32,RiftSkillStyle.White);RiftWidgets.Label(body,player.MyCharacter.Health.CharacterName+" · Warrior",0,48,720,32,21,RiftSkillStyle.Muted);learned=RiftWidgets.Label(body,"",780,48,288,32,18,RiftSkillStyle.Muted);learned.alignment=TextAnchor.MiddleRight;
 var labels=new[]{"TODAS","ATAQUE","APOYO"};for(int i=0;i<3;i++){int mode=i;filters.Add(RiftSkillStyle.Button(body,labels[i],i*132,96,124,34,()=>SetFilter(mode),i==0));}
 list=RiftWidgets.ScrollArea(body,"Spell list",0,142,388,438,7*86);BuildRows();
 var card=RiftSkillStyle.Panel(body,"Selected ability details",408,96,660,484,.55f);detailIcon=RiftWidgets.Image(card,"Selected ability icon",258,22,144,144,Color.white);detailIcon.preserveAspect=true;
 detailTitle=RiftWidgets.Label(card,"",24,180,612,50,29,RiftSkillStyle.White);detailTitle.alignment=TextAnchor.MiddleCenter;detailState=RiftWidgets.Label(card,"",24,234,612,30,18,RiftSkillStyle.Muted);detailState.alignment=TextAnchor.MiddleCenter;RiftWidgets.Image(card,"Divider",24,280,612,1,new Color(.4f,.38f,.5f,.55f));var factsArea=RiftWidgets.ScrollArea(card,"Selected facts",24,302,612,158,280);detail=RiftWidgets.Label(factsArea,"",0,0,596,280,20,RiftSkillStyle.White);detail.alignment=TextAnchor.UpperLeft;
 var bar=RiftSkillStyle.Panel(body,"Editable action bar",0,600,1068,150,.6f);RiftWidgets.Label(bar,"BARRA DE ACCIÓN",18,6,360,30,20,RiftSkillStyle.White);RiftWidgets.Label(bar,"Arrastrá para asignar o intercambiar",410,6,638,30,16,RiftSkillStyle.Muted).alignment=TextAnchor.MiddleRight;
 for(int i=0;i<7;i++){int slot=i;var button=RiftSkillStyle.Button(bar,"",18+i*149,42,136,96,()=>AssignSlot(slot));button.name="Book slot "+i;button.gameObject.AddComponent<RiftHotbarSlot>().Slot=i;icons.Add(RiftWidgets.Image(button.transform,"Ability icon",30,7,76,76,Color.white));icons[i].preserveAspect=true;var key=RiftWidgets.Label(button.transform,"",7,4,50,26,17,RiftSkillStyle.White);key.alignment=TextAnchor.MiddleCenter;keys.Add(key);}
 assign=RiftSkillStyle.Button(body,"ASIGNAR AL SLOT",0,774,310,48,()=>AssignSlot(selectedSlot),true);var remove=RiftSkillStyle.Button(body,"QUITAR SLOT",326,774,238,48,()=>RiftHotbar.Clear(selectedSlot));remove.gameObject.AddComponent<RiftSpellRemoveTarget>();RiftSkillStyle.Button(body,"TALENTOS",580,774,232,48,()=>hub.ShowTalents());RiftSkillStyle.Button(body,"CERRAR",828,774,240,48,()=>hub.ResumeRequested?.Invoke());
 status=RiftWidgets.Label(body,"",0,840,1068,28,17,RiftSkillStyle.Muted);RiftHotbar.Changed+=Refresh;Refresh();
 }
 void SetFilter(int mode){filter=mode;RiftAbilityDrag.Cancel();BuildRows();Refresh();}
 void BuildRows(){foreach(Transform child in list.Cast<Transform>().ToArray())Destroy(child.gameObject);rowIds.Clear();names.Clear();facts.Clear();rowImages.Clear();surfaces.Clear();int count=0;
 for(int i=1;i<=7;i++){int id=i;var a=combat.Definition(id);if(!a||filter==1&&a.Damage<=0||filter==2&&a.Damage>0)continue;var card=RiftSkillStyle.Panel(list,"Spell "+id,0,count*86,372,78,.6f);var surface=card.GetComponent<RiftMapSurface>();surfaces.Add(surface);var button=card.gameObject.AddComponent<Button>();button.targetGraphic=surface;var colors=button.colors;colors.highlightedColor=new Color(1.12f,1.08f,1.3f);colors.selectedColor=colors.highlightedColor;button.colors=colors;button.onClick.AddListener(()=>SelectAbility(id));
 rowImages.Add(RiftWidgets.Image(card,"Spell icon",7,7,64,64,Color.white,a.Icon));rowImages.Last().preserveAspect=true;names.Add(RiftWidgets.Label(card,"",84,4,274,40,21,RiftSkillStyle.White));facts.Add(RiftWidgets.Label(card,"",84,45,274,26,16,RiftSkillStyle.Muted));rowIds.Add(id);card.gameObject.AddComponent<RiftTooltipTrigger>().Ability=id;var focus=card.gameObject.AddComponent<RiftSpellFocus>();focus.Book=this;focus.Ability=id;var drag=card.gameObject.AddComponent<RiftSpellDrag>();drag.Ability=id;drag.Icon=a.Icon;count++;}
 list.sizeDelta=new Vector2(list.sizeDelta.x,Mathf.Max(438,count*86));list.anchoredPosition=Vector2.zero;
 }
 public void Reveal(Transform row){var rect=(RectTransform)row;float y=-rect.anchoredPosition.y;float current=list.anchoredPosition.y;if(y<current)list.anchoredPosition=new Vector2(0,y);else if(y+78>current+438)list.anchoredPosition=new Vector2(0,y+78-438);}
 public void SelectAbility(int ability){if(combat.Definition(ability)){selected=ability;Refresh();}}
 public bool AssignSlot(int slot){selectedSlot=slot;bool result=RiftHotbar.Assign(selected,slot);Refresh();return result;}
 void Update(){if(Time.unscaledTime>=next){next=Time.unscaledTime+.2f;Refresh();}if(RiftControls.TextEntryFocused||RiftControls.Rebinding||RiftHotbar.Dragging)return;for(int i=0;i<7;i++)if(RiftControls.Pressed("Ability"+i)){AssignSlot(i);break;}}
 void Refresh(){if(!combat||!detail)return;learned.text=Enumerable.Range(1,7).Count(combat.IsAbilityLearned)+" / 7 disponibles";
 for(int i=0;i<filters.Count;i++){var surface=filters[i].GetComponent<RiftMapSurface>();surface.Border=i==filter?RiftSkillStyle.Violet:new Color(.3f,.3f,.4f);surface.Glow=i==filter?3:0;surface.SetVerticesDirty();}
 for(int i=0;i<names.Count;i++){int id=rowIds[i];var a=combat.Definition(id);bool available=combat.IsAbilityLearned(id);names[i].text=a.DisplayName;names[i].color=available?RiftSkillStyle.White:RiftSkillStyle.Muted;facts[i].text=available?combat.AbilityCost(id,a).ToString("0")+" Furia · "+combat.AbilityCooldown(id,a).ToString("0.#")+" s":combat.AbilityUnavailableReason(id);rowImages[i].color=available?Color.white:new Color(.35f,.35f,.35f);surfaces[i].Fill=id==selected?new Color(.18f,.1f,.29f,.85f):new Color(.04f,.044f,.065f,.6f);surfaces[i].Border=id==selected?RiftSkillStyle.Violet:new Color(.3f,.3f,.4f,.7f);surfaces[i].Glow=id==selected?3:0;surfaces[i].SetVerticesDirty();}
 var definition=combat.Definition(selected);bool valid=combat.IsAbilityLearned(selected);detailIcon.sprite=definition.Icon;detailTitle.text=definition.DisplayName;detailState.text=valid?"Habilidad de Warrior · Lista para asignar":combat.AbilityUnavailableReason(selected);detail.text=(string.IsNullOrWhiteSpace(definition.Description)?"Habilidad de Warrior.":definition.Description)+"\n\n"+combat.AbilityDamageSummary(definition)+" · Área/alcance: "+combat.AbilityRadius(selected,definition).ToString("0.#")+" m\nFuria: "+combat.AbilityCost(selected,definition).ToString("0")+" · Recarga: "+combat.AbilityCooldown(selected,definition).ToString("0.#")+" s\nEjecución: "+definition.CastDuration.ToString("0.#")+" s";assign.interactable=valid;
 for(int i=0;i<7;i++){int id=RiftHotbar.Resolve(i);var a=id>0?combat.Definition(id):null;icons[i].enabled=a;icons[i].sprite=a?a.Icon:null;keys[i].text=RiftControls.ActionLabel("Ability"+i);var surface=icons[i].transform.parent.GetComponent<RiftMapSurface>();surface.Border=i==selectedSlot?RiftSkillStyle.Violet:new Color(.35f,.35f,.44f);surface.Glow=i==selectedSlot?4:0;surface.SetVerticesDirty();}
 status.text="Elegí técnica + tecla del slot · Clic derecho: quitar · Escape: cancelar arrastre o cerrar";
 }
 void OnDisable(){RiftHotbar.Changed-=Refresh;RiftAbilityDrag.Cancel();RiftAbilityTooltip.HideAll();}
}
}
