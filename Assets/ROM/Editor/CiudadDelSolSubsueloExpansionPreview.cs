using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ROM.EditorTools
{
    public static partial class CiudadDelSolNaveJardinesPreview
    {
        const string ExpansionMenu = "ROM/Dungeon Ciudad del Sol/Rediseño subsuelo/";
        const string ExpansionSourceHash = "59448B459F7DEAB271CC61D1B228F495FF12BA8C21B05493386D949B27EF752B";

        static readonly Sector PorticoSouthwest = new Sector("Nave suroeste bajo Pórtico Solar",
            "Subsuelo_Ala_Occidental_Etapa03", -150, -132, -120, -84, -2.9f,
            new[] { -114f, -102f, -90f }, new ulong[] { 255738547, 1500714496, 1264856402 },
            profilePower: 6, minimumPeak: 13);

        static readonly Sector EastGallery = new Sector("Galería del borde oriental",
            "Subsuelo_Ala_Este_Cierre_Etapa05", 204, 222, 114, 144, .6f,
            new[] { 120f, 138f }, Array.Empty<ulong>(), eastBearing: true);

        static readonly Sector GardensNorthLink = new Sector("Transición norte de Jardines Dorados",
            "Subsuelo_Ala_Occidental_Etapa03", -90, -72, 30, 60, -1.15f,
            new[] { 36f, 54f }, new ulong[] { 314526179, 1005028357 },
            profilePower: 4, minimumPeak: 14.7f);

        [MenuItem(ExpansionMenu + "Candidatos/1. Suroeste bajo Pórtico Solar")]
        static void PreviewPorticoSouthwest() => OpenExpansion(PorticoSouthwest);

        [MenuItem(ExpansionMenu + "Candidatos/2. Borde oriental")]
        static void PreviewEastGallery() => OpenExpansion(EastGallery);

        [MenuItem(ExpansionMenu + "Candidatos/3. Transición norte de Jardines")]
        static void PreviewGardensNorthLink() => OpenExpansion(GardensNorthLink);

        [MenuItem(ExpansionMenu + "Candidatos/4. Vista a 1,90 m")]
        static void ExpansionEyeLevel() => Warrior();

        [MenuItem(ExpansionMenu + "Candidatos/5. Cerrar copia")]
        static void CloseExpansion() => Close();

        [MenuItem(ExpansionMenu + "6. Abrir conjunto temporal")]
        static void OpenExpansionJoint()
        {
            if (StageUtility.GetCurrentStage() is ExpansionStage) return;
            Close();
            CheckExpansionSource();
            Require(StageUtility.GetCurrentStage() == StageUtility.GetMainStage() &&
                !EditorApplication.isPlayingOrWillChangePlaymode, "Abrí la escena principal fuera de Play.");
            var stage = ScriptableObject.CreateInstance<ExpansionStage>();
            stage.Source = SceneManager.GetActiveScene();
            StageUtility.GoToStage(stage, true);
        }

        [MenuItem(ExpansionMenu + "7. Cerrar conjunto temporal")]
        static void CloseExpansionJoint()
        {
            if (StageUtility.GetCurrentStage() is ExpansionStage) StageUtility.GoToMainStage();
        }

        [MenuItem(ExpansionMenu + "8. Siguiente sector a 1,90 m")]
        static void FrameNextExpansionSector()
        {
            if (StageUtility.GetCurrentStage() is ExpansionStage stage) stage.FrameNext();
        }

        sealed class ExpansionStage : PreviewSceneStage
        {
            public Scene Source;
            public Sector[] Designs;
            public string SourceHash = ExpansionSourceHash;
            public bool ClearColumnGrid;
            readonly List<UnityEngine.Object> resources = new List<UnityEngine.Object>();
            readonly List<MountPlan> previewPlans = new List<MountPlan>();
            readonly List<GameObject> previewCuts = new List<GameObject>();
            readonly List<GameObject> gridCuts = new List<GameObject>();
            int nextSector;
            bool showBefore;

            public void ToggleComparison()
            {
                showBefore = !showBefore;
                if (ClearColumnGrid)
                {
                    foreach (var piece in gridCuts) piece.SetActive(showBefore);
                    SceneView.RepaintAll();
                    return;
                }
                foreach (var plan in previewPlans) plan.Hall.SetActive(!showBefore);
                foreach (var piece in previewCuts) piece.SetActive(showBefore);
                SceneView.RepaintAll();
            }

            public void FrameNext()
            {
                var designs = Designs ?? ExpansionDesigns();
                var design = designs[nextSector++ % designs.Length];
                var view = SceneView.lastActiveSceneView ?? EditorWindow.GetWindow<SceneView>();
                view.LookAt(new Vector3((design.West + design.East) * .5f, Floor + 1.9f, design.South + 10),
                    Quaternion.LookRotation(Vector3.forward), 4, false, true);
                view.ShowNotification(new GUIContent(design.Name));
            }

            protected override GUIContent CreateHeaderContent() =>
                new GUIContent("Ampliación del subsuelo · copia temporal");

            protected override bool OnOpenStage()
            {
                if (!base.OnOpenStage()) return false;
                try
                {
                    BuildExpansionJoint(Source, scene, resources, previewPlans, Designs, SourceHash, previewCuts);
                    if (ClearColumnGrid) ClearRepetitiveColumns(Source, scene, previewPlans, gridCuts);
                    EditorApplication.delayCall += () =>
                    {
                        if (!this || StageUtility.GetCurrentStage() != this) return;
                        var view = SceneView.lastActiveSceneView ?? EditorWindow.GetWindow<SceneView>();
                        view.LookAt(new Vector3(39, -12, 25),
                            Quaternion.LookRotation(new Vector3(.22f, -.86f, .45f)), 265, true, true);
                    };
                    return true;
                }
                catch (Exception error) { Debug.LogException(error); return false; }
            }

            protected override void OnCloseStage()
            {
                base.OnCloseStage();
                foreach (var resource in resources) if (resource) UnityEngine.Object.DestroyImmediate(resource);
            }
        }

        static void BuildExpansionJoint(Scene source, Scene joint, List<UnityEngine.Object> resources,
            List<MountPlan> reviewPlans = null, IEnumerable<Sector> designs = null,
            string sourceHash = ExpansionSourceHash, List<GameObject> deactivatedCopies = null, bool allowRejected = false,
            int protectedCount = 0)
        {
            var context = new GameObject("Subsuelo definitivo - contexto temporal");
            SceneManager.MoveGameObjectToScene(context, joint);
            context.SetActive(false);
            var copies = new Dictionary<GameObject, Transform>();
            foreach (var root in source.GetRootGameObjects().Where(root =>
                root.name.StartsWith("Subsuelo_", StringComparison.Ordinal) ||
                root.name == "Nucleo_Monumental_Conjunto"))
            {
                var clone = UnityEngine.Object.Instantiate(root, context.transform);
                clone.name = root.name;
                foreach (var behaviour in clone.GetComponentsInChildren<MonoBehaviour>(true))
                    if (behaviour) behaviour.enabled = false;
                copies.Add(root, clone.transform);
            }
            context.SetActive(true);

            var mounted = new List<MountPlan>();
            var targets = new HashSet<GlobalObjectId>();
            var cuts = new Dictionary<MountPlan, List<GameObject>>();
            foreach (var design in designs ?? ExpansionDesigns())
            {
                var scratch = EditorSceneManager.NewPreviewScene();
                try
                {
                    var plans = new List<MountPlan>();
                    Build(source, scratch, resources, design, plans);
                    var plan = plans.Single();
                    cuts.Add(plan, new List<GameObject>());
                    foreach (var id in plan.Targets)
                    {
                        Require(targets.Add(id), "Los recortes del conjunto se superponen.");
                        var original = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as GameObject;
                        Require(original && original.scene == source, "Pieza original ausente: " + id);
                        var piece = original.transform;
                        var copy = copies[piece.root.gameObject]
                            .GetChild(piece.parent.GetSiblingIndex()).GetChild(piece.GetSiblingIndex());
                        Require(copy.name == piece.name && copy.gameObject.activeInHierarchy &&
                            Vector3.Distance(copy.position, piece.position) < .001f &&
                            Quaternion.Angle(copy.rotation, piece.rotation) < .01f &&
                            Vector3.Distance(copy.lossyScale, piece.lossyScale) < .001f,
                            "Identidad de copia distinta: " + id);
                        copy.gameObject.SetActive(false);
                        cuts[plan].Add(copy.gameObject);
                    }
                    plan.Hall.transform.SetParent(null, true);
                    SceneManager.MoveGameObjectToScene(plan.Hall, joint);
                    plan.Hall.transform.SetParent(context.transform, true);
                    mounted.Add(plan);
                }
                finally { EditorSceneManager.ClosePreviewScene(scratch); }
            }
            for (int i = 0; i < mounted.Count; i++)
            {
                var plan = mounted[i];
                try { ValidateMounted(plan.Hall, plan.Design, plan.EndCapitals); }
                catch (InvalidOperationException error) when (allowRejected && i >= protectedCount)
                {
                    plan.Hall.SetActive(false);
                    foreach (var piece in cuts[plan]) piece.SetActive(true);
                    foreach (var id in plan.Targets) targets.Remove(id);
                    mounted.RemoveAt(i);
                    Debug.Log($"SUBSUELO_ABIERTO_RECHAZADO_CONJUNTO {plan.Design.Name}: {error.Message}");
                    i = -1;
                }
            }
            deactivatedCopies?.AddRange(mounted.SelectMany(plan => cuts[plan]));
            reviewPlans?.AddRange(mounted);
            Require(!source.isDirty && Hash() == sourceHash, "La fuente cambió durante la composición.");
            Debug.Log($"SUBSUELO_AMPLIACION_CONJUNTA_OK: {mounted.Count} salas, {targets.Count} piezas inactivas únicamente en la copia.");
        }

        static void OpenExpansion(Sector design)
        {
            CheckExpansionSource();
            Open(design);
        }

        static Sector[] ExpansionDesigns() =>
            new[] { PorticoSouthwest, GardensNorthLink, EastGallery }.Concat(ConnectionDesigns()).ToArray();

        public static void ValidateExpansionInBatch()
        {
            var source = EditorSceneManager.OpenScene(Original, OpenSceneMode.Single);
            CheckExpansionSource();
            var joint = EditorSceneManager.NewPreviewScene();
            var resources = new List<UnityEngine.Object>();
            try { BuildExpansionJoint(source, joint, resources); }
            finally
            {
                EditorSceneManager.ClosePreviewScene(joint);
                foreach (var resource in resources) if (resource) UnityEngine.Object.DestroyImmediate(resource);
            }
        }

        static void CheckExpansionSource() =>
            Require(SceneManager.GetActiveScene().path == Original &&
                !SceneManager.GetActiveScene().isDirty && Hash() == ExpansionSourceHash,
                "La ampliación requiere la escena definitiva limpia de las cuatro salas integradas.");
    }
}
