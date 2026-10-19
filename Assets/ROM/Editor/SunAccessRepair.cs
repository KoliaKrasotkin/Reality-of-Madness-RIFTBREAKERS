using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
namespace ROM.Horde.EditorTools {
public static partial class FracturedForestBuilder {
public static void RepairSunAccessFromCommandLine(){try{
var source=EditorSceneManager.OpenScene(SunDemo);var terrain=UnityEngine.Object.FindFirstObjectByType<Terrain>();
var data=terrain.terrainData;var pos=terrain.transform.position;
var scene=EditorSceneManager.OpenScene(ScenePath);var old=GameObject.Find("Relieve original de Sun Temple");
if(!old)throw new Exception("Missing Sun relief");root=old.transform.parent;soil=old.GetComponent<Renderer>().sharedMaterial;
var materialPath=SunRoot+"/SunWorldGround.mat";var world=AssetDatabase.LoadAssetAtPath<Material>(materialPath);if(!world){world=new Material(Shader.Find("ROM/World Terrain"));AssetDatabase.CreateAsset(world,materialPath);}world.shader=Shader.Find("ROM/World Terrain");world.SetTexture("_BaseMap",soil.GetTexture("_BaseMap"));world.SetTexture("_RockMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/HIVEMIND/PostApocalypticTown/URP/Art/Textures/LandscapeMaterial/T_Canyon_A.png"));world.SetColor("_BaseColor",new Color(.8f,.82f,.78f));world.SetFloat("_WorldScale",.12f);EditorUtility.SetDirty(world);soil=world;
var temp=Terrain.CreateTerrainGameObject(data);temp.transform.position=pos;CopySunGround(temp.GetComponent<Terrain>());UnityEngine.Object.DestroyImmediate(temp);UnityEngine.Object.DestroyImmediate(old);
var report=new List<string>();int removed=0;
var authored=GameObject.Find("Sun Temple - composicion original");
var decks=UnityEngine.Object.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None).Where(c=>c.name.Contains("suelo continuo") && Vector3.Distance(c.transform.position,SunCenter)<320).ToArray();
var trees=new HashSet<GameObject>();
if(authored)foreach(var renderer in authored.GetComponentsInChildren<MeshRenderer>(true)){
string names=(renderer.name+" "+AssetDatabase.GetAssetPath(renderer.GetComponent<MeshFilter>()?.sharedMesh)).ToLowerInvariant();
if(!names.Contains("tree")&&!names.Contains("cypress")&&!names.Contains("pine"))continue;
var bounds=renderer.bounds;
if(decks.Any(d=>{var local=d.transform.InverseTransformPoint(bounds.center)-d.center;var width=Mathf.Min(bounds.extents.x,bounds.extents.z);return Mathf.Abs(local.x)<d.size.x*.5f+width && Mathf.Abs(local.z)<d.size.z*.5f+width && bounds.min.y<d.bounds.max.y+3 && bounds.max.y>d.bounds.min.y;})){
var lod=renderer.GetComponentInParent<LODGroup>();var node=lod?lod.gameObject:renderer.gameObject;
if(node.transform.IsChildOf(authored.transform))trees.Add(node);}}
foreach(var tree in trees){if(tree){UnityEngine.Object.DestroyImmediate(tree);removed++;}}
Physics.SyncTransforms();
var surface=UnityEngine.Object.FindFirstObjectByType<NavMeshSurface>();surface.BuildNavMesh();var nav=AssetDatabase.LoadAssetAtPath<NavMeshData>(Root+"/NavMesh-RiftVisuals.asset");
if(nav){EditorUtility.CopySerialized(surface.navMeshData,nav);surface.RemoveData();surface.navMeshData=nav;surface.AddData();EditorUtility.SetDirty(nav);}else throw new Exception("Missing destination NavMesh");
VerifyPath(SunCourt);VerifyPath(SanctuaryCenter);foreach(var c in SatelliteCenters)VerifyPath(c);
report.Add("Access excavation blends over 39 m instead of an abrupt 20 m cut; original heightfield retained outside the access.");report.Add("Trees removed from bridge clearance: "+removed);report.Add("Destination connectivity PASS; original recovered source remains approximate.");
EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
CaptureEclipse("Documentation/sun-access-after.png",SunCenter+new Vector3(-130,48,-130),SunCourt+Vector3.up*8);
File.WriteAllLines("Documentation/sun-access-validation.txt",report);EditorApplication.Exit(0);
}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}}

