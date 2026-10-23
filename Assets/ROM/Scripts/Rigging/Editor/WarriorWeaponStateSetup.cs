using System;
using System.Linq;
using MultiFPS.Gameplay.RiftCombat;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;
using UnityEngine.Playables;

// All pose, visibility and rig-weight changes are authored in the Animator.
public static class WarriorWeaponStateSetup
{
    public const string ControllerPath = "Assets/ROM/Characters/Warrior/WarriorLocomotion.controller";
    public const string PrefabPath = "Assets/ROM/Characters/Warrior/WarriorPlayer.prefab";
    private const string Folder = "Assets/ROM/Animation/WarriorWeaponStates";
    private const string Pack = "Assets/ExplosiveLLC/RPG Character Mecanim Animation Pack FREE/Animations/";

    private static AnimationClip Clip(string family, string name) => AssetDatabase.LoadAllAssetsAtPath(
        Pack + family + "/RPG-Character@" + family + "-" + name + ".FBX")
        .OfType<AnimationClip>().Single(c => c.name == family + "-" + name);

    // Bake reversed curves so Animator exits and interruptions use ordinary forward time.
    // The imported pack and its animation events remain untouched.
    private static AnimationClip Reversed(AnimationClip source)
    {
        string path = Folder + "/2Hand-Sword-Sheath-Back-Unarmed.anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (!clip)
        {
            clip = new AnimationClip { name = "2Hand-Sword-Sheath-Back-Unarmed" };
            AssetDatabase.CreateAsset(clip, path);
        }
        clip.ClearCurves();
        foreach (var binding in AnimationUtility.GetCurveBindings(source))
        {
            var curve = AnimationUtility.GetEditorCurve(source, binding);
            var reversed = curve.keys.Reverse().Select(key => new Keyframe(source.length - key.time, key.value,
                -key.outTangent, -key.inTangent, key.outWeight, key.inWeight)
            {
                weightedMode = key.weightedMode == WeightedMode.In ? WeightedMode.Out
                    : key.weightedMode == WeightedMode.Out ? WeightedMode.In : key.weightedMode
            }).ToArray();
            AnimationUtility.SetEditorCurve(clip, binding, new AnimationCurve(reversed));
        }
        var settings = AnimationUtility.GetAnimationClipSettings(source);
        settings.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        clip.frameRate = source.frameRate;
        EditorUtility.SetDirty(clip);
        AssetDatabase.SaveAssetIfDirty(clip);
        return clip;
    }

    private static void ConfigureBackSocket()
    {
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var animator = root.GetComponent<WarriorAvatar>().Animator;
            var constraint = animator.GetComponentInChildren<AnimatedWeaponGripConstraint>(true);
            var data = constraint.data;
            if (!data.backSocket)
            {
                var chest = animator.GetBoneTransform(HumanBodyBones.Chest);
                var socket = new GameObject("Weapon Back Socket").transform;
                socket.SetParent(chest, false);
                data.backSocket = socket;
                data.backWeight = 0;
                data.carryPoseCalibrated = false;
                constraint.data = data;
            }
            if (!data.carryPoseCalibrated)
            {
                // Match the handle to the reaching palm at the pickup frame.
                // Save only socket calibration, never the sampled character pose.
                var transforms = animator.GetComponentsInChildren<Transform>(true);
                var positions = transforms.Select(t => t.localPosition).ToArray();
                var rotations = transforms.Select(t => t.localRotation).ToArray();
                var scales = transforms.Select(t => t.localScale).ToArray();
                var graph = PlayableGraph.Create("Calibrate back pickup");
                Vector3 socketPosition;
                Quaternion socketRotation;
                try
                {
                    var source = Clip("2Hand-Sword", "Unsheath-Back-Unarmed");
                    var pose = AnimationClipPlayable.Create(graph, source);
                    pose.SetTime(source.length * .4f);
                    pose.SetSpeed(0);
                    var output = AnimationPlayableOutput.Create(graph, "Pickup pose", animator);
                    output.SetSourcePlayable(pose);
                    graph.Play();
                    graph.Evaluate(0);
                    float scale = Mathf.Abs(data.weapon.lossyScale.x);
                    var palm = data.rightHand.position + data.rightHand.rotation * (data.rightPalmOffset * scale);
                    var socket = data.backSocket;
                    // Blade down, handle above the shoulder for this back-unsheath clip.
                    socket.rotation = Quaternion.LookRotation((-animator.transform.up - animator.transform.right * .35f).normalized,
                        -animator.transform.forward);
                    data.carryContact = new Vector3(0, 0, -.55f);
                    socket.position = palm - socket.rotation * (data.carryContact * scale);
                    data.carryRotation = Quaternion.Inverse(data.rightHand.rotation) * socket.rotation;
                    data.carryPoseCalibrated = true;
                    socketPosition = socket.localPosition;
                    socketRotation = socket.localRotation;
                }
                finally
                {
                    graph.Destroy();
                    for (int i = 0; i < transforms.Length; i++)
                    {
                        transforms[i].localPosition = positions[i];
                        transforms[i].localRotation = rotations[i];
                        transforms[i].localScale = scales[i];
                    }
                }
                data.backSocket.SetLocalPositionAndRotation(socketPosition, socketRotation);
                constraint.data = data;
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        // Mirror clears IDs on isolated prefab contents. Resolve it on the saved asset.
        var identity = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath).GetComponent<Mirror.NetworkIdentity>();
        _ = identity.assetId;
        EditorUtility.SetDirty(identity);
        AssetDatabase.SaveAssetIfDirty(identity);
    }

    private static AnimatorState State(AnimatorStateMachine machine, string name, Vector3 position) =>
        machine.states.FirstOrDefault(s => s.state.name == name).state ?? machine.AddState(name, position);

    private static void ClearTransitions(AnimatorState state)
    {
        foreach (var transition in state.transitions) state.RemoveTransition(transition);
    }

    private static AnimatorStateTransition Link(AnimatorState from, AnimatorState to,
        bool exitTime, string parameter = null, bool value = false)
    {
        var transition = from.AddTransition(to);
        transition.hasExitTime = exitTime;
        transition.exitTime = 1f;
        transition.hasFixedDuration = true;
        transition.duration = .12f;
        if (parameter != null) transition.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, parameter);
        return transition;
    }

    [MenuItem("ROM/Animation Rigging/Configure Warrior Weapon States")]
    public static void Configure()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/ROM/Animation", "WarriorWeaponStates");
        ConfigureBackSocket();
        if (!controller.parameters.Any(p => p.name == "WeaponDrawn"))
        {
            controller.AddParameter("WeaponDrawn", AnimatorControllerParameterType.Bool);
            var parameters = controller.parameters;
            parameters.Single(p => p.name == "WeaponDrawn").defaultBool = true;
            controller.parameters = parameters;
        }
        var machine = controller.layers[0].stateMachine;
        var armed = machine.states.Single(s => s.state.name == "Locomotion").state;
        var sheath = State(machine, "SheathWeapon", new Vector3(-500, 100));
        var draw = State(machine, "DrawWeapon", new Vector3(-500, 0));
        var unarmed = State(machine, "UnarmedLocomotion", new Vector3(-800, 100));
        var jump = State(machine, "UnarmedJump", new Vector3(-1050, 200));
        var air = State(machine, "UnarmedAir", new Vector3(-800, 250));
        draw.motion = Clip("2Hand-Sword", "Unsheath-Back-Unarmed");
        sheath.motion = Reversed((AnimationClip)draw.motion);
        sheath.speed = draw.speed = 1;
        jump.motion = Clip("Unarmed", "Jump");
        air.motion = Clip("Unarmed", "Fall");
        var tree = unarmed.motion as BlendTree;
        if (tree == null)
        {
            tree = new BlendTree { name = "Unarmed directional locomotion" };
            AssetDatabase.AddObjectToAsset(tree, controller);
            unarmed.motion = tree;
        }
        tree.blendType = BlendTreeType.FreeformCartesian2D;
        tree.blendParameter = "MoveX";
        tree.blendParameterY = "MoveY";
        tree.children = Array.Empty<ChildMotion>();
        tree.AddChild(Clip("Unarmed", "Idle"), Vector2.zero);
        tree.AddChild(Clip("Unarmed", "Run-Forward"), new Vector2(0, .5f));
        tree.AddChild(Clip("Unarmed", "Run-Backward"), new Vector2(0, -.5f));
        tree.AddChild(Clip("Unarmed", "Run-Left"), new Vector2(-.5f, 0));
        tree.AddChild(Clip("Unarmed", "Run-Right"), new Vector2(.5f, 0));
        foreach (var state in new[] { sheath, draw, unarmed, jump, air }) ClearTransitions(state);

        // Insert holstering before existing exits, including defensive hold and attacks.
        foreach (var child in machine.states)
        {
            var state = child.state;
            if (new[] { sheath, draw, unarmed, jump, air }.Contains(state) || state.name == "Death" || state.name == "RollForward" || state.name == "UnarmedRollForward") continue;
            foreach (var old in state.transitions.Where(t => t.destinationState == sheath).ToArray()) state.RemoveTransition(old);
            var transition = Link(state, sheath, false, "WeaponDrawn", false);
            transition.AddCondition(AnimatorConditionMode.IfNot, 0, "Dead");
            state.transitions = new[] { transition }.Concat(state.transitions.Where(t => t != transition)).ToArray();
            EditorUtility.SetDirty(state);
        }
        Link(sheath, draw, false, "WeaponDrawn", true);
        Link(sheath, unarmed, true, "WeaponDrawn", false);
        Link(draw, sheath, false, "WeaponDrawn", false);
        Link(draw, armed, true, "WeaponDrawn", true);
        foreach (var state in new[] { unarmed, jump, air }) Link(state, draw, false, "WeaponDrawn", true);
        Link(unarmed, jump, false, "Grounded", false);
        Link(jump, unarmed, false, "Grounded", true);
        Link(jump, air, true, "Grounded", false);
        Link(air, unarmed, false, "Grounded", true);

        const string layerName = "Weapon Presentation";
        var layer = controller.layers.FirstOrDefault(l => l.name == layerName);
        if (layer == null)
        {
            layer = new AnimatorControllerLayer
            {
                name = layerName, defaultWeight = 1,
                stateMachine = new AnimatorStateMachine { name = layerName }
            };
            AssetDatabase.AddObjectToAsset(layer.stateMachine, controller);
            controller.AddLayer(layer);
        }
        var presentation = layer.stateMachine;
        var shown = State(presentation, "Drawn", Vector3.zero);
        var hiding = State(presentation, "Sheathing", new Vector3(250, 0));
        var hidden = State(presentation, "Holstered", new Vector3(250, 120));
        var showing = State(presentation, "Drawing", new Vector3(0, 120));
        presentation.defaultState = shown;
        foreach (var state in new[] { shown, hiding, hidden, showing }) ClearTransitions(state);

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        var avatar = prefab.GetComponent<WarriorAvatar>();
        var animator = avatar.Animator;
        var rigs = animator.GetComponentsInChildren<Rig>(true);
        var arms = animator.GetComponentsInChildren<TwoBoneIKConstraint>(true);
        var gripConstraint = animator.GetComponentInChildren<AnimatedWeaponGripConstraint>(true);
        var renderers = avatar.EquippedWeapon.GetComponentsInChildren<Renderer>(true);
        AnimationClip Presentation(string name, float length, AnimationCurve grip, AnimationCurve visible,
            AnimationCurve carry = null, AnimationCurve weaponRig = null, AnimationCurve back = null)
        {
            var path = Folder + "/" + name + ".anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (!clip) { clip = new AnimationClip { name = name }; AssetDatabase.CreateAsset(clip, path); }
            clip.ClearCurves();
            foreach (var rig in rigs) AnimationUtility.SetEditorCurve(clip,
                EditorCurveBinding.FloatCurve(AnimationUtility.CalculateTransformPath(rig.transform, animator.transform), typeof(Rig), "m_Weight"),
                weaponRig != null && gripConstraint.transform.IsChildOf(rig.transform) ? weaponRig : grip);
            foreach (var arm in arms) AnimationUtility.SetEditorCurve(clip,
                EditorCurveBinding.FloatCurve(AnimationUtility.CalculateTransformPath(arm.transform, animator.transform), typeof(TwoBoneIKConstraint), "m_Weight"), grip);
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(
                AnimationUtility.CalculateTransformPath(gripConstraint.transform, animator.transform),
                typeof(AnimatedWeaponGripConstraint), "m_Data.rightHandFollow"), carry ?? AnimationCurve.Constant(0, length, 0));
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(
                AnimationUtility.CalculateTransformPath(gripConstraint.transform, animator.transform),
                typeof(AnimatedWeaponGripConstraint), "m_Data.backWeight"), back ?? AnimationCurve.Constant(0, length, 0));
            foreach (var renderer in renderers) AnimationUtility.SetEditorCurve(clip,
                EditorCurveBinding.FloatCurve(AnimationUtility.CalculateTransformPath(renderer.transform, animator.transform), renderer.GetType(), "m_Enabled"), visible);
            clip.frameRate = 60;
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssetIfDirty(clip);
            return clip;
        }
        AnimationCurve Constant(float length, float value) => AnimationCurve.Constant(0, length, value);
        shown.motion = Presentation("WeaponDrawn", .1f, Constant(.1f, 1), Constant(.1f, 1));
        hidden.motion = Presentation("WeaponHolstered", .1f, Constant(.1f, 0), Constant(.1f, 1),
            Constant(.1f, 0), Constant(.1f, 1), Constant(.1f, 1));
        float sheathLength = ((AnimationClip)sheath.motion).length;
        float drawLength = ((AnimationClip)draw.motion).length;
        hiding.motion = Presentation("WeaponSheathing", sheathLength,
            new AnimationCurve(new Keyframe(0, 1), new Keyframe(sheathLength * .2f, 0), new Keyframe(sheathLength, 0)), Constant(sheathLength, 1),
            new AnimationCurve(new Keyframe(0, 0), new Keyframe(sheathLength * .2f, 1), new Keyframe(sheathLength, 1)), Constant(sheathLength, 1),
            new AnimationCurve(new Keyframe(0, 0), new Keyframe(sheathLength * .56f, 0), new Keyframe(sheathLength * .64f, 1), new Keyframe(sheathLength, 1)));
        showing.motion = Presentation("WeaponDrawing", drawLength,
            new AnimationCurve(new Keyframe(0, 0), new Keyframe(drawLength * .8f, 0), new Keyframe(drawLength, 1)), Constant(drawLength, 1),
            new AnimationCurve(new Keyframe(0, 1), new Keyframe(drawLength * .8f, 1), new Keyframe(drawLength, 0)), Constant(drawLength, 1),
            new AnimationCurve(new Keyframe(0, 1), new Keyframe(drawLength * .36f, 1), new Keyframe(drawLength * .44f, 0), new Keyframe(drawLength, 0)));
        Link(shown, hiding, false, "WeaponDrawn", false);
        Link(hiding, showing, false, "WeaponDrawn", true);
        Link(hiding, hidden, true, "WeaponDrawn", false);
        Link(hidden, showing, false, "WeaponDrawn", true);
        Link(showing, hiding, false, "WeaponDrawn", false);
        Link(showing, shown, true, "WeaponDrawn", true);
        foreach (var child in presentation.states) EditorUtility.SetDirty(child.state);
        foreach (var state in new[] { sheath, draw, unarmed, jump, air }) EditorUtility.SetDirty(state);
        EditorUtility.SetDirty(tree);
        EditorUtility.SetDirty(machine);
        EditorUtility.SetDirty(presentation);
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssetIfDirty(controller);
        WarriorRollSetup.Configure();
        Debug.Log("Warrior weapon states configured: WeaponDrawn, sheath/draw, Unarmed movement and Animator-driven grip/visibility.");
    }
}
