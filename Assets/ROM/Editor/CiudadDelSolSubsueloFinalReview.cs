using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ROM.EditorTools
{
    public static partial class CiudadDelSolNaveJardinesPreview
    {
        const string ReviewFolder = "Backups/CiudadDelSol/RevisionSubsuelo";

        static readonly Sector[] RestrictedAlternatives =
        {
            Link("O1 variante", WestRoot, -150, -78, -60, false, 981306585),
            Link("O2 variante", WestRoot, -90, -84, -66, false, 1752911320),
            Link("O7 variante", WestRoot, -66, 30, 48, false, 786692939),
            Link("N2 variante", NorthRoot, -30, 108, 126, false, 1451771354),
            Link("N4 variante", NorthRoot, 42, 156, 174, false, 1763290661),
            Link("S1 variante", SouthRoot, 138, -120, -102, false, 1522184970),
            Link("E1 variante", EastRoot, 198, -48, -18, false, 983808960, 2005529955),
            Link("E5 variante", EastRoot, 126, 156, 174, false, 856710452),
        };

        public static void ReviewExpansionInBatch()
        {
            var source = EditorSceneManager.OpenScene(Original, OpenSceneMode.Single);
            CheckExpansionSource();
            foreach (var candidate in RestrictedAlternatives)
            {
                string rejection = null;
                foreach (float crown in new[] { .5f, -1.15f, -2.9f })
                {
                    try
                    {
                        Validate(source, WithCrown(candidate, crown));
                        rejection = null;
                        Debug.Log($"REVISION_ALTERNATIVA_OK {candidate.Name}; clave Y={crown}");
                        break;
                    }
                    catch (InvalidOperationException error) { rejection = error.Message; }
                }
                if (rejection != null) Debug.Log($"REVISION_ALTERNATIVA_RESTRINGIDA {candidate.Name}: {rejection}");
            }

            Directory.CreateDirectory(ReviewFolder);
            var joint = EditorSceneManager.NewPreviewScene();
            var resources = new List<Object>();
            try
            {
                var plans = new List<MountPlan>();
                BuildExpansionJoint(source, joint, resources, plans);
                VerifyReviewPlan(source, plans);
                if (!SystemInfo.graphicsDeviceType.Equals(UnityEngine.Rendering.GraphicsDeviceType.Null))
                    CaptureExpansion(joint, plans);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(joint);
                foreach (var resource in resources) if (resource) Object.DestroyImmediate(resource);
            }
            CheckExpansionSource();
        }

        static void VerifyReviewPlan(Scene source, List<MountPlan> plans)
        {
            bool Overlaps(Sector a, Sector b) =>
                a.West < b.East && a.East > b.West && a.South < b.North && a.North > b.South;
            Require(plans.Count == 16 && plans.Sum(plan => plan.Targets.Length) == 381,
                "El plan no coincide con los 16 sectores y 381 recortes revisados.");
            Require(source.GetRootGameObjects().Single(g => g.name == MountedRoot).transform.childCount == 4,
                "No se encontró el conjunto de cuatro salas integrado.");
            var lines = new List<string> { "SHA256 de origen\t" + ExpansionSourceHash,
                "Sector\tRaíz\tID de objeto\tEstado original\tEstado propuesto" };
            for (int i = 0; i < plans.Count; i++)
            {
                var plan = plans[i];
                Require(!Sectors.Any(protectedHall => Overlaps(plan.Design, protectedHall)) &&
                    !plans.Take(i).Any(neighbor => Overlaps(plan.Design, neighbor.Design)),
                    "Huella superpuesta: " + plan.Design.Name);
                Require(plan.Hall.GetComponentsInChildren<MeshFilter>().Length == 2,
                    "La bóveda requiere dos mallas: " + plan.Design.Name);
                foreach (var id in plan.Targets)
                {
                    var piece = UnityEditor.GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as GameObject;
                    Require(piece && piece.scene == source && piece.activeSelf && piece.transform.root.name == plan.Design.Root &&
                        (piece.transform.parent.name.StartsWith("02 - Bovedas generales", StringComparison.Ordinal) ||
                         piece.transform.parent.name.StartsWith("03 - Vigas", StringComparison.Ordinal) ||
                         piece.transform.parent.name.StartsWith("04 - Columnas monumentales", StringComparison.Ordinal)),
                        "Recorte fuera del alcance: " + id);
                    lines.Add($"{plan.Design.Name}\t{plan.Design.Root}\t{id}\tactivo\tinactivo");
                }
            }
            File.WriteAllLines(Path.Combine(ReviewFolder, "Montaje-reversible.tsv"), lines);
            Debug.Log("REVISION_MONTAJE_OK: 16 sectores, 32 mallas y 381 recortes reversibles.");
        }

        static void CaptureExpansion(Scene joint, List<MountPlan> plans, string output = ReviewFolder)
        {
            const int width = 640, height = 360;
            var cameraObject = new GameObject("Cámara de inspección temporal");
            SceneManager.MoveGameObjectToScene(cameraObject, joint);
            var camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.scene = joint;
            camera.fieldOfView = 72;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.25f, .33f, .44f);
            var light = cameraObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 2;
            light.shadows = LightShadows.None;
            var target = new RenderTexture(width, height, 24);
            var pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                for (int i = 0; i < plans.Count; i++)
                {
                    var design = plans[i].Design;
                    for (int direction = 0; direction < 2; direction++)
                    {
                        bool north = direction == 0;
                        camera.transform.SetPositionAndRotation(
                            new Vector3((design.West + design.East) * .5f, Floor + 1.9f,
                                north ? design.South + 2 : design.North - 2),
                            Quaternion.LookRotation(new Vector3(.13f, .12f, north ? 1 : -1)));
                        camera.Render();
                        RenderTexture.active = target;
                        pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                        File.WriteAllBytes(Path.Combine(output, $"{i + 1:00}-{direction + 1}.png"),
                            pixels.EncodeToPNG());
                    }
                }
                Debug.Log($"REVISION_CAPTURAS_OK: {plans.Count * 2} vistas a 1,90 m con el contexto vecino.");
            }
            finally
            {
                RenderTexture.active = previous;
                camera.targetTexture = null;
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(pixels);
                Object.DestroyImmediate(cameraObject);
            }
        }
    }
}
