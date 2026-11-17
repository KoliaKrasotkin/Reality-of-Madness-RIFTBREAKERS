using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace ROM.EditorTools
{
    internal enum NavIssueKind { Partial, Support, Corridor, Island, Coverage }

    internal sealed class CiudadDelSolNavMeshDiagnostics : EditorWindow
    {
        sealed class Issue
        {
            internal Vector3 Point;
            internal Vector3? Last, Destination;
            internal string Detail;
        }

        sealed class Cluster
        {
            internal NavIssueKind Kind;
            internal int Priority;
            internal string Group;
            internal readonly List<Issue> Issues = new List<Issue>();
            internal Vector3 Point => Issues[0].Point;
        }

        static readonly List<Cluster> Clusters = new List<Cluster>();
        static Scene diagnosedScene;
        ListView list;
        ScrollView details;
        Label summary;
        Cluster selected;
        int issueIndex;
        Collider[] sceneColliders;

        [MenuItem("ROM/Dungeon Ciudad del Sol/NavMesh exterior/5. Inspeccionar cortes en Scene View")]
        static void Open() => GetWindow<CiudadDelSolNavMeshDiagnostics>("Cortes de NavMesh");

        internal static void Begin(Scene scene)
        {
            diagnosedScene = scene;
            Clusters.Clear();
        }

        internal static void Add(string group, int priority, NavIssueKind kind, Vector3 point, string detail,
            Vector3? last, Vector3? destination)
        {
            var cluster = Clusters.FirstOrDefault(item => item.Kind == kind && item.Group == group &&
                Mathf.Abs(item.Point.y - point.y) <= .80f &&
                new Vector2(item.Point.x - point.x, item.Point.z - point.z).sqrMagnitude <= 4f);
            if (cluster == null)
            {
                cluster = new Cluster { Kind = kind, Priority = priority, Group = group };
                Clusters.Add(cluster);
            }
            cluster.Issues.Add(new Issue { Point = point, Detail = detail, Last = last, Destination = destination });
        }

        internal static void Refresh()
        {
            foreach (var window in Resources.FindObjectsOfTypeAll<CiudadDelSolNavMeshDiagnostics>()) window.Populate();
            SceneView.RepaintAll();
        }

        void OnEnable()
        {
            SceneView.duringSceneGui += DrawScene;
            EditorSceneEvents(true);
        }

        void OnDisable()
        {
            SceneView.duringSceneGui -= DrawScene;
            EditorSceneEvents(false);
            SceneView.RepaintAll();
        }

        void EditorSceneEvents(bool subscribe)
        {
            if (subscribe) UnityEditor.SceneManagement.EditorSceneManager.activeSceneChangedInEditMode += OnSceneChanged;
            else UnityEditor.SceneManagement.EditorSceneManager.activeSceneChangedInEditMode -= OnSceneChanged;
        }

        void OnSceneChanged(Scene previous, Scene next) => Populate();

        public void CreateGUI()
        {
            rootVisualElement.Clear();
            summary = new Label { style = { whiteSpace = WhiteSpace.Normal } };
            list = new ListView
            {
                fixedItemHeight = 24,
                makeItem = () => new Label(),
                bindItem = (element, index) =>
                {
                    var item = (Cluster)list.itemsSource[index];
                    var label = (Label)element;
                    label.text = $"{item.Group} · {KindName(item.Kind)} · {item.Issues.Count}";
                    label.tooltip = $"{label.text} · {item.Point:F2}";
                    label.style.color = ColorFor(item.Kind);
                },
                style = { flexGrow = 1, minHeight = 120 }
            };
            list.selectionChanged += items =>
            {
                selected = items.OfType<Cluster>().FirstOrDefault();
                issueIndex = 0;
                ShowIssue();
            };
            details = new ScrollView { style = { flexGrow = 1, minHeight = 120 } };
            rootVisualElement.Add(summary);
            rootVisualElement.Add(list);
            rootVisualElement.Add(details);
            Populate();
        }

        static bool HasCurrentScene => diagnosedScene.IsValid() && diagnosedScene.isLoaded &&
            SceneManager.GetActiveScene() == diagnosedScene && !EditorApplication.isPlayingOrWillChangePlaymode;

        void Populate()
        {
            if (list == null) return;
            sceneColliders = null;
            selected = null;
            list.ClearSelection();
            list.itemsSource = HasCurrentScene
                ? Clusters.OrderBy(item => item.Priority).ThenBy(item => item.Group)
                    .ThenByDescending(item => item.Issues.Count).ToList()
                : new List<Cluster>();
            list.Rebuild();
            summary.text = list.itemsSource.Count > 0
                ? $"{Clusters.Sum(item => item.Issues.Count)} observaciones en {Clusters.Count} grupos. Primero los recorridos obligatorios."
                : "Sin cortes registrados para esta escena. Ejecutá «1. Hornear y validar candidata».";
            ShowIssue();
        }

        void ShowIssue()
        {
            if (details == null) return;
            details.Clear();
            SceneView.RepaintAll();
            if (selected == null || !HasCurrentScene) return;
            var issue = selected.Issues[issueIndex];
            details.Add(new Label($"Caso {issueIndex + 1}/{selected.Issues.Count} · {KindName(selected.Kind)}"));
            details.Add(new Label(issue.Detail) { style = { whiteSpace = WhiteSpace.Normal } });
            var controls = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            var previous = new Button(() => { issueIndex--; ShowIssue(); }) { text = "Anterior" };
            var next = new Button(() => { issueIndex++; ShowIssue(); }) { text = "Siguiente" };
            previous.SetEnabled(issueIndex > 0);
            next.SetEnabled(issueIndex + 1 < selected.Issues.Count);
            controls.Add(previous);
            controls.Add(next);
            controls.Add(new Button(() => Focus(issue.Point)) { text = "Enfocar problema" });
            details.Add(controls);
            if (issue.Last.HasValue)
                details.Add(new Button(() => Focus(issue.Last.Value)) { text = $"Último punto disponible {issue.Last.Value:F3}" });
            if (issue.Destination.HasValue)
                details.Add(new Button(() => Focus(issue.Destination.Value)) { text = $"Destino {issue.Destination.Value:F3}" });
            ShowColliders(issue.Point, "Colliders cerca del problema");
            if (issue.Last.HasValue && Vector3.Distance(issue.Point, issue.Last.Value) > 4f)
                ShowColliders(issue.Last.Value, "Colliders cerca del último punto");
            if (issue.Destination.HasValue && Vector3.Distance(issue.Point, issue.Destination.Value) > 4f)
                ShowColliders(issue.Destination.Value, "Colliders cerca del destino");
        }

        void ShowColliders(Vector3 point, string heading)
        {
            details.Add(new Label(heading));
            if (sceneColliders == null)
                sceneColliders = diagnosedScene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Collider>()).ToArray();
            var nearby = sceneColliders.Where(item => item && item.enabled)
                .Select(item => new { Collider = item, Distance = item.bounds.SqrDistance(point) })
                .Where(item => item.Distance <= 16f).OrderBy(item => item.Distance).Take(10).ToArray();
            if (nearby.Length == 0) details.Add(new Label("Sin colliders activos a 4 m."));
            foreach (var item in nearby)
            {
                var collider = item.Collider;
                string asset = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(collider.gameObject);
                string hierarchy = Hierarchy(collider.transform);
                details.Add(new Button(() =>
                {
                    if (!collider) return;
                    Selection.activeGameObject = collider.gameObject;
                    EditorGUIUtility.PingObject(collider.gameObject);
                }) { text = $"{collider.GetType().Name} · {Mathf.Sqrt(item.Distance):F2} m de su caja · {collider.name}" });
                details.Add(new Label(hierarchy + "\n" + (string.IsNullOrEmpty(asset) ? "Objeto de escena" : asset))
                    { style = { whiteSpace = WhiteSpace.Normal } });
            }
        }

        static string Hierarchy(Transform node)
        {
            var names = new Stack<string>();
            for (; node; node = node.parent) names.Push(node.name);
            return string.Join("/", names);
        }

        static void Focus(Vector3 point)
        {
            var view = SceneView.lastActiveSceneView ?? GetWindow<SceneView>();
            view.Frame(new Bounds(point, Vector3.one * 6f), false);
            view.Repaint();
        }

        void DrawScene(SceneView view)
        {
            if (!HasCurrentScene || Event.current.type != EventType.Repaint) return;
            var previousColor = Handles.color;
            try
            {
                foreach (var item in Clusters)
                {
                    Handles.color = ColorFor(item.Kind);
                    Handles.DrawWireDisc(item.Point, Vector3.up, item.Priority == 0 ? .65f : .35f);
                }
                if (selected == null) return;
                var issue = selected.Issues[issueIndex];
                Handles.color = ColorFor(selected.Kind);
                Handles.DrawWireDisc(issue.Point, Vector3.up, 1f);
                Handles.Label(issue.Point + Vector3.up * .4f, KindName(selected.Kind));
                if (issue.Last.HasValue)
                {
                    Handles.color = Color.cyan;
                    Handles.DrawWireDisc(issue.Last.Value, Vector3.up, .45f);
                    Handles.Label(issue.Last.Value + Vector3.up * .7f, "Último punto disponible");
                }
                if (issue.Destination.HasValue)
                {
                    Handles.color = Color.yellow;
                    Handles.DrawDottedLine(issue.Last ?? issue.Point, issue.Destination.Value, 5f);
                    Handles.DrawWireDisc(issue.Destination.Value, Vector3.up, .45f);
                    Handles.Label(issue.Destination.Value + Vector3.up * .4f, "Destino");
                }
            }
            finally { Handles.color = previousColor; }
        }

        static string KindName(NavIssueKind kind) => kind switch
        {
            NavIssueKind.Partial => "Ruta incompleta",
            NavIssueKind.Support => "Apoyo físico",
            NavIssueKind.Corridor => "Corredor",
            NavIssueKind.Island => "Triángulo desconectado",
            _ => "Cobertura"
        };

        static Color ColorFor(NavIssueKind kind) => kind switch
        {
            NavIssueKind.Partial => new Color(1f, .3f, .28f),
            NavIssueKind.Support => new Color(1f, .65f, .2f),
            NavIssueKind.Corridor => new Color(.4f, .83f, 1f),
            NavIssueKind.Island => new Color(.85f, .5f, 1f),
            _ => new Color(.7f, 1f, .4f)
        };
    }
}
