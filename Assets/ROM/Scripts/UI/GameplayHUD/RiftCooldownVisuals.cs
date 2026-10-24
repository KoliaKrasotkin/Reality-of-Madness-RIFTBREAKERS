using UnityEngine;
using UnityEngine.UI;
namespace ROM.UI.Gameplay {
// Presentation only: reads replicated timestamps, never starts or changes cooldowns.
public sealed class RiftCooldownVisuals:MonoBehaviour {
 RiftGameplayHUD hud;Material grey;Material[] originals;
 void Awake(){hud=GetComponent<RiftGameplayHUD>();originals=new Material[hud.Icons.Length];var shader=Resources.Load<Shader>("RiftAbilityDesaturate");if(shader)grey=new Material(shader){name="Runtime cooldown grayscale"};for(int i=0;i<hud.Icons.Length;i++){originals[i]=hud.Icons[i].material;var text=hud.Timers[i];text.color=Color.white;text.fontSize=28;text.fontStyle=FontStyle.Bold;text.raycastTarget=false;var edge=text.GetComponent<Outline>();if(!edge)edge=text.gameObject.AddComponent<Outline>();edge.effectColor=new Color(0,0,0,.98f);edge.effectDistance=new Vector2(2,-2);text.transform.SetAsLastSibling();hud.Cooldowns[i].color=new Color(.015f,.02f,.035f,.68f);hud.Cooldowns[i].type=Image.Type.Filled;hud.Cooldowns[i].fillMethod=Image.FillMethod.Radial360;hud.Cooldowns[i].fillOrigin=2;hud.Cooldowns[i].fillClockwise=true;}}
 public void Apply(int index,bool unlocked,bool enough,float remaining,float duration){bool cooling=unlocked&&remaining>0;var icon=hud.Icons[index];icon.material=cooling&&grey?grey:originals[index];icon.color=cooling?new Color(.8f,.8f,.8f,1):enough?Color.white:new Color(.72f,.65f,.65f,1);hud.Cooldowns[index].fillAmount=cooling?Mathf.Clamp01(remaining/Mathf.Max(.1f,duration)):0;hud.Timers[index].text=cooling?(remaining>=1?Mathf.CeilToInt(remaining).ToString():remaining.ToString("0.0")):"";}
 void OnDestroy(){if(hud&&originals!=null)for(int i=0;i<originals.Length;i++)if(hud.Icons[i])hud.Icons[i].material=originals[i];if(grey)Destroy(grey);}
}}
