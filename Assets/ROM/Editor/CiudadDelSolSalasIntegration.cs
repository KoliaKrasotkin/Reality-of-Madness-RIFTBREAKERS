using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ROM.EditorTools
{
    public static partial class CiudadDelSolNaveJardinesPreview
    {
        const string ApprovedSceneHash = "F260566E867B8158229BFDD3D424B3E7A70F37A60B152A41E7F1C73A1819E741";
        const string MountedRoot = "Subsuelo_Salas_Monumentales";
        const string AssetFolder = "Assets/ROM/CiudadDelSolSalasMonumentales";
        const string BackupFolder = "Backups/CiudadDelSol/SalasMonumentales";

        sealed class MountPlan
        {
            public readonly Sector Design;
            public readonly GameObject Hall;
            public readonly GlobalObjectId[] Targets;
            public readonly Bounds[] EndCapitals;
            public MountPlan(Sector design,GameObject hall,GlobalObjectId[] targets,Bounds[] endCapitals)
            {
                Design=design; Hall=hall; Targets=targets; EndCapitals=endCapitals;
            }
        }

        [MenuItem(Phase + "Integrar cuatro salas autorizadas")]
        static void IntegrateFromMenu()
        {
            Close();
            Integrate(SceneManager.GetActiveScene());
        }

        public static void IntegrateInBatch() => Integrate(EditorSceneManager.OpenScene(Original,OpenSceneMode.Single));

        static void Integrate(Scene source)
        {
            Require(source.path==Original && !source.isDirty && !EditorApplication.isPlayingOrWillChangePlaymode &&
                StageUtility.GetCurrentStage()==StageUtility.GetMainStage(),"Abrir la escena definitiva limpia fuera de Play.");
            Require(Hash()==ApprovedSceneHash,"Hash distinto de la escena aprobada.");
            Require(source.GetRootGameObjects().All(g => g.name!=MountedRoot) &&
                !Directory.Exists(AssetFolder),"Ya existe la integracion o su carpeta de assets.");
            Directory.CreateDirectory(BackupFolder);
            string backup=Path.Combine(BackupFolder,"Dungeon_Isla_del_Sol.antes-salas."+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")+".unity");
            File.Copy(Original,backup);
            Require(File.ReadAllBytes(Original).SequenceEqual(File.ReadAllBytes(backup)),"El respaldo difiere de la escena.");
            Debug.Log("SALAS_RESPALDO_OK: "+backup);

            var preview=EditorSceneManager.NewPreviewScene();
            var resources=new List<Object>();
            var plans=new List<MountPlan>();
            bool ownFolder=false, sceneTouched=false, saved=false;
            try
            {
                foreach (var sector in Sectors) Build(source,preview,resources,sector,plans);
                Require(plans.Count==4 && plans.SelectMany(p=>p.Targets).Distinct().Count()==175 &&
                    plans.Sum(p=>p.Targets.Length)==175 && resources.OfType<Mesh>().Count()==8,
                    "El plan no coincide con las 175 piezas y ocho mallas aprobadas.");
                Require(Hash()==ApprovedSceneHash,"La fuente cambio durante la prevalidacion.");
                Require(!string.IsNullOrEmpty(AssetDatabase.CreateFolder("Assets/ROM","CiudadDelSolSalasMonumentales")),
                    "No se pudo crear la carpeta de assets independientes.");
                ownFolder=true;
                var stone=Object.Instantiate(resources.OfType<Material>().First());
                stone.hideFlags=HideFlags.None;
                stone.name="Piedra opaca - Salas monumentales";
                AssetDatabase.CreateAsset(stone,AssetFolder+"/PiedraOpaca.mat");
                sceneTouched=true;
                var root=new GameObject(MountedRoot);
                SceneManager.MoveGameObjectToScene(root,source);
                for (int i=0;i<plans.Count;i++)
                {
                    var plan=plans[i];
                    foreach (var id in plan.Targets)
                    {
                        var piece=GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as GameObject;
                        Require(piece && piece.scene==source && piece.activeInHierarchy && piece.transform.root.name==plan.Design.Root,
                            "Una pieza aprobada no coincide al montar: "+id);
                        piece.SetActive(false);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(piece);
                    }
                    var hall=plan.Hall;
                    hall.name=plan.Design.Name;
                    hall.transform.SetParent(null,true);
                    SceneManager.MoveGameObjectToScene(hall,source);
                    hall.transform.SetParent(root.transform,true);
                    int part=0;
                    foreach (var filter in hall.GetComponentsInChildren<MeshFilter>())
                    {
                        var mesh=Object.Instantiate(filter.sharedMesh);
                        mesh.hideFlags=HideFlags.None;
                        AssetDatabase.CreateAsset(mesh,AssetFolder+$"/Sala_{i+1}_{++part}.asset");
                        filter.sharedMesh=mesh;
                        filter.GetComponent<MeshCollider>().sharedMesh=mesh;
                        filter.GetComponent<MeshRenderer>().sharedMaterial=stone;
                    }
                }
                EditorSceneManager.ClosePreviewScene(preview);
                AssetDatabase.SaveAssets();
                ValidateIntegrated(source,plans);
                Require(Hash()==ApprovedSceneHash,"La escena en disco cambio durante el montaje.");
                EditorSceneManager.MarkSceneDirty(source);
                Require(EditorSceneManager.SaveScene(source),"Unity no pudo guardar la escena definitiva.");
                saved=true;
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                var reopened=EditorSceneManager.OpenScene(Original,OpenSceneMode.Single);
                ValidateIntegrated(reopened,plans);
                Debug.Log("SALAS_INTEGRADAS_OK: respaldo="+backup+"; hash="+Hash()+"; 175 piezas desactivadas, ocho mallas y un material persistentes.");
            }
            catch
            {
                if (sceneTouched)
                {
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                    if (saved) File.Copy(backup,Original,true);
                    EditorSceneManager.OpenScene(Original,OpenSceneMode.Single);
                }
                if (ownFolder) AssetDatabase.DeleteAsset(AssetFolder);
                throw;
            }
            finally
            {
                if (preview.IsValid()) EditorSceneManager.ClosePreviewScene(preview);
                foreach (var item in resources) if (item) Object.DestroyImmediate(item);
            }
        }

        static void ValidateIntegrated(Scene scene,List<MountPlan> plans)
        {
            var root=scene.GetRootGameObjects().Single(g=>g.name==MountedRoot);
            Require(root.transform.childCount==4,"Numero de salas montadas inesperado.");
            var assetMeshes=new HashSet<Mesh>();
            foreach (var plan in plans)
            {
                foreach (var id in plan.Targets)
                {
                    var original=GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as GameObject;
                    Require(original && original.scene==scene && !original.activeSelf &&
                        original.transform.root.name==plan.Design.Root,"No persistio una desactivacion: "+id);
                }
                var hall=root.transform.Find(plan.Design.Name).gameObject;
                var filters=hall.GetComponentsInChildren<MeshFilter>();
                Require(filters.Length==2 && filters.All(filter =>
                {
                    var mesh=filter.sharedMesh;
                    var collider=filter.GetComponent<MeshCollider>();
                    var material=filter.GetComponent<MeshRenderer>().sharedMaterial;
                    return mesh && assetMeshes.Add(mesh) && collider.enabled && collider.sharedMesh==mesh &&
                        AssetDatabase.GetAssetPath(mesh).StartsWith(AssetFolder+"/",StringComparison.Ordinal) &&
                        AssetDatabase.GetAssetPath(material)==AssetFolder+"/PiedraOpaca.mat" &&
                        material.GetFloat("_AlphaClip")==0 && !material.IsKeywordEnabled("_ALPHATEST_ON");
                }),"Referencias persistentes incorrectas: "+plan.Design.Name);
                var (low,peak,samples,joints)=ValidateMounted(hall,plan.Design,plan.EndCapitals);
                Debug.Log($"SALAS_ESCENA_OK {plan.Design.Name} {plan.Design.Width}x{plan.Design.Length}; "+
                    $"altura={low:F2}/{peak:F2}; pasos={samples}; juntas={joints}; piezas={plan.Targets.Length}");
            }
            Require(assetMeshes.Count==8,"Conteo de mallas persistentes incorrecto.");
        }
    }
}
