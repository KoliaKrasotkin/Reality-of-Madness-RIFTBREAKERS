using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace ROM.Rendering
{
    [DisallowMultipleComponent]
    public sealed class CiudadDelSolAtmosphereCamera : MonoBehaviour
    {
        [SerializeField] int volumeLayer = 31;
        readonly Dictionary<Camera, CameraSettings> rendering = new Dictionary<Camera, CameraSettings>();

        struct CameraSettings
        {
            public UniversalAdditionalCameraData Data;
            public CameraOverrideOption Depth;
            public LayerMask VolumeMask;
            public bool PostProcessing;
            public bool Hdr;
        }

        void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += BeginCamera;
            RenderPipelineManager.endCameraRendering += EndCamera;
        }

        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= BeginCamera;
            RenderPipelineManager.endCameraRendering -= EndCamera;
            foreach (var pair in rendering) Restore(pair.Key, pair.Value);
            rendering.Clear();
        }

        void BeginCamera(ScriptableRenderContext context, Camera camera)
        {
            if (!camera || camera.cameraType != CameraType.Game || rendering.ContainsKey(camera) ||
                !camera.TryGetComponent(out UniversalAdditionalCameraData data) ||
                data.renderType != CameraRenderType.Base) return;
            bool sceneCamera = camera.gameObject.scene == gameObject.scene && camera.CompareTag("MainCamera");
            bool gameplayCamera = SceneManager.GetActiveScene() == gameObject.scene &&
                camera.TryGetComponent<MultiFPS.GameplayCamera>(out _);
            if (!sceneCamera && !gameplayCamera) return;

            rendering.Add(camera, new CameraSettings
            {
                Data = data, Depth = data.requiresDepthOption, VolumeMask = data.volumeLayerMask,
                PostProcessing = data.renderPostProcessing, Hdr = camera.allowHDR
            });
            data.requiresDepthOption = CameraOverrideOption.On;
            data.volumeLayerMask |= 1 << volumeLayer;
            data.renderPostProcessing = true;
            camera.allowHDR = true;
        }

        void EndCamera(ScriptableRenderContext context, Camera camera)
        {
            if (!rendering.TryGetValue(camera, out var settings)) return;
            rendering.Remove(camera);
            Restore(camera, settings);
        }

        static void Restore(Camera camera, CameraSettings settings)
        {
            if (camera) camera.allowHDR = settings.Hdr;
            if (!settings.Data) return;
            settings.Data.requiresDepthOption = settings.Depth;
            settings.Data.volumeLayerMask = settings.VolumeMask;
            settings.Data.renderPostProcessing = settings.PostProcessing;
        }
    }
}
