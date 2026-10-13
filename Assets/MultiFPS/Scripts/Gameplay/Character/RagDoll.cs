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
            
            // --------------------------------------------------
            // BUSCAR RIG SOLDIER DEL RAGDOLL
            // --------------------------------------------------

            Transform rigSoldier = null;

            foreach (Transform t in GetComponentsInChildren<Transform>(true))
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




            if (!SkinBoneMapper.TryMapBones(
                    skin,
                    rigSoldier,
                    out Transform[] newBones,
                    "[RagDoll]"))
            {
                Debug.LogError(
                    "[RagDoll] No se aplicó la skin por un error de mapeo."
                );
                return;
            }


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
            
            if (skin.Materials == null ||
                skin.Materials.Length != skin.Mesh.subMeshCount)
            {
                Debug.LogError(
                    $"[RagDoll] Skin '{skin.SkinName}': cantidad de materiales incorrecta."
                );
                return;
            }

            _characterMesh.sharedMesh = skin.Mesh;
            _characterMesh.sharedMaterials = skin.Materials;
            _characterMesh.bones = newBones;

            // Buscar hips en el rig de destino.
            Transform hips = null;

            foreach (Transform bone in
                    rigSoldier.GetComponentsInChildren<Transform>(true))
            {
                if (bone.name == "hips")
                {
                    hips = bone;
                    break;
                }
            }

            if (hips == null)
            {
                Debug.LogError("[RagDoll] No se encontró hips.");
                return;
            }

            _characterMesh.rootBone = hips;
            _characterMesh.updateWhenOffscreen = true;

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