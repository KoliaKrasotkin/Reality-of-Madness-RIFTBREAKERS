using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using ROM.UI.Gameplay;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ROM.EditorTools
{
    // Literal orthographic raster from saved-scene geometry and existing navigation. No baking or visibility changes.
    public static class CiudadDelSolExteriorCartographyBaker
    {
        const string ScenePath = "Assets/Scenes/Dungeon_Isla_del_Sol.unity";
        const string OutputRoot = "Assets/ROM/Cartography/CiudadDelSol/Exterior/Revision_";
        const int Size = 4096;
        static readonly string[] Sectors = { "01 - Entrada Portico Solar", "02 - Jardines Dorados", "03 - Plaza del Eclipse", "04 - Archivos Solares", "05 - Claustro del Eclipse", "06 - Santuario Profano", "07 - El Sol Corrompido" };
        static readonly string[] Routes = { "01-02", "01-06", "02-03", "03-04", "03-05", "03-06", "04-05", "05-07", "06-07" };
        sealed class Part { public Collider Source; public Vector3[] V; public int[] I; public Bounds Bounds; public int Group; public bool Floor, Stair; }
        [Serializable] sealed class GroupReport { public string name; public Vector3 pivot, minimum, maximum; public int floorColliders, navigationPixels; }
        [Serializable] sealed class Report
        {
            public string scene, sceneSHA256, navigationAsset, navigationSHA256, atlas, image, orientation = "North=+Z (top); east=+X (right)", method = "Orthographic plan of actual physical exterior floors; gold=upper floor with existing NavMesh; cyan=lower/intermediate floor with existing NavMesh; muted stone=physical floor WITHOUT confirmed navigation (not a path); dark strokes=actual architectural sections. Overlapping floors are projected into one atlas, not separate interior maps. Not a new NavMesh or certification of connectivity.";
            public int width = Size, height = Size, floorColliders, obstacleColliders, walkingPixels, underpassPixels, physicalPixels;
            public Vector2 center, worldSize; public GroupReport[] groups;
        }
        static Rect extent;
        struct FloorSample { public float Height; public int Group; }
        static float[] upper;
        // Allocate extra samples only where real floors overlap in plan. Do not discard intermediate storeys.
        static Dictionary<int, List<FloorSample>> overlappingFloors;
        static uint[] navigationGroups;
        static byte[] walk;
        static int[] owners;
        static Color32[] pixels;

        static void AddFloor(int pixel, float height, int group)
        {
            if (owners[pixel] < 0) { upper[pixel] = height; owners[pixel] = group; return; }
            if (owners[pixel] == group && Mathf.Abs(upper[pixel] - height) < .02f) return;
            if (!overlappingFloors.TryGetValue(pixel, out var samples))
                overlappingFloors.Add(pixel, samples = new List<FloorSample>(2));
            if (height > upper[pixel])
            {
                samples.Add(new FloorSample { Height = upper[pixel], Group = owners[pixel] });
                upper[pixel] = height; owners[pixel] = group;
            }
            else if (!samples.Any(s => s.Group == group && Mathf.Abs(s.Height - height) < .02f))
                samples.Add(new FloorSample { Height = height, Group = group });
        }

        static void MatchNavigation(int pixel, float height)
        {
            if (owners[pixel] >= 0 && Mathf.Abs(upper[pixel] - height) <= .35f)
            { walk[pixel] = 1; navigationGroups[pixel] |= 1u << owners[pixel]; }
            if (!overlappingFloors.TryGetValue(pixel, out var samples)) return;
            foreach (var sample in samples)
                if (Mathf.Abs(sample.Height - height) <= .35f)
                {
                    if (walk[pixel] == 0) walk[pixel] = 2;
                    navigationGroups[pixel] |= 1u << sample.Group;
                }
        }

        static void Progress(string phase, float value)
        {
            if (EditorUtility.DisplayCancelableProgressBar("Cartografia exterior de Ciudad del Sol", phase, value))
                throw new OperationCanceledException("Cartografía cancelada; no se guardó la escena ni el NavMesh.");
        }

        static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        static string Hash(string path) { using (var sha = SHA256.Create()) using (var file = File.OpenRead(path)) return BitConverter.ToString(sha.ComputeHash(file)).Replace("-", ""); }
        static Vector2 Pixel(Vector3 p) => new Vector2((p.x - extent.xMin) / extent.width * Size, (p.z - extent.yMin) / extent.height * Size);
        static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        static void Triangle(Vector3 a, Vector3 b, Vector3 c, Action<int, float> visit)
        {
            var x = Pixel(a); var y = Pixel(b); var z = Pixel(c);
            float den = Cross(y - x, z - x); if (Mathf.Abs(den) < .0001f) return;
            int minX = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(x.x, y.x, z.x)), 0, Size - 1), maxX = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(x.x, y.x, z.x)), 0, Size - 1);
            int minY = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(x.y, y.y, z.y)), 0, Size - 1), maxY = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(x.y, y.y, z.y)), 0, Size - 1);
            for (int row = minY; row <= maxY; row++) for (int col = minX; col <= maxX; col++)
            {
                var q = new Vector2(col + .5f, row + .5f);
                float u = Cross(y - q, z - q) / den, v = Cross(z - q, x - q) / den;
                if (u >= -.00001f && v >= -.00001f && u + v <= 1.00001f) visit(row * Size + col, a.y * u + b.y * v + c.y * (1 - u - v));
            }
        }

        static Part Geometry(Collider source, int group, bool floor, bool stair)
        {
            Vector3[] vertices; int[] indices;
            if (source is BoxCollider box)
            {
                var e = box.size * .5f; var c = box.center;
                vertices = new[] { c + new Vector3(-e.x,-e.y,-e.z), c + new Vector3(e.x,-e.y,-e.z), c + new Vector3(e.x,e.y,-e.z), c + new Vector3(-e.x,e.y,-e.z), c + new Vector3(-e.x,-e.y,e.z), c + new Vector3(e.x,-e.y,e.z), c + new Vector3(e.x,e.y,e.z), c + new Vector3(-e.x,e.y,e.z) };
                indices = new[] {3,7,6,3,6,2,0,1,5,0,5,4,0,3,2,0,2,1,4,5,6,4,6,7,0,4,7,0,7,3,1,2,6,1,6,5};
            }
            else if (source is MeshCollider mesh && mesh.sharedMesh) { vertices = mesh.sharedMesh.vertices; indices = mesh.sharedMesh.triangles; }
            else return null;
            var matrix = source.transform.localToWorldMatrix;
            for (int i = 0; i < vertices.Length; i++) vertices[i] = matrix.MultiplyPoint3x4(vertices[i]);
            return new Part { Source = source, V = vertices, I = indices, Bounds = source.bounds, Group = group, Floor = floor, Stair = stair };
        }

        static bool FloorRole(Collider col, Transform group, out bool stair)
        {
            stair = false; bool floor = false;
            if (CiudadDelSolExteriorSourceRoles.IsStructuralPlatform(col, group)) return false;
            for (var t = col.transform; t && t != group; t = t.parent)
            {
                stair |= t.name.StartsWith("Staircase_01", StringComparison.Ordinal);
                floor |= t.name.StartsWith("Platform_Base_01", StringComparison.Ordinal) || t.name.StartsWith("Road_01", StringComparison.Ordinal) || t.name.StartsWith("Road_End_01", StringComparison.Ordinal) || t.name.StartsWith("Plaza_Floor_01", StringComparison.Ordinal);
            }
            return floor || stair;
        }

        static void Section(Part p, float height)
        {
            if (p.Bounds.min.y >= height || p.Bounds.max.y <= height) return;
            for (int i = 0; i < p.I.Length; i += 3)
            {
                var a = p.V[p.I[i]]; var b = p.V[p.I[i + 1]]; var c = p.V[p.I[i + 2]];
                var points = new List<Vector3>(3);
                void Edge(Vector3 u, Vector3 v) { if ((u.y <= height && v.y > height) || (v.y <= height && u.y > height)) points.Add(Vector3.LerpUnclamped(u, v, (height - u.y) / (v.y - u.y))); }
                Edge(a,b); Edge(b,c); Edge(c,a); if (points.Count != 2) continue;
                var x = Pixel(points[0]); var y = Pixel(points[1]); int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(x,y)));
                for (int n = 0; n <= steps; n++)
                {
                    var q = Vector2.Lerp(x,y,(float)n/steps); int col = Mathf.FloorToInt(q.x), row = Mathf.FloorToInt(q.y);
                    if (col < 0 || row < 0 || col >= Size || row >= Size) continue;
                    int k = row * Size + col;
                    // Do not draw upper-storey walls over unrelated lower floors.
                    if (!float.IsNegativeInfinity(upper[k]) && Mathf.Abs(upper[k] - (height - 1)) < .15f)
                        pixels[k] = new Color32(49,51,57,255);
                }
            }
        }

        [MenuItem("ROM/Dungeon Ciudad del Sol/Cartografia exterior/Generar atlas exterior para revision", false, -100)]
        public static void Build()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling, "Salir de Play y esperar compilación.");
            var setup = EditorSceneManager.GetSceneManagerSetup(); var oldActive = SceneManager.GetActiveScene();
            var scene = SceneManager.GetSceneByPath(ScenePath); bool opened = false; Texture2D texture = null;
            var protectedPaths = new[] { ScenePath, "Assets/Scenes/Dungeon_Isla_del_Sol/NavMesh-NavMesh_Exterior_CiudadDelSol.asset", "Assets/ROM/UI/GameplayHUD/Resources/RiftMinimapAtlas.asset", "Assets/ROM/UI/GameplayHUD/Resources/RiftMinimap.png" };
            var hashes = protectedPaths.ToDictionary(path => path, Hash);
            try
            {
                Progress("Comprobar escena guardada y fuentes existentes", .01f);
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    for(int i=0;i<SceneManager.sceneCount;i++)Require(!SceneManager.GetSceneAt(i).isDirty,"Hay una escena con cambios sin guardar; se canceló sin descartarlos.");
                    scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single); opened = true;
                }
                Require(!scene.isDirty, "La escena tiene cambios sin guardar: se canceló, sin guardar ni descartar nada.");
                Require(SceneManager.sceneCount == 1, "Dejar únicamente Ciudad del Sol cargada para contrastar el NavMesh existente sin datos de otras escenas.");
                SceneManager.SetActiveScene(scene); Physics.SyncTransforms();
                var roots = scene.GetRootGameObjects();
                var city = roots.Single(item => item.name == "Dungeon_Ciudad_del_Sol_Generated");
                var nodes = city.GetComponentsInChildren<Transform>();
                var groups = Sectors.Select(name => nodes.Single(t => t.name == name))
                    .Concat(Routes.Select(id => nodes.Single(t => t.name.StartsWith(id + " - ", StringComparison.Ordinal) && t.name.Contains("ancho util"))))
                    .Append(roots.Single(t => t.name == "Salida_Meridional_Generated").transform).ToArray();
                var surface = roots.SelectMany(r => r.GetComponentsInChildren<NavMeshSurface>()).Single(s => s.name == "NavMesh_Exterior_CiudadDelSol");
                Require(surface.navMeshData && surface.agentTypeID == 0 && surface.isActiveAndEnabled, "Falta la superficie manual existente con sus datos persistentes.");
                string navigationPath = AssetDatabase.GetAssetPath(surface.navMeshData);
                Require(!string.IsNullOrEmpty(navigationPath) && File.Exists(navigationPath), "El NavMeshData manual no es un recurso persistente.");
                if (!hashes.ContainsKey(navigationPath)) hashes.Add(navigationPath, Hash(navigationPath));
                Require(roots.SelectMany(r => r.GetComponentsInChildren<NavMeshSurface>())
                    .Where(s => s.isActiveAndEnabled && s.navMeshData).All(s => s == surface),
                    "Hay otra superficie activa: no se puede atribuir la triangulación global exclusivamente al NavMesh manual.");
                var parts = new List<Part>(); var seen = new HashSet<Collider>();
                for (int g = 0; g < groups.Length; g++) foreach (var col in groups[g].GetComponentsInChildren<Collider>())
                {
                    if (!col.enabled || col.isTrigger || !seen.Add(col)) continue;
                    bool floor = FloorRole(col,groups[g],out bool stair);
                    var part = Geometry(col,g,floor,stair);
                    Require(!floor || part != null, "Collider de piso no soportado: " + AnimationUtility.CalculateTransformPath(col.transform, null));
                    if (part != null) parts.Add(part);
                }
                // Monumental gates, arches and towers are outside sector roots but must remain recognizable.
                var monuments = nodes.Single(t => t.name == "Arquitectura monumental - fuera de los pasos");
                foreach (var col in monuments.GetComponentsInChildren<Collider>()) if (col.enabled && !col.isTrigger && seen.Add(col))
                { var part = Geometry(col,-1,false,false); if (part != null) parts.Add(part); }
                var floors = parts.Where(p => p.Floor).ToArray();
                Require(groups.All((g) => floors.Any(p => p.Group == Array.IndexOf(groups,g))), "Falta geometría de piso en un sector, conexión o entrada.");
                var bounds = floors[0].Bounds; foreach (var part in floors) bounds.Encapsulate(part.Bounds);
                float side = Mathf.Ceil(Mathf.Max(bounds.size.x,bounds.size.z) + 24);
                extent = new Rect(bounds.center.x-side*.5f,bounds.center.z-side*.5f,side,side);
                upper = Enumerable.Repeat(float.NegativeInfinity,Size*Size).ToArray();
                overlappingFloors = new Dictionary<int, List<FloorSample>>();
                navigationGroups = new uint[Size*Size];
                owners = Enumerable.Repeat(-1,Size*Size).ToArray(); walk = new byte[Size*Size];
                var sectionHeights = new HashSet<float>();
                for (int f = 0; f < floors.Length; f++)
                {
                  if (f % 32 == 0) Progress("Proyectar pisos físicos: " + f + "/" + floors.Length, .1f + .3f*f/floors.Length);
                  var p = floors[f];
                  for (int i = 0; i < p.I.Length; i += 3)
                  {
                    var a=p.V[p.I[i]]; var b=p.V[p.I[i+1]]; var c=p.V[p.I[i+2]];
                    if (Vector3.Cross(b-a,c-a).normalized.y < .55f) continue;
                    if (!p.Stair && Mathf.Max(a.y,b.y,c.y)-Mathf.Min(a.y,b.y,c.y)<.01f) sectionHeights.Add(Mathf.Round(a.y*100)/100+1);
                    Triangle(a,b,c,(k,y) => AddFloor(k,y,p.Group));
                  }
                }
                var nav = NavMesh.CalculateTriangulation(); Require(nav.indices.Length > 0,"El NavMesh persistente no está registrado; no se generó un sustituto.");
                for (int i=0;i<nav.indices.Length;i+=3)
                {
                    if (i % 300 == 0) Progress("Contrastar todos los niveles con el NavMesh persistente", .4f + .2f*i/nav.indices.Length);
                    Triangle(nav.vertices[nav.indices[i]],nav.vertices[nav.indices[i+1]],nav.vertices[nav.indices[i+2]],MatchNavigation);
                }
                pixels = new Color32[Size*Size]; int[] navigationCounts = new int[groups.Length];
                var report = new Report { scene=ScenePath,sceneSHA256=hashes[ScenePath],navigationAsset=navigationPath,navigationSHA256=hashes[navigationPath],center=extent.center,worldSize=new Vector2(side,side),floorColliders=floors.Length,obstacleColliders=parts.Count(p=>!p.Floor) };
                for (int k=0;k<pixels.Length;k++)
                {
                    if (float.IsNegativeInfinity(upper[k])) { pixels[k]=new Color32(18,22,32,255); continue; }
                    report.physicalPixels++;
                    if(walk[k]>0) { report.walkingPixels++; for(int g=0;g<groups.Length;g++) if((navigationGroups[k] & (1u<<g))!=0) navigationCounts[g]++; }
                    if(walk[k]==2) report.underpassPixels++;
                    float light=Mathf.Clamp01((upper[k]+2)/42);
                    pixels[k]=walk[k]==1 ? (Color32)Color.Lerp(new Color(.63f,.51f,.31f),new Color(.91f,.81f,.58f),light) : walk[k]==2 ? new Color32(103,170,182,255) : new Color32(74,75,78,255);
                }
                // Outline actual slab edges and steps; never connect pivots or synthesize corridors.
                for(int y=1;y<Size-1;y++)for(int x=1;x<Size-1;x++)
                { int k=y*Size+x; if(walk[k]>0 && (float.IsNegativeInfinity(upper[k-1])||float.IsNegativeInfinity(upper[k+1])||float.IsNegativeInfinity(upper[k-Size])||float.IsNegativeInfinity(upper[k+Size])||Mathf.Abs(upper[k]-upper[k+1])>.18f||Mathf.Abs(upper[k]-upper[k+Size])>.18f))pixels[k]=new Color32(213,194,139,255); }
                var obstacles = parts.Where(p=>!p.Floor).ToArray();
                for(int i=0;i<obstacles.Length;i++)
                { if(i%32==0)Progress("Dibujar secciones arquitectónicas reales",.65f+.2f*i/obstacles.Length); foreach(float height in sectionHeights)Section(obstacles[i],height); }
                report.groups=groups.Select((g,i)=> { var f=floors.Where(p=>p.Group==i).ToArray();var b=f[0].Bounds;foreach(var p in f)b.Encapsulate(p.Bounds);return new GroupReport{name=g.name,pivot=g.position,minimum=b.min,maximum=b.max,floorColliders=f.Length,navigationPixels=navigationCounts[i]}; }).ToArray();
                Require(navigationCounts.All(n=>n>0),"Hay un sector o conexión sin ningún píxel de navegación respaldado; revisar clasificación antes de publicar.");
                Progress("Comprobar integridad antes de escribir recursos nuevos", .9f);
                foreach(var pair in hashes)Require(Hash(pair.Key)==pair.Value,"Cambió un archivo protegido: "+pair.Key);
                Require(!scene.isDirty,"La escena quedó dirty; se canceló la publicación.");
                string folder=OutputRoot+DateTime.Now.ToString("yyyyMMdd_HHmmss")+"_"+Guid.NewGuid().ToString("N").Substring(0,6);Directory.CreateDirectory(folder);
                File.WriteAllText(folder+"/LEEME.txt", "Atlas exterior de revisión, sin integración al HUD. Norte +Z, este +X.\nDorado: piso superior con NavMesh y apoyo físico coincidentes. Cian: paso inferior/intermedio confirmado.\nGris: geometría física sin navegación confirmada; NO representa un camino. Secciones oscuras: arquitectura.\nRevisar los 17 grupos del informe y las superposiciones antes de aprobar. No certifica conectividad ni incluye plantas interiores.\n");
                report.image=folder+"/CiudadDelSol_Exterior.png";report.atlas=folder+"/CiudadDelSol_Exterior_Atlas.asset";
                texture=new Texture2D(Size,Size,TextureFormat.RGB24,false);texture.SetPixels32(pixels);texture.Apply();
                File.WriteAllBytes(report.image,texture.EncodeToPNG());AssetDatabase.ImportAsset(report.image,ImportAssetOptions.ForceSynchronousImport);
                var importer=(TextureImporter)AssetImporter.GetAtPath(report.image);importer.wrapMode=TextureWrapMode.Clamp;importer.mipmapEnabled=true;importer.maxTextureSize=Size;importer.textureCompression=TextureImporterCompression.Compressed;importer.SaveAndReimport();
                var atlas=ScriptableObject.CreateInstance<RiftMinimapAtlas>();atlas.Map=AssetDatabase.LoadAssetAtPath<Texture2D>(report.image);atlas.Center=report.center;atlas.WorldSize=report.worldSize;
                AssetDatabase.CreateAsset(atlas,report.atlas);AssetDatabase.SaveAssetIfDirty(atlas);
                File.WriteAllText(folder+"/Informe_Exterior.json",JsonUtility.ToJson(report,true));AssetDatabase.ImportAsset(folder+"/Informe_Exterior.json");
                foreach(var pair in hashes)Require(Hash(pair.Key)==pair.Value,"Cambió un archivo protegido: "+pair.Key);
                Require(!scene.isDirty,"La escena quedó dirty; no se guardará.");
                Debug.Log("CIUDAD_ATLAS_EXTERIOR_OK "+report.image+"; 4096x4096; centro="+report.center+"; cobertura="+report.worldSize+"; grupos="+groups.Length+"; pasos inferiores/intermedios="+report.underpassPixels+" píxeles; no integrado al HUD. Gris no significa transitable. Revisar Informe_Exterior.json y LEEME.txt; no certifica conectividad.");
            }
            finally
            {
                if(texture)Object.DestroyImmediate(texture);
                upper=null;owners=null;walk=null;pixels=null;navigationGroups=null;overlappingFloors=null;
                if(opened)EditorSceneManager.RestoreSceneManagerSetup(setup);
                else if(oldActive.IsValid()&&oldActive.isLoaded)SceneManager.SetActiveScene(oldActive);
                EditorUtility.ClearProgressBar();
                foreach(var pair in hashes)if(Hash(pair.Key)!=pair.Value)Debug.LogError("Cartografía: archivo protegido cambió: "+pair.Key);
            }
        }
    }
}
