using System.Collections.Generic;
using UnityEngine;

namespace MultiFPS.Gameplay
{
    public class RagDoll : MonoBehaviour
    {
        [SerializeField] SkinnedMeshRenderer _characterMesh;
        [SerializeField] float _ragdollRecoil;
        [SerializeField] float _ragdollMovementRecoil = 100f;
        [SerializeField] GameObject _headExplosion;

        // for animation ragdoll on death
        public Rigidbody _head;
        public Rigidbody _footL;
        public Rigidbody _footR;
        public Rigidbody _hips;

        // for enabling/disabling physics
        [SerializeField] Rigidbody[] _rigidBodies;

        // for synchronization
        public Transform[] SynchronizedRigidBodies;
        public Transform[] SynchronizedLimbFlexors;

        [Tooltip("If You want character moving direction to be used for ragdoll then set this to true")]
        public bool ApplyMovementVelocity = true;

        private void Awake()
        {
            GameTools.SetLayerRecursively(
                gameObject,
                (int)GameLayers.ragdoll
            );

            EnablePhysics(false);
        }

        public void ApplySkin(SkinContainer skin)
        {
            if (skin == null)
            {
                Debug.LogError(
                    "[RagDoll] ApplySkin: skin es NULL."
                );

                return;
            }

            if (_characterMesh == null)
            {
                Debug.LogError(
                    "[RagDoll] ApplySkin: _characterMesh es NULL."
                );

                return;
            }

            if (skin.Mesh == null)
            {
                Debug.LogError(
                    "[RagDoll] ApplySkin: skin.Mesh es NULL."
                );

                return;
            }

            if (skin.Renderer == null)
            {
                Debug.LogError(
                    "[RagDoll] ApplySkin: skin.Renderer no está asignado."
                );

                return;
            }

            // --------------------------------------------------
            // BUSCAR RIG SOLDIER DEL RAGDOLL
            // --------------------------------------------------

            Transform rigSoldier = null;

            foreach (Transform t in
                     GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "rig_soldier")
                {
                    rigSoldier = t;
                    break;
                }
            }

            if (rigSoldier == null)
            {
                Debug.LogError(
                    "[RagDoll] ApplySkin: no se encontró rig_soldier."
                );

                return;
            }

            // --------------------------------------------------
            // CREAR DICCIONARIO DE BONES DEL SOLDIER
            // --------------------------------------------------

            Dictionary<string, Transform> soldierBones =
                new Dictionary<string, Transform>();

            foreach (Transform bone in
                     rigSoldier.GetComponentsInChildren<Transform>(true))
            {
                if (!soldierBones.ContainsKey(bone.name))
                {
                    soldierBones.Add(
                        bone.name,
                        bone
                    );
                }
            }

            // --------------------------------------------------
            // OBTENER BONES DEL REDHEAD ORIGINAL
            // --------------------------------------------------

            Transform[] sourceBones =
                skin.Renderer.bones;

            Transform[] newBones =
                new Transform[sourceBones.Length];

            bool mappingOK = true;

            // --------------------------------------------------
            // MAPEAR CADA BONE POR NOMBRE
            // --------------------------------------------------

            for (int i = 0; i < sourceBones.Length; i++)
            {
                Transform sourceBone =
                    sourceBones[i];

                if (sourceBone == null)
                {
                    newBones[i] = null;
                    continue;
                }

                string boneName =
                    sourceBone.name;

                // Corrección del typo que tenía el RedHead original
                if (boneName == "lowerReg.R")
                {
                    boneName = "lowerLeg.R";
                }

                if (soldierBones.TryGetValue(
                        boneName,
                        out Transform soldierBone))
                {
                    newBones[i] = soldierBone;
                }
                else
                {
                    newBones[i] = null;

                    // Comprobar si realmente es utilizado
                    bool used = false;

                    BoneWeight[] weights =
                        skin.Mesh.boneWeights;

                    foreach (BoneWeight w in weights)
                    {
                        if ((w.weight0 > 0 &&
                             w.boneIndex0 == i) ||

                            (w.weight1 > 0 &&
                             w.boneIndex1 == i) ||

                            (w.weight2 > 0 &&
                             w.boneIndex2 == i) ||

                            (w.weight3 > 0 &&
                             w.boneIndex3 == i))
                        {
                            used = true;
                            break;
                        }
                    }

                    if (used)
                    {
                        Debug.LogError(
                            "[RagDoll] FALTA BONE UTILIZADO: " +
                            boneName
                        );

                        mappingOK = false;
                    }
                }
            }

            if (!mappingOK)
            {
                Debug.LogError(
                    "[RagDoll] Mapping incompleto. " +
                    "NO se aplicó el skin."
                );

                return;
            }

            // --------------------------------------------------
            // APLICAR MESH
            // --------------------------------------------------

            _characterMesh.sharedMesh =
                skin.Mesh;

            if (skin.Material != null)
            {
                _characterMesh.sharedMaterial =
                    skin.Material;
            }

            // --------------------------------------------------
            // APLICAR BONES REMAPEADOS
            // --------------------------------------------------

            _characterMesh.bones =
                newBones;

            // --------------------------------------------------
            // ROOT BONE
            // --------------------------------------------------

            if (soldierBones.TryGetValue(
                    "hips",
                    out Transform hips))
            {
                _characterMesh.rootBone =
                    hips;
            }
            else
            {
                Debug.LogError(
                    "[RagDoll] No se encontró hips."
                );

                return;
            }

            _characterMesh.updateWhenOffscreen = true;

            Debug.Log(
                "[RagDoll] RedHead aplicado correctamente al ragdoll."
            );
        }

        // client side ragdoll preparation
        public void ActivateRagdoll(CharacterPart hittedPart)
        {
            if (hittedPart == CharacterPart.head)
            {
                _head.transform.localScale =
                    Vector3.zero;

                _ragdollRecoil *= 1.6f;

                if (_headExplosion)
                {
                    Instantiate(
                        _headExplosion,
                        _head.position,
                        _head.rotation
                    ).transform.SetParent(transform);
                }
            }
        }

        // server side ragdoll preparation
        public void ServerActivateRagdoll(
            Vector3 myPosition,
            Vector3 killerPosition,
            Vector3 movementDirection,
            int attackForce)
        {
            EnablePhysics(true);

            _head.AddForce(
                (myPosition - killerPosition).normalized *
                _ragdollRecoil
            );

            if (ApplyMovementVelocity)
            {
                _hips.AddForce(
                    movementDirection *
                    _ragdollMovementRecoil
                );
            }

            _hips.AddForce(
                (myPosition - killerPosition).normalized *
                attackForce
            );

            int foot =
                Random.Range(0, 2);

            if (foot == 0)
            {
                float footForceDirection =
                    Random.Range(-1, 2);

                if (Random.Range(1, 3) == 1)
                {
                    _footL.AddForce(
                        (killerPosition - transform.position).normalized *
                        footForceDirection *
                        _ragdollRecoil
                    );
                }
                else
                {
                    _footR.AddForce(
                        (killerPosition - transform.position).normalized *
                        footForceDirection *
                        _ragdollRecoil
                    );
                }
            }
        }

        public void EnablePhysics(bool enable)
        {
            foreach (Rigidbody rg in _rigidBodies)
            {
                if (rg == null)
                    continue;

                rg.useGravity = enable;
                rg.isKinematic = !enable;
            }
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _rigidBodies.Length; i++)
            {
                _rigidBodies[i] = null;
            }

            _head = null;
            _footL = null;
            _footR = null;
            _hips = null;
        }
    }
}