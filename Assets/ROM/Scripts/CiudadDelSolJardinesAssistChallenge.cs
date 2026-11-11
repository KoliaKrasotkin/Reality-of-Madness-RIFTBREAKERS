using System;
using System.Collections.Generic;
using Mirror;
using MultiFPS.Gameplay;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ROM.Gameplay
{
    public sealed class CiudadDelSolJardinesAssistChallenge : MonoBehaviour
    {
        const float PlateRadius = .60f;
        const double HoldSeconds = 2;
        const double NavigationLimit = 15;
        static readonly Color ArmedColor = new Color(.85f, .50f, .12f);
        static readonly Color WaitingColor = new Color(.38f, .32f, .88f);
        public readonly struct Post
        {
            public readonly string Id;
            public readonly Vector3 Position;
            public readonly bool AllowsAssistant;

            public Post(string id, Vector3 position, bool allowsAssistant)
            {
                Id = id;
                Position = position;
                AllowsAssistant = allowsAssistant;
            }
        }

        sealed class PostState
        {
            public Post Definition;
            public Renderer Plate;
            public PlayerInstance Human;
            public AssistantReservation Reservation;
        }

        sealed class AssistantReservation
        {
            public GameObject Npc;
            public CiudadDelSolJardinesAssistNavigation Navigation;
            public double DispatchAt;
        }

        Scene scene;
        Vector3 entry;
        Bounds court;
        PostState[] posts;
        PostState startPost;
        readonly HashSet<PlayerInstance> assignedHumans = new HashSet<PlayerInstance>();
        bool initialized, active, waitForExit, completed;
        double holdSince = -1;

        public void Initialize(IReadOnlyList<Post> definitions, string startId, Vector3 entry, Bounds court)
        {
            Check(NetworkServer.active && !initialized, "El desafío debe inicializarse una sola vez en el servidor.");
            Check(definitions != null && definitions.Count >= 1 && definitions.Count <= 4,
                "El desafío requiere de uno a cuatro puestos.");
            scene = gameObject.scene;
            this.entry = entry;
            this.court = court;

            var ids = new HashSet<string>(StringComparer.Ordinal);
            posts = new PostState[definitions.Count];
            for (int i = 0; i < definitions.Count; i++)
            {
                var definition = definitions[i];
                Check(!string.IsNullOrWhiteSpace(definition.Id) && ids.Add(definition.Id) && court.Contains(definition.Position),
                    "Identidad duplicada, vacía o posición inválida en el puesto " + definition.Id + ".");
                var post = posts[i] = new PostState { Definition = definition };
                if (definition.Id == startId) startPost = post;
            }
            Check(startPost != null && !startPost.Definition.AllowsAssistant,
                "El puesto inicial debe existir y ser exclusivo para humanos.");

            foreach (var post in posts)
            {
                var definition = post.Definition;
                post.Plate = MakeMarker(PrimitiveType.Cylinder, definition.Id, transform,
                    definition.Position, new Vector3(1.2f, .025f, 1.2f), PostColor(post));
            }
            initialized = true;
            Debug.Log($"Jardines Dorados. {posts.Length} placas preparadas. Pisá la placa dorada en {startPost.Definition.Position:F3}.");
        }

        Color PostColor(PostState post) => post == startPost ? ArmedColor : WaitingColor;

        void Update()
        {
            if (!initialized) return;
            if (!NetworkServer.active || SceneManager.GetActiveScene() != scene)
            {
                CancelTrial();
                return;
            }
            if (completed) return;
            try { Evaluate(); }
            catch (Exception error)
            {
                AbortAttempt();
                Debug.LogError("Jardines Dorados. Intento cancelado. " + error.Message);
            }
        }

        public void CancelTrial()
        {
            enabled = false;
            Destroy(gameObject);
        }

        void OnDisable() => ClearPosts();

        static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }


        void Evaluate()
        {
            assignedHumans.Clear();
            var primary = HumanOn(startPost.Definition.Position);
            if (!primary)
            {
                if (active)
                {
                    AbortAttempt();
                    Debug.Log("Jardines Dorados. Placa inicial liberada. Asistencia retirada.");
                }
                waitForExit = false;
                return;
            }
            if (waitForExit) return;
            if (!active)
            {
                active = true;
                Debug.Log("Jardines Dorados. Intento iniciado.");
            }

            SetHuman(startPost, primary);
            foreach (var post in posts)
            {
                if (post != startPost) SetHuman(post, HumanOn(post.Definition.Position));
            }

            double now = Time.timeAsDouble;
            bool allOccupied = true;
            foreach (var post in posts)
            {
                if (!EvaluateOccupancy(post, now)) allOccupied = false;
            }

            if (!allOccupied) { holdSince = -1; return; }
            if (holdSince < 0)
            {
                holdSince = now;
                Debug.Log("Jardines Dorados. Todas las placas ocupadas. Mantener 2 segundos.");
            }
            if (now - holdSince < HoldSeconds) return;
            completed = true;
            active = false;
            ClearPosts();
            foreach (var post in posts) Paint(post.Plate, Color.green);
            Debug.Log("Jardines Dorados. ÉXITO: todas las placas ocupadas durante 2 segundos. Asistencia retirada.");
        }

        void SetHuman(PostState post, PlayerInstance human)
        {
            if (!ReferenceEquals(post.Human, human)) holdSince = -1;
            post.Human = human;
            if (!human) return;
            assignedHumans.Add(human);
            RetireAssistant(post);
        }

        bool EvaluateOccupancy(PostState post, double now)
        {
            if (post.Human) return true;
            if (!post.Definition.AllowsAssistant) return false;
            if (post.Reservation == null)
            {
                AssignAssistant(post, now);
                return false;
            }

            var reservation = post.Reservation;
            Check(reservation.Npc && reservation.Navigation.IsNavigatingSafely,
                $"El asistente de {post.Definition.Id} salió del NavMesh o del patio.");
            bool arrived = reservation.Navigation.Arrived(post.Definition.Position);
            Check(arrived || (now - reservation.DispatchAt < NavigationLimit && reservation.Navigation.HasValidPath),
                $"El asistente de {post.Definition.Id} no tiene una ruta completa o no llegó a tiempo.");
            return arrived;
        }

        PlayerInstance HumanOn(Vector3 point)
        {
            foreach (var connection in NetworkServer.connections.Values)
            {
                if (connection == null || !connection.isReady || !connection.identity) continue;
                var player = connection.identity.GetComponent<PlayerInstance>();
                if (!player || assignedHumans.Contains(player) || player.BOT) continue;
                var actor = player.CurrentlyControlledCharacter;
                if (!actor || !actor.Health || actor.Health.CurrentHealth <= 0 || actor.connectionToClient != connection ||
                    actor.gameObject.scene != scene) continue;
                var offset = actor.transform.position - point;
                if (Mathf.Abs(offset.y) <= .25f && offset.x * offset.x + offset.z * offset.z <= PlateRadius * PlateRadius)
                    return player;
            }
            return null;
        }

        void AssignAssistant(PostState post, double now)
        {
            var reservation = post.Reservation = new AssistantReservation { DispatchAt = now };
            var npc = reservation.Npc = new GameObject("TEMP NPC asistente · " + post.Definition.Id);
            npc.hideFlags = HideFlags.DontSave;
            SceneManager.MoveGameObjectToScene(npc, scene);
            npc.SetActive(false);
            npc.transform.position = entry;
            npc.AddComponent<NetworkIdentity>();
            MakeMarker(PrimitiveType.Capsule, "Asistente violeta", npc.transform,
                entry + Vector3.up * .65f, new Vector3(.55f, .65f, .55f), WaitingColor);
            reservation.Navigation = new CiudadDelSolJardinesAssistNavigation(npc, court);
            npc.SetActive(true);
            Check(reservation.Navigation.Assign(entry, post.Definition.Position),
                $"No hay ruta completa dentro del patio hasta {post.Definition.Id}.");
            NetworkServer.Spawn(npc);
            Check(npc.GetComponent<NetworkIdentity>().netId != 0, "Mirror no registró al asistente temporal.");
            Debug.Log($"Jardines Dorados. Un asistente asignado a {post.Definition.Id} desde {entry:F3}.");
        }

        static void RetireAssistant(PostState post)
        {
            var npc = post.Reservation?.Npc;
            post.Reservation = null;
            if (!npc) return;
            var identity = npc.GetComponent<NetworkIdentity>();
            if (NetworkServer.active && identity && identity.netId != 0) NetworkServer.Destroy(npc);
            else Object.Destroy(npc);
        }

        void ClearPosts()
        {
            assignedHumans.Clear();
            if (posts == null) return;
            foreach (var post in posts)
            {
                if (post == null) continue;
                post.Human = null;
                RetireAssistant(post);
            }
        }

        void AbortAttempt()
        {
            active = false;
            holdSince = -1;
            waitForExit = true;
            ClearPosts();
        }

        public void ResetAttempt()
        {
            if (!initialized || !isActiveAndEnabled || !NetworkServer.active) return;
            AbortAttempt();
            completed = false;
            foreach (var post in posts) Paint(post.Plate, PostColor(post));
            Debug.Log("Jardines Dorados. Intento reiniciado. Salí de la placa dorada y volvé a pisarla.");
        }

        static Renderer MakeMarker(PrimitiveType shape, string name, Transform parent,
            Vector3 position, Vector3 scale, Color color)
        {
            var marker = GameObject.CreatePrimitive(shape);
            marker.name = name;
            marker.transform.SetParent(parent, false);
            marker.transform.position = position;
            marker.transform.localScale = scale;
            var collider = marker.GetComponent<Collider>();
            collider.enabled = false;
            Object.Destroy(collider);
            var renderer = marker.GetComponent<Renderer>();
            Paint(renderer, color);
            return renderer;
        }

        static void Paint(Renderer renderer, Color color)
        {
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", color);
            block.SetColor("_Color", color);
            renderer.SetPropertyBlock(block);
        }

    }
}
