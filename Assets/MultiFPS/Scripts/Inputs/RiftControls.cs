using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
namespace MultiFPS
{
    // One binding registry for gameplay, HUD and settings. Uses the existing Input System.
    public static class RiftControls
    {
        public const string Preference="ROM.Controls.v1";
        [Flags] public enum Modifiers { None=0,Shift=1,Ctrl=2,Alt=4 }
        [Serializable] public sealed class Binding { public string Path="";public Modifiers Mods;public Binding(){}public Binding(string path,Modifiers mods=0){Path=path;Mods=mods;} }
        [Serializable] sealed class Saved { public List<Row> Rows=new List<Row>(); }
        [Serializable] sealed class Row {public string Id;public Binding Primary,Secondary;}
        public sealed class Entry
        {
            public string Id,Name,Group;public bool Continuous;public Binding[] Bindings;internal InputAction[] Actions;
            internal Binding[] Defaults;internal readonly string[] Labels=new string[2];
        }
        static readonly List<Entry> entries=new List<Entry>();static readonly Dictionary<string,Entry> byId=new Dictionary<string,Entry>();
        public static IReadOnlyList<Entry> Entries{get{Ensure();return entries;}}
        public static bool Rebinding {get;set;}
        static string capturedPath;
        public static void SuppressCapturedInput(string path){capturedPath=path;TextInputConsumedFrame=InterfaceConsumedFrame=Time.frameCount;}
        static bool CaptureHeld{get{if(string.IsNullOrEmpty(capturedPath))return false;using(var controls=InputSystem.FindControls(capturedPath)){foreach(var control in controls)if(control is UnityEngine.InputSystem.Controls.ButtonControl button&&button.isPressed)return true;}capturedPath=null;return false;}}
        public static int EscapeConsumedFrame=-1;public static int TextInputConsumedFrame=-1;
        public static int InterfaceConsumedFrame=-1;
        public static bool TextEntryFocused {get {if(TextInputConsumedFrame==Time.frameCount||ChatBehaviour._instance&&ChatBehaviour._instance.ChatWriting)return true;var events=UnityEngine.EventSystems.EventSystem.current;var selected=events?events.currentSelectedGameObject:null;return selected&&selected.activeInHierarchy&&(selected.GetComponent<UnityEngine.UI.InputField>()||selected.GetComponent("TMP_InputField"));}}
        public static event Action Changed;
        static Binding B(string path,Modifiers mods=0)=>new Binding(path,mods);
        static void Add(string id,string name,string group,string path,string second="",bool continuous=false,Modifiers mods=0){var e=new Entry{Id=id,Name=name,Group=group,Continuous=continuous,Bindings=new[]{B(path,mods),B(second)},Defaults=new[]{B(path,mods),B(second)}};entries.Add(e);byId.Add(id,e);}
        static void Ensure()
        {
            if(entries.Count>0)return;
            Add("Forward","Avanzar","Movimiento","<Keyboard>/w","<Keyboard>/upArrow",true);Add("Backward","Retroceder","Movimiento","<Keyboard>/s","<Keyboard>/downArrow",true);
            Add("TurnLeft","Girar a la izquierda","Movimiento","<Keyboard>/a","<Keyboard>/leftArrow",true);Add("TurnRight","Girar a la derecha","Movimiento","<Keyboard>/d","<Keyboard>/rightArrow",true);
            Add("StrafeLeft","Strafe izquierdo","Movimiento","<Keyboard>/q","",true);Add("StrafeRight","Strafe derecho","Movimiento","<Keyboard>/e","",true);
            Add("Roll","Rodar hacia adelante","Movimiento","<Keyboard>/leftCtrl","<Keyboard>/rightCtrl",true);
            Add("Jump","Saltar","Movimiento","<Keyboard>/space","",true);Add("Sprint","Correr rápido","Movimiento","<Keyboard>/leftShift","",true);Add("Autorun","Activar/cancelar avance automático","Movimiento","<Keyboard>/numLock");Add("Walk","Alternar caminar/correr","Movimiento","<Keyboard>/numpadDivide");Add("Crouch","Agacharse (personajes compatibles)","Movimiento","<Keyboard>/c","",true);
            Add("Orbit","Orbitar cámara","Cámara","<Mouse>/leftButton","",true);Add("Steer","Girar personaje con cámara","Cámara","<Mouse>/rightButton","",true);Add("ZoomIn","Acercar cámara","Cámara","<Mouse>/scroll/up");Add("ZoomOut","Alejar cámara","Cámara","<Mouse>/scroll/down");
            Add("Select","Seleccionar por clic","Objetivos","<Mouse>/leftButton");Add("NextTarget","Siguiente enemigo","Objetivos","<Keyboard>/tab");Add("PreviousTarget","Enemigo anterior","Objetivos","<Keyboard>/tab","",false,Modifiers.Shift);Add("ClearTarget","Limpiar objetivo","Objetivos","<Keyboard>/escape");Add("SelfTarget","Seleccionarme","Objetivos","<Keyboard>/f6");
            for(int i=0;i<7;i++)Add("Ability"+i,"Barra de habilidades · ranura "+(i+1),"Combate","<Keyboard>/"+(i+1));
            Add("AutoAttack","Activar/cancelar autoataque","Combate","<Keyboard>/t");
            Add("Interact","Interactuar / hablar / portal","Mundo","<Keyboard>/f");Add("Potion","Poción de vida","Mundo","<Keyboard>/h");Add("PotionMajor","Poción de vida mayor","Mundo","<Keyboard>/h","",false,Modifiers.Shift);Add("WorldMap","Mapa completo","Interfaz","<Keyboard>/m");
            Add("WeaponFire","Disparar arma convencional","Armas","<Mouse>/middleButton","",true);
            Add("Reload","Recargar arma","Armas","<Keyboard>/r");Add("Aim","Apuntar arma","Armas","<Keyboard>/z","",true);Add("Melee","Golpe con arma","Armas","<Keyboard>/v");Add("Drop","Soltar arma","Armas","<Keyboard>/y");Add("PreviousWeapon","Arma anterior","Armas","<Keyboard>/x");for(int i=0;i<5;i++)Add("Weapon"+i,"Equipar arma "+(i+1),"Armas","<Keyboard>/f"+(i+1));
            Add("Talents","Talentos","Interfaz","<Keyboard>/p");Add("Friends","Amigos y mensajes","Interfaz","<Keyboard>/n");Add("Party","Chat de grupo","Interfaz","<Keyboard>/l");Add("Equipment","Arsenal / equipo","Interfaz","<Keyboard>/b","",false,Modifiers.Shift);
            Add("Journal","Diario de misiones","Interfaz","<Keyboard>/j");Add("Achievements","Logros","Interfaz","<Keyboard>/u");
            Add("Inventory","Inventario","Interfaz","<Keyboard>/i");Add("Character","Personaje","Interfaz","<Keyboard>/b");Add("Abilities","Habilidades","Interfaz","<Keyboard>/k");Add("Pause","Menú / pausa","Interfaz","<Keyboard>/escape");Add("Chat","Abrir chat","Interfaz","<Keyboard>/enter");Add("Squad","Ver escuadra","Interfaz","<Keyboard>/o","",true);
            Add("Settings","Abrir ajustes","Interfaz","<Keyboard>/f10");Add("JournalPrevious","Misión anterior","Diario","<Keyboard>/pageUp");Add("JournalNext","Misión siguiente","Diario","<Keyboard>/pageDown");Add("JournalTrack","Seguir / ocultar misión","Diario","<Keyboard>/g");
            var savedIds=new HashSet<string>();
            try{var saved=JsonUtility.FromJson<Saved>(PlayerPrefs.GetString(Preference,""));if(saved!=null&&saved.Rows!=null)foreach(var row in saved.Rows)if(byId.TryGetValue(row.Id,out var e)){savedIds.Add(row.Id);e.Bindings[0]=row.Primary??B("");e.Bindings[1]=row.Secondary??B("");}}catch(Exception){/* Ignore an incomplete preferences record and retain defaults. */}
            foreach(var id in new[]{"Settings","JournalPrevious","JournalNext","JournalTrack"}){var e=byId[id];if(!savedIds.Contains(id)&&entries.Exists(other=>other.Id!=id&&Array.Exists(other.Bindings,b=>b.Path==e.Bindings[0].Path&&b.Mods==e.Bindings[0].Mods)))e.Bindings[0]=B("");}
            foreach(var e in entries)Build(e);
        }
        static void Build(Entry e){e.Labels[0]=e.Labels[1]=null;if(e.Actions!=null)foreach(var a in e.Actions)a.Dispose();e.Actions=new InputAction[2];for(int i=0;i<2;i++){var a=e.Actions[i]=new InputAction(e.Id+i,InputActionType.Button);if(!string.IsNullOrEmpty(e.Bindings[i].Path))a.AddBinding(e.Bindings[i].Path);a.Enable();}}
        public static Entry Find(string id){Ensure();return byId[id];}
        public static Modifiers CurrentModifiers{get{var k=Keyboard.current;if(k==null)return 0;return(k.shiftKey.isPressed?Modifiers.Shift:0)|(k.ctrlKey.isPressed?Modifiers.Ctrl:0)|(k.altKey.isPressed?Modifiers.Alt:0);}}
        static bool Match(Entry e,int index,bool held){var required=e.Bindings[index].Mods;return e.Continuous&&required==0||CurrentModifiers==required;}
        public static bool Held(string id){if(Rebinding||CaptureHeld||TextEntryFocused)return false;var e=Find(id);for(int i=0;i<2;i++)if(e.Actions[i].IsPressed()&&Match(e,i,true))return true;return false;}
        public static bool Pressed(string id){if(Rebinding||CaptureHeld||(TextEntryFocused&&id!="Pause"&&id!="Chat"))return false;var e=Find(id);for(int i=0;i<2;i++)if(e.Actions[i].WasPressedThisFrame()&&Match(e,i,false))return true;return false;}
        public static string Label(string id,int slot=0){var entry=Find(id);if(entry.Labels[slot]!=null)return entry.Labels[slot];var b=entry.Bindings[slot];if(string.IsNullOrEmpty(b.Path))return"—";string text=InputControlPath.ToHumanReadableString(b.Path,InputControlPath.HumanReadableStringOptions.OmitDevice);return entry.Labels[slot]=(b.Mods.HasFlag(Modifiers.Ctrl)?"Ctrl+":"")+(b.Mods.HasFlag(Modifiers.Shift)?"Shift+":"")+(b.Mods.HasFlag(Modifiers.Alt)?"Alt+":"")+text;}
        public static string ActionLabel(string id)=>string.IsNullOrEmpty(Find(id).Bindings[0].Path)?Label(id,1):Label(id);
        public static string BindingLabel(string id){var e=Find(id);var first=ActionLabel(id);return !string.IsNullOrEmpty(e.Bindings[0].Path)&&!string.IsNullOrEmpty(e.Bindings[1].Path)?first+" / "+Label(id,1):first;}
        static bool Shared(string a,string b)=>a==b||(a=="Select"&&b=="Orbit")||(a=="Orbit"&&b=="Select")||(a=="Pause"&&b=="ClearTarget")||(a=="ClearTarget"&&b=="Pause");
        public static string Conflict(string id,string path,Modifiers mods){Ensure();foreach(var e in entries)for(int i=0;i<2;i++)if(!Shared(id,e.Id)&&e.Bindings[i].Path==path&&e.Bindings[i].Mods==mods)return e.Name;return null;}
        public static void Bind(string id,int slot,string path,Modifiers mods=0){var e=Find(id);e.Bindings[Mathf.Clamp(slot,0,1)]=B(path,mods);Build(e);Save();Changed?.Invoke();}
        public static void Reset(){Ensure();foreach(var e in entries){e.Bindings=new[]{B(e.Defaults[0].Path,e.Defaults[0].Mods),B(e.Defaults[1].Path,e.Defaults[1].Mods)};Build(e);}Save();Changed?.Invoke();}
        public static void Reload(){capturedPath=null;foreach(var e in entries)if(e.Actions!=null)foreach(var a in e.Actions)a.Dispose();entries.Clear();byId.Clear();Ensure();}
        static void Save(){var saved=new Saved();foreach(var e in entries)saved.Rows.Add(new Row{Id=e.Id,Primary=e.Bindings[0],Secondary=e.Bindings[1]});PlayerPrefs.SetString(Preference,JsonUtility.ToJson(saved));PlayerPrefs.Save();}
    }
}
