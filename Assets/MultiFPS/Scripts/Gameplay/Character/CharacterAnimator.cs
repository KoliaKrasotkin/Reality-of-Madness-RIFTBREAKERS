using MultiFPS.Gameplay;
using MultiFPS.UI;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
                using System.Text;

namespace MultiFPS
{
    [RequireComponent(typeof(CharacterInstance))]

    /// <summary>
    /// Class responsible for animating character, this means: Animating character on runtime and applying appropriate animations
    /// based on currently used item
    /// </summary>
    public class CharacterAnimator : MonoBehaviour
    {
        CharacterInstance _characterInstance;
        CharacterItemManager _characterItemManager;

        [SerializeField] OverrideTransform _chestIK;
        [SerializeField] OverrideTransform _headIK;

        bool fppPerspective = false;

        [SerializeField] Animator _animator;

        [SerializeField] Animator _tppModelAnimator;
        [SerializeField] Animator _fppModelAnimator;

        public RuntimeAnimatorController _baseAnimatorController;
        public RuntimeAnimatorController BaseAnimatorControllerFPP;

        [SerializeField] RigBuilder _rigBuilder;
        /// <summary>
        /// player model animator
        /// </summary>
        [Header("Character meshes")]
        [SerializeField] SkinnedMeshRenderer[] _characterMeshes;
        [SerializeField] SkinnedMeshRenderer tppRenderer;
        [SerializeField] SkinnedMeshRenderer _fppCharacterMesh;

        
        [Header("Skin rig mapping")]
        [SerializeField] private Transform _tppRigRoot;
        [SerializeField] private Transform _tppRootBone;

        [SerializeField] private Transform _fppRigRoot;
        [SerializeField] private Transform _fppRootBone;


        [SerializeField] GameObject[] _idsNativeModels;
        GameObject _nativeIDSVisual;
        public SkinContainer MySkin { private set; get; }

        #region step animation variables
        
        [SerializeField] float _rotationAngleThreshold = 80;
        [SerializeField] float _rotationAngleThresholdLeftSide = 20;
        [SerializeField] float _adjustingRotationLerpSpeed = 180;
        [Tooltip("Segundos de suavizado de la dirección de movimiento en el blend tree de locomoción.")]
        [SerializeField] float _movementBlendDamping = 0.12f;
        bool _adjustingRotation = false;
        #endregion

        CharacterMotor _motor;
        Vector3 _lastMotionPosition;
        Vector3 _animationVelocity;
        float _currentBodyRotation;
        float _upperBodyAnimateFactor;
        float _speed;

        float _recoilX;
        float _recoilStabilizationSpeed = 5f;

        Vector3 _takingDamge_IKSpine;
        Vector3 _takingDamge_IKHead;

        [SerializeField] float _stabilizationAfterTakingDamageSpeed = 4f;

        float _fallingTime;
        
        [Header("Audio")]
        [SerializeField] AudioSource _audioSource;
        [SerializeField] AudioClip _landingSound;
        [SerializeField] float _landingSoundThresholdTime = 0.75f;

        void Awake()
        {
#if UNITY_SERVER
            _rigBuilder.enabled = false;
#endif

            if (_audioSource)
            {
                _audioSource.spatialBlend = 1f;

                //set other audiosource values to work in 3D space
                _audioSource.spread = 180;
                _audioSource.rolloffMode = AudioRolloffMode.Linear;
                _audioSource.maxDistance = 30;
                _audioSource.minDistance = 10;
            }

            for (int i = 0; i < _characterMeshes.Length; i++)
                _characterMeshes[i].updateWhenOffscreen = false;
            

            _characterInstance = GetComponent<CharacterInstance>();
            _motor = GetComponent<CharacterMotor>();
            _lastMotionPosition = transform.position;
            _characterItemManager = GetComponent<CharacterItemManager>();

            _characterInstance.Client_OnPerspectiveSet += OnPerspectiveChanged;
            _characterItemManager.Client_EquipmentChanged += AssingCharacterAnimationsForCurrentlyUsedItem;
        }

        private void Start()
        {
            fppPerspective = _characterInstance.FPP;
            _characterInstance.Health.Client_OnHealthStateChanged += OnDamaged;

            _characterInstance.Health.Client_Resurrect += OnResurrected;
            _characterInstance.Health.Client_OnHealthDepleted += OnDeath;

            _animator.cullingMode = AnimatorCullingMode.CullCompletely;
            // The invisible gameplay skeleton must continue driving native IDS visuals.
            _tppModelAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }
        private void Update()
        {
            float dt = Mathf.Max(.001f, Time.deltaTime);
            var delta = transform.position - _lastMotionPosition;
            _lastMotionPosition = transform.position;
            var measured = _characterInstance.IsClientOrBot() && _motor
                ? _motor.PlanarVelocity : (delta.sqrMagnitude < 4f ? delta / dt : Vector3.zero);
            measured.y = 0;
            _animationVelocity = Vector3.Lerp(_animationVelocity, measured, 1f - Mathf.Exp(-16f * dt));
            if (_characterInstance.Health.CurrentHealth > 0) UpdateAnimationProperties();
            if (!_characterInstance.FPP)
                UpdateRotationAnimation(Time.deltaTime);

            if (_landingSound && _fallingTime > _landingSoundThresholdTime && _characterInstance.isGrounded)
            {
                _audioSource.pitch = 1f - 0.4f*Mathf.Lerp(0, 1, Mathf.Clamp(_fallingTime - _landingSoundThresholdTime, 0, 1));
                _audioSource.PlayOneShot(_landingSound);
            }

            if (!_characterInstance.isGrounded)
            {
                _fallingTime += Time.deltaTime;
            }
            else _fallingTime = 0;
        }

        private void OnDeath(CharacterPart hittedPartID, byte attackerID)
        {
            enabled = false;
        }

        private void OnResurrected(int health)
        {
            _takingDamge_IKHead = Vector3.zero;
            _takingDamge_IKSpine = Vector3.zero;
            _recoilX = 0;
            _currentBodyRotation = _characterInstance.Input.LookY;
            _animator.transform.localEulerAngles = Vector3.zero;

            _chestIK.transform.localEulerAngles = Vector3.zero;
            _headIK.transform.localEulerAngles = Vector3.zero;
            _adjustingRotation = false;

            enabled = true;
        }

        private string GetPath(Transform t)
        {
            string path = t.name;

            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }

            return path;
        }

       
        public void ApplySkin(int skinID)
        {
            var manager = ClientInterfaceManager.Instance;

            if (manager == null || manager.characterSkins == null)
            {
                Debug.LogError("ApplySkin: ClientInterfaceManager o characterSkins es null.");
                return;
            }

            if (skinID < 0 || skinID >= manager.characterSkins.Length)
            {
                Debug.LogError($"ApplySkin: ID de skin inválido: {skinID}.");
                return;
            }

            MySkin = manager.characterSkins[skinID];

            if (!MySkin)
            {
                Debug.LogError($"ApplySkin: La skin del ID {skinID} es null.");
                return;
            }

            if (_idsNativeModels != null && skinID < _idsNativeModels.Length && _idsNativeModels[skinID])
            {
                InstallNativeIDSVisual(skinID);
                ApplyFirstPersonSkin();
                return;
            }

            if (tppRenderer == null)
            {
                Debug.LogError("ApplySkin: Falta asignar el renderer TPP de destino.");
                return;
            }

            if (_tppRigRoot == null || _tppRootBone == null)
            {
                Debug.LogError("ApplySkin: Faltan asignar las referencias del rig TPP.");
                return;
            }

            Mesh tppMesh = MySkin.Mesh;

            if (tppMesh == null)
            {
                Debug.LogError("ApplySkin: MySkin.Mesh es null.");
                return;
            }

            if (!ValidateSkinMaterials("TPP", tppMesh, MySkin.Materials))
                return;

            if (!IsWithinRig(_tppRootBone, _tppRigRoot))
            {
                Debug.LogError(
                    "ApplySkin: _tppRootBone no pertenece a _tppRigRoot."
                );
                return;
            }

            // Preparar el mapeo TPP sin modificar todavía el renderer de destino.
            if (!TryBuildBoneMap(
                    "TPP",
                    MySkin.Renderer,
                    tppMesh,
                    _tppRigRoot,
                    out Transform[] tppBones))
            {
                Debug.LogError("ApplySkin: No se pudo validar el mapeo TPP.");
                return;
            }

            // FPP es opcional: solo se mapea si existen el destino y la malla.
            Mesh fppMesh = MySkin.FPP_Mesh;

            bool applyFPP =
                _fppCharacterMesh != null &&
                fppMesh != null;

            Transform[] fppBones = null;

            if (applyFPP)
            {
                if (_fppRigRoot == null || _fppRootBone == null)
                {
                    Debug.LogError("ApplySkin: Faltan asignar las referencias del rig FPP.");
                    return;
                }

                if (!IsWithinRig(_fppRootBone, _fppRigRoot))
                {
                    Debug.LogError(
                        "ApplySkin: _fppRootBone no pertenece a _fppRigRoot."
                    );
                    return;
                }

                if (!ValidateSkinMaterials(
                        "FPP",
                        fppMesh,
                        MySkin.FPP_Materials))
                {
                    return;
                }

                // Importante: usar el renderer de origen FPP, no el Renderer de TPP.
                if (!TryBuildBoneMap(
                        "FPP",
                        MySkin.fppRenderer,
                        fppMesh,
                        _fppRigRoot,
                        out fppBones))
                {
                    Debug.LogError("ApplySkin: No se pudo validar el mapeo FPP.");
                    return;
                }
            }
            else if (fppMesh != null && _fppCharacterMesh == null)
            {
                Debug.LogWarning(
                    "ApplySkin: No hay renderer FPP de destino; se aplicará solamente TPP."
                );
            }

            // Ambos mapeos requeridos se validaron. Ahora sí aplicamos los cambios.

            tppRenderer.sharedMesh = tppMesh;
            tppRenderer.sharedMaterials = MySkin.Materials;
            tppRenderer.bones = tppBones;
            tppRenderer.rootBone = _tppRootBone;
            tppRenderer.updateWhenOffscreen = true;

            _characterMeshes = new SkinnedMeshRenderer[] { tppRenderer };
            InstallNativeIDSVisual(skinID);

            if (applyFPP)
            {
                _fppCharacterMesh.sharedMesh = fppMesh;
                _fppCharacterMesh.sharedMaterials = MySkin.FPP_Materials;
                _fppCharacterMesh.bones = fppBones;
                _fppCharacterMesh.rootBone = _fppRootBone;
            }
        }

                
        void ApplyFirstPersonSkin()
        {
            if (!_fppCharacterMesh || !MySkin.FPP_Mesh || !MySkin.fppRenderer) return;
            if (!ValidateSkinMaterials("FPP", MySkin.FPP_Mesh, MySkin.FPP_Materials)) return;
            if (!TryBuildBoneMap("FPP", MySkin.fppRenderer, MySkin.FPP_Mesh, _fppRigRoot, out var bones)) return;
            _fppCharacterMesh.sharedMesh = MySkin.FPP_Mesh;
            _fppCharacterMesh.sharedMaterials = MySkin.FPP_Materials;
            _fppCharacterMesh.bones = bones;
            _fppCharacterMesh.rootBone = _fppRootBone;
        }

        void InstallNativeIDSVisual(int skinID)
        {
            if (_nativeIDSVisual) Destroy(_nativeIDSVisual);
            if (_idsNativeModels == null || skinID < 0 || skinID >= _idsNativeModels.Length || !_idsNativeModels[skinID]) return;
            _nativeIDSVisual = Instantiate(_idsNativeModels[skinID], _tppModelAnimator.transform.parent);
            _nativeIDSVisual.name = "IDS gameplay visual - " + MySkin.SkinName;
            GameManager.SetLayerRecursively(_nativeIDSVisual, 8);
            var pivot = _nativeIDSVisual.transform;
            pivot.localPosition = _tppModelAnimator.transform.localPosition;
            pivot.localRotation = _tppModelAnimator.transform.localRotation;
            var native = _nativeIDSVisual.GetComponent<Animator>();
            native.runtimeAnimatorController = null;
            native.applyRootMotion = false;
            native.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            foreach (var script in _nativeIDSVisual.GetComponents<MonoBehaviour>())
                if (script.GetType().Name == "NaturalLegStance") script.enabled = false;
            var meshes = _nativeIDSVisual.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            // Preserve native proportions while fitting the two-metre gameplay capsule.
            if (meshes.Length > 0)
            {
                var bounds = meshes[0].bounds;
                foreach (var mesh in meshes) bounds.Encapsulate(mesh.bounds);
                pivot.localScale *= 2f / Mathf.Max(.1f, bounds.size.y);
            }
            var retarget = _nativeIDSVisual.AddComponent<IDSGameplayPose>();
            retarget.Source = _tppModelAnimator; retarget.Target = native; retarget.Character = _characterInstance;
            _tppModelAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            tppRenderer.enabled = false;
            _characterMeshes = meshes;
            foreach (var mesh in meshes) { mesh.updateWhenOffscreen = true; mesh.enabled = !_characterInstance.FPP; }
        }

        private bool TryBuildBoneMap(
            string label,
            SkinnedMeshRenderer sourceRenderer,
            Mesh mesh,
            Transform targetRigRoot,
            out Transform[] mappedBones)
        {
            mappedBones = null;

            if (sourceRenderer == null)
            {
                Debug.LogError($"[{label}] Falta asignar el renderer de origen.");
                return false;
            }

            if (mesh == null)
            {
                Debug.LogError($"[{label}] La malla es null.");
                return false;
            }

            if (targetRigRoot == null)
            {
                Debug.LogError($"[{label}] La raíz del rig de destino es null.");
                return false;
            }

            // El renderer de origen debe describir esta misma malla.
            if (sourceRenderer.sharedMesh != mesh)
            {
                Debug.LogError(
                    $"[{label}] La malla de origen no coincide con la malla asignada. " +
                    $"Renderer: {sourceRenderer.sharedMesh?.name ?? "null"}, " +
                    $"Mesh: {mesh.name}."
                );
                return false;
            }

            Transform[] sourceBones = sourceRenderer.bones;

            if (mesh.bindposes.Length != sourceBones.Length)
            {
                Debug.LogError(
                    $"[{label}] Cantidades incompatibles: " +
                    $"Bindposes={mesh.bindposes.Length}, " +
                    $"SourceBones={sourceBones.Length}, Mesh={mesh.name}."
                );
                return false;
            }

            bool[] usedBones = new bool[sourceBones.Length];

            if (!CollectUsedBones(mesh, usedBones, label))
                return false;

            // Crear el índice del rig de destino sin aceptar nombres ambiguos.
            var targetByName = new Dictionary<string, Transform>();
            var ambiguousNames = new HashSet<string>();

            Transform[] targetTransforms =
                targetRigRoot.GetComponentsInChildren<Transform>(true);

            foreach (Transform targetBone in targetTransforms)
            {
                string boneName = targetBone.name;

                if (ambiguousNames.Contains(boneName))
                    continue;

                if (targetByName.ContainsKey(boneName))
                {
                    targetByName.Remove(boneName);
                    ambiguousNames.Add(boneName);
                }
                else
                {
                    targetByName.Add(boneName, targetBone);
                }
            }

            mappedBones = new Transform[sourceBones.Length];

            bool mappingOK = true;

            for (int i = 0; i < sourceBones.Length; i++)
            {
                Transform sourceBone = sourceBones[i];

                if (sourceBone == null)
                {
                    if (usedBones[i])
                    {
                        Debug.LogError(
                            $"[{label}] El índice {i} está utilizado, " +
                            "pero su hueso de origen es null."
                        );

                        mappingOK = false;
                    }

                    continue;
                }

                string boneName = sourceBone.name.Replace("Reg.", "Leg.");

                if (ambiguousNames.Contains(boneName))
                {
                    if (usedBones[i])
                    {
                        Debug.LogError(
                            $"[{label}] El hueso utilizado '{boneName}' " +
                            "tiene múltiples coincidencias en el rig de destino."
                        );

                        mappingOK = false;
                    }

                    continue;
                }

                if (targetByName.TryGetValue(
                        boneName,
                        out Transform targetBone))
                {
                    mappedBones[i] = targetBone;
                }
                else if (usedBones[i])
                {
                    Debug.LogError(
                        $"[{label}] Falta el hueso utilizado '{boneName}' " +
                        $"(índice {i}) en el rig de destino."
                    );

                    mappingOK = false;
                }
                // Si no se utiliza, el hueso puede quedar sin mapear.
                // Esto permite tolerar huesos terminales adicionales.
            }

            if (!mappingOK)
                mappedBones = null;

            return mappingOK;
        }

        
        private bool CollectUsedBones(Mesh mesh, bool[] usedBones, string label)
        {
            BoneWeight[] weights = mesh.boneWeights;

            if (weights.Length != mesh.vertexCount)
            {
                Debug.LogError(
                    $"[{label}] La cantidad de BoneWeights ({weights.Length}) " +
                    $"no coincide con los vértices ({mesh.vertexCount}) " +
                    $"en {mesh.name}."
                );

                return false;
            }

            bool valid = true;
            var reportedInvalidIndices = new HashSet<int>();

            for (int vertex = 0; vertex < weights.Length; vertex++)
            {
                BoneWeight weight = weights[vertex];

                valid = MarkBoneUsed(
                    weight.boneIndex0, weight.weight0, usedBones,
                    reportedInvalidIndices, label, vertex) && valid;

                valid = MarkBoneUsed(
                    weight.boneIndex1, weight.weight1, usedBones,
                    reportedInvalidIndices, label, vertex) && valid;

                valid = MarkBoneUsed(
                    weight.boneIndex2, weight.weight2, usedBones,
                    reportedInvalidIndices, label, vertex) && valid;

                valid = MarkBoneUsed(
                    weight.boneIndex3, weight.weight3, usedBones,
                    reportedInvalidIndices, label, vertex) && valid;
            }

            return valid;
        }

        private bool MarkBoneUsed(
            int index,
            float weight,
            bool[] usedBones,
            HashSet<int> reportedInvalidIndices,
            string label,
            int vertex)
        {
            if (weight <= 0f)
                return true;

            if (index < 0 || index >= usedBones.Length)
            {
                if (reportedInvalidIndices.Add(index))
                {
                    Debug.LogError(
                        $"[{label}] El vértice {vertex} utiliza el índice " +
                        $"{index}, fuera del rango de huesos " +
                        $"0..{usedBones.Length - 1}."
                    );
                }

                return false;
            }

            usedBones[index] = true;
            return true;
        }

        private bool ValidateSkinMaterials(
            string label,
            Mesh mesh,
            Material[] materials)
        {
            int materialCount = materials == null ? 0 : materials.Length;

            if (materialCount != mesh.subMeshCount)
            {
                Debug.LogError(
                    $"[{label}] La malla '{mesh.name}' tiene " +
                    $"{mesh.subMeshCount} submeshes, pero " +
                    $"{materialCount} materiales."
                );

                return false;
            }

            return true;
        }

        private bool IsWithinRig(Transform bone, Transform rigRoot)
        {
            return bone != null &&
                rigRoot != null &&
                (bone == rigRoot || bone.IsChildOf(rigRoot));
        }




        #region callbacks

        private void OnHealthDepleted(CharacterPart damagedPart, byte attackerID)
        {
            _animator.runtimeAnimatorController = _baseAnimatorController;
        }

        void OnPerspectiveChanged(bool fpp)
        {
            //we cannot just disable character model, because it has hitboxes so host
            //would be immortal to bots
            foreach (SkinnedMeshRenderer mesh in _characterMeshes)
            {
                if (mesh == null)
                    continue;

                mesh.enabled = !fpp &&
                            _characterInstance != null &&
                            _characterInstance.Health.CurrentHealth > 0;
            }

            _fppModelAnimator.gameObject.SetActive(fpp);
            _animator = fpp ? _fppModelAnimator : _tppModelAnimator;
            _fppCharacterMesh.gameObject.layer = (int)GameLayers.fppModels;

            //dont set animator controllers twice for same perspective
            if (fppPerspective != fpp)
                AssingCharacterAnimationsForCurrentlyUsedItem(_characterItemManager.CurrentlyUsedSlotID);

            fppPerspective = fpp;
        }
        #endregion


        void AssingCharacterAnimationsForCurrentlyUsedItem(int slotID = -1)
        {
            Item currentlyUsedItem = slotID >= 0
                ? _characterItemManager.Slots[slotID].Item
                : null;

            if (currentlyUsedItem)
                _animator.runtimeAnimatorController = _characterInstance.FPP
                    ? currentlyUsedItem.AnimatorControllerForCharacterFPP
                    : currentlyUsedItem.AnimatorControllerForCharacter;
            else
                _animator.runtimeAnimatorController = _characterInstance.FPP
                    ? BaseAnimatorControllerFPP
                    : _baseAnimatorController;

            if (_animator.runtimeAnimatorController == null)
                _animator.runtimeAnimatorController = _baseAnimatorController;

            SetTrigger(AnimationNames.ITEM_TAKE);
        }


        void UpdateAnimationProperties()
        {
            if (!_animator.runtimeAnimatorController) return;

            bool animIsGrounded = _characterInstance.isGrounded;

            _characterInstance.IsRunning = _motor && _motor.WantsToSprint && animIsGrounded;

            if (_characterInstance.IsRunning)
                _characterInstance.IsAbleToUseItem = false;

            _speed = _animationVelocity.sqrMagnitude <= .0225f ? 0f : (_characterInstance.IsRunning &&
                !_characterInstance.ReadActionKeyCode(ActionCodes.Trigger1) &&
                !_characterInstance.ReadActionKeyCode(ActionCodes.Trigger2) && 
                !_characterInstance.IsUsingItem && 
                !_characterInstance.IsScoping && 
                !_characterInstance.IsCrouching ? 2f : (_animationVelocity.sqrMagnitude > .0225f ? 1f : 0f));

            if (!_characterInstance.FPP)
            {
                float strideReference = _characterInstance.IsCrouching ? 2.2f : (_characterInstance.IsRunning ? 6f : 3.4f);
                _animator.SetFloat("locomotionRate", Mathf.Clamp(_animationVelocity.magnitude / strideReference, .35f, 1.5f), .1f, Time.deltaTime);
            }

            //animate character
            if (!_animator.runtimeAnimatorController) return;

            _animator.SetFloat(AnimationNames.ITEM_SPEED, _characterInstance.Block ? 0f : _speed); //universal parameter for fpp and tpp models

            _upperBodyAnimateFactor = !_characterInstance.IsRunning || _characterInstance.IsReloading || _characterInstance.IsScoping
                ? 1f : (_characterItemManager.CurrentlyUsedItem ? .8f : 0f);

            Vector3 localVelocity = Quaternion.Inverse(Quaternion.Euler(0, _currentBodyRotation, 0)) * _animationVelocity;
            float referenceSpeed = _motor ? (_characterInstance.IsCrouching ? _motor.CrouchSpeed : (_characterInstance.IsRunning ? _motor.RunSpeed : _motor.WalkSpeed)) : 5f;
            Vector2 movementAnim = Vector2.ClampMagnitude(new Vector2(localVelocity.x, localVelocity.z) / Mathf.Max(.1f, referenceSpeed), 1f);
            if (_characterInstance.Block) movementAnim = Vector2.zero;

            //parameter for tpp character model only
            if (!_characterInstance.FPP) 
            {
                _animator.SetFloat(AnimationNames.CHARACTER_LOOK, -_characterInstance.Input.LookX);
                //suavizado: el input de remotos llega cuantizado y el de los bots salta en cada
                //esquina del camino; sin damping el blend tree cambia de clip de golpe y las piernas tiemblan
                _animator.SetFloat(AnimationNames.CHARACTER_MOVEMENT_HORIZONTAL, movementAnim.x, _movementBlendDamping, Time.deltaTime);
                _animator.SetFloat(AnimationNames.CHARACTER_MOVEMENT_VERTICAL, movementAnim.y, _movementBlendDamping, Time.deltaTime);
                _animator.SetBool(AnimationNames.CHARACTER_ISGROUNDED, animIsGrounded);
                _animator.SetBool(AnimationNames.CHARACTER_ISCROUCHING, _characterInstance.IsCrouching);
                _animator.SetFloat(AnimationNames.CHARACTER_ISCROUCHING_FLOAT, _characterInstance.IsCrouching ? 1f : 0f);
                _animator.SetLayerWeight(1, Mathf.Lerp(_animator.GetLayerWeight(1),
                    _upperBodyAnimateFactor, 12f * Time.deltaTime));
            }
        }

        void UpdateRotationAnimation(float timestep)
        {
            if (_characterInstance.Health.CurrentHealth <= 0) return;
            if (!_animator.runtimeAnimatorController) return;

            float difference = -Mathf.DeltaAngle(_characterInstance.Input.LookY, _currentBodyRotation);
            if (difference > 0 && Mathf.Abs(difference) > _rotationAngleThreshold ||
                difference < 0 && Mathf.Abs(difference) > _rotationAngleThresholdLeftSide)
            {
                if (!_adjustingRotation)
                {
                    if (_speed == 0)
                        _animator.CrossFade("turn", 0.05f);

                    _adjustingRotation = true;
                }
            }
            else if (Mathf.Abs(difference) < 1)
                _adjustingRotation = false;

            if (_characterInstance.Input.Movement != Vector2.zero || !_characterInstance.isGrounded)
                _currentBodyRotation = Mathf.MoveTowardsAngle(_currentBodyRotation, _characterInstance.Input.LookY, 540f * timestep);

            if (_adjustingRotation)
                _currentBodyRotation = ClampRotation(
                    Mathf.MoveTowardsAngle(_currentBodyRotation, _characterInstance.Input.LookY, _adjustingRotationLerpSpeed * timestep),
                    _characterInstance.Input.LookY, _rotationAngleThreshold);


            _recoilX = Mathf.Lerp(_recoilX, 0f, _recoilStabilizationSpeed * timestep);
            
            _takingDamge_IKSpine = Vector3.Lerp(_takingDamge_IKSpine, Vector3.zero, _stabilizationAfterTakingDamageSpeed * timestep);
            _takingDamge_IKHead = Vector3.Lerp(_takingDamge_IKHead, Vector3.zero, _stabilizationAfterTakingDamageSpeed * 0.7f * timestep);

            // Apply the body rotation before assigning world-space aim targets.
            _animator.transform.eulerAngles = new Vector3(0, _currentBodyRotation, 0);
            _chestIK.weight = _upperBodyAnimateFactor;
            Vector3 chestRot = new Vector3(-_recoilX + _characterInstance.Input.LookX * 0.15f + _takingDamge_IKSpine.z, _characterInstance.Input.LookY, -_takingDamge_IKSpine.x);
            var chestData = _chestIK.data; chestData.rotation = chestRot; _chestIK.data = chestData;

            _headIK.weight = _upperBodyAnimateFactor;
            Vector3 headRot = new Vector3(_characterInstance.Input.LookX * 0.8f + _takingDamge_IKHead.z, _characterInstance.Input.LookY, -_takingDamge_IKHead.x);
            var headData = _headIK.data; headData.rotation = headRot; _headIK.data = headData;
        }

        private void OnDamaged(int currentHealth, CharacterPart hittedPartID, AttackType attackType, byte attackerID)
        {
            Health attacker = GameSync.Singleton.Healths.GetObj(attackerID);

            if (!attacker) return;

            Vector3 direction = transform.position - attacker.transform.position;
            direction = transform.InverseTransformDirection(direction);
            direction.Normalize();

            if (hittedPartID != CharacterPart.head)
            {
                _takingDamge_IKSpine.x = direction.x * 30f + UnityEngine.Random.Range(-3f, 3f);
                _takingDamge_IKSpine.z = direction.z * 30f;
            }
            else
            {
                _takingDamge_IKSpine.x = direction.x * 20f + UnityEngine.Random.Range(-2f, 2f);
                _takingDamge_IKSpine.z = direction.z * 20f;

                _takingDamge_IKHead.x = direction.x * 58 + UnityEngine.Random.Range(-3f, 3f);
                _takingDamge_IKHead.z = direction.z * 58;
            }
        }

        public void AddRecoil(float recoil) 
        {
            _recoilX = recoil;
        }

        float ClampRotation(float currentRot, float targetRot, float threshold)
        {
            float difference = Mathf.DeltaAngle(currentRot, targetRot);

            if (difference > threshold || difference < -threshold)
            {
                return targetRot - Mathf.Clamp(difference,-threshold,threshold);
            }
            else
                return currentRot;
        }

        #region external access

        public void PlayAnimation(string name)
        {
            if (_animator != null &&
                _animator.isActiveAndEnabled)
            {
                _animator.Play(name);
                ForwardVisualState(Animator.StringToHash(name));
            }
        }

        public void PlayAnimation(int hash)
        {
            if (_animator != null &&
                _animator.isActiveAndEnabled)
            {
                _animator.Play(hash);
                ForwardVisualState(hash);
            }
        }

        public void SetTrigger(string triggerName)
        {
            if (_animator.gameObject.activeInHierarchy) 
                SetTrigger(Animator.StringToHash(triggerName));
        }

        public void ResetTrigger(string triggerName) => ResetTrigger(Animator.StringToHash(triggerName));
        public void SetTrigger(int triggerHash) { _animator.SetTrigger(triggerHash); ForwardVisualTrigger(triggerHash, true); }
        public void ResetTrigger(int triggerHash) { _animator.ResetTrigger(triggerHash); ForwardVisualTrigger(triggerHash, false); }
        void ForwardVisualTrigger(int hash, bool value)
        {
            if (_animator == _tppModelAnimator && _nativeIDSVisual)
                _nativeIDSVisual.GetComponent<IDSGameplayPose>()?.SetVisualTrigger(hash, value);
        }
        void ForwardVisualState(int hash)
        {
            if (_animator == _tppModelAnimator && _nativeIDSVisual)
                _nativeIDSVisual.GetComponent<IDSGameplayPose>()?.PlayVisualState(hash);
        }

        public void ResetAnimator() 
        {
            _tppModelAnimator.Rebind();
            _tppModelAnimator.WriteDefaultValues();
        }

        public void RenderCharacterModel(bool render)
        {
            _rigBuilder.enabled = render;
            _tppModelAnimator.gameObject.SetActive(render);
            if (_nativeIDSVisual) _nativeIDSVisual.SetActive(render);
        }
        #endregion

        private void OnDestroy()
        {
            _characterInstance.Client_OnPerspectiveSet -= OnPerspectiveChanged;
            _characterItemManager.Client_EquipmentChanged -= AssingCharacterAnimationsForCurrentlyUsedItem;
            _characterInstance.Health.Client_OnHealthDepleted -= OnHealthDepleted;

            _characterInstance = null;
            _characterItemManager = null;
        }
    }
}
