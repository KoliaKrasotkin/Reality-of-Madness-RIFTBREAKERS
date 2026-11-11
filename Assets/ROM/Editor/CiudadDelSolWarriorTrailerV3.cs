using System;
using System.Collections.Generic;
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
        const string EclipseFolder = "Assets/ROM/Enemies/Bestiary/eclipse-sworn/";
        const int TrailerV3Seconds = 60;
        const float TitleV3Time = 56.5f;

        [MenuItem("ROM/Dungeon Ciudad del Sol/Cinemática/Exportar tráiler Warrior V3")]
        public static void ExportWarriorTrailerV3()
        {
            var args = Environment.GetCommandLineArgs();
            if (args.Contains("-warrior-v3-probes"))
            {
                ExportWarriorV3(1);
                ExportWarriorV3(2);
                return;
            }
            ExportWarriorV3(0);
        }

        static void ExportWarriorV3(int mode, bool v4 = false)
        {
            string stem = mode == 1 ? "Warrior_V3_PruebaMovimiento" :
                mode == 2 ? "Warrior_V3_PruebaCombate" : "CiudadDelSol_Warrior_Trailer_V3";
            if (v4) stem = mode == 1 ? "Warrior_V4_PruebaDescenso" : mode == 2 ? "Warrior_V4_PruebaCombate" : "CiudadDelSol_Warrior_Trailer_V4";
            string[] originals = { "Ciudad-del-Sol-Riftbreakers.mp4", "CiudadDelSol_Warrior_Trailer.mp4",
                "CiudadDelSol_Warrior_Trailer_V2.mp4" };
            if (v4) originals = originals.Append("CiudadDelSol_Warrior_Trailer_V3.mp4").ToArray();
            string[] hashes = originals.Select(name => SceneFileHash(Path.Combine(FilmFolder, name))).ToArray();
            Require(!EditorApplication.isPlayingOrWillChangePlaymode && Hash() == PlayDecorHash &&
                SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null, "V3 requiere la escena original y una GPU.");

            var scene = EditorSceneManager.OpenPreviewScene(Original);
            using var resources = new V3Resources();
            var target = resources.Own(new RenderTexture(FilmWidth, FilmHeight, 24, RenderTextureFormat.ARGBHalf));
            var pixels = resources.Own(new Texture2D(FilmWidth, FilmHeight, TextureFormat.RGBA32, false));
            var previous = RenderTexture.active;
            string output = Path.GetFullPath(Path.Combine(FilmFolder, stem + ".mp4"));
            string working = Path.ChangeExtension(output, ".en-proceso.mp4");
            try
            {
                ValidateFinalLook(scene);
                var camera = V3Object(scene, "Cámara V3").AddComponent<Camera>();
                camera.enabled = false;
                camera.scene = scene;
                camera.clearFlags = CameraClearFlags.Skybox;
                camera.nearClipPlane = .09f;
                camera.farClipPlane = 3000;
                camera.allowHDR = true;
                camera.targetTexture = target;
                var urp = camera.GetUniversalAdditionalCameraData();
                urp.requiresDepthTexture = true;
                urp.renderPostProcessing = true;
                urp.volumeLayerMask = 1 << PreviewVolumeLayer;
                urp.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                urp.antialiasingQuality = AntialiasingQuality.High;

                var warrior = new V3Actor(scene, resources, WarriorFolder + "WarriorLobby.prefab",
                    V3Clip(WarriorClips + "SwordGrip_1Hand_Up_Stand_Idle_A_2.anim"),
                    V3Clip("Assets/ExplosiveLLC/RPG Character Mecanim Animation Pack FREE/Animations/2Hand-Sword/RPG-Character@2Hand-Sword-Walk.FBX"),
                    V3Clip(WarriorClips + "SwordGrip_1Hand_Up_Run_A_F_InPlace.anim"),
                    V3Clip(WarriorClips + "SwordGrip_1Hand_Up_Shield_Block_Idle_1.anim"),
                    V3Clip(WarriorClips + "SwordGrip_1Hand_Up_Attack_A_1_InPlace.anim"),
                    V3Clip(WarriorClips + "SwordGrip_1Hand_Up_Attack_B_1_InPlace.anim"));
                var original = AssetDatabase.LoadAssetAtPath<GameObject>(WarriorFolder + "WarriorPlayer.prefab");
                Require(warrior.Animator.isHuman && original.GetComponentInChildren<Animator>().avatar == warrior.Animator.avatar,
                    "Avatar cinematográfico distinto al Warrior.");
                warrior.Root.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Equipped Warrior Axe").gameObject.SetActive(false);
                var weapon = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(WarriorFolder + "WarriorSword.asset");
                var grips = weapon.EquippedParts.Select(def =>
                {
                    Require(def.UseApprovedCarryGrip, "Falta un agarre aprobado.");
                    var model = Object.Instantiate(def.Model, warrior.Root.transform);
                    model.transform.localScale = def.Scale;
                    V3DisableGameplay(model);
                    return (def, model: model.transform, hand: warrior.Animator.GetBoneTransform(def.Socket));
                }).ToArray();
                Require(grips.Length == 2 && grips.All(grip => grip.hand), "Faltan espada y escudo.");
                void Grip()
                {
                    foreach (var (def, model, hand) in grips)
                    {
                        var rot = hand.rotation * Quaternion.Inverse(Quaternion.Euler(def.CarryGripRotation));
                        model.SetPositionAndRotation(hand.position - rot * Vector3.Scale(def.CarryGripPosition, model.lossyScale), rot);
                    }
                }

                var enemy = new V3Actor(scene, resources, EclipseFolder + "eclipse-sworn.prefab",
                    V3Clip(EclipseFolder + "Animations/Idle01.anim"),
                    V3Clip(EclipseFolder + "Animations/Walk01.anim"),
                    V3Clip(EclipseFolder + "Animations/Attack01.anim"),
                    V3Clip(EclipseFolder + "Animations/Hit01.anim"),
                    V3Clip(EclipseFolder + "Animations/Death01.anim"));
                var sword = grips.Single(g => g.def.Socket == HumanBodyBones.RightHand);
                var blade = sword.model.GetComponentsInChildren<MeshRenderer>().OrderByDescending(r => r.localBounds.size.sqrMagnitude).First();
                var enemyBlade = enemy.Root.GetComponentsInChildren<Transform>().Single(t => t.name == "Sword");
                var attackPhases = new[] { MeasureStrike(4), MeasureStrike(5) };
                float enemyStrike = MeasureEnemyStrike();
                Debug.Log($"V3_CONTACT_PHASES {attackPhases[0].phase:F3} / {attackPhases[1].phase:F3} / {enemyStrike:F3}");

                (float phase, Vector3 point) MeasureStrike(int clip)
                {
                    float best = float.NegativeInfinity;
                    (float phase, Vector3 point) result = default;
                    for (float t = .20f; t < warrior.Length(clip) * .72f; t += 1f / 72)
                    {
                        warrior.Sample(Vector3.zero, 0, clip, t);
                        Grip();
                        Vector3 center = blade.transform.TransformPoint(blade.localBounds.center);
                        Vector3 point = Vector3.LerpUnclamped(sword.hand.position, center, 1.45f);
                        float score = point.z - Mathf.Abs(point.x) * .42f - Mathf.Abs(point.y - 1.21f) * .82f;
                        if (score <= best) continue;
                        best = score;
                        result = (t, point);
                    }
                    Require(result.point.z > .45f, "La espada no alcanza el espacio delantero.");
                    return result;
                }

                float MeasureEnemyStrike()
                {
                    float phase = .4f;
                    float best = float.NegativeInfinity;
                    var mesh = enemyBlade.GetComponentInChildren<Renderer>();
                    for (float t = .16f; t < enemy.Length(2) * .76f; t += 1f / 72)
                    {
                        enemy.Sample(Vector3.zero, 0, 2, t);
                        Vector3 p = mesh ? mesh.bounds.center : enemyBlade.position;
                        float score = p.z - .4f * Mathf.Abs(p.x) - .6f * Mathf.Abs(p.y - 1.12f);
                        if (score > best) { best = score; phase = t; }
                    }
                    return phase;
                }

                var key = V3Object(scene, "Luz cinematográfica V3").AddComponent<Light>();
                key.type = LightType.Point;
                key.range = 9;
                key.intensity = 5;
                key.color = new Color(1, .84f, .72f);
                key.shadows = LightShadows.None;
                var impactPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ROM/Combat/Prefabs/MeleeImpact.prefab");
                Require(impactPrefab, "Falta el efecto MeleeImpact.");
                var impactObject = Object.Instantiate(impactPrefab);
                SceneManager.MoveGameObjectToScene(impactObject, scene);
                V3DisableGameplay(impactObject);
                var impact = impactObject.GetComponent<ParticleSystem>();
                foreach (var system in impactObject.GetComponentsInChildren<ParticleSystem>())
                {
                    system.useAutoRandomSeed = false;
                    system.randomSeed = 157;
                }

                var title = resources.Own(new Texture2D(2, 2));
                Require(title.LoadImage(File.ReadAllBytes(Path.Combine(FilmFolder, "Warrior-Titulo-V2.png"))), "Falta el título.");
                var score = BuildWarriorV3Score(v4);
                int total = FilmFps * (v4 ? mode == 1 ? 18 : mode == 2 ? 17 : 78 : mode != 0 ? 8 : TrailerV3Seconds);
                if (mode == 0) SaveWarriorMarch(score, Path.Combine(FilmFolder, $"CiudadDelSol_Warrior_BandaSonora_V{(v4 ? 4 : 3)}.wav"));
                var video = new VideoTrackEncoderAttributes(new H264EncoderAttributes
                {
                    gopSize = FilmFps * 2, numConsecutiveBFrames = 0, profile = VideoEncodingProfile.H264High
                })
                {
                    frameRate = new MediaRational(FilmFps), width = FilmWidth, height = FilmHeight,
                    includeAlpha = false, bitRateMode = VideoBitrateMode.High, targetBitRate = 8500000
                };
                var audio = new AudioTrackAttributes { sampleRate = new MediaRational(FilmAudioRate), channelCount = 2, language = "" };
                int perFrame = FilmAudioRate * 2 / FilmFps;
                var physics = scene.GetPhysicsScene();
                Physics.SyncTransforms();
                int grounded = 0;
                using (var encoder = new MediaEncoder(working, video, audio))
                using (var samples = new NativeArray<float>(perFrame, Allocator.Persistent))
                {
                    for (int frame = 0; frame < total; frame++)
                    {
                        float time = frame / (float)FilmFps;
                        float t = v4 ? time + (mode == 1 ? 30 : mode == 2 ? 55 : 0) : mode == 2 ? time + 24 : time;
                        bool duel = v4 ? t >= 51 && t < 72 : mode == 2 || mode == 0 && t >= 22 && t < 50;
                        bool reveal = mode == 0 && (v4 ? t >= 8 && t < 12 || t >= 17 && t < 19 : t >= 8 && t < 10 || t >= 14.6f && t < 16 || t >= 56);
                        Vector3 eye, aim, mark;
                        float fov;
                        float eventAge = -1;
                        Vector3 flash = Vector3.zero;
                        enemy.Root.SetActive(duel);
                        if (v4 && duel)
                        {
                            float beat = t - 55;
                            Vector3 anchor = new Vector3(16, -38, -9);
                            float advance = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.2f, 3.3f, beat));
                            float contact = beat < 9.5f ? 7.65f : beat < 12.6f ? 11.1f : 14.25f;
                            int index = contact == 11.1f ? 0 : 1;
                            var strike = attackPhases[index];
                            float thrust = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(-strike.phase, 0, beat - contact)) *
                                (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.13f, .9f, beat - contact)));
                            Vector3 targetSpacing = new Vector3(strike.point.x * .88f, 0, Mathf.Max(.95f, strike.point.z + .12f));
                            Vector3 spacing = Vector3.Lerp(new Vector3(-.22f, 0, 1.75f), targetSpacing, thrust);
                            if (beat >= 14.25f) spacing = targetSpacing + Vector3.forward * (.32f * Mathf.SmoothStep(0, 1, beat - 14.25f));
                            mark = Floor(anchor);
                            Vector3 foe = Floor(anchor + spacing + Vector3.forward * (3.8f * (1 - advance)));
                            float local = beat - contact;
                            bool swordSwing = local >= -strike.phase && local < .95f;
                            float wt = local + strike.phase;
                            float blend = Mathf.Min(Mathf.InverseLerp(0, .2f, wt), Mathf.InverseLerp(.95f, .58f, local));
                            warrior.Sample(mark, 0, swordSwing ? index + 4 : beat < -2.5f ? 0 : 3,
                                swordSwing ? wt : t, 3, swordSwing ? blend : 1);
                            bool counter = beat >= 4.65f - enemyStrike && beat < 5.75f;
                            bool down = beat >= 14.25f;
                            bool hit = !down && local >= 0 && local < 1.08f;
                            float et = down ? (beat - 14.25f) * 1.22f : counter ? beat - 4.65f + enemyStrike :
                                hit ? local * 1.7f : t;
                            int clip = down ? 4 : hit ? 3 : counter ? 2 : beat > .2f && beat < 3.3f ? 1 : 0;
                            float ew = clip == 1 ? Mathf.Min(Mathf.InverseLerp(.2f, .48f, beat), Mathf.InverseLerp(3.3f, 2.95f, beat)) :
                                clip == 2 ? Mathf.Min(Mathf.InverseLerp(0, .22f, et), Mathf.InverseLerp(5.75f, 5.35f, beat)) :
                                clip == 3 ? Mathf.Min(Mathf.InverseLerp(0, .12f, local), Mathf.InverseLerp(1.08f, .79f, local)) : 1;
                            float yaw = Quaternion.LookRotation(Vector3.ProjectOnPlane(mark - foe, Vector3.up)).eulerAngles.y;
                            enemy.Sample(foe, yaw, clip, et, 0, ew);
                            enemy.Bake();
                            if (local >= 0 && local < .30f) { eventAge = local; flash = mark + strike.point; }
                            if (beat >= 4.65f && beat < 4.93f) { eventAge = beat - 4.65f; flash = mark + new Vector3(-.18f, 1.27f, .56f); }
                            var mid = Vector3.Lerp(mark, foe, .43f);
                            aim = mid + Vector3.up * 1.1f;
                            if (beat < 3.5f)
                                eye = mid + Vector3.Lerp(new Vector3(3.7f, 1.9f, -4.5f), new Vector3(3.6f, 1.7f, -2.8f), Mathf.InverseLerp(-4, 3.5f, beat));
                            else if (beat < 9.5f)
                                eye = mid + Vector3.Lerp(new Vector3(3.55f, 1.48f, 1.3f), new Vector3(3.85f, 1.65f, -.8f), Mathf.InverseLerp(3.5f, 9.5f, beat));
                            else
                                eye = mid + Vector3.Lerp(new Vector3(-3.55f, 1.65f, -.85f), new Vector3(-4.6f, 2.05f, 1.2f), Mathf.InverseLerp(9.5f, 17, beat));
                            fov = 54;
                        }
                        else if (v4)
                        {
                            var pose = WarriorV4Pose(t);
                            mark = t >= 30 && t < 48 ? V4Ground(pose.mark, physics) : Floor(pose.mark);
                            warrior.Sample(mark, pose.yaw, pose.clip, pose.phase);
                            if (t >= 30 && t < 48) V4Feet(warrior.Animator, physics);
                            eye = mark + pose.eye;
                            aim = mark + pose.aim;
                            fov = pose.fov;
                            if (reveal)
                                (eye, aim, fov) = FilmPose(t < 12 ? t - 8 : 9 + t - 17);
                        }
                        else if (duel)
                        {
                            bool deep = t >= 36;
                            float beat = t - (deep ? 36 : 22);
                            Vector3 anchor = deep ? new Vector3(16, -38, -9) : new Vector3(7, 12, -36);
                            float hitTime = 5.5f;
                            float blockTime = 8.35f;
                            float finishTime = 11.25f;
                            int attackIndex = beat >= 9.65f ? 1 : 0;
                            var strike = attackPhases[attackIndex];
                            Vector3 spacing = new Vector3(strike.point.x * .88f, 0, Mathf.Max(.95f, strike.point.z + .12f));
                            float advance = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.3f, 3.25f, beat));
                            Vector3 offset = Vector3.back * (2.7f * (1 - advance));
                            mark = Floor(anchor + offset);
                            Vector3 foeMark = Floor(anchor + spacing + Vector3.forward * (.78f * (1 - advance)));
                            bool final = beat >= finishTime - .001f;
                            bool firstHit = beat >= hitTime;
                            bool counter = beat >= blockTime - enemyStrike && beat < blockTime + .72f;
                            bool strikeOne = beat >= hitTime - attackPhases[0].phase && beat < hitTime + .93f;
                            bool strikeTwo = beat >= finishTime - attackPhases[1].phase && beat < finishTime + 1.14f;
                            int w = strikeTwo ? 5 : strikeOne ? 4 : beat < 3.25f && beat >= .30f ? 1 : 3;
                            float wt = strikeTwo ? beat - finishTime + attackPhases[1].phase :
                                strikeOne ? beat - hitTime + attackPhases[0].phase : beat;
                            float blend = strikeTwo || strikeOne ? Mathf.Min(Mathf.Clamp01(wt / .18f),
                                Mathf.Clamp01(((strikeTwo ? finishTime + 1.14f : hitTime + .93f) - beat) / .28f)) : 1;
                            if (w == 1)
                                warrior.Sample(mark, 0, w, wt, 3, Mathf.SmoothStep(0, 1,
                                    Mathf.Min(Mathf.InverseLerp(.30f, .55f, beat), Mathf.InverseLerp(3.25f, 2.90f, beat))));
                            else
                                warrior.Sample(mark, 0, w, wt, 3, blend);

                            int e = final ? 4 : counter ? 2 : firstHit && beat < hitTime + 1.6f ? 3 : 0;
                            float et = final ? (beat - finishTime) * 1.18f : counter ? beat - blockTime + enemyStrike :
                                e == 3 ? (beat - hitTime) * 1.65f : beat;
                            float enemyBlend = e == 3 ? Mathf.Min(Mathf.Clamp01(et / .13f), Mathf.Clamp01((hitTime + 1.6f - beat) / .25f)) :
                                e == 2 ? Mathf.Min(Mathf.Clamp01(et / .17f), Mathf.Clamp01((blockTime + .72f - beat) / .25f)) : 1;
                            float yaw = Quaternion.LookRotation(Vector3.ProjectOnPlane(mark - foeMark, Vector3.up)).eulerAngles.y;
                            enemy.Sample(foeMark, yaw, e, et, 0, enemyBlend);
                            enemy.Bake();
                            if (firstHit && beat < hitTime + .45f)
                            {
                                eventAge = beat - hitTime;
                                flash = anchor + attackPhases[0].point;
                            }
                            if (beat >= blockTime && beat < blockTime + .38f)
                            {
                                eventAge = beat - blockTime;
                                flash = mark + new Vector3(-.18f, 1.27f, .56f);
                            }
                            if (final && beat < finishTime + .50f)
                            {
                                eventAge = beat - finishTime;
                                flash = anchor + attackPhases[1].point;
                            }

                            Vector3 middle = (mark + foeMark) * .5f;
                            aim = middle + Vector3.up * 1.16f;
                            if (beat < 3.5f)
                            {
                                eye = middle + Vector3.Lerp(new Vector3(4.8f, 2.25f, -4.3f), new Vector3(3.8f, 1.8f, -2.65f), beat / 3.5f);
                                fov = 51;
                                if (deep)
                                {
                                    float establishing = 1 - Mathf.SmoothStep(0, 1, beat / 3.5f);
                                    eye = Vector3.Lerp(eye, new Vector3(18, -35.9f, -12), establishing);
                                    aim = Vector3.Lerp(aim, new Vector3(18, -27, 21), establishing);
                                    fov = Mathf.Lerp(fov, 63, establishing);
                                }
                            }
                            else if (beat < 9.65f)
                            {
                                eye = middle + Vector3.Lerp(new Vector3(4.3f, 1.55f, .95f), new Vector3(3.85f, 1.67f, -.75f), (beat - 3.5f) / 6.15f);
                                fov = 51;
                            }
                            else
                            {
                                eye = middle + Vector3.Lerp(new Vector3(-3.55f, 1.55f, -.85f), new Vector3(-4.6f, 2.35f, .95f), (beat - 9.65f) / 4.35f);
                                fov = 54;
                            }
                        }
                        else if (mode == 1)
                        {
                            float sample = t < 1.7f ? 2 + t : t < 3.8f ? 10 + t - 1.7f : 16 + Mathf.Min(t - 3.8f, 2);
                            var pose = V3Explore(sample);
                            mark = Floor(pose.mark);
                            int clip = t < 5.8f ? pose.clip : 4;
                            float phase = t < 5.8f ? pose.phase : (t - 5.8f) * 1.15f;
                            warrior.Sample(mark, pose.yaw, clip, phase);
                            aim = mark + pose.aim;
                            eye = mark + pose.eye;
                            fov = pose.fov;
                        }
                        else
                        {
                            var pose = V3Explore(t);
                            mark = Floor(pose.mark);
                            warrior.Sample(mark, pose.yaw, pose.clip, pose.phase);
                            aim = mark + pose.aim;
                            eye = mark + pose.eye;
                            fov = pose.fov;
                            if (reveal)
                            {
                                var panorama = FilmPose(t < 10 ? t - 8 : t < 16 ? 9 + t - 14.6f : 30 + t - 56);
                                (eye, aim, fov) = panorama;
                            }
                        }
                        grounded++;
                        Grip();
                        warrior.Bake();
                        impactObject.SetActive(eventAge >= 0);
                        if (eventAge >= 0)
                        {
                            impactObject.transform.position = flash;
                            if (v4) impactObject.transform.localScale = Vector3.one * .60f;
                            impact.Simulate(eventAge + .04f, true, true, true);
                        }

                        var ray = eye - aim;
                        if (!reveal && physics.SphereCast(aim, .12f, ray.normalized, out var block, ray.magnitude,
                            ~0, QueryTriggerInteraction.Ignore) && block.distance > .7f)
                            eye = aim + ray.normalized * Mathf.Max(.65f, block.distance - .16f);
                        key.transform.position = Vector3.Lerp(mark + Vector3.up * 2.7f, eye, .28f);
                        if (reveal) key.enabled = false;
                        else key.enabled = true;
                        camera.fieldOfView = fov;
                        camera.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(aim - eye));
                        camera.Render();
                        RenderTexture.active = target;
                        float titleTime = v4 ? 75 : TitleV3Time;
                        if (mode == 0 && t >= titleTime)
                        {
                            GL.PushMatrix();
                            GL.LoadPixelMatrix(0, FilmWidth, FilmHeight, 0);
                            var full = new Rect(0, 0, FilmWidth, FilmHeight);
                            float a = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(titleTime, titleTime + .35f, t));
                            Graphics.DrawTexture(full, title, new Rect(0, 0, 1, 1), 0, 0, 0, 0, new Color(.5f, .5f, .5f, a * .5f));
                            if (t >= titleTime + 2.1f)
                            {
                                float fade = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(titleTime + 2.1f, titleTime + (v4 ? 2.85f : 3.35f), t));
                                Graphics.DrawTexture(full, Texture2D.whiteTexture, new Rect(0, 0, 1, 1), 0, 0, 0, 0, new Color(0, 0, 0, fade * .5f));
                            }
                            GL.PopMatrix();
                        }

                        pixels.ReadPixels(new Rect(0, 0, FilmWidth, FilmHeight), 0, 0, false);
                        Require(encoder.AddFrame(FilmWidth, FilmHeight, 0, TextureFormat.RGBA32, pixels.GetRawTextureData<byte>()), "Falló el fotograma V3.");
                        int sampleFrame = v4 ? frame + (mode == 1 ? 30 : mode == 2 ? 55 : 0) * FilmFps : mode == 2 ? frame + 24 * FilmFps : frame;
                        NativeArray<float>.Copy(score, sampleFrame * perFrame, samples, 0, perFrame);
                        Require(encoder.AddSamples(samples), "Falló el audio V3.");
                        if (frame % 120 == 0) Debug.Log($"WARRIOR_V3 {stem} {frame}/{total}");
                        if (v4 ? (mode != 0 ? frame % 12 == 0 : frame % 120 == 60) :
                            mode != 0 ? frame % 6 == 0 || mode == 2 && (frame >= 80 && frame <= 90 || frame >= 149 && frame <= 156) :
                            new[] { 60, 224, 325, 455, 663, 729, 911, 1140, 1284, 1377 }.Contains(frame))
                            File.WriteAllBytes(Path.Combine(FilmFolder, $"{stem}-{frame:D4}.png"), pixels.EncodeToPNG());
                    }
                }

                Require(new FileInfo(working).Length > 1000000 && grounded == total && Hash() == PlayDecorHash &&
                    originals.Select(name => SceneFileHash(Path.Combine(FilmFolder, name))).SequenceEqual(hashes),
                    "Falló la verificación de V3 o cambió un original.");
                if (File.Exists(output)) File.Replace(working, output, null);
                else File.Move(working, output);
                Debug.Log($"WARRIOR_V3_OK {stem} | {grounded}/{total} sobre piso");

                Vector3 Floor(Vector3 p)
                {
                    Require(physics.Raycast(p + Vector3.up * .46f, Vector3.down, out var hit, .90f, ~0,
                        QueryTriggerInteraction.Ignore) && hit.normal.y > .72f, "Sin piso V3 en " + p.ToString("F3"));
                    p.y = hit.point.y + .025f;
                    return p;
                }
            }
            finally
            {
                RenderTexture.active = previous;
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        static (Vector3 mark, float yaw, int clip, float phase, Vector3 eye, Vector3 aim, float fov) V3Explore(float t)
        {
            if (t < 10)
            {
                return (new Vector3(7, 12, -38), 12, 0, t,
                    Vector3.Lerp(new Vector3(4.6f, 2.8f, -7.7f), new Vector3(3.0f, 1.9f, -4.9f), t / 8),
                    new Vector3(0, 1.2f, 2), 55);
            }
            if (t < 16)
            {
                float p = t - 10;
                return (new Vector3(7, 12, -38 + p * 1.32f), 0, 1, p,
                    Vector3.Lerp(new Vector3(-4.9f, 1.8f, 2.5f), new Vector3(-5.0f, 2.3f, -.5f), p / 6),
                    new Vector3(0, .95f, .25f), 60);
            }
            if (t < 22)
            {
                float p = t - 16;
                return (new Vector3(-81, -16, -40 + p * 3.65f), 0, 2, p * 1.13f,
                    Vector3.Lerp(new Vector3(2.1f, 1.6f, -3.4f), new Vector3(3.6f, 1.65f, -2.25f), p / 6),
                    new Vector3(0, 1.12f, .95f), 55);
            }
            return (new Vector3(174, 36, 143), 0, 0, t - 50,
                Vector3.Lerp(new Vector3(1.8f, 1.65f, -3.9f), new Vector3(10.5f, 7.4f, -15f), (t - 50) / 6),
                new Vector3(0, 1.6f, 4.5f), 54);
        }

        static GameObject V3Object(Scene scene, string name)
        {
            var obj = new GameObject(name);
            SceneManager.MoveGameObjectToScene(obj, scene);
            return obj;
        }

        static AnimationClip V3Clip(string path)
        {
            var clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                .FirstOrDefault(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal));
            Require(clip && clip.length > .2f, "Falta el clip " + path);
            return clip;
        }

        static void V3DisableGameplay(GameObject root)
        {
            foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
            foreach (var collider in root.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (var audio in root.GetComponentsInChildren<AudioSource>(true)) audio.enabled = false;
        }

        sealed class V3Resources : IDisposable
        {
            readonly List<Object> objects = new();
            readonly List<PlayableGraph> graphs = new();
            public T Own<T>(T value) where T : Object { objects.Add(value); return value; }
            public PlayableGraph Graph()
            {
                var graph = PlayableGraph.Create("Actor cinematográfico V3");
                graphs.Add(graph);
                return graph;
            }
            public void Dispose()
            {
                foreach (var graph in graphs) if (graph.IsValid()) graph.Destroy();
                foreach (var obj in objects) if (obj) Object.DestroyImmediate(obj);
            }
        }

        sealed class V3Actor
        {
            public readonly GameObject Root;
            public readonly Animator Animator;
            readonly AnimationClip[] clips;
            readonly AnimationClipPlayable[] tracks;
            readonly AnimationMixerPlayable mixer;
            readonly PlayableGraph graph;
            readonly (SkinnedMeshRenderer source, Mesh frame)[] skins;

            public V3Actor(Scene scene, V3Resources resources, string path, params AnimationClip[] clips)
            {
                this.clips = clips;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Require(prefab, "Falta el actor " + path);
                Root = Object.Instantiate(prefab);
                SceneManager.MoveGameObjectToScene(Root, scene);
                V3DisableGameplay(Root);
                Animator = Root.GetComponentInChildren<Animator>();
                Require(Animator && Animator.avatar && Animator.avatar.isValid, "Avatar no válido para " + path);
                Debug.Log($"V3_RIG {prefab.name} | humanoide={Animator.isHuman} | controlador={Animator.runtimeAnimatorController?.name}");
                Animator.runtimeAnimatorController = null;
                Animator.Rebind();
                Animator.applyRootMotion = false;
                Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                skins = Root.GetComponentsInChildren<SkinnedMeshRenderer>().Where(skin => skin.enabled).Select(skin =>
                {
                    var surface = new GameObject(skin.name + " · captura");
                    surface.layer = skin.gameObject.layer;
                    surface.transform.SetParent(skin.transform, false);
                    var mesh = resources.Own(new Mesh { name = surface.name });
                    mesh.MarkDynamic();
                    surface.AddComponent<MeshFilter>().sharedMesh = mesh;
                    var renderer = surface.AddComponent<MeshRenderer>();
                    renderer.sharedMaterials = skin.sharedMaterials;
                    renderer.shadowCastingMode = skin.shadowCastingMode;
                    renderer.receiveShadows = skin.receiveShadows;
                    skin.updateWhenOffscreen = true;
                    skin.forceRenderingOff = true;
                    return (skin, mesh);
                }).ToArray();
                Require(skins.Length > 0, "El actor no tiene superficies skinned.");
                graph = resources.Graph();
                graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                mixer = AnimationMixerPlayable.Create(graph, clips.Length);
                tracks = clips.Select((clip, i) =>
                {
                    var track = AnimationClipPlayable.Create(graph, clip);
                    track.SetApplyFootIK(false);
                    track.SetSpeed(0);
                    graph.Connect(track, 0, mixer, i);
                    return track;
                }).ToArray();
                AnimationPlayableOutput.Create(graph, "Malla animada", Animator).SetSourcePlayable(mixer);
                graph.Play();

                var bones = skins.SelectMany(skin => skin.source.bones).Where(b => b).Distinct().ToArray();
                for (int clip = 0; clip < clips.Length; clip++)
                {
                    Sample(Vector3.zero, 0, clip, .03f);
                    var initial = bones.Select(b => b.localRotation).ToArray();
                    float swing = 0;
                    for (int step = 1; step < 12; step++)
                    {
                        Sample(Vector3.zero, 0, clip, clips[clip].length * step / 12);
                        for (int b = 0; b < bones.Length; b++)
                            swing = Mathf.Max(swing, Quaternion.Angle(initial[b], bones[b].localRotation));
                    }
                    Debug.Log($"V3_POSE {prefab.name} {clips[clip].name} | {swing:F1} grados");
                    Require(swing > 2, "Clip sin movimiento óseo: " + clips[clip].name);
                }
            }

            public float Length(int clip) => clips[clip].length;

            public void Sample(Vector3 mark, float yaw, int clip, float phase, int other = -1, float weight = 1)
            {
                var rotation = Quaternion.Euler(0, yaw, 0);
                Root.transform.SetPositionAndRotation(mark, rotation);
                if (other == clip || other < 0) weight = 1;
                for (int i = 0; i < clips.Length; i++)
                {
                    mixer.SetInputWeight(i, i == clip ? weight : i == other ? 1 - weight : 0);
                    if (i == clip) tracks[i].SetTime(clip < 2 || Root.name.StartsWith("Warrior", StringComparison.Ordinal) && clip < 4 ?
                        Mathf.Repeat(phase, clips[i].length) : Mathf.Clamp(phase, 0, clips[i].length - .01f));
                    else if (i == other) tracks[i].SetTime(Mathf.Repeat(phase, clips[i].length));
                }
                graph.Evaluate(1f / FilmFps);
                Root.transform.SetPositionAndRotation(mark, rotation);
            }

            public void Bake()
            {
                foreach (var (source, frame) in skins) source.BakeMesh(frame);
            }
        }
    }
}

