using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using MultiFPS;
using MultiFPS.UI;
using MultiFPS.Gameplay.RiftCombat;
using ROM.Progression;
using ROM.UI.Gameplay;
namespace ROM.UI.Sprint01 {
public sealed partial class RiftHub {
 void ModernPause(){
  const float width=400,height=540;var panel=RiftSkillStyle.Panel(content,"Modern pause landing",(1848-width)*.5f,120,width,height,.88f);
  RiftWidgets.Label(panel,"EL RIFT",24,20,width-48,44,29,RiftSkillStyle.White).alignment=TextAnchor.MiddleCenter;
  string[] labels={"OPCIONES","AYUDA","NOVEDADES","TIENDA","SALIR DEL RIFT","SALIR DEL JUEGO"};
  System.Action[] callbacks={ShowGameSettings,()=>Navigate(HubPage.Help),()=>Navigate(HubPage.News),()=>{shopTab="Marketplace";Navigate(HubPage.Marketplace);},()=>Dialog("Salir del Rift","Vas a desconectarte y volver al menú principal.",()=>LeaveRequested?.Invoke()),()=>Dialog("Salir del juego",Application.isEditor?"En el Editor esta acción no cierra Unity. En el juego cerrará la aplicación.":"¿Querés cerrar Riftbreakers?",()=>{if(Application.isEditor)Notify("Unity permanece abierto; cierre de juego confirmado.");else if(Application.platform==RuntimePlatform.WebGLPlayer)Notify("Cerrá la pestaña del juego para salir.");else Application.Quit();})};
  for(int i=0;i<labels.Length;i++){int index=i;var button=RiftSkillStyle.Button(panel,labels[i],30,88+i*61,340,49,()=>callbacks[index]());button.name="Pause "+labels[i];button.GetComponentInChildren<Text>().fontSize=19;}
  RiftWidgets.Label(panel,"Esc · Volver al juego",24,478,width-48,26,15,RiftSkillStyle.Muted).alignment=TextAnchor.MiddleCenter;
  RiftWidgets.Label(panel,"Versión del proyecto "+Application.version,24,507,width-48,22,12,RiftSkillStyle.Muted).alignment=TextAnchor.MiddleCenter;
  if(EventSystem.current)EventSystem.current.SetSelectedGameObject(panel.GetComponentInChildren<Button>().gameObject);
 }
 void PauseInformation(bool news){
  const float width=740;var panel=RiftSkillStyle.Panel(content,news?"Rift release notes":"Rift gameplay help",(1848-width)*.5f,70,width,540,.94f);
  RiftWidgets.Label(panel,news?"NOVEDADES":"AYUDA",30,20,width-60,48,29,RiftSkillStyle.White);
  RiftWidgets.Label(panel,"Versión del proyecto "+Application.version,30,73,width-60,26,16,RiftSkillStyle.Muted);
  string text;
  if(news){var caps=RiftProgressionCaps.Load;text="EXPLORACIÓN Y PROGRESIÓN\nTu progreso está limitado al nivel "+caps.ActivePlayerCap+" en este playtest. El mundo incluye zonas hasta nivel "+caps.WorldCap+", accesibles sin barrera de nivel.\n\nCOMBATE Y BOTÍN\nWarrior usa furia, habilidades con recarga y daño del arma equipada. Los enemigos pueden soltar oro común y hachas con nivel y rareza.\n\nINTERFAZ\nTooltips junto al cursor, frames compactos y modo minimalista configurables en Opciones > Interfaz.\n\nLa Tienda de apariencias es una demostración: no realiza compras reales.";}
  else{text="EXPLORAR\nMoverte: WASD o ambos botones del mouse. Saltar: ["+RiftControls.ActionLabel("Jump")+"]. Correr: ["+RiftControls.ActionLabel("Sprint")+"].\n\nCOMBATE E INTERACCIÓN\nSeleccioná un objetivo con clic. Usá los atajos de la barra para atacar; el tooltip muestra coste y recarga. Hablá con un NPC cercano usando ["+RiftControls.ActionLabel("Interact")+"].\n\nTUS PANELES\nInventario ["+RiftControls.ActionLabel("Inventory")+"] · Habilidades ["+RiftControls.ActionLabel("Abilities")+"] · Talentos ["+RiftControls.ActionLabel("Talents")+"]\nDiario ["+RiftControls.ActionLabel("Journal")+"] · Mapa ["+RiftControls.ActionLabel("WorldMap")+"]\n\nPodés cambiar los atajos en Opciones > Controles. Abrir este menú no detiene la partida.";}
  var body=RiftWidgets.Label(panel,text,30,120,width-60,330,19,RiftSkillStyle.White);body.alignment=TextAnchor.UpperLeft;body.horizontalOverflow=HorizontalWrapMode.Wrap;body.verticalOverflow=VerticalWrapMode.Overflow;
  RiftSkillStyle.Button(panel,"VOLVER",30,466,width-60,48,()=>Navigate(HubPage.Home));
 }
 RectTransform gameplayOverlay;readonly System.Collections.Generic.List<RectTransform> overlayParts=new System.Collections.Generic.List<RectTransform>();
 void EnsureGameplayOverlay(){if(!InGame||!content)return;if(!gameplayOverlay)gameplayOverlay=RiftWidgets.Rect(Design,"Gameplay overlay viewport",0,0,1920,1080);content.SetParent(gameplayOverlay,false);foreach(var header in new[]{fullGameplayHeader,compactGameplayHeader,modernInventoryHeader})if(header)header.SetParent(gameplayOverlay,false);}
 void FitGameplayOverlay(){if(!gameplayOverlay)return;if(Page==HubPage.Home||Page==HubPage.Help||Page==HubPage.News){gameplayOverlay.localScale=Vector3.one;gameplayOverlay.anchoredPosition=Vector2.zero;return;}overlayParts.Clear();foreach(Transform child in content)if(child is RectTransform rect&&rect.gameObject.activeSelf)overlayParts.Add(rect);foreach(var header in new[]{fullGameplayHeader,compactGameplayHeader,modernInventoryHeader})if(header&&header.gameObject.activeSelf)overlayParts.Add(header);RiftHUDPanelDock.FitContents(gameplayOverlay,overlayParts);}
 void ShowEquipmentActions(){
  var player=ClientFrontend.ClientPlayerInstance;var combat=player?player.GetComponent<AbilityController>():null;var journal=player?player.GetComponent<RiftJournalPlayer>():null;
  if(!combat||!combat.Prototype||!journal){Notify("El equipo está disponible cuando el Warrior está en partida.");return;}
  bool sword=combat.SwordAndShield;string current=sword?"Espada y escudo":"Hacha a dos manos";string next=sword?"Hacha a dos manos":"Espada y escudo";
  string info="Actual: "+current+"\nNivel "+combat.Level+" · XP "+combat.Progression.InLevel(combat.Experience)+" / "+combat.Progression.Required(combat.Level)+"\nCambiar a: "+next+"\nEl servidor valida el cambio de arma.";
  Dialog("Equipo del Warrior",info,()=>{if(journal&&combat)journal.CmdWarriorWeapon(!sword);});
 }
}
}
