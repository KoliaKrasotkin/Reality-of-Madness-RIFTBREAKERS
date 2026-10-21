using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using ROM.Horde;
namespace ROM.EditorTools {
public static class LittleDemonSpawnAudit {
 public static void Run(){try{EditorSceneManager.OpenScene(ProjectSceneCleanup.Rift);var world=UnityEngine.Object.FindFirstObjectByType<ROM.Horde.Horde>();var population=world.GetComponent<RiftEnemyPopulation>();var report=population.Zones.Select(z=>{int valid=z.SpawnPoints.Count(p=>p&&!world.IsSafe(p.position)&&NavMesh.SamplePosition(p.position,out var sample,12,NavMesh.AllAreas)&&!world.IsSafe(sample.position));if(valid!=z.Count)throw new Exception(z.Title+": valid spawn points "+valid+" / "+z.Count);return "PASS "+z.Title+": "+valid+" navigable points outside safe zones";}).ToArray();File.WriteAllLines("Documentation/little-demon/spawn-validation.txt",report);EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
}
