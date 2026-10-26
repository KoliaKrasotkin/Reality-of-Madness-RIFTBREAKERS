using Mirror;using UnityEngine;using UnityEngine.InputSystem;
namespace MultiFPS.Gameplay.RiftCombat {
public sealed partial class AbilityController {
 public uint GroundLeapSequence{get;private set;}public Vector3 LastGroundLeapOrigin{get;private set;}public Vector3 LastGroundLeapLanding{get;private set;}
 bool hasGroundLeap;Vector3 leapDestination,aimRequested;LineRenderer leapRing;MeshRenderer leapRunes;Mesh leapRuneMesh;Material leapMaterial;
 public bool GroundAiming{get;private set;}
 public bool GroundAimValid{get;private set;}public Vector3 GroundAimPosition{get;private set;}
 [Command]public void CmdGroundLeap(Vector3 point){ServerGroundLeap(point);}
 [Server]public bool ServerGroundLeap(Vector3 point){if(!Actor||!TryGroundLeapLanding(Actor.GetComponent<CharacterController>(),Actor.transform.position,point,GameManager.environmentLayer,out _))return RejectAction("Destino de salto inválido");leapDestination=point;hasGroundLeap=true;bool accepted=ServerUse(4);if(!accepted)hasGroundLeap=false;return accepted;}
 public static bool TryGroundLeapLanding(CharacterController body,Vector3 origin,Vector3 destination,int groundMask,out Vector3 landing){landing=origin;if(!Finite(destination)||!Finite(origin))return false;var flat=destination-origin;flat.y=0;if(flat.magnitude>12.01f||flat.magnitude<.3f||Mathf.Abs(destination.y-origin.y)>4)return false;return FindLeapLanding(body,null,origin,destination,groundMask,out landing);}
 static bool Finite(Vector3 v)=>!float.IsNaN(v.x)&&!float.IsNaN(v.y)&&!float.IsNaN(v.z)&&!float.IsInfinity(v.x)&&!float.IsInfinity(v.y)&&!float.IsInfinity(v.z);
 public void BeginGroundAim(){if(!Actor||!IsAbilityLearned(4)||NetworkTime.time<Ready[4]||NetworkTime.time<CastEnd||NetworkTime.time<GlobalReady)return;GroundAiming=true;GroundAimValid=false;UnityEngine.SceneManagement.SceneManager.activeSceneChanged-=GroundSceneChanged;UnityEngine.SceneManagement.SceneManager.activeSceneChanged+=GroundSceneChanged;LocalNotice("Elegí el terreno: clic para saltar · Escape para cancelar");}
 public void CancelGroundAim(){UnityEngine.SceneManagement.SceneManager.activeSceneChanged-=GroundSceneChanged;GroundAiming=false;GroundAimValid=false;if(leapRing)leapRing.gameObject.SetActive(false);if(leapRunes)leapRunes.gameObject.SetActive(false);}
 void GroundSceneChanged(UnityEngine.SceneManagement.Scene before,UnityEngine.SceneManagement.Scene after){DisposeGroundAim();}
 public void TickGroundAim(bool pointerOnUI){if(!GroundAiming)return;if(!Actor||Actor.Health.CurrentHealth<=0){CancelGroundAim();return;}if((Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame)||(Mouse.current!=null&&Mouse.current.rightButton.wasPressedThisFrame)){RiftControls.EscapeConsumedFrame=Time.frameCount;CancelGroundAim();return;}
  var camera=GameplayCamera._instance?GameplayCamera._instance.GetComponent<Camera>():null;bool hit=false;RaycastHit ground=default;if(camera&&Mouse.current!=null)hit=Physics.Raycast(camera.ScreenPointToRay(Mouse.current.position.ReadValue()),out ground,180,GameManager.environmentLayer,QueryTriggerInteraction.Ignore);
  Vector3 landing=Actor.transform.position;GroundAimValid=hit&&!pointerOnUI&&Vector3.Angle(ground.normal,Vector3.up)<=45&&TryGroundLeapLanding(Actor.GetComponent<CharacterController>(),Actor.transform.position,ground.point,GameManager.environmentLayer,out landing);
  aimRequested=hit?ground.point:Actor.transform.position;GroundAimPosition=GroundAimValid?landing:hit?ground.point:Actor.transform.position;
  var runeTexture=Resources.Load<Texture2D>("RiftMenu/GroundLeapIndicator");
  if(runeTexture){
   if(!leapRunes){var go=new GameObject("Ground leap destination runes"){hideFlags=HideFlags.DontSave};leapRuneMesh=new Mesh{name="Compact ground leap marker"};leapRuneMesh.vertices=new[]{new Vector3(-.65f,0,-.65f),new Vector3(-.65f,0,.65f),new Vector3(.65f,0,.65f),new Vector3(.65f,0,-.65f)};leapRuneMesh.uv=new[]{Vector2.zero,Vector2.up,Vector2.one,Vector2.right};leapRuneMesh.triangles=new[]{0,1,2,0,2,3};leapRuneMesh.RecalculateNormals();leapRuneMesh.RecalculateBounds();go.AddComponent<MeshFilter>().sharedMesh=leapRuneMesh;leapRunes=go.AddComponent<MeshRenderer>();leapRunes.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;leapRunes.receiveShadows=false;leapMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit")){hideFlags=HideFlags.DontSave};leapMaterial.SetTexture("_BaseMap",runeTexture);leapMaterial.SetFloat("_Surface",1);leapMaterial.SetFloat("_Blend",0);leapMaterial.SetInt("_SrcBlend",(int)UnityEngine.Rendering.BlendMode.SrcAlpha);leapMaterial.SetInt("_DstBlend",(int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);leapMaterial.SetInt("_ZWrite",0);leapMaterial.SetInt("_Cull",0);leapMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");leapMaterial.renderQueue=3000;leapRunes.sharedMaterial=leapMaterial;}
   var point=GroundAimPosition;var normal=hit?ground.normal:Vector3.up;if(Physics.Raycast(point+Vector3.up*1.5f,Vector3.down,out var surface,4,GameManager.environmentLayer,QueryTriggerInteraction.Ignore)){point=surface.point;normal=surface.normal;}
   leapRunes.transform.SetPositionAndRotation(point+normal*.035f,Quaternion.FromToRotation(Vector3.up,normal));leapRunes.gameObject.SetActive(true);leapMaterial.SetColor("_BaseColor",GroundAimValid?new Color(.9f,.9f,.9f,.58f):new Color(1,.05f,.07f,.72f));
  }else{
   // Functional fallback if an optional presentation asset is unavailable.
   if(!leapRing){var go=new GameObject("Ground leap destination"){hideFlags=HideFlags.DontSave};leapRing=go.AddComponent<LineRenderer>();leapRing.useWorldSpace=true;leapRing.positionCount=49;leapRing.widthMultiplier=.07f;leapRing.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;leapMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit")){hideFlags=HideFlags.DontSave};leapRing.sharedMaterial=leapMaterial;}
   leapRing.gameObject.SetActive(true);leapMaterial.SetColor("_BaseColor",GroundAimValid?new Color(.25f,1,.62f):new Color(1,.18f,.25f));for(int i=0;i<49;i++){float a=i*Mathf.PI*2/48;leapRing.SetPosition(i,GroundAimPosition+new Vector3(Mathf.Cos(a)*.65f,.09f,Mathf.Sin(a)*.65f));}
  }
  if(!pointerOnUI&&Mouse.current!=null&&Mouse.current.leftButton.wasPressedThisFrame){if(GroundAimValid){ConfirmGroundAim();}else LocalNotice("Elegí un destino libre dentro de 12 m");}
 }
 public bool ConfirmGroundAim(){if(!GroundAiming||!GroundAimValid)return false;CmdGroundLeap(aimRequested);CancelGroundAim();return true;}
 void DisposeGroundAim(){CancelGroundAim();if(leapRing)Destroy(leapRing.gameObject);if(leapRunes)Destroy(leapRunes.gameObject);if(leapRuneMesh)Destroy(leapRuneMesh);if(leapMaterial)Destroy(leapMaterial);leapRing=null;leapRunes=null;leapRuneMesh=null;leapMaterial=null;}
}
}


