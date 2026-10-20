using System;
using System.IO;
using System.Linq;
using ROM.Horde;
using MultiFPS.Gameplay.Gamemodes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
namespace ROM.EditorTools {
    public static class RiftExplorationBuilder {
        const string Scene="Assets/ROM/Levels/map_rom_horde.unity";
        const string Root="Assets/ROM/Levels/Horde/FracturedForest";
        public static void Run(){try{Build();RiftExplorationProbe.Run();}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
        [MenuItem("ROM/El Rift/Configurar exploración")]
        public static void Build(){
            EditorSceneManager.OpenScene(Scene);
            var mode=UnityEngine.Object.FindFirstObjectByType<ROM.Horde.Horde>();
            if(!mode)throw new Exception("Horde mode missing");
            var existing=GameObject.Find("El Rift - Exploración");if(existing)UnityEngine.Object.DestroyImmediate(existing);
            var root=new GameObject("El Rift - Exploración").transform;
            var spawnContainer=UnityEngine.Object.FindFirstObjectByType<SpawnpointsContainer>();
            var spawns=spawnContainer.Spawnpoints.ToArray();
            var start=spawns.Length>0?spawns[0].position:Vector3.zero;
            var centers=new[]{start,new Vector3(-310,8,0),new Vector3(310,8,0),new Vector3(0,8,450),new Vector3(100,8,-470),new Vector3(820,8,370),new Vector3(-620,8,430)};
            string[] names={"Refugio inicial","Ruinas occidentales","Bosque de cristal","Bosque del norte","Distrito urbano","Ciudad del Sol","Santuario del Eclipse"};
            var checkpoints=new System.Collections.Generic.List<RiftCheckpoint>();var encounters=new System.Collections.Generic.List<RiftEncounter>();
            var mesh=AssetDatabase.FindAssets("CrystalShard t:Mesh").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<Mesh>).FirstOrDefault();
            var mat=AssetDatabase.FindAssets("DimensionalCrystal t:Material").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<Material>).FirstOrDefault();
            for(int i=0;i<centers.Length;i++){
                var direction=(centers[i]-start).normalized;
                var desired=i==0?centers[i]+Vector3.right*9:centers[i]-direction*(i==5?165:80);
                var pos=Ground(desired,60);
                var go=new GameObject(names[i]);go.transform.SetParent(root);go.transform.position=pos;
                var cp=go.AddComponent<RiftCheckpoint>();cp.Title=names[i];cp.StartingCamp=i==0;cp.SafeRadius=i==0?110:22;
                var respawn=new GameObject("Respawn").transform;respawn.SetParent(go.transform);respawn.position=Ground(pos+Vector3.back*5,10)+Vector3.up*.15f;cp.Respawn=respawn;
                var pedestal=GameObject.CreatePrimitive(PrimitiveType.Cylinder);pedestal.name="Base del cristal";pedestal.transform.SetParent(go.transform);pedestal.transform.position=pos+Vector3.up*.15f;pedestal.transform.localScale=new Vector3(4,.15f,4);if(mat)pedestal.GetComponent<Renderer>().sharedMaterial=mat;
                GameObject crystal;
                if(mesh){crystal=new GameObject("Cristal de checkpoint",typeof(MeshFilter),typeof(MeshRenderer));crystal.GetComponent<MeshFilter>().sharedMesh=mesh;crystal.GetComponent<MeshRenderer>().sharedMaterial=mat;}
                else {crystal=GameObject.CreatePrimitive(PrimitiveType.Cube);crystal.name="Cristal provisional";crystal.transform.rotation=Quaternion.Euler(0,45,35);}
                crystal.transform.SetParent(go.transform);crystal.transform.position=pos+Vector3.up*3;crystal.transform.localScale=new Vector3(1.4f,4,1.4f);
                var light=new GameObject("Luz del cristal",typeof(Light)).GetComponent<Light>();light.transform.SetParent(go.transform);light.transform.position=pos+Vector3.up*3;light.color=Color.cyan;light.range=16;light.intensity=2;
                var label=new GameObject("Señal del cristal",typeof(TextMesh)).GetComponent<TextMesh>();label.transform.SetParent(go.transform);label.transform.position=pos+Vector3.up*6;label.text=names[i]+"\nCHECKPOINT";label.characterSize=.10f;label.fontSize=40;label.anchor=TextAnchor.MiddleCenter;label.color=new Color(.3f,1,1);
                label.gameObject.AddComponent<RiftCheckpointLabel>();checkpoints.Add(cp);
                if(i==0)continue;
                // Three pockets per island, with the guardian at the deepest pocket.
                for(int j=0;j<3;j++){
                    var flank=Vector3.Cross(Vector3.up,direction).normalized;
                    var offset=j==0?flank*70:j==1?-flank*70:direction*85;
                    var ep=Ground(centers[i]+offset,55);
                    if(checkpoints.Any(c=>c.Contains(ep)))throw new Exception("Encounter overlaps checkpoint: "+names[i]);
                    var zone=new GameObject(names[i]+(j==2?" · Guardián":" · Patrulla "+(j+1))).AddComponent<RiftEncounter>();zone.transform.SetParent(root);zone.transform.position=ep;zone.Title=zone.name;zone.MiniBoss=j==2;
                    zone.SpawnPoints=new Transform[4];
                    for(int n=0;n<4;n++){
                        float a=n*Mathf.PI*.5f;var point=new GameObject("Enemigo "+n).transform;point.SetParent(zone.transform);point.position=Ground(ep+new Vector3(Mathf.Cos(a)*8,0,Mathf.Sin(a)*8),15)+Vector3.up*.1f;zone.SpawnPoints[n]=point;
                    }
                    encounters.Add(zone);
                }
            }
            mode.ExplorationMode=true;mode.Checkpoints=checkpoints.ToArray();mode.Encounters=encounters.ToArray();EditorUtility.SetDirty(mode);
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Directory.CreateDirectory("Documentation/exploration");File.WriteAllText("Documentation/exploration/layout.txt",string.Join("\n",checkpoints.Select(c=>c.Title+" "+c.transform.position))+"\nEncounters: "+encounters.Count+"; guardians: "+encounters.Count(c=>c.MiniBoss));
        }
        static Vector3 Ground(Vector3 point,float radius){if(NavMesh.SamplePosition(point,out var hit,radius,NavMesh.AllAreas))return hit.position;throw new Exception("No walkable NavMesh near "+point);}
    }
}
