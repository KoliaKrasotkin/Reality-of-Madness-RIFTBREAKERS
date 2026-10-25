using MultiFPS;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;
using W=ROM.UI.Sprint01.RiftWidgets;
namespace ROM.UI.Sprint01
{
    public sealed partial class RiftHub
    {
        string rebindingId;int rebindingSlot;float captureAfter,captureUntil;ButtonControl modifierCandidate;Text bindingPrompt;
        void ControlsSettings(Transform panel,RiftVideoSettings video)
        {
            W.Label(panel,"Sensibilidad",0,0,290,30,20);var sensitivity=W.Slider(panel,"MouseSensitivity",0,34,650,.1f,5,draft.Sensitivity,v=>draft.Sensitivity=v);sensitivity.handleRect.sizeDelta=new Vector2(14,-20);
            W.Label(panel,"Campo de visión",810,0,290,30,20);var fov=W.Slider(panel,"Fov",810,34,650,60,110,draft.Fov,v=>draft.Fov=v);fov.handleRect.sizeDelta=new Vector2(14,-20);
            bindingPrompt=W.Label(panel,"Elegí una tecla para cambiarla. Se guardan automáticamente. Dos asignaciones por acción.",0,82,1700,30,18,W.Muted);
            var scroll=W.ScrollArea(panel,"Keybindings",0,120,1740,365,RiftControls.Entries.Count*55+250);
            int y=0;string group="";foreach(var entry in RiftControls.Entries){if(group!=entry.Group){group=entry.Group;W.Label(scroll,group.ToUpperInvariant(),0,y,1650,38,21,W.Gold);y+=40;}var id=entry.Id;W.Label(scroll,entry.Name,12,y,750,45,20);for(int slot=0;slot<2;slot++){int capturedSlot=slot;W.Button(scroll,RiftControls.Label(id,slot),780+slot*440,y,350,42,()=>BeginBinding(id,capturedSlot));W.Button(scroll,"×",1140+slot*440,y,55,42,()=>{RiftControls.Bind(id,capturedSlot,"");Navigate(HubPage.Settings);});}y+=55;}
            scroll.sizeDelta=new Vector2(1740,y+16);SaveSettings(panel,video);W.Button(panel,"TECLAS POR DEFECTO",480,510,600,64,()=>Dialog("Restaurar controles","Se restaurarán todas las teclas del esquema MMO.",()=>{RiftControls.Reset();Navigate(HubPage.Settings);}));
        }
        public void ShowControlBindings(){settingsTab="Controles";ShowGameSettings();}
        public void BeginBinding(string id,int slot){CancelBinding();rebindingId=id;rebindingSlot=slot;captureAfter=Time.unscaledTime+.15f;captureUntil=Time.unscaledTime+15;RiftControls.Rebinding=true;if(bindingPrompt)bindingPrompt.text="Presioná una tecla o botón para «"+RiftControls.Find(id).Name+"». Ctrl / Shift / Alt combinables. Escape cancela.";}
        void CancelBinding(){if(rebindingId==null)return;rebindingId=null;modifierCandidate=null;RiftControls.Rebinding=false;}
        void PollBinding()
        {
            if(rebindingId==null)return;if(Time.unscaledTime>captureUntil){CancelBinding();Navigate(HubPage.Settings);return;}if(Time.unscaledTime<captureAfter)return;
            var keyboard=Keyboard.current;if(keyboard!=null){if(keyboard.escapeKey.wasPressedThisFrame){RiftControls.EscapeConsumedFrame=Time.frameCount;CancelBinding();Navigate(HubPage.Settings);return;}foreach(var key in keyboard.allKeys){bool modifier=key.keyCode==Key.LeftShift||key.keyCode==Key.RightShift||key.keyCode==Key.LeftCtrl||key.keyCode==Key.RightCtrl||key.keyCode==Key.LeftAlt||key.keyCode==Key.RightAlt;if(key.wasPressedThisFrame){if(modifier){modifierCandidate=key;continue;}FinishBinding("<Keyboard>/"+key.name,RiftControls.CurrentModifiers);return;}}}
            if(modifierCandidate!=null&&modifierCandidate.wasReleasedThisFrame){FinishBinding("<Keyboard>/"+modifierCandidate.name,0);return;}
            var mouse=Mouse.current;if(mouse!=null){foreach(var button in new[]{mouse.leftButton,mouse.rightButton,mouse.middleButton,mouse.forwardButton,mouse.backButton})if(button.wasPressedThisFrame){FinishBinding("<Mouse>/"+button.name,RiftControls.CurrentModifiers);return;}if(Mathf.Abs(mouse.scroll.ReadValue().y)>0){FinishBinding(mouse.scroll.ReadValue().y>0?"<Mouse>/scroll/up":"<Mouse>/scroll/down",RiftControls.CurrentModifiers);return;}}
            var pad=Gamepad.current;if(pad!=null)foreach(var control in pad.allControls)if(control is ButtonControl button&&button.wasPressedThisFrame){FinishBinding("<Gamepad>"+button.path.Substring(pad.path.Length),0);return;}
        }
        void FinishBinding(string path,RiftControls.Modifiers mods)
        {
            string id=rebindingId;int slot=rebindingSlot;CancelBinding();string conflict=RiftControls.Conflict(id,path,mods);
            if(conflict!=null){Dialog("Tecla ocupada","Ya está asignada a «"+conflict+"». Liberá esa asignación o elegí otra tecla.",null,()=>Navigate(HubPage.Settings));return;}
            RiftControls.Bind(id,slot,path,mods);Navigate(HubPage.Settings);Notify("Control guardado: "+RiftControls.Find(id).Name+" → "+RiftControls.Label(id,slot));
        }
        void OnDisable(){CancelBinding();if(InGame&&modal)DismissModal();}
    }
}
