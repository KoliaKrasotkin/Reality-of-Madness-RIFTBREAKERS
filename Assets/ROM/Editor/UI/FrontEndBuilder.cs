using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MultiFPS;
using MultiFPS.Gameplay;
using MultiFPS.UI;
using ROM.UI.Sprint01;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace ROM.UI.EditorTools
{
    public static class FrontEndBuilder
    {
        public const string Root="Assets/ROM/UI/FrontEnd";
        static string report;
        [MenuItem("ROM/UI/Construir lobby 3D de El Rift")]
        public static void Build()
        {
            Directory.CreateDirectory(Root+"/Models");Directory.CreateDirectory(Root+"/Materials");Directory.CreateDirectory("Assets/ROM/Rendering");AssetDatabase.Refresh();report="IDS FRONTEND\n";
            var catalog=AssetDatabase.LoadAssetAtPath<FrontEndCatalog>(Root+"/FrontEndCatalog.asset");
            if(!catalog){catalog=ScriptableObject.CreateInstance<FrontEndCatalog>();AssetDatabase.CreateAsset(catalog,Root+"/FrontEndCatalog.asset");}
            RepairDinork();var skins=AssetDatabase.FindAssets("t:SkinContainer").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<SkinContainer>).Where(s=>s&&s.Renderer&&AssetDatabase.GetAssetPath(s.Renderer).StartsWith("Assets/IDS ASSETS/",StringComparison.OrdinalIgnoreCase)).OrderBy(s=>s.SkinName).ToArray();
            var models=new List<PreviewModel>();
            for(int i=0;i<skins.Length;i++){
                var skin=skins[i];string key="ids-character-"+i;
                var prefab=CharacterPreview(skin,key);
                models.Add(new PreviewModel{Key=key,Name=skin.SkinName,Category="Personaje",SkinIndex=i,Prefab=prefab,ForSale=true,GoldPrice=800+i*200});
            }
            string[] files={"rifle_GR.fbx","pistol_V4.fbx","shotgun_v2.fbx","Sniper_Rifle_V3_FBX.fbx"};
            string[] names={"Fusil IDS","Pistola IDS","Escopeta IDS","Francotirador IDS"};
            for(int i=0;i<files.Length;i++){
                var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/IDS ASSETS/Weapon pack IDS/"+files[i]);if(!source)continue;
                string key="ids-weapon-"+i;var clone=UnityEngine.Object.Instantiate(source);clone.name=names[i];Strip(clone);
                foreach(var r in clone.GetComponentsInChildren<Renderer>(true))r.sharedMaterials=r.sharedMaterials.Select(Convert).ToArray();
                var prefab=PrefabUtility.SaveAsPrefabAsset(clone,Root+"/Models/"+key+".prefab");UnityEngine.Object.DestroyImmediate(clone);
                models.Add(new PreviewModel{Key=key,Name=names[i],Category="Arma",Prefab=prefab,ForSale=true,GoldPrice=400+i*100});
            }
            catalog.Models=models.ToArray();catalog.StagePrefab=BuildStage();EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
            SprintUIBuilder.Build();
            var scene=EditorSceneManager.OpenScene("Assets/MultiFPS/Levels/hub.unity");
            var manager=UnityEngine.Object.FindFirstObjectByType<ClientInterfaceManager>(FindObjectsInactive.Include);if(manager){manager.characterSkins=skins;EditorUtility.SetDirty(manager);}
            var hub=UnityEngine.Object.FindFirstObjectByType<RiftHub>();hub.Catalog=catalog;hub.Connector=UnityEngine.Object.FindFirstObjectByType<RoomConnector>(FindObjectsInactive.Include);
            var map=AssetDatabase.LoadAssetAtPath<MapRepresenter>("Assets/ROM/Levels/representer_map_rom_horde.asset");map.Name="El Rift";map.MaxPlayersPresets=new[]{1,2,3,4};EditorUtility.SetDirty(map);
            if(hub.Creator){hub.Creator.Maps=new[]{map};EditorUtility.SetDirty(hub.Creator);}
            var account=UnityEngine.Object.FindFirstObjectByType<UIUsernameSet>(FindObjectsInactive.Include);
            if(account){var so=new SerializedObject(account);var dropdown=so.FindProperty("CharacterSkinSelector").objectReferenceValue as Dropdown;if(dropdown){dropdown.ClearOptions();dropdown.AddOptions(skins.Select(s=>s.SkinName).ToList());dropdown.value=0;EditorUtility.SetDirty(dropdown);}}
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.4f,.48f,.62f);RenderSettings.ambientEquatorColor=new Color(.22f,.24f,.3f);RenderSettings.ambientGroundColor=new Color(.08f,.08f,.12f);
            EditorSceneManager.SaveScene(scene);
            EditorBuildSettings.scenes=EditorBuildSettings.scenes.Select(s=>new EditorBuildSettingsScene(s.path,s.path=="Assets/MultiFPS/Levels/hub.unity"||s.path=="Assets/ROM/Levels/map_rom_horde.unity")).ToArray();
            InstallStance();CharacterPresentationUpgrade.Build();LizardmanIntegration.Build();AssetDatabase.SaveAssets();File.WriteAllText("Documentation/frontend-ids-models.txt",report);
            Debug.Log("[FrontEnd] IDS catalog, lobby and single destination saved.");
        }
        static void RepairDinork()
        {
            var skin=AssetDatabase.LoadAssetAtPath<SkinContainer>("Assets/MultiFPS/Scripts/Gameplay/Skins/CharacterSkin_Dinork.asset");
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/IDS ASSETS/JP/Dinork/Dinork.fbx");if(!skin||!source)return;
            var renderer=source.GetComponentsInChildren<SkinnedMeshRenderer>(true).OrderByDescending(r=>r.sharedMesh?r.sharedMesh.vertexCount:0).FirstOrDefault();if(!renderer)return;
            skin.SkinName="Dinork";skin.Renderer=renderer;skin.Mesh=renderer.sharedMesh;skin.Materials=renderer.sharedMaterials.Select(DinorkMaterial).Select(Convert).ToArray();EditorUtility.SetDirty(skin);
        }
        static GameObject CharacterPreview(SkinContainer skin,string key)
        {
            string native=skin.SkinName switch {"Black Panther"=>"BlackPanter/Black_Panther_rig.fbx","Dinork"=>"Dinork/Dinork_Skin_01_Rig.fbx","Purplehead"=>"PurpleHead/Purple_Head_rig.fbx","Redhead"=>"Redhead/Red_Head_Rig.fbx","White Knight"=>"White Knight/White_Knight_Rig.fbx",_=>null};
            var source=native==null?skin.Renderer.transform.root.gameObject:AssetDatabase.LoadAssetAtPath<GameObject>("Assets/IDS ASSETS/Characters/"+native);
            if(!source)throw new Exception("Missing native IDS model: "+native);
            var clone=UnityEngine.Object.Instantiate(source);clone.name=skin.SkinName;Strip(clone);
            foreach(var renderer in clone.GetComponentsInChildren<SkinnedMeshRenderer>(true)){
                renderer.enabled=true;
                if(skin.SkinName=="Dinork")renderer.sharedMaterials=renderer.sharedMaterials.Select(DinorkMaterial).Select(Convert).ToArray();
                else if(skin.Materials.Length==renderer.sharedMesh.subMeshCount)renderer.sharedMaterials=skin.Materials.Select(Convert).ToArray();
                else renderer.sharedMaterials=renderer.sharedMaterials.Select(Convert).ToArray();
                renderer.updateWhenOffscreen=true;
            }
            foreach(var animator in clone.GetComponentsInChildren<Animator>(true))UnityEngine.Object.DestroyImmediate(animator);
            var avatar=BuildAvatar(clone,key);
            if(avatar&&avatar.isHuman&&avatar.isValid){
                var animator=clone.AddComponent<Animator>();animator.avatar=avatar;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.runtimeAnimatorController=IdleController();
                var stance=clone.AddComponent<NaturalLegStance>();stance.Animator=animator;stance.RelaxedPreviewIdle=true;
            }
            var prefab=PrefabUtility.SaveAsPrefabAsset(clone,Root+"/Models/"+key+".prefab");UnityEngine.Object.DestroyImmediate(clone);return prefab;
        }
        static Avatar BuildAvatar(GameObject root,string key)
        {
            string[,] names={{"Hips","hips"},{"Spine","spine"},{"Chest","chest"},{"Neck","neck"},{"Head","head"},{"LeftUpperLeg","upperLeg.L"},{"LeftLowerLeg","lowerLeg.L"},{"LeftFoot","foot.L"},{"LeftToes","toe.L"},{"RightUpperLeg","upperLeg.R"},{"RightLowerLeg","lowerLeg.R"},{"RightFoot","foot.R"},{"RightToes","toe.R"},{"LeftShoulder","shoulder.L"},{"LeftUpperArm","upperArm.L"},{"LeftLowerArm","lowerArm.L"},{"LeftHand","hand.L"},{"RightShoulder","shoulder.R"},{"RightUpperArm","upperArm.R"},{"RightLowerArm","lowerArm.R"},{"RightHand","hand.R"}};
            var bones=root.GetComponentsInChildren<Transform>(true);var human=new List<HumanBone>();
            for(int i=0;i<names.GetLength(0);i++){
                string requested=names[i,1];var match=bones.FirstOrDefault(t=>t.name==requested);
                if(!match)match=bones.FirstOrDefault(t=>NormalizeBoneName(t.name)==NormalizeBoneName(requested));
                if(!match)continue;human.Add(new HumanBone{humanName=names[i,0],boneName=match.name,limit=new HumanLimit{useDefaultValues=true}});
                if(match.name!=requested)report+=$"  Alias {requested} -> {match.name}\n";
            }
            if(human.Count<15){report+=root.name+": native rig retained; missing humanoid mapping.\n";return null;}
            var description=new HumanDescription{human=human.ToArray(),skeleton=bones.Select(t=>new SkeletonBone{name=t.name,position=t.localPosition,rotation=t.localRotation,scale=t.localScale}).ToArray(),upperArmTwist=.5f,lowerArmTwist=.5f,upperLegTwist=.5f,lowerLegTwist=.5f,armStretch=.02f,legStretch=.02f,feetSpacing=0};
            var avatar=AvatarBuilder.BuildHumanAvatar(root,description);avatar.name=key+" Avatar";report+=$"{root.name}: humanoid valid={avatar.isValid}, human={avatar.isHuman}; own skeleton preserved.\n";
            string path=Root+"/Models/"+key+"-Avatar.asset";var old=AssetDatabase.LoadAssetAtPath<Avatar>(path);
            if(old){EditorUtility.CopySerialized(avatar,old);UnityEngine.Object.DestroyImmediate(avatar);EditorUtility.SetDirty(old);return old;}
            AssetDatabase.CreateAsset(avatar,path);return avatar;
        }
        static string NormalizeBoneName(string value)=>value.ToLowerInvariant().Replace("reg","leg").Replace(".","").Replace("_","");
        static Material DinorkMaterial(Material source)
        {
            string name=source?source.name.ToLowerInvariant():"";string file=name.Contains("skin")||name.Contains("s01")?"Dinork_S01.mat":"Dinork_Body.mat";
            return AssetDatabase.LoadAssetAtPath<Material>("Assets/IDS ASSETS/Characters/Dinork/Textures/"+file)??source;
        }
        static RuntimeAnimatorController IdleController()
        {
            string path=Root+"/Models/IDS_LobbyIdle.controller";var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);if(controller)return controller;
            controller=AnimatorController.CreateAnimatorControllerAtPath(path);
            var source=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/MultiFPS/Art/Animations/Player/Soldier_EmptyHanded.controller");
            var idle=source.animationClips.First(c=>c.name.EndsWith("|emptyHanded"));var state=controller.layers[0].stateMachine.AddState("Idle");state.motion=idle;controller.layers[0].stateMachine.defaultState=state;return controller;
        }
        static void Strip(GameObject root)
        {
            foreach(var script in root.GetComponentsInChildren<MonoBehaviour>(true))if(script)UnityEngine.Object.DestroyImmediate(script);
            foreach(var c in root.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
            foreach(var c in root.GetComponentsInChildren<Camera>(true))UnityEngine.Object.DestroyImmediate(c);
            foreach(var c in root.GetComponentsInChildren<Light>(true))UnityEngine.Object.DestroyImmediate(c);
        }
        public static Material Convert(Material source)
        {
            if(!source)return null;if(source.shader&&source.shader.name.StartsWith("Universal Render Pipeline/"))return source;
            string path=Root+"/Materials/"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source))+"_"+Math.Abs(source.GetInstanceID())+".mat";
            // Stable material key also works with multiple subassets in a model.
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source,out string guid,out long local);path=Root+"/Materials/"+guid+"_"+local+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
            mat.name=source.name+" URP";mat.SetColor("_BaseColor",source.HasProperty("_Color")?source.GetColor("_Color"):Color.white);
            SavedTexture(source,mat,"_BaseMap",new[]{"_BaseMap","_BaseColorMap","_MainTex","_Albedo_Map"});SavedTexture(source,mat,"_BumpMap",new[]{"_BumpMap","_NormalMap","_Normal_Map"});
            if(mat.GetTexture("_BumpMap"))mat.EnableKeyword("_NORMALMAP");mat.SetFloat("_Smoothness",.3f);mat.SetFloat("_Metallic",.15f);mat.enableInstancing=true;EditorUtility.SetDirty(mat);return mat;
        }
        static void SavedTexture(Material source,Material target,string destination,string[] candidates)
        {
            var array=new SerializedObject(source).FindProperty("m_SavedProperties.m_TexEnvs");foreach(var candidate in candidates)for(int i=0;i<array.arraySize;i++){
                var item=array.GetArrayElementAtIndex(i);if(item.FindPropertyRelative("first").stringValue!=candidate)continue;var value=item.FindPropertyRelative("second");var tex=value.FindPropertyRelative("m_Texture").objectReferenceValue as Texture;if(!tex)continue;target.SetTexture(destination,tex);return;
            }
        }
        static GameObject BuildStage()
        {
            var root=new GameObject("IDS Lobby Courtyard");
            var volume=root.AddComponent<Volume>();volume.isGlobal=true;volume.priority=20;volume.sharedProfile=ROM.Horde.EditorTools.FracturedForestBuilder.EnsureCinematicProfile();
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Lobby Floor";floor.transform.SetParent(root.transform);floor.transform.localPosition=new Vector3(0,-.15f,3);floor.transform.localScale=new Vector3(30,.3f,35);UnityEngine.Object.DestroyImmediate(floor.GetComponent<Collider>());
            var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/ROM/Levels/FracturedForest/Materials/EclipseCourt.mat");floor.GetComponent<Renderer>().sharedMaterial=mat;
            string temple="Assets/Standard Assets/Eternal Temple/Prefabs/";
            for(int side=-1;side<=1;side+=2){
                StageProp(root,temple+"Arch Alley/Column_Group_01.prefab",new Vector3(side*8,0,10),7);
                StageProp(root,temple+"Flora/Tree_01.prefab",new Vector3(side*10,0,12),13);
                StageProp(root,temple+"Cliffs/Cliff_01.prefab",new Vector3(side*15,-3,18),15);
            }
            var portal=GameObject.CreatePrimitive(PrimitiveType.Quad);portal.transform.SetParent(root.transform);portal.transform.localPosition=new Vector3(0,4.7f,17);portal.transform.localScale=new Vector3(8,10,1);UnityEngine.Object.DestroyImmediate(portal.GetComponent<Collider>());
            string portalMat=Root+"/Materials/DimensionalBackdrop.mat";var energy=AssetDatabase.LoadAssetAtPath<Material>(portalMat);if(!energy){energy=new Material(Shader.Find("ROM/Rift Portal"));AssetDatabase.CreateAsset(energy,portalMat);}energy.shader=Shader.Find("ROM/Rift Portal");energy.SetColor("_Tint",new Color(.7f,.06f,2.8f));energy.SetFloat("_Intensity",2);portal.GetComponent<Renderer>().sharedMaterial=energy;EditorUtility.SetDirty(energy);
            string file=Root+"/RiftLobbyStage.prefab";var prefab=PrefabUtility.SaveAsPrefabAsset(root,file);UnityEngine.Object.DestroyImmediate(root);return prefab;
        }
        static void StageProp(GameObject root,string path,Vector3 position,float height)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!prefab)return;var instance=UnityEngine.Object.Instantiate(prefab,root.transform);Strip(instance);instance.transform.localPosition=Vector3.zero;
            var renderers=instance.GetComponentsInChildren<Renderer>(true);if(renderers.Length==0)return;var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
            float scale=height/Mathf.Max(.01f,bounds.size.y);instance.transform.localScale*=scale;instance.transform.position=position-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z)*scale;
            foreach(var renderer in renderers)renderer.sharedMaterials=renderer.sharedMaterials.Select(Convert).ToArray();
        }
        static void InstallStance()
        {
            string path="Assets/MultiFPS/Levels/Prefabs/Gameplay/MultiFPS_Character.prefab";var root=PrefabUtility.LoadPrefabContents(path);
            var character=root.GetComponent<CharacterInstance>();var animator=root.GetComponentsInChildren<Animator>(true).FirstOrDefault(a=>a.avatar&&a.avatar.isHuman);
            if(animator){var stance=animator.GetComponent<NaturalLegStance>()??animator.gameObject.AddComponent<NaturalLegStance>();stance.Animator=animator;stance.Character=character;stance.MaximumAbduction=.18f;}
            var ca=root.GetComponent<CharacterAnimator>();var setup=new SerializedObject(ca);var models=setup.FindProperty("_idsNativeModels");var catalog=AssetDatabase.LoadAssetAtPath<FrontEndCatalog>(Root+"/FrontEndCatalog.asset");models.arraySize=5;foreach(var model in catalog.Characters)models.GetArrayElementAtIndex(model.SkinIndex).objectReferenceValue=model.Prefab;setup.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);
        }
        public static void FromCommandLine(){try{Build();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    }
}

