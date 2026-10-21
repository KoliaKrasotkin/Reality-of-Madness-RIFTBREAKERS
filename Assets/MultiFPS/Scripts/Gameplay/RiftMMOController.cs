using System.Collections.Generic;
using Mirror;
using MultiFPS.UI;
using MultiFPS.Gameplay.RiftCombat;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
namespace MultiFPS.Gameplay
{
    // Camera yaw is deliberately local and independent of replicated character facing.
    [DefaultExecutionOrder(-100)]
    public sealed class RiftMMOController:MonoBehaviour
    {
        public static RiftMMOController Instance {get;private set;}
        public float TurnSpeed=120,TargetRange=45,CameraYaw,CameraPitch=12,CameraDistance=5.5f;
        public bool Walking {get;private set;}public bool Autorun {get;private set;}public bool AutoAttack {get;private set;}
        public bool Running=>!Walking||RiftControls.Held("Sprint");
        public bool Dragging=>orbit||steer;
        public Health Selected {get;private set;}public Health Hovered {get;private set;}
        public RiftWorldTarget SelectedWorld {get;private set;}public RiftWorldTarget HoveredWorld {get;private set;}public RiftWorldTarget PickedWorld {get;private set;}
        public bool HasSelection=>Selected||SelectedWorld;
        public RaycastHit HoverHit {get;private set;}public bool HasHoverHit {get;private set;}
        public CharacterInstance Actor {get;private set;}
        static readonly List<RaycastResult> uiHits=new List<RaycastResult>();static PointerEventData pointerData;static EventSystem pointerSystem;static int pointerFrame=-1;static bool pointerBlocked;
        public static bool PointerOnUI{get{var events=EventSystem.current;if(!events||Mouse.current==null)return false;if(pointerFrame==Time.frameCount)return pointerBlocked;if(pointerSystem!=events){pointerSystem=events;pointerData=new PointerEventData(events);}pointerFrame=Time.frameCount;pointerData.position=Mouse.current.position.ReadValue();uiHits.Clear();events.RaycastAll(pointerData,uiHits);pointerBlocked=false;foreach(var hit in uiHits)if(hit.gameObject.GetComponentInParent<UnityEngine.UI.Selectable>()||hit.gameObject.GetComponentInParent<UnityEngine.UI.ScrollRect>()){pointerBlocked=true;break;}return pointerBlocked;}}

        readonly RaycastHit[] hits=new RaycastHit[128];readonly List<Health> candidates=new List<Health>();
        bool orbit,steer,selectPending,steerPending;Vector2 selectStart,steerStart,savedPointer;float selectDistance,steerDistance,nextAuto;Vector3 lastPosition;
        int readFrame=-1;
        public static RiftMMOController Ensure(GameObject owner){if(!Instance)owner.AddComponent<RiftMMOController>();return Instance;}
        void Awake(){RiftInputUI.Ensure();Instance=this;pointerFrame=-1;var _=RiftControls.Entries;}
        void Update(){var actor=ClientFrontend.OwnedCharacterInstance;if(actor)ReadCharacter(actor);}
        void OnDestroy(){if(Instance==this)Instance=null;}
        void OnDisable(){ReleasePointer();if(Actor)Actor.Input.Movement=Vector2.zero;AutoAttack=Autorun=false;}
        void OnApplicationFocus(bool focused){if(!focused){ReleasePointer();AutoAttack=Autorun=false;}}
        void ReleasePointer(){if(!Dragging)return;orbit=steer=selectPending=steerPending=false;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;if(Mouse.current!=null)Mouse.current.WarpCursorPosition(savedPointer);}
        public bool Available=>Actor&&Actor.Health.CurrentHealth>0&&!ClientFrontend.Pause&&!ClientFrontend.Hub&&!RiftControls.Rebinding&&UICursorManager.GamePlayInput();
        public void ReadCharacter(CharacterInstance actor)
        {
            if(readFrame==Time.frameCount)return;readFrame=Time.frameCount;
            if(Actor!=actor){Actor=actor;CameraYaw=actor.Input.LookY;CameraPitch=12;lastPosition=actor.transform.position;Select(null);Autorun=false;}
            if(EventSystem.current)EventSystem.current.sendNavigationEvents=!Available;
            if(!Available){ReleasePointer();actor.Input.Movement=Vector2.zero;actor.Input.ActionCodes=0;orbit=steer=selectPending=steerPending=false;Hovered=null;HoveredWorld=null;HasHoverHit=false;Autorun=false;AutoAttack=false;return;}
            if(!ReferenceEquals(Selected,null)&&!Valid(Selected,false))Select(null);if(!ReferenceEquals(SelectedWorld,null)&&!ValidWorld(SelectedWorld))Select(null);
            if((actor.transform.position-lastPosition).sqrMagnitude>225){CameraYaw=actor.Input.LookY;CameraPitch=12;}lastPosition=actor.transform.position;
            var mouse=Mouse.current;Vector2 pointer=mouse!=null?mouse.position.ReadValue():Vector2.zero;bool ui=PointerOnUI;bool wasDragging=Dragging;
            if(RiftControls.Pressed("Orbit")&&!ui)orbit=true;if(!RiftControls.Held("Orbit"))orbit=false;
            if(RiftControls.Pressed("Steer")&&!ui){steer=true;steerPending=true;steerStart=pointer;steerDistance=0;}if(!RiftControls.Held("Steer"))steer=false;
            if(RiftControls.Pressed("Select")&&!ui){selectPending=true;selectStart=pointer;selectDistance=0;}
            if(mouse!=null){if(selectPending&&!RiftControls.Pressed("Select"))selectDistance+=mouse.delta.ReadValue().magnitude;if(steerPending&&!RiftControls.Pressed("Steer"))steerDistance+=mouse.delta.ReadValue().magnitude;}
            if(!wasDragging&&Dragging)savedPointer=pointer;
            RefreshHover(ui||Dragging);
            if(selectPending&&!RiftControls.Held("Select")){selectPending=false;if(selectDistance<6&&!ui){var target=Pick(selectStart);if(target)Select(target);else SelectWorld(PickedWorld);}}
            if(steerPending&&!RiftControls.Held("Steer")){steerPending=false;if(steerDistance<6&&!ui){var target=Pick(steerStart);if(target){Select(target);AutoAttack=Valid(target,true);}else if(PickedWorld){SelectWorld(PickedWorld);if(Vector3.Distance(actor.transform.position,PickedWorld.transform.position)<=PickedWorld.InteractionRange)PickedWorld.Interact();}}}
            if(Dragging&&mouse!=null){Vector2 delta=mouse.delta.ReadValue()*.1f*UserSettings.MouseSensitivity;CameraYaw+=delta.x;CameraPitch=Mathf.Clamp(CameraPitch-delta.y,-65,75);if(steer)actor.Input.LookY=CameraYaw;}
            float turn=(RiftControls.Held("TurnRight")?1:0)-(RiftControls.Held("TurnLeft")?1:0);
            if(!steer&&turn!=0){float angle=turn*TurnSpeed*Time.deltaTime;actor.Input.LookY+=angle;if(!orbit)CameraYaw+=angle;}
            float strafe=(RiftControls.Held("StrafeRight")?1:0)-(RiftControls.Held("StrafeLeft")?1:0)+(steer?turn:0);
            float forward=(RiftControls.Held("Forward")?1:0)-(RiftControls.Held("Backward")?1:0);
            if(RiftControls.Pressed("Autorun"))Autorun=!Autorun;if(Mathf.Abs(forward)>0)Autorun=false;
            if(RiftControls.Pressed("Walk"))Walking=!Walking;
            if(orbit&&steer){forward=1;Autorun=false;selectPending=steerPending=false;}else if(Autorun)forward=1;
            actor.Input.Movement=Vector2.ClampMagnitude(new Vector2(strafe,forward),1);actor.SetActionKeyCode(ActionCodes.Sprint,Running&&forward>=0);actor.SetActionKeyCode(ActionCodes.Crouch,RiftControls.Held("Crouch"));
            if(!Dragging&&actor.Input.Movement.sqrMagnitude>.01f)CameraYaw=Mathf.LerpAngle(CameraYaw,actor.Input.LookY,Time.deltaTime*3);
            actor.Input.LookX=CameraPitch;actor.Input.LookY=Mathf.Repeat(actor.Input.LookY+180,360)-180;CameraYaw=Mathf.Repeat(CameraYaw+180,360)-180;
            if(RiftControls.Pressed("Jump")){var native=actor.GetComponent<WarriorAvatar>();if(native)native.RequestJump();else{var motor=actor.GetComponent<CharacterMotor>();if(motor)motor.Jump();}}
            if(!ui){if(RiftControls.Pressed("ZoomIn"))CameraDistance=Mathf.Clamp(CameraDistance-.8f,2,14);if(RiftControls.Pressed("ZoomOut"))CameraDistance=Mathf.Clamp(CameraDistance+.8f,2,14);}
            if(RiftControls.Pressed("NextTarget"))Cycle(1);if(RiftControls.Pressed("PreviousTarget"))Cycle(-1);if(RiftControls.Pressed("SelfTarget"))Select(actor.Health);
            if(RiftControls.Pressed("ClearTarget")&&(HasSelection||AutoAttack)){Select(null);RiftControls.EscapeConsumedFrame=Time.frameCount;}
            var player=ClientFrontend.ClientPlayerInstance;var combat=player?player.GetComponent<AbilityController>():null;
            if(combat&&combat.Prototype){actor.SetActionKeyCode(ActionCodes.Trigger1,false);actor.SetActionKeyCode(ActionCodes.Trigger2,false);for(int i=0;i<7;i++)if(RiftControls.Pressed("Ability"+i)){if(i==0)AutoAttack=Valid(Selected,true);combat.Request(i);}if(AutoAttack&&Valid(Selected,true)&&Time.unscaledTime>=nextAuto){nextAuto=Time.unscaledTime+.15f;var basic=combat.Definition(0);var delta=Selected.transform.position-actor.transform.position;delta.y=0;if(basic&&delta.magnitude<=basic.Radius&&Vector3.Angle(Quaternion.Euler(0,actor.Input.LookY,0)*Vector3.forward,delta)<=basic.Arc*.5f)combat.Request(0);}}
            else if(player){if(RiftControls.Pressed("Ability1"))player.CmdUseClassAbility(0);if(RiftControls.Pressed("Ability2"))player.CmdUseClassAbility(1);ReadWeapons(actor);}
            Cursor.visible=!Dragging;Cursor.lockState=Dragging?CursorLockMode.Locked:CursorLockMode.None;if(wasDragging&&!Dragging&&mouse!=null)mouse.WarpCursorPosition(savedPointer);
        }
        void ReadWeapons(CharacterInstance actor){var items=actor.CharacterItemManager;actor.SetActionKeyCode(ActionCodes.Trigger1,RiftControls.Held("Ability0"));actor.SetActionKeyCode(ActionCodes.Trigger2,RiftControls.Held("Aim"));if(RiftControls.Pressed("Reload"))items.Reload();if(RiftControls.Pressed("Drop"))items.TryDropItem();if(RiftControls.Pressed("PreviousWeapon"))items.TakePreviousItem();if(RiftControls.Pressed("Interact"))items.TryGrabItem();if(RiftControls.Pressed("Melee")&&items.CurrentlyUsedItem)items.CurrentlyUsedItem.PushMeele();for(int i=0;i<5;i++)if(RiftControls.Pressed("Weapon"+i))items.ClientTakeItem(i);}
        public bool Valid(Health health,bool hostile){return Actor&&health&&health.CurrentHealth>0&&health.gameObject.activeInHierarchy&&Vector3.Distance(Actor.transform.position,health.transform.position)<=TargetRange&&(!hostile||health!=Actor.Health&&health.Team!=Actor.Health.Team);}
        public void Select(Health health){if(health&&!Valid(health,false))return;if(ReferenceEquals(Selected,health)&&!SelectedWorld)return;SelectedWorld=null;Selected=health;AutoAttack=false;var p=ClientFrontend.ClientPlayerInstance;var c=p?p.GetComponent<AbilityController>():null;if(c)c.CmdSetTarget(health?health.netId:0);}
        public bool ValidWorld(RiftWorldTarget target)=>Actor&&target&&target.gameObject.activeInHierarchy&&Vector3.Distance(Actor.transform.position,target.transform.position)<=TargetRange;
        public void SelectWorld(RiftWorldTarget target){if(target&&!ValidWorld(target))return;Select(null);SelectedWorld=target;}
        public void Cycle(int direction)
        {
            if(!Actor)return;candidates.Clear();var camera=GameplayCamera._instance?GameplayCamera._instance.GetComponent<Camera>():null;
            foreach(var p in GameManager.Players){var h=p&&p.MyCharacter?p.MyCharacter.Health:null;if(!Valid(h,true)||!CombatController.Visible(Actor,h))continue;if(camera){var screen=camera.WorldToViewportPoint(h.transform.position+Vector3.up);if(screen.z<=0||screen.x<-.1f||screen.x>1.1f||screen.y<-.1f||screen.y>1.1f)continue;}candidates.Add(h);}
            candidates.Sort((a,b)=>{float da=(a.transform.position-Actor.transform.position).sqrMagnitude,db=(b.transform.position-Actor.transform.position).sqrMagnitude;int order=da.CompareTo(db);return order!=0?order:a.netId.CompareTo(b.netId);});
            if(candidates.Count==0)return;int index=candidates.IndexOf(Selected);index=index<0?(direction>0?0:candidates.Count-1):(index+direction+candidates.Count)%candidates.Count;Select(candidates[index]);
        }
        public Health Pick(Vector2 pointer)
        {
            HasHoverHit=false;PickedWorld=null;var camera=GameplayCamera._instance?GameplayCamera._instance.GetComponent<Camera>():null;if(!camera||!Actor)return null;
            int count=Physics.RaycastNonAlloc(camera.ScreenPointToRay(pointer),hits,150,~0,QueryTriggerInteraction.Collide);float nearest=float.PositiveInfinity;Health health=null;RaycastHit found=default;
            for(int i=0;i<count;i++){var collider=hits[i].collider;var box=collider.GetComponent<HitBox>();var h=box?box._health:collider.GetComponentInParent<Health>();if(h==Actor.Health)continue;if(collider.isTrigger&&!h)continue;if(hits[i].distance<nearest){nearest=hits[i].distance;health=h;found=hits[i];}}
            if(nearest<float.PositiveInfinity){HasHoverHit=true;HoverHit=found;var world=found.collider.GetComponentInParent<RiftWorldTarget>();if(ValidWorld(world))PickedWorld=world;}return Valid(health,false)?health:null;
        }
        void RefreshHover(bool blocked){Hovered=null;HoveredWorld=null;HasHoverHit=false;if(!blocked&&Mouse.current!=null){Hovered=Pick(Mouse.current.position.ReadValue());HoveredWorld=PickedWorld;}}
    }
}
