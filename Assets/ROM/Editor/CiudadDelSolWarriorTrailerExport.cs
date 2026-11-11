using System;
using System.IO;
using System.Linq;
using MultiFPS.Gameplay.RiftCombat;
using Unity.Collections;
using UnityEditor;
using UnityEditor.Media;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ROM.EditorTools
{
    public static partial class CiudadDelSolNaveJardinesPreview
    {
        const int WarriorTrailerSeconds = 40;
        const string WarriorTrailerName = "CiudadDelSol_Warrior_Trailer_V2.mp4";
        const string WarriorFolder = "Assets/ROM/Characters/Warrior/";
        const string WarriorClips = "Assets/ROM/Animation/WarriorEquipment/";
        static readonly int[] WarriorCutFrames = { 0, 192, 288, 408, 624, 816 };

        [MenuItem("ROM/Dungeon Ciudad del Sol/Cinemática/Exportar tráiler Warrior")]
        public static void ExportWarriorTrailer()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode && Hash() == PlayDecorHash &&
                SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null, "El tráiler requiere la escena integrada y una GPU.");
            Directory.CreateDirectory(FilmFolder);
            bool probe = Environment.GetCommandLineArgs().Contains("-warrior-motion-probe");
            string output = Path.GetFullPath(Path.Combine(FilmFolder, probe ? "Warrior_PruebaMovimiento_V2.mp4" : WarriorTrailerName));
            string working = Path.ChangeExtension(output, ".en-proceso.mp4");
            string first = Path.Combine(FilmFolder, "Ciudad-del-Sol-Riftbreakers.mp4");
            string firstHash = SceneFileHash(first);
            string previousTrailer = Path.Combine(FilmFolder, "CiudadDelSol_Warrior_Trailer.mp4");
            string previousHash = SceneFileHash(previousTrailer);
            var scene = EditorSceneManager.OpenPreviewScene(Original);
            var target = new RenderTexture(FilmWidth, FilmHeight, 24, RenderTextureFormat.ARGBHalf);
            var pixels = new Texture2D(FilmWidth, FilmHeight, TextureFormat.RGBA32, false);
            var previous = RenderTexture.active;
            var graph = PlayableGraph.Create("Warrior · interpretación temporal");
            Texture2D title = null;
            var bakedMeshes = new System.Collections.Generic.List<Mesh>();
            try
            {
                ValidateFinalLook(scene);
                var cameraObject = new GameObject("Cámara de tráiler · temporal");
                SceneManager.MoveGameObjectToScene(cameraObject, scene);
                var camera = cameraObject.AddComponent<Camera>();
                camera.enabled = false;
                camera.scene = scene;
                camera.clearFlags = CameraClearFlags.Skybox;
                camera.nearClipPlane = .10f;
                camera.farClipPlane = 3000;
                camera.allowHDR = true;
                camera.targetTexture = target;
                var data = camera.GetUniversalAdditionalCameraData();
                data.requiresDepthTexture = true;
                data.renderPostProcessing = true;
                data.volumeLayerMask = 1 << PreviewVolumeLayer;
                data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                data.antialiasingQuality = AntialiasingQuality.High;

                var originalPlayer = AssetDatabase.LoadAssetAtPath<GameObject>(WarriorFolder + "WarriorPlayer.prefab");
                var lobby = AssetDatabase.LoadAssetAtPath<GameObject>(WarriorFolder + "WarriorLobby.prefab");
                Require(originalPlayer && lobby, "Faltan los prefabs originales del Warrior.");
                var actor = Object.Instantiate(lobby);
                actor.name = "Warrior real · doble cinematográfico";
                SceneManager.MoveGameObjectToScene(actor, scene);
                foreach (var behaviour in actor.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
                foreach (var collider in actor.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
                var axe = actor.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Equipped Warrior Axe");
                axe.gameObject.SetActive(false);
                var animator = actor.GetComponent<Animator>();
                Require(animator && animator.isHuman && animator.avatar == originalPlayer.GetComponentInChildren<Animator>().avatar,
                    "La presentación no comparte el avatar del Warrior real.");
                animator.runtimeAnimatorController = null;
                animator.Rebind();
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var skins = actor.GetComponentsInChildren<SkinnedMeshRenderer>().Where(skin => skin.enabled).Select(skin =>
                {
                    skin.updateWhenOffscreen = true;
                    var surface = new GameObject(skin.name + " · fotograma");
                    surface.layer = skin.gameObject.layer;
                    surface.transform.SetParent(skin.transform, false);
                    var mesh = new Mesh { name = surface.name };
                    mesh.MarkDynamic();
                    bakedMeshes.Add(mesh);
                    surface.AddComponent<MeshFilter>().sharedMesh = mesh;
                    var renderer = surface.AddComponent<MeshRenderer>();
                    renderer.sharedMaterials = skin.sharedMaterials;
                    renderer.shadowCastingMode = skin.shadowCastingMode;
                    renderer.receiveShadows = skin.receiveShadows;
                    skin.forceRenderingOff = true;
                    return (skin, mesh);
                }).ToArray();
                var sword = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(WarriorFolder + "WarriorSword.asset");
                var equipment = sword.EquippedParts.Select(definition =>
                {
                    var model = Object.Instantiate(definition.Model, actor.transform);
                    model.transform.localScale = definition.Scale;
                    foreach (var collider in model.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
                    return (definition, model: model.transform);
                }).ToArray();
                Require(equipment.Length == 2 && equipment.All(e => e.definition.UseApprovedCarryGrip),
                    "No se encontraron los agarres de espada y escudo aprobados.");

                var walk = AssetDatabase.LoadAllAssetsAtPath(
                    "Assets/ExplosiveLLC/RPG Character Mecanim Animation Pack FREE/Animations/2Hand-Sword/RPG-Character@2Hand-Sword-Walk.FBX")
                    .OfType<AnimationClip>().First(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal));
                var run = AssetDatabase.LoadAssetAtPath<AnimationClip>(WarriorClips + "SwordGrip_1Hand_Up_Run_A_F_InPlace.anim");
                var guard = AssetDatabase.LoadAssetAtPath<AnimationClip>(WarriorClips + "SwordGrip_1Hand_Up_Shield_Block_Idle_1.anim");
                var idle = AssetDatabase.LoadAssetAtPath<AnimationClip>(WarriorClips + "SwordGrip_1Hand_Up_Stand_Idle_A_2.anim");
                var clips = new[] { idle, walk, run, guard };
                Require(clips.All(clip => clip && clip.length > .2f), "Faltan clips para caminar, correr o cubrirse.");
                graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                var mixer = AnimationMixerPlayable.Create(graph, clips.Length);
                var tracks = clips.Select((clip, index) =>
                {
                    var playable = AnimationClipPlayable.Create(graph, clip);
                    playable.SetApplyFootIK(false);
                    playable.SetSpeed(0);
                    graph.Connect(playable, 0, mixer, index);
                    return playable;
                }).ToArray();
                AnimationPlayableOutput.Create(graph, "Warrior", animator).SetSourcePlayable(mixer);
                graph.Play();
                var bones = new[] { HumanBodyBones.LeftUpperLeg, HumanBodyBones.RightUpperLeg,
                    HumanBodyBones.LeftLowerLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.LeftUpperArm,
                    HumanBodyBones.RightUpperArm, HumanBodyBones.Spine, HumanBodyBones.Head }
                    .Select(animator.GetBoneTransform).ToArray();
                Require(bones.All(bone => bone), "El avatar no expone todos los huesos de validación.");
                var movement = new float[clips.Length, bones.Length];
                for (int motion = 0; motion < clips.Length; motion++)
                {
                    EvaluatePose(motion, .12f, 0);
                    var baseline = bones.Select(bone => bone.localRotation).ToArray();
                    EvaluatePose(motion, clips[motion].length * .48f, 0);
                    float frozenStep = bones.Select((bone, index) => Quaternion.Angle(baseline[index], bone.localRotation)).Max();
                    EvaluatePose(motion, .12f, 1f / FilmFps);
                    baseline = bones.Select(bone => bone.localRotation).ToArray();
                    for (int sample = 1; sample <= 16; sample++)
                    {
                        EvaluatePose(motion, clips[motion].length * sample / 17, 1f / FilmFps);
                        for (int bone = 0; bone < bones.Length; bone++)
                            movement[motion, bone] = Mathf.Max(movement[motion, bone],
                                Quaternion.Angle(baseline[bone], bones[bone].localRotation));
                    }
                    var amplitudes = Enumerable.Range(0, bones.Length).Select(i => movement[motion, i]).ToArray();
                    Debug.Log($"WARRIOR_RIG {clips[motion].name}: delta con paso cero={frozenStep:F1}; paso positivo={string.Join("/", amplitudes.Select(a => a.ToString("F1")))}");
                    Require(amplitudes.Max() > (motion == 1 || motion == 2 ? 16 : 2) &&
                        (motion == 0 || motion == 3 || amplitudes.Take(4).Max() > 16 && amplitudes.Skip(4).Take(2).Max() > 4),
                        "No se comprobó movimiento esquelético: " + clips[motion].name);
                }

                void EvaluatePose(int motion, float phase, float step)
                {
                    for (int i = 0; i < tracks.Length; i++) mixer.SetInputWeight(i, i == motion ? 1 : 0);
                    tracks[motion].SetTime(phase);
                    graph.Evaluate(step);
                }

                var keyObject = new GameObject("Luz de retrato · temporal");
                SceneManager.MoveGameObjectToScene(keyObject, scene);
                var key = keyObject.AddComponent<Light>();
                key.type = LightType.Point;
                key.range = 8;
                key.intensity = 7.5f;
                key.color = new Color(1, .83f, .68f);
                key.shadows = LightShadows.None;
                Physics.SyncTransforms();
                var physics = scene.GetPhysicsScene();
                var video = new VideoTrackEncoderAttributes(new H264EncoderAttributes
                {
                    gopSize = FilmFps * 2, numConsecutiveBFrames = 0, profile = VideoEncodingProfile.H264High
                })
                {
                    frameRate = new MediaRational(FilmFps), width = FilmWidth, height = FilmHeight,
                    includeAlpha = false, bitRateMode = VideoBitrateMode.High, targetBitRate = 5600000
                };
                var audio = new AudioTrackAttributes
                {
                    sampleRate = new MediaRational(FilmAudioRate), channelCount = 2, language = ""
                };
                var score = BuildWarriorMarch();
                int audioPerFrame = FilmAudioRate / FilmFps * 2;
                int total = (probe ? 5 : WarriorTrailerSeconds) * FilmFps;
                if (!probe)
                {
                    SaveWarriorMarch(score, Path.Combine(FilmFolder, "CiudadDelSol_MarchaDeGuerra_V2.wav"));
                    title = new Texture2D(2, 2);
                    Require(title.LoadImage(File.ReadAllBytes(Path.Combine(FilmFolder, "Warrior-Titulo-V2.png"))),
                        "Falta la placa del título.");
                }
                int grounded = 0;
                float lastGround = 0;
                using (var encoder = new MediaEncoder(working, video, audio))
                using (var samples = new NativeArray<float>(audioPerFrame, Allocator.Persistent))
                {
                    for (int frame = 0; frame < total; frame++)
                    {
                        float time = probe ? (frame < 60 ? 8.5f + frame / (float)FilmFps : 12.4f + (frame - 60) / (float)FilmFps)
                            : frame / (float)FilmFps;
                        var pose = WarriorTrailerPose(time);
                        var mark = pose.mark;
                        if (pose.visible && physics.Raycast(mark + Vector3.up * .48f, Vector3.down,
                            out var hit, .94f, ~0, QueryTriggerInteraction.Ignore) && hit.normal.y > .75f)
                        {
                            mark.y = hit.point.y;
                            grounded++;
                            lastGround = mark.y;
                        }
                        else if (pose.visible && !WarriorCutFrames.Contains(frame) && Mathf.Abs(lastGround - mark.y) < .4f)
                            mark.y = lastGround;

                        var placement = mark + Vector3.up * .025f;
                        var heading = Quaternion.Euler(0, pose.yaw, 0);
                        actor.transform.SetPositionAndRotation(placement, heading);
                        float phase = Mathf.Repeat(pose.time * (pose.motion == 2 ? 1.13f : 1), clips[pose.motion].length);
                        EvaluatePose(pose.motion, phase, 1f / FilmFps);
                        actor.transform.SetPositionAndRotation(placement, heading);
                        foreach (var (definition, model) in equipment)
                        {
                            var hand = animator.GetBoneTransform(definition.Socket);
                            var rotation = hand.rotation * Quaternion.Inverse(Quaternion.Euler(definition.CarryGripRotation));
                            model.SetPositionAndRotation(hand.position -
                                rotation * Vector3.Scale(definition.CarryGripPosition, model.lossyScale), rotation);
                        }
                        // Render the sampled pose rather than relying on the Editor's deferred skinning update.
                        foreach (var (skin, mesh) in skins) skin.BakeMesh(mesh);
                        key.enabled = pose.visible;
                        Vector3 aim = pose.visible ? mark + pose.aim : pose.aim;
                        Vector3 eye = pose.visible ? mark + pose.eye : pose.eye;
                        if (pose.visible)
                        {
                            Vector3 direction = eye - aim;
                            if (physics.SphereCast(aim, .16f, direction.normalized, out var obstruction, direction.magnitude,
                                ~0, QueryTriggerInteraction.Ignore) && obstruction.distance > .7f)
                                eye = aim + direction.normalized * Mathf.Max(.55f, obstruction.distance - .2f);
                            keyObject.transform.position = mark + Vector3.up * 2.7f + Vector3.ClampMagnitude(eye - mark, 2.8f);
                        }
                        camera.fieldOfView = pose.fov;
                        camera.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(aim - eye));
                        camera.Render();
                        RenderTexture.active = target;
                        if (title && time >= 37.27f)
                        {
                            float alpha = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(37.27f, 37.64f, time));
                            GL.PushMatrix();
                            GL.LoadPixelMatrix(0, FilmWidth, FilmHeight, 0);
                            Graphics.DrawTexture(new Rect(0, 0, FilmWidth, FilmHeight), title,
                                new Rect(0, 0, 1, 1), 0, 0, 0, 0, new Color(.5f, .5f, .5f, alpha * .5f));
                            GL.PopMatrix();
                        }
                        pixels.ReadPixels(new Rect(0, 0, FilmWidth, FilmHeight), 0, 0, false);
                        Require(encoder.AddFrame(FilmWidth, FilmHeight, 0, TextureFormat.RGBA32,
                            pixels.GetRawTextureData<byte>()), "Falló el fotograma " + frame);
                        NativeArray<float>.Copy(score, frame * audioPerFrame, samples, 0, audioPerFrame);
                        Require(encoder.AddSamples(samples), "Falló el audio del fotograma " + frame);
                        if (probe ? frame % 3 == 0 : WarriorCutFrames.Any(cut => frame == cut + 36) || frame % 120 == 0 || frame == 918)
                        {
                            Debug.Log($"WARRIOR_TRAILER {frame}/{total}");
                            if (probe || WarriorCutFrames.Any(cut => frame == cut + 36) || frame == 918)
                                File.WriteAllBytes(Path.Combine(FilmFolder, $"Warrior-{(probe ? "Prueba" : "V2")}-{frame:D3}.png"), pixels.EncodeToPNG());
                        }
                    }
                }
                Require(new FileInfo(working).Length > 1000000 && Hash() == PlayDecorHash &&
                    SceneFileHash(first) == firstHash && SceneFileHash(previousTrailer) == previousHash &&
                    grounded == total, "El MP4, los apoyos o los archivos de origen no pasaron la verificación.");
                if (File.Exists(output)) File.Replace(working, output, null);
                else File.Move(working, output);
                Debug.Log($"WARRIOR_TRAILER_OK {output} | apoyos {grounded}/{total}");
            }
            finally
            {
                graph.Destroy();
                RenderTexture.active = previous;
                EditorSceneManager.ClosePreviewScene(scene);
                foreach (var mesh in bakedMeshes) Object.DestroyImmediate(mesh);
                Object.DestroyImmediate(pixels);
                Object.DestroyImmediate(target);
                if (title) Object.DestroyImmediate(title);
            }
        }

        static (bool visible, Vector3 mark, float yaw, int motion, float time, Vector3 eye, Vector3 aim, float fov)
            WarriorTrailerPose(float time)
        {
            if (time < 8)
            {
                return (true, new Vector3(7, 12, -38), 22, 0, time,
                    Vector3.Lerp(new Vector3(-5.3f, 2.7f, -6.6f), new Vector3(-3.7f, 1.75f, -4.2f), time / 8),
                    new Vector3(.5f, 1.48f, 1.35f), 49);
            }
            if (time < 12)
            {
                float t = time - 8;
                return (true, new Vector3(7, 12, -38 + t * 1.32f), 0, 1, t,
                    Vector3.Lerp(new Vector3(-4.9f, 1.75f, 2.5f), new Vector3(-4.9f, 1.95f, 1.4f), t / 4),
                    new Vector3(0, 1.07f, .2f), 54);
            }
            if (time < 17)
            {
                float t = time - 12;
                return (true, new Vector3(-81, -16, -40 + t * 3.65f), 0, 2, t,
                    Vector3.Lerp(new Vector3(2.1f, 1.6f, -3.4f), new Vector3(3.35f, 1.55f, -2.25f), t / 7),
                    new Vector3(0, 1.12f, .95f), 54);
            }
            if (time < 26)
            {
                float t = time - 17;
                return (true, new Vector3(16, -38, -9),
                    Mathf.Lerp(0, 22, Mathf.InverseLerp(0, 2, t)), 0, t,
                    Vector3.Lerp(new Vector3(-1.9f, 2.1f, -4.2f), new Vector3(-2.5f, 1.95f, -4.8f), t / 9),
                    new Vector3(.4f, 1.65f, 3.1f), 55);
            }
            if (time < 34)
            {
                float t = time - 26;
                float angle = Mathf.Lerp(149, 217, t / 8) * Mathf.Deg2Rad;
                return (true, new Vector3(174, 36, 143), 183, t < 1.6f ? 0 : 3, t < 1.6f ? t : t - 1.6f,
                    new Vector3(Mathf.Sin(angle) * 3.45f, 1.28f + t * .04f, Mathf.Cos(angle) * 3.45f),
                    new Vector3(0, 1.04f, 0), 43);
            }
            float outro = (time - 34) / 6;
            return (true, new Vector3(174, 36, 143), 0, 0, time - 34,
                Vector3.Lerp(new Vector3(1.8f, 1.65f, -3.9f), new Vector3(11.5f, 8.4f, -17f), outro),
                Vector3.Lerp(new Vector3(0, 1.18f, .7f), new Vector3(-3.4f, 3.8f, 11f), outro), 52);
        }
    }
}
