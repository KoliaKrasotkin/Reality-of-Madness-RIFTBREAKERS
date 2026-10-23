using UnityEngine;
using ROM.UI.Gameplay;
using W=ROM.UI.Sprint01.RiftWidgets;
namespace ROM.UI.Sprint01 {public sealed partial class RiftHub {
 void InterfaceOptions(Transform panel){
  W.Label(panel,"INTERFAZ DE COMBATE",0,0,1500,48,28);
  W.Label(panel,"Se aplica al instante y se guarda en esta PC. Podés volver al diseño normal cuando quieras.",0,62,1650,62,22,W.Muted);
  W.Label(panel,"Tamaño de frames de jugador y objetivo",0,155,780,36,23);
  var frameChoice=W.Dropdown(panel,"HUD frame size",0,205,780,new[]{"Normal (predeterminado)","Pequeños (80 %)"},RiftHUDPreferences.CompactFrames?1:0,v=>RiftHUDPreferences.SetCompactFrames(v==1));
  W.Label(panel,"Mantiene retratos animados, nombre, nivel, vida y furia.",0,270,780,72,20,W.Muted);
  W.Label(panel,"Detalle de la barra",900,155,780,36,23);
  var detailChoice=W.Dropdown(panel,"HUD detail mode",900,205,780,new[]{"Normal (predeterminado)","Minimalista: iconos y atajos"},RiftHUDPreferences.MinimalHUD?1:0,v=>RiftHUDPreferences.SetMinimalHUD(v==1));
  W.Label(panel,"Reduce las etiquetas de acceso. Conserva iconos, teclas, recargas y disponibilidad. La descripción completa aparece al pasar el mouse.",900,270,780,128,20,W.Muted);
  W.Button(panel,"RESTABLECER INTERFAZ",0,490,540,64,()=>{RiftHUDPreferences.RestoreDefaults();frameChoice.SetValueWithoutNotify(0);frameChoice.RefreshShownValue();detailChoice.SetValueWithoutNotify(0);detailChoice.RefreshShownValue();});
 }
}}
