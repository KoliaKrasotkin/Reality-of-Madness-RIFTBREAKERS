using System;
using System.IO;
using Unity.Collections;
using UnityEditor;
using UnityEditor.Media;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ROM.EditorTools
{
    public static partial class CiudadDelSolNaveJardinesPreview
    {
        const string FilmFolder = "Backups/CiudadDelSol/Cinematica";
        const int FilmFps = 24;
        const int FilmSeconds = 35;
        const int FilmWidth = 1920;
        const int FilmHeight = 1080;
        const int FilmAudioRate = 48000;

        [MenuItem("ROM/Dungeon Ciudad del Sol/Cinemática/Exportar presentación MP4")]
        public static void ExportCiudadDelSolFilm()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode && Hash() == PlayDecorHash &&
                SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null, "La cinemática requiere la escena integrada y una GPU.");
            Directory.CreateDirectory(FilmFolder);
            string output = Path.GetFullPath(Path.Combine(FilmFolder, "Ciudad-del-Sol-Riftbreakers.mp4"));
            string partial = Path.Combine(Path.GetDirectoryName(output), "Ciudad-del-Sol-en-proceso.mp4");
            var scene = EditorSceneManager.OpenPreviewScene(Original);
            var target = new RenderTexture(FilmWidth, FilmHeight, 24, RenderTextureFormat.ARGBHalf);
            var pixels = new Texture2D(FilmWidth, FilmHeight, TextureFormat.RGBA32, false);
            var previous = RenderTexture.active;
            try
            {
                ValidateFinalLook(scene);
                var cameraObject = new GameObject("Cámara de cinemática · temporal");
                SceneManager.MoveGameObjectToScene(cameraObject, scene);
                var camera = cameraObject.AddComponent<Camera>();
                camera.enabled = false;
                camera.scene = scene;
                camera.clearFlags = CameraClearFlags.Skybox;
                camera.nearClipPlane = .12f;
                camera.farClipPlane = 3000;
                camera.allowHDR = true;
                camera.targetTexture = target;
                var data = camera.GetUniversalAdditionalCameraData();
                data.requiresDepthTexture = true;
                data.renderPostProcessing = true;
                data.volumeLayerMask = 1 << PreviewVolumeLayer;
                data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                data.antialiasingQuality = AntialiasingQuality.High;

                var video = new VideoTrackEncoderAttributes(new H264EncoderAttributes
                {
                    gopSize = FilmFps * 2, numConsecutiveBFrames = 0, profile = VideoEncodingProfile.H264High
                })
                {
                    frameRate = new MediaRational(FilmFps), width = FilmWidth, height = FilmHeight,
                    includeAlpha = false, bitRateMode = VideoBitrateMode.High, targetBitRate = 5400000
                };
                var audio = new AudioTrackAttributes
                {
                    sampleRate = new MediaRational(FilmAudioRate), channelCount = 2, language = ""
                };
                var score = BuildFilmScore();
                int audioPerFrame = FilmAudioRate / FilmFps * 2;
                using (var encoder = new MediaEncoder(partial, video, audio))
                using (var samples = new NativeArray<float>(audioPerFrame, Allocator.Persistent))
                {
                    for (int frame = 0; frame < FilmSeconds * FilmFps; frame++)
                    {
                        var shot = FilmPose(frame / (float)FilmFps);
                        camera.fieldOfView = shot.fov;
                        camera.transform.SetPositionAndRotation(shot.eye, Quaternion.LookRotation(shot.aim - shot.eye));
                        camera.Render();
                        RenderTexture.active = target;
                        pixels.ReadPixels(new Rect(0, 0, FilmWidth, FilmHeight), 0, 0, false);
                        Require(encoder.AddFrame(FilmWidth, FilmHeight, 0, TextureFormat.RGBA32,
                            pixels.GetRawTextureData<byte>()), "Falló el fotograma " + frame);
                        NativeArray<float>.Copy(score, frame * audioPerFrame, samples, 0, audioPerFrame);
                        Require(encoder.AddSamples(samples), "Falló el audio en el fotograma " + frame);
                        if (frame == 0 || frame % 120 == 0)
                        {
                            Debug.Log($"CINEMATICA {frame}/{FilmSeconds * FilmFps}");
                            File.WriteAllBytes(Path.Combine(FilmFolder, $"Plano-{frame:D3}.png"), pixels.EncodeToPNG());
                        }
                    }
                }
                Require(new FileInfo(partial).Length > 1000000 && Hash() == PlayDecorHash, "Falló el MP4 o cambió la escena.");
                if (File.Exists(output)) File.Replace(partial, output, null);
                else File.Move(partial, output);
                Debug.Log("CINEMATICA_MP4_OK " + output);
            }
            finally
            {
                RenderTexture.active = previous;
                EditorSceneManager.ClosePreviewScene(scene);
                Object.DestroyImmediate(pixels);
                Object.DestroyImmediate(target);
            }
        }

        static (Vector3 eye, Vector3 aim, float fov) FilmPose(float time)
        {
            if (time < 7f) return Shot(time, 0, 7,
                new Vector3(550, 110, -500), new Vector3(415, 111, -475),
                new Vector3(33, -55, 18), new Vector3(25, -37, 24), 50, 47);
            if (time < 13f) return Shot(time, 7, 13,
                new Vector3(-364, 20, -223), new Vector3(-272, 12, -203),
                new Vector3(-150, -30, -104), new Vector3(-162, -19, -106), 54, 53);
            if (time < 19f) return Shot(time, 13, 19,
                new Vector3(-121, 64, -141), new Vector3(-93, 53, -100),
                new Vector3(13, 17, 16), new Vector3(24, 18, 28), 55, 51);
            if (time < 25f) return Shot(time, 19, 25,
                new Vector3(18, -36.1f, -7), new Vector3(16, -35.5f, -.5f),
                new Vector3(18, -29, 25), new Vector3(18, -23, 27), 58, 61);
            if (time < 30f) return Shot(time, 25, 30,
                new Vector3(212, 82, 14), new Vector3(196, 72, 42),
                new Vector3(65, 13, 58), new Vector3(49, 15, 47), 60, 56);
            return Shot(time, 30, 35,
                new Vector3(493, 109, 521), new Vector3(555, 129, 565),
                new Vector3(26, -42, 23), new Vector3(31, -53, 20), 50, 49);

            (Vector3, Vector3, float) Shot(float t, float begin, float end, Vector3 eyeA,
                Vector3 eyeB, Vector3 aimA, Vector3 aimB, float fovA, float fovB)
            {
                float phase = Mathf.InverseLerp(begin, end, t);
                float blend = .78f * phase + .22f * Mathf.SmoothStep(0, 1, phase);
                return (Vector3.Lerp(eyeA, eyeB, blend), Vector3.Lerp(aimA, aimB, blend), Mathf.Lerp(fovA, fovB, blend));
            }
        }

        static float[] BuildFilmScore(int seconds = FilmSeconds, double[] cutTimes = null)
        {
            int count = seconds * FilmAudioRate;
            var output = new float[count * 2];
            var random = new System.Random(724);
            double[] roots = { 36.71, 29.14, 32.70, 36.71 };
            double[] cuts = cutTimes ?? new[] { .18, 7, 13, 19, 25, 30, 33.5 };
            const double pulse = .545454545;
            double tau = Math.PI * 2;
            double filteredNoise = 0;
            for (int index = 0; index < count; index++)
            {
                double t = index / (double)FilmAudioRate;
                double beat = t / pulse;
                int step = (int)beat;
                double age = (beat - step) * pulse;
                double root = roots[Math.Min(3, (int)(t / 8.75))];
                double noise = random.NextDouble() * 2 - 1;
                filteredNoise += .16 * (noise - filteredNoise);
                double swell = .46 + .42 * (t / seconds);
                double harmony = (.07 * Math.Sin(tau * root * t) + .034 * Math.Sin(tau * root * 1.5 * t) +
                    .026 * Math.Sin(tau * root * 2.002 * t)) * (.74 + .26 * Math.Sin(t * 1.4));
                double drums = (step % 4 == 0 ? .41 : step % 2 == 0 ? .25 : .15) *
                    Math.Sin(tau * (49 * age + 3.1 * (1 - Math.Exp(-age * 21)))) *
                    Math.Exp(-age * 8.5) * Math.Min(1, age * 165);
                double half = age % (pulse * .5);
                double skins = (noise * .06 + filteredNoise * .12) * Math.Exp(-half * 23) *
                    Math.Min(1, half * 300) * (t > 13 ? 1 : .52);
                double impact = 0;
                foreach (var cut in cuts)
                {
                    double tail = t - cut;
                    if (tail < 0 || tail > 1.3) continue;
                    impact += (Math.Sin(tau * (36 * tail + 4 * (1 - Math.Exp(-tail * 12)))) * .30 +
                        filteredNoise * .23) * Math.Exp(-tail * 4.8) * Math.Min(1, tail * 180);
                }
                double fade = Math.Min(1, t * 3) * Math.Min(1, (seconds - t) / 1.25);
                double motif = .033 * Math.Sin(tau * root * (step % 8 < 4 ? 4 : 6) * t) *
                    Math.Exp(-age * 3.6) * Math.Min(1, age * 55);
                double dry = (harmony + drums + skins + impact + motif) * swell * fade;
                output[index * 2] = (float)Math.Tanh(dry * 1.23 + motif * fade * .16);
                output[index * 2 + 1] = (float)Math.Tanh(dry * 1.23 - motif * fade * .16);
            }
            return output;
        }
    }
}
