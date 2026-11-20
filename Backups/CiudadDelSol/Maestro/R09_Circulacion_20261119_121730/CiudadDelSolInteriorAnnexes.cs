using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace ROM.EditorTools
{
    public static partial class CiudadDelSolInteriorStageTwo
    {
        static void UnifyAnnexRunes(Scene scene)
        {
            if (scene.isDirty) throw new System.InvalidOperationException("Escena con cambios pendientes.");
            var root = scene.GetRootGameObjects().Single(g => g.name == RuneRoot);
            var violet = AssetDatabase.LoadAssetAtPath<Material>(RuneAssets + "/Violeta_5.mat");
            if (!violet) throw new FileNotFoundException("Material violeta existente requerido.");
            int count = 0;
            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>())
            {
                var t = renderer.transform;
                if (!renderer.name.Contains("_Inscripcion_") || !renderer.sharedMaterial ||
                    !AssetDatabase.GetAssetPath(renderer.sharedMaterial).StartsWith(RuneAssets + "/")) continue;
                var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                var size = mesh.bounds.size;
                // Contract only along an axis tangent to an existing flat wall.
                // Vertices are projected in room coordinates, so preserve the mesh centre.
                if (size.x < .02f || size.z < .02f)
                {
                    var before = t.TransformPoint(mesh.bounds.center);
                    var scale = Vector3.one * .72f;
                    if (size.x < .02f) scale.x = 1; else scale.z = 1;
                    t.localScale = scale;
                    t.position += before - t.TransformPoint(mesh.bounds.center);
                }
                renderer.sharedMaterial = violet;
                count++;
            }
            if (count == 0) throw new System.InvalidOperationException("Sin inscripciones.");
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Guardado fallido.");
            var saved = EditorSceneManager.OpenScene(City);
            var persisted = saved.GetRootGameObjects().Single(g => g.name == RuneRoot)
                .GetComponentsInChildren<MeshRenderer>().Count(r => r.name.Contains("_Inscripcion_") && r.sharedMaterial == violet);
            if (persisted != count) throw new IOException("Cantidad de runas recargadas inesperada.");
            File.WriteAllText("Backups/CiudadDelSol/Maestro/20261119_113720/Runas.txt", persisted.ToString());
        }
        // Sample the actual perimeter at floor level. A clear broad phase is a
        // candidate for a doorway; it does not certify walkability or a room.
        static void SurveyAnnexSites(Scene scene)
        {
            Physics.SyncTransforms();
            const string west = "Backups/CiudadDelSol/Maestro/R09_20261119_121347";
            if (File.Exists(west + "/Medir.pending"))
            {
                var rows = new List<string>{"x|z|floor|body|upper|nav"};
                for (int x = -174; x <= -120; x++)
                for (int z = 49; z <= 89; z++)
                {
                    var p = new Vector3(x, -16, z);
                    string Collisions(float lo, float hi) => string.Join(",", Physics.OverlapBox(
                        p + Vector3.up * ((lo+hi)*.5f), new Vector3(.49f,(hi-lo)*.5f,.49f),
                        Quaternion.identity, ~0, QueryTriggerInteraction.Ignore).Select(c=>c.name).Distinct().OrderBy(n=>n));
                    var floor = Physics.Raycast(p+Vector3.up*.35f,Vector3.down,out var support,.75f,~0,QueryTriggerInteraction.Ignore)
                        ? support.collider.name : "";
                    var nav = NavMesh.SamplePosition(p,out var sample,.55f,NavMesh.AllAreas) && Mathf.Abs(sample.position.y+16)<.35f;
                    rows.Add(string.Join("|",x,z,floor,Collisions(.1f,2.2f),Collisions(2.2f,5.8f),nav));
                }
                File.WriteAllLines(west+"/Mediciones.txt",rows);
                File.Move(west+"/Medir.pending",west+"/Medir.ejecutado");
                return;
            }
            var lines = new List<string> { "room|threshold|outward|navInside|doorColliders|roomColliders|roomRenderers" };
            var renderers = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<MeshRenderer>())
                .Where(r => r.enabled).ToArray();
            void Check(string room, Vector3 threshold, Vector3 outward)
            {
                Vector3 center = threshold + outward * 8 + Vector3.up * 3;
                Vector3 extent = outward.x != 0 ? new Vector3(7.95f, 2.9f, 5.95f) : new Vector3(5.95f, 2.9f, 7.95f);
                var bounds = new Bounds(center, extent * 2);
                var obstacles = Physics.OverlapBox(center, extent, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
                Vector3 doorExtent = outward.x != 0 ? new Vector3(1, 1.45f, 2) : new Vector3(2, 1.45f, 1);
                var doors = Physics.OverlapBox(threshold + Vector3.up * 1.55f, doorExtent, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
                bool nav = NavMesh.SamplePosition(threshold - outward * 2, out var hit, .65f, NavMesh.AllAreas)
                    && Mathf.Abs(hit.position.y - threshold.y) < .4f;
                string Names(IEnumerable<Component> items) => string.Join(",", items.Select(c => c.name).Distinct().OrderBy(n => n));
                lines.Add(string.Join("|", room, threshold, outward, nav, Names(doors), Names(obstacles),
                    Names(renderers.Where(r => bounds.Intersects(r.bounds)))));
            }
            for (int z = 39; z <= 87; z += 3) Check("D01", new Vector3(132,-34,z), Vector3.right);
            for (int z = -39; z <= 21; z += 3) Check("D02", new Vector3(132,-34,z), Vector3.right);
            for (int x = 75; x <= 123; x += 3) Check("D02", new Vector3(x,-34,-48), Vector3.back);
            for (int z = -45; z <= 9; z += 3) Check("R07", new Vector3(-138,-16,z), Vector3.left);
            for (int z = 34; z <= 50; z += 2) Check("R08", new Vector3(-138,-16,z), Vector3.left);
            for (int z = 63; z <= 75; z += 3) Check("R09", new Vector3(-138,-16,z), Vector3.left);
            File.WriteAllLines("Backups/CiudadDelSol/Maestro/20261119_113720/Anexos.txt", lines);
        }
    }
}
