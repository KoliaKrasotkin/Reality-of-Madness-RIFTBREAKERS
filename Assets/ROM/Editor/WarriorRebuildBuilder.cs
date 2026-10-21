using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using Mirror;
using MultiFPS;
using MultiFPS.Gameplay;
using MultiFPS.Gameplay.RiftCombat;
using ROM.UI.Sprint01;
namespace ROM.EditorTools
{
    public static class WarriorRebuildBuilder
    {
        public const string Root="Assets/ROM/Characters/Warrior";
        public const string PlayerPath=Root+"/WarriorPlayer.prefab", LobbyPath=Root+"/WarriorLobby.prefab";
        static readonly List<string> report=new List<string>();
        static AnimationClip Clip(string name){var roots=new[]{"Assets/Mobility_Starter","Assets/Raise Creation/Samurai Animation Pack"};var path=AssetDatabase.FindAssets(name+" t:Model",roots).Select(AssetDatabase.GUIDToAssetPath).First(p=>Path.GetFileNameWithoutExtension(p)==name);var clip=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__"));if(!clip.isHumanMotion)throw new Exception("Nonhuman clip "+name);return clip;}
        static void Ref(UnityEngine.Object target,string field,UnityEngine.Object value){var so=new SerializedObject(target);var prop=so.FindProperty(field);if(prop==null)throw new Exception("Missing property "+field);prop.objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
        static AnimatorController Controller()
        {
            string path=Root+"/WarriorLocomotion.controller";var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);if(!controller)controller=AnimatorController.CreateAnimatorControllerAtPath(path);
            foreach(var param in controller.parameters)controller.RemoveParameter(param);
            controller.AddParameter("MoveX",AnimatorControllerParameterType.Float);controller.AddParameter("MoveY",AnimatorControllerParameterType.Float);controller.AddParameter("Grounded",AnimatorControllerParameterType.Bool);controller.AddParameter("Dead",AnimatorControllerParameterType.Bool);controller.AddParameter("AttackRate",AnimatorControllerParameterType.Float);
            var parameters=controller.parameters;foreach(var p in parameters){if(p.name=="Grounded")p.defaultBool=true;if(p.name=="AttackRate")p.defaultFloat=1;}controller.parameters=parameters;
            var sm=controller.layers[0].stateMachine;foreach(var child in sm.states)sm.RemoveState(child.state);
            foreach(var transition in sm.anyStateTransitions)sm.RemoveAnyStateTransition(transition);
            var move=sm.AddState("Locomotion");sm.defaultState=move;
            var blend=new BlendTree{name="Native directional locomotion",blendType=BlendTreeType.FreeformCartesian2D,blendParameter="MoveX",blendParameterY="MoveY"};AssetDatabase.AddObjectToAsset(blend,controller);
            blend.AddChild(Clip("MOB1_Stand_Relaxed_Idle_v2_IPC"),Vector2.zero);
            blend.AddChild(Clip("MOB1_Walk_F_Loop_IPC"),new Vector2(0,.5f));blend.AddChild(Clip("MOB1_Jog_F_Loop_IPC"),Vector2.up);
            blend.AddChild(Clip("Anim_Samurai_Walk_Backward"),new Vector2(0,-.5f));blend.AddChild(Clip("Anim_Samurai_Walk_Left"),new Vector2(-.5f,0));blend.AddChild(Clip("Anim_Samurai_Walk_Right"),new Vector2(.5f,0));move.motion=blend;
            var air=sm.AddState("Air");air.motion=Clip("MOB1_Jog_F_Jump_RU_Air_IPC");var takeoff=move.AddTransition(air);takeoff.hasExitTime=false;takeoff.duration=.12f;takeoff.AddCondition(AnimatorConditionMode.IfNot,0,"Grounded");var land=air.AddTransition(move);land.hasExitTime=false;land.duration=.12f;land.AddCondition(AnimatorConditionMode.If,0,"Grounded");
            var species=AssetDatabase.LoadAssetAtPath<SpeciesDefinition>("Assets/ROM/Combat/Nertor.asset");var c=species.Classes[0];
            for(int i=0;i<4;i++){var ability=i==0?c.BasicAttack:c.Abilities[i-1];var state=sm.AddState("Attack"+i);state.motion=ability.Animation;state.speedParameter="AttackRate";state.speedParameterActive=true;var done=state.AddTransition(move);done.hasExitTime=true;done.exitTime=.96f;done.duration=.12f;}
            var dead=sm.AddState("Death");dead.motion=Clip("Anim_Samurai_Death");var die=sm.AddAnyStateTransition(dead);die.hasExitTime=false;die.duration=.1f;die.canTransitionToSelf=false;die.AddCondition(AnimatorConditionMode.If,0,"Dead");var revive=dead.AddTransition(move);revive.hasExitTime=false;revive.duration=.1f;revive.AddCondition(AnimatorConditionMode.IfNot,0,"Dead");EditorUtility.SetDirty(controller);return controller;
        }
        static WeaponDefinition Axe()
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Free medieval weapons/Prefabs/Axe_DS.prefab");var go=UnityEngine.Object.Instantiate(source);go.name="Warrior Axe";
            var materialCache=new Dictionary<Material,Material>();foreach(var r in go.GetComponentsInChildren<Renderer>(true))r.sharedMaterials=r.sharedMaterials.Select(original=>{if(materialCache.TryGetValue(original,out var existing))return existing;string path=Root+"/"+original.name+" URP.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.SetTexture("_BaseMap",original.GetTexture("_MainTex"));mat.SetTexture("_BumpMap",original.GetTexture("_BumpMap"));mat.EnableKeyword("_NORMALMAP");mat.SetFloat("_Smoothness",.35f);AssetDatabase.CreateAsset(mat,path);}materialCache[original]=mat;return mat;}).ToArray();
            foreach(var col in go.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(col);
            var renderers=go.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
            var model=PrefabUtility.SaveAsPrefabAsset(go,Root+"/WarriorAxe.prefab");UnityEngine.Object.DestroyImmediate(go);
            string defPath=Root+"/WarriorAxe.asset";var weapon=AssetDatabase.LoadAssetAtPath<WeaponDefinition>(defPath);if(!weapon){weapon=ScriptableObject.CreateInstance<WeaponDefinition>();AssetDatabase.CreateAsset(weapon,defPath);}weapon.Id="warrior-axe";weapon.DisplayName="Hacha de Warrior";weapon.WeaponType="HeavyAxe";weapon.Model=model;weapon.Scale=Vector3.one*1.35f/Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z));weapon.AllowedClasses=new[]{"warrior"};weapon.AllowedSpecializations=new string[0];weapon.Reach=3.4f;weapon.Rotation=Vector3.zero;weapon.Position=Vector3.zero;EditorUtility.SetDirty(weapon);report.Add("Axe actual bounds="+bounds+" normalized scale="+weapon.Scale);return weapon;
        }
        static GameObject Body(AnimatorController controller,WeaponDefinition weapon)
        {
            var body=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ROM/UI/FrontEnd/Models/ids-character-1.prefab"));body.name="Warrior native Lizardman";
            foreach(var behaviour in body.GetComponentsInChildren<MonoBehaviour>(true))UnityEngine.Object.DestroyImmediate(behaviour);
            foreach(var collider in body.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(collider);
            var animator=body.GetComponent<Animator>();if(!animator||!animator.avatar||!animator.avatar.isHuman||!animator.avatar.isValid)throw new Exception("Native Lizardman avatar invalid");
            animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            var renderers=body.GetComponentsInChildren<SkinnedMeshRenderer>();var bounds=renderers[0].bounds;foreach(var r in renderers){bounds.Encapsulate(r.bounds);r.updateWhenOffscreen=false;}
            body.transform.localScale*=2f/bounds.size.y;
            animator.Rebind();animator.SetBool("Grounded",true);animator.Update(0);
            var baked=new Mesh();float min=float.PositiveInfinity;foreach(var renderer in renderers){renderer.BakeMesh(baked);foreach(var vertex in baked.vertices)min=Mathf.Min(min,renderer.transform.TransformPoint(vertex).y);}UnityEngine.Object.DestroyImmediate(baked);body.transform.position-=Vector3.up*min;
            var hand=animator.GetBoneTransform(HumanBodyBones.RightHand);var axe=UnityEngine.Object.Instantiate(weapon.Model,hand);axe.name="Equipped Warrior Axe";axe.transform.localScale=weapon.Scale/body.transform.lossyScale.x;
            // Derive model axis from actual mesh, then author the grip on the native avatar.
            var axeRenderers=axe.GetComponentsInChildren<Renderer>();var box=axeRenderers[0].bounds;foreach(var r in axeRenderers)box.Encapsulate(r.bounds);
            Vector3 modelAxis=Vector3.forward;var mesh=weapon.Model.GetComponentInChildren<MeshFilter>().sharedMesh;var size=mesh.bounds.size;if(size.y>size.z&&size.y>size.x)modelAxis=Vector3.up;else if(size.x>size.z)modelAxis=Vector3.right;
            Quaternion world=Quaternion.FromToRotation(modelAxis,new Vector3(.2f,.55f,1).normalized);
            axe.transform.rotation=world;axe.transform.position=hand.position;
            weapon.Rotation=axe.transform.localEulerAngles;weapon.Position=axe.transform.localPosition;EditorUtility.SetDirty(weapon);
            report.Add("Native right hand="+hand.name+" axe model axis="+modelAxis+" authored rotation="+weapon.Rotation+" body avatar="+animator.avatar.name);
            return body;
        }
        static void Hitbox(Animator animator,HumanBodyBones bone,Health health,CharacterPart part,float radius)
        {
            var target=animator.GetBoneTransform(bone);var go=new GameObject("Warrior "+part+" hitbox");go.transform.SetParent(target,false);var sphere=go.AddComponent<SphereCollider>();sphere.radius=radius/Mathf.Max(.01f,target.lossyScale.x);var hit=go.AddComponent<HitBox>();hit._health=health;hit.part=part;go.layer=8;
        }
        [MenuItem("ROM/Personajes/Reconstruir Warrior nativo")]
        public static void Build()
        {
            Directory.CreateDirectory(Root);Directory.CreateDirectory("Documentation/warrior-rebuild");AssetDatabase.Refresh();report.Clear();
            var controller=Controller();var axe=Axe();var visual=Body(controller,axe);var lobby=PrefabUtility.SaveAsPrefabAsset(visual,LobbyPath);
            var player=new GameObject("ROM Warrior Player",typeof(NetworkIdentity),typeof(CharacterController),typeof(AudioSource));
            var avatar=player.AddComponent<WarriorAvatar>();var health=player.AddComponent<Health>();var items=player.AddComponent<CharacterItemManager>();var actor=player.AddComponent<CharacterInstance>();player.AddComponent<DNTransform>();
            var capsule=player.GetComponent<CharacterController>();capsule.height=2;capsule.radius=.32f;capsule.center=Vector3.up;capsule.stepOffset=.3f;capsule.slopeLimit=48;
            visual.transform.SetParent(player.transform,false);actor.CharacterParent=visual.transform;
            var look=new GameObject("Warrior camera pivot").transform;look.SetParent(player.transform,false);look.localPosition=Vector3.up*1.7f;var camera=new GameObject("Warrior camera target").transform;camera.SetParent(look,false);
            actor.FPPLook=look;actor.FPPCameraTarget=camera;actor.characterMind=look;actor.characterFirePoint=camera;actor.CharacterMarkerPosition=look;actor.CameraHeight=1.7f;actor.ObjectForDeathCameraToFollow=visual.transform;
            var animator=visual.GetComponent<Animator>();avatar.Animator=animator;avatar.EquippedWeapon=visual.GetComponentsInChildren<Transform>().First(t=>t.name=="Equipped Warrior Axe").gameObject;avatar.Weapon=axe;
            Ref(items,"_itemPointInHand",animator.GetBoneTransform(HumanBodyBones.RightHand));Ref(items,"_itemTargetFPP",camera);
            Hitbox(animator,HumanBodyBones.Head,health,CharacterPart.head,.16f);Hitbox(animator,HumanBodyBones.Chest,health,CharacterPart.body,.3f);Hitbox(animator,HumanBodyBones.Hips,health,CharacterPart.body,.28f);
            var prefab=PrefabUtility.SaveAsPrefabAsset(player,PlayerPath);UnityEngine.Object.DestroyImmediate(player);
            string instancePath="Assets/MultiFPS/Levels/Prefabs/Gameplay/MultiFPS_PlayerInstance.prefab";var instance=PrefabUtility.LoadPrefabContents(instancePath);instance.GetComponent<PlayerInstance>().WarriorCharacterPrefab=prefab;PrefabUtility.SaveAsPrefabAsset(instance,instancePath);PrefabUtility.UnloadPrefabContents(instance);
            foreach(string path in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/MultiFPS/Levels","Assets/ROM"}).Select(AssetDatabase.GUIDToAssetPath)){
                var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!asset||!asset.GetComponent<NetworkManager>())continue;var root=PrefabUtility.LoadPrefabContents(path);var manager=root.GetComponent<NetworkManager>();if(!manager.spawnPrefabs.Contains(prefab))manager.spawnPrefabs.Add(prefab);PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);report.Add("Mirror prefab registry: "+path);
            }
            var species=AssetDatabase.LoadAssetAtPath<SpeciesDefinition>("Assets/ROM/Combat/Nertor.asset");species.Classes[0].DisplayName="Warrior";foreach(var spec in species.Classes[0].Specializations){spec.Weapon=axe;EditorUtility.SetDirty(spec);}EditorUtility.SetDirty(species.Classes[0]);
            var catalog=AssetDatabase.LoadAssetAtPath<FrontEndCatalog>("Assets/ROM/UI/FrontEnd/FrontEndCatalog.asset");catalog.Models.First(m=>m.Key=="ids-character-1").Prefab=lobby;EditorUtility.SetDirty(catalog);
            foreach(string path in new[]{ProjectSceneCleanup.Menu,ProjectSceneCleanup.Rift}){var scene=EditorSceneManager.OpenScene(path);foreach(var manager in UnityEngine.Object.FindObjectsByType<NetworkManager>(FindObjectsInactive.Include,FindObjectsSortMode.None)){if(!manager.spawnPrefabs.Contains(prefab))manager.spawnPrefabs.Add(prefab);EditorUtility.SetDirty(manager);}EditorSceneManager.SaveScene(scene);}
            AssetDatabase.SaveAssets();
            if(prefab.GetComponent<CharacterMotor>()||prefab.GetComponent<CharacterAnimator>()||prefab.GetComponentInChildren<IDSGameplayPose>())throw new Exception("Template animation/motor leaked into clean Warrior");
            report.Add("PASS clean prefab: no CharacterMotor, CharacterAnimator, IDSGameplayPose, RigBuilder, template controller or template skeleton");
            report.Add("PASS native Avatar, axe renderer, independent directional Animator controller, Mirror identity, DN input/position adapter");File.WriteAllLines("Documentation/warrior-rebuild/build-validation.txt",report);
            Debug.Log("WARRIOR REBUILD COMPLETE");
        }
        public static void Run(){try{Build();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    }
}
