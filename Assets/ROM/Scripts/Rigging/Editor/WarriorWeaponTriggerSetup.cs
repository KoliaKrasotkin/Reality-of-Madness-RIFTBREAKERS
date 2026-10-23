using System;
using System.IO;
using System.Linq;
using System.Text;
using MultiFPS.Gameplay.RiftCombat;
using ROM.Horde;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public static class WarriorWeaponTriggerSetup
{
    private const string ScenePath = "Assets/ROM/Levels/map_rom_horde.unity";
    private const string RootName = "Warrior Weapon Triggers - Spawn";
    private const string Folder = "Assets/ROM/Characters/Warrior";

    [MenuItem("ROM/Animation Rigging/Place Weapon Triggers Near Spawn")]
    public static void Build()
    {
        var previousScene = SceneManager.GetActiveScene();
        var scene = SceneManager.GetSceneByPath(ScenePath);
        bool openedHere = !scene.IsValid() || !scene.isLoaded;
        if (openedHere) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        if (!openedHere && scene.isDirty) throw new InvalidOperationException("Save the gameplay scene's pending edits before configuring the weapon triggers.");
        try
        {
            var horde = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Horde>(true)).Single();
            var points = horde.defaultSpawnPoints.Spawnpoints.Where(t => t).ToArray();
            if (points.Length == 0) throw new InvalidOperationException("Player spawn points are missing.");
            var anchor = points[0];
            Vector3 origin = points.Aggregate(Vector3.zero, (sum, p) => sum + p.position) / points.Length;
            if (points.Any(p => Vector3.Distance(p.position, origin) > 20)) origin = anchor.position;
            var forward = Vector3.ProjectOnPlane(anchor.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < .01f) forward = Vector3.forward;
            var rotation = Quaternion.LookRotation(forward);
            Physics.SyncTransforms();
            var root = scene.GetRootGameObjects().FirstOrDefault(g => g.name == RootName);
            if (!root)
            {
                root = new GameObject(RootName);
                SceneManager.MoveGameObjectToScene(root, scene);
            }
            Material Material(string name, Color color)
            {
                string path = Folder + "/" + name + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (!material)
                {
                    material = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = name };
                    AssetDatabase.CreateAsset(material, path);
                }
                material.SetColor("_BaseColor", color);
                EditorUtility.SetDirty(material);
                AssetDatabase.SaveAssetIfDirty(material);
                return material;
            }
            Vector3 Ground(Vector3 desired)
            {
                if (NavMesh.SamplePosition(desired, out var nav, 3, NavMesh.AllAreas) && Mathf.Abs(nav.position.y - origin.y) < 2.5f)
                    return nav.position;
                if (scene.GetPhysicsScene().Raycast(desired + Vector3.up * 3, Vector3.down, out var hit, 6, ~0, QueryTriggerInteraction.Ignore))
                    return hit.point;
                return desired;
            }
            void Gate(string name, string label, float distance, WarriorWeaponTrigger.WeaponAction action, Material material)
            {
                var existing = root.transform.Find(name);
                // Owned demo objects only; rebuilding must not accumulate gates.
                if (existing) UnityEngine.Object.DestroyImmediate(existing.gameObject);
                var gate = new GameObject(name);
                gate.layer = (int)MultiFPS.GameLayers.trigger;
                gate.transform.SetParent(root.transform, false);
                gate.transform.SetPositionAndRotation(Ground(origin + forward * distance), rotation);
                var trigger = gate.AddComponent<WarriorWeaponTrigger>();
                trigger.Action = action;
                var box = gate.GetComponent<BoxCollider>();
                box.isTrigger = true;
                box.center = new Vector3(0, 1.4f, 0);
                box.size = new Vector3(3.2f, 2.8f, .8f);
                var body = gate.GetComponent<Rigidbody>();
                body.isKinematic = true;
                body.useGravity = false;
                void Cube(string part, Vector3 position, Vector3 scale)
                {
                    var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    visual.name = part;
                    visual.transform.SetParent(gate.transform, false);
                    visual.transform.localPosition = position;
                    visual.transform.localScale = scale;
                    UnityEngine.Object.DestroyImmediate(visual.GetComponent<Collider>());
                    visual.GetComponent<Renderer>().sharedMaterial = material;
                }
                Cube("Left marker", new Vector3(-1.8f, 1.4f, 0), new Vector3(.12f, 2.8f, .12f));
                Cube("Right marker", new Vector3(1.8f, 1.4f, 0), new Vector3(.12f, 2.8f, .12f));
                Cube("Top marker", new Vector3(0, 2.8f, 0), new Vector3(3.72f, .12f, .12f));
                Cube("Floor marker", new Vector3(0, .02f, 0), new Vector3(3.2f, .025f, .8f));
                for (int side = 0; side < 2; side++)
                {
                    var textObject = new GameObject(side == 0 ? "Label front" : "Label back");
                    textObject.transform.SetParent(gate.transform, false);
                    textObject.transform.localPosition = new Vector3(0, 3.1f, side == 0 ? -.1f : .1f);
                    textObject.transform.localRotation = Quaternion.Euler(0, side * 180, 0);
                    var text = textObject.AddComponent<TextMesh>();
                    text.text = label;
                    text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    text.fontSize = 64;
                    text.characterSize = .055f;
                    text.anchor = TextAnchor.MiddleCenter;
                    text.alignment = TextAlignment.Center;
                    text.color = Color.white;
                    textObject.GetComponent<MeshRenderer>().sharedMaterial = text.font.material;
                }
            }
            Gate("Guardar arma", "GUARDAR ARMA", 4, WarriorWeaponTrigger.WeaponAction.Holster,
                Material("Weapon Trigger Holster", new Color(1f, .6f, .12f)));
            Gate("Sacar arma", "SACAR ARMA", 9, WarriorWeaponTrigger.WeaponAction.Draw,
                Material("Weapon Trigger Draw", new Color(.15f, .85f, .45f)));
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Gameplay scene could not be saved.");
            Validate(scene, origin, root);
        }
        finally
        {
            if (openedHere) EditorSceneManager.CloseScene(scene, true);
            if (previousScene.IsValid() && previousScene.isLoaded) SceneManager.SetActiveScene(previousScene);
        }
    }

    private static void Validate(Scene scene, Vector3 spawn, GameObject root)
    {
        var report = new StringBuilder("Weapon trigger setup validation\n");
        var triggers = root.GetComponentsInChildren<WarriorWeaponTrigger>();
        if (triggers.Length != 2) throw new InvalidOperationException("Two weapon gates are required.");
        foreach (var trigger in triggers)
        {
            var collider = trigger.GetComponent<BoxCollider>();
            var body = trigger.GetComponent<Rigidbody>();
            if (!collider.isTrigger || !body.isKinematic || body.useGravity) throw new InvalidOperationException("Invalid gate physics.");
            if (trigger.gameObject.layer != (int)MultiFPS.GameLayers.trigger || Physics.GetIgnoreLayerCollision(trigger.gameObject.layer, (int)MultiFPS.GameLayers.character))
                throw new InvalidOperationException("Weapon gate cannot receive character trigger entries.");
            if (Vector3.Distance(trigger.transform.position, spawn) > 15) throw new InvalidOperationException("Gate is not near spawn.");
            report.AppendLine($"PASS {trigger.name}: {trigger.Action}, position {trigger.transform.position}, distance from spawn {Vector3.Distance(trigger.transform.position, spawn):F1} m");
        }
        var avatarRoot = PrefabUtility.LoadPrefabContents(WarriorWeaponStateSetup.PrefabPath);
        try
        {
            var avatar = avatarRoot.GetComponent<WarriorAvatar>();
            var collider = avatarRoot.GetComponent<CharacterController>();
            // CharacterInstance.Awake assigns this layer when spawned in game.
            avatarRoot.layer = (int)MultiFPS.GameLayers.character;
            if (triggers.Any(t => Physics.GetIgnoreLayerCollision(t.gameObject.layer, collider.gameObject.layer)))
                throw new InvalidOperationException("Collision matrix blocks the Warrior's actual collider layer.");
            report.AppendLine("PASS scene trigger layers collide with the Warrior CharacterController layer.");
            var holster = triggers.Single(t => t.Action == WarriorWeaponTrigger.WeaponAction.Holster);
            var draw = triggers.Single(t => t.Action == WarriorWeaponTrigger.WeaponAction.Draw);
            if (!holster.TryApply(collider) || avatar.WeaponDrawn) throw new InvalidOperationException("Holster trigger did not change weapon state.");
            if (holster.TryApply(collider)) throw new InvalidOperationException("Duplicate enter restarted the action.");
            var childCollider = avatarRoot.GetComponentsInChildren<Collider>(true).First(c => c.gameObject != avatarRoot);
            if (!draw.TryApply(childCollider) || !avatar.WeaponDrawn) throw new InvalidOperationException("Child hitbox did not resolve the player.");
            if (draw.TryApply(collider)) throw new InvalidOperationException("Duplicate draw restarted the action.");
            report.AppendLine("PASS root collider: holster; child hitbox: draw; duplicate enters are idempotent.");
            var prop = new GameObject("Unrelated collider test");
            try { if (holster.TryApply(prop.AddComponent<BoxCollider>())) throw new InvalidOperationException("Non-player collider was accepted."); }
            finally { UnityEngine.Object.DestroyImmediate(prop); }
            report.AppendLine("PASS non-player colliders ignored. Scene has kinematic trigger bodies for physics callbacks.");
            report.AppendLine("PASS project trigger layer and trigger-only colliders keep the walking path clear.");
        }
        finally { PrefabUtility.UnloadPrefabContents(avatarRoot); }
        Directory.CreateDirectory("Documentation/warrior-rebuild");
        File.WriteAllText("Documentation/warrior-rebuild/weapon-trigger-validation.txt", report.ToString());
        Debug.Log(report.ToString());
    }
}
