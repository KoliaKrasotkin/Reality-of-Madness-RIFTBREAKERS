#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class RagdollComponentCopierWindow : EditorWindow
{
    private GameObject oldRagdoll;
    private GameObject newRagdoll;

    private Vector2 scroll;

    [MenuItem("MultiFPS/Ragdoll Copier")]
    public static void OpenWindow()
    {
        RagdollComponentCopierWindow window =
            GetWindow<RagdollComponentCopierWindow>(
                "Ragdoll Copier"
            );

        window.minSize = new Vector2(520f, 650f);
    }

    private void OnGUI()
    {
        GUILayout.Space(10);

        EditorGUILayout.LabelField(
            "MultiFPS Ragdoll Copier",
            EditorStyles.boldLabel
        );

        EditorGUILayout.HelpBox(
            "Copia Rigidbody, Colliders y CharacterJoints " +
            "del ragdoll viejo al nuevo buscando los huesos por nombre.\n\n" +
            "Los CharacterJoint.connectedBody serán remapeados " +
            "automáticamente al Rigidbody correspondiente del nuevo ragdoll.",
            MessageType.Info
        );

        GUILayout.Space(10);

        // ========================================================
        // OLD
        // ========================================================

        EditorGUILayout.LabelField(
            "Ragdoll VIEJO",
            EditorStyles.boldLabel
        );

        oldRagdoll = (GameObject)EditorGUILayout.ObjectField(
            oldRagdoll,
            typeof(GameObject),
            true
        );

        GUILayout.Space(8);

        // ========================================================
        // NEW
        // ========================================================

        EditorGUILayout.LabelField(
            "Ragdoll NUEVO",
            EditorStyles.boldLabel
        );

        newRagdoll = (GameObject)EditorGUILayout.ObjectField(
            newRagdoll,
            typeof(GameObject),
            true
        );

        GUILayout.Space(10);

        if (oldRagdoll == null || newRagdoll == null)
        {
            EditorGUILayout.HelpBox(
                "Arrastrá el ragdoll viejo y el ragdoll nuevo.",
                MessageType.Warning
            );

            return;
        }

        if (oldRagdoll == newRagdoll)
        {
            EditorGUILayout.HelpBox(
                "El ragdoll viejo y el nuevo no pueden ser el mismo.",
                MessageType.Error
            );

            return;
        }

        DrawPreview();

        GUILayout.FlexibleSpace();

        GUILayout.Space(10);

        if (GUILayout.Button(
                "COPY RAGDOLL COMPONENTS",
                GUILayout.Height(45)))
        {
            if (EditorUtility.DisplayDialog(
                "Confirmar",
                "Se van a modificar los componentes físicos del " +
                "ragdoll NUEVO.\n\n" +
                "El ragdoll viejo no será modificado.",
                "Copiar",
                "Cancelar"))
            {
                CopyRagdoll();
            }
        }

        GUILayout.Space(10);
    }

    // ============================================================
    // PREVIEW
    // ============================================================

    private void DrawPreview()
    {
        Dictionary<string, Transform> newBones =
            BuildBoneDictionary(newRagdoll);

        Transform[] oldTransforms =
            oldRagdoll.GetComponentsInChildren<Transform>(true);

        int physicalBones = 0;
        int foundBones = 0;
        int missingBones = 0;

        scroll = EditorGUILayout.BeginScrollView(
            scroll,
            GUILayout.Height(300)
        );

        EditorGUILayout.LabelField(
            "Huesos físicos encontrados",
            EditorStyles.boldLabel
        );

        foreach (Transform oldBone in oldTransforms)
        {
            Rigidbody rb =
                oldBone.GetComponent<Rigidbody>();

            Collider[] colliders =
                oldBone.GetComponents<Collider>();

            CharacterJoint joint =
                oldBone.GetComponent<CharacterJoint>();

            bool hasPhysics =
                rb != null ||
                colliders.Length > 0 ||
                joint != null;

            if (!hasPhysics)
                continue;

            physicalBones++;

            if (newBones.ContainsKey(oldBone.name))
            {
                foundBones++;

                EditorGUILayout.LabelField(
                    "✓ " + oldBone.name
                );
            }
            else
            {
                missingBones++;

                EditorGUILayout.LabelField(
                    "✗ " + oldBone.name + "  - NO ENCONTRADO",
                    EditorStyles.boldLabel
                );
            }
        }

        EditorGUILayout.EndScrollView();

        MessageType type =
            missingBones == 0
                ? MessageType.Info
                : MessageType.Warning;

        EditorGUILayout.HelpBox(
            "Huesos físicos: " + physicalBones +
            "\nEncontrados: " + foundBones +
            "\nFaltantes: " + missingBones,
            type
        );
    }

    // ============================================================
    // COPY
    // ============================================================

    private void CopyRagdoll()
    {
        if (oldRagdoll == null || newRagdoll == null)
        {
            Debug.LogError(
                "[Ragdoll Copier] Old o New es null."
            );

            return;
        }

        Debug.Log(
            "============================================"
        );

        Debug.Log(
            "[Ragdoll Copier] INICIO"
        );

        Debug.Log(
            "[Ragdoll Copier] OLD: " +
            oldRagdoll.name
        );

        Debug.Log(
            "[Ragdoll Copier] NEW: " +
            newRagdoll.name
        );

        Dictionary<string, Transform> newBones =
            BuildBoneDictionary(newRagdoll);

        Transform[] oldTransforms =
            oldRagdoll.GetComponentsInChildren<Transform>(true);

        int rigidbodies = 0;
        int colliders = 0;
        int joints = 0;
        int missing = 0;

        Undo.RegisterFullObjectHierarchyUndo(
            newRagdoll,
            "Copy Ragdoll Components"
        );

        // ========================================================
        // PASS 1
        // RIGIDBODIES + COLLIDERS
        // ========================================================

        foreach (Transform oldBone in oldTransforms)
        {
            Rigidbody oldRB =
                oldBone.GetComponent<Rigidbody>();

            Collider[] oldColliders =
                oldBone.GetComponents<Collider>();

            CharacterJoint oldJoint =
                oldBone.GetComponent<CharacterJoint>();

            bool hasPhysics =
                oldRB != null ||
                oldColliders.Length > 0 ||
                oldJoint != null;

            if (!hasPhysics)
                continue;

            if (!newBones.TryGetValue(
                    oldBone.name,
                    out Transform newBone))
            {
                Debug.LogWarning(
                    "[Ragdoll Copier] No se encontró el hueso: " +
                    oldBone.name
                );

                missing++;
                continue;
            }

            // ----------------------------------------------------
            // RIGIDBODY
            // ----------------------------------------------------

            if (oldRB != null)
            {
                Rigidbody newRB =
                    newBone.GetComponent<Rigidbody>();

                if (newRB == null)
                {
                    newRB =
                        Undo.AddComponent<Rigidbody>(
                            newBone.gameObject
                        );
                }

                if (newRB == null)
                {
                    Debug.LogError(
                        "[Ragdoll Copier] No se pudo crear Rigidbody en " +
                        newBone.name
                    );

                    continue;
                }

                CopyRigidbody(
                    oldRB,
                    newRB
                );

                rigidbodies++;
            }

            // ----------------------------------------------------
            // COLLIDERS
            // ----------------------------------------------------

            foreach (Collider oldCollider in oldColliders)
            {
                if (oldCollider == null)
                    continue;

                CopyCollider(
                    oldCollider,
                    newBone
                );

                colliders++;
            }
        }

        // ========================================================
        // PASS 2
        // CHARACTER JOINTS
        // ========================================================

        foreach (Transform oldBone in oldTransforms)
        {
            CharacterJoint oldJoint =
                oldBone.GetComponent<CharacterJoint>();

            if (oldJoint == null)
                continue;

            if (!newBones.TryGetValue(
                    oldBone.name,
                    out Transform newBone))
            {
                continue;
            }

            CharacterJoint newJoint =
                newBone.GetComponent<CharacterJoint>();

            if (newJoint == null)
            {
                newJoint =
                    Undo.AddComponent<CharacterJoint>(
                        newBone.gameObject
                    );
            }

            if (newJoint == null)
            {
                Debug.LogError(
                    "[Ragdoll Copier] No se pudo crear CharacterJoint en " +
                    newBone.name
                );

                continue;
            }

            CopyCharacterJoint(
                oldJoint,
                newJoint,
                newBones
            );

            joints++;
        }

        // ========================================================
        // SAVE
        // ========================================================

        EditorUtility.SetDirty(newRagdoll);

        foreach (Transform t in
                 newRagdoll.GetComponentsInChildren<Transform>(true))
        {
            if (t.GetComponent<Rigidbody>() != null ||
                t.GetComponent<CharacterJoint>() != null)
            {
                EditorUtility.SetDirty(t.gameObject);
            }

            foreach (Collider c in
                     t.GetComponents<Collider>())
            {
                if (c != null)
                    EditorUtility.SetDirty(t.gameObject);
            }
        }

        AssetDatabase.SaveAssets();

        Debug.Log(
            "============================================"
        );

        Debug.Log(
            "[Ragdoll Copier] TERMINADO"
        );

        Debug.Log(
            "[Ragdoll Copier] Rigidbody: " +
            rigidbodies
        );

        Debug.Log(
            "[Ragdoll Copier] Colliders: " +
            colliders
        );

        Debug.Log(
            "[Ragdoll Copier] CharacterJoints: " +
            joints
        );

        Debug.Log(
            "[Ragdoll Copier] Huesos faltantes: " +
            missing
        );

        Debug.Log(
            "============================================"
        );

        EditorUtility.DisplayDialog(
            "Ragdoll Copier",
            "Proceso terminado.\n\n" +
            "Rigidbody: " + rigidbodies +
            "\nColliders: " + colliders +
            "\nCharacterJoints: " + joints +
            "\nHuesos faltantes: " + missing,
            "OK"
        );
    }

    // ============================================================
    // BUILD DICTIONARY
    // ============================================================

    private Dictionary<string, Transform>
        BuildBoneDictionary(GameObject root)
    {
        Dictionary<string, Transform> result =
            new Dictionary<string, Transform>();

        if (root == null)
            return result;

        Transform[] transforms =
            root.GetComponentsInChildren<Transform>(true);

        foreach (Transform t in transforms)
        {
            if (t == null)
                continue;

            if (!result.ContainsKey(t.name))
            {
                result.Add(t.name, t);
            }
            else
            {
                Debug.LogWarning(
                    "[Ragdoll Copier] Nombre duplicado: " +
                    t.name
                );
            }
        }

        return result;
    }

    // ============================================================
    // RIGIDBODY
    // ============================================================

    private void CopyRigidbody(
        Rigidbody source,
        Rigidbody destination)
    {
        if (source == null)
        {
            Debug.LogError(
                "[Ragdoll Copier] Source Rigidbody es NULL."
            );

            return;
        }

        if (destination == null)
        {
            Debug.LogError(
                "[Ragdoll Copier] Destination Rigidbody es NULL."
            );

            return;
        }

        destination.mass =
            source.mass;

        destination.linearDamping =
            source.linearDamping;

        destination.angularDamping =
            source.angularDamping;

        destination.useGravity =
            source.useGravity;

        destination.isKinematic =
            source.isKinematic;

        destination.interpolation =
            source.interpolation;

        destination.collisionDetectionMode =
            source.collisionDetectionMode;

        destination.constraints =
            source.constraints;

        destination.detectCollisions =
            source.detectCollisions;
    }

    // ============================================================
    // COLLIDER
    // ============================================================

    private void CopyCollider(
        Collider source,
        Transform destination)
    {
        if (source == null || destination == null)
            return;

        // --------------------------------------------------------
        // CAPSULE
        // --------------------------------------------------------

        CapsuleCollider sourceCapsule =
            source as CapsuleCollider;

        if (sourceCapsule != null)
        {
            CapsuleCollider destinationCapsule =
                destination.GetComponent<CapsuleCollider>();

            if (destinationCapsule == null)
            {
                destinationCapsule =
                    Undo.AddComponent<CapsuleCollider>(
                        destination.gameObject
                    );
            }

            destinationCapsule.center =
                sourceCapsule.center;

            destinationCapsule.radius =
                sourceCapsule.radius;

            destinationCapsule.height =
                sourceCapsule.height;

            destinationCapsule.direction =
                sourceCapsule.direction;

            CopyColliderBase(
                sourceCapsule,
                destinationCapsule
            );

            return;
        }

        // --------------------------------------------------------
        // BOX
        // --------------------------------------------------------

        BoxCollider sourceBox =
            source as BoxCollider;

        if (sourceBox != null)
        {
            BoxCollider destinationBox =
                destination.GetComponent<BoxCollider>();

            if (destinationBox == null)
            {
                destinationBox =
                    Undo.AddComponent<BoxCollider>(
                        destination.gameObject
                    );
            }

            destinationBox.center =
                sourceBox.center;

            destinationBox.size =
                sourceBox.size;

            CopyColliderBase(
                sourceBox,
                destinationBox
            );

            return;
        }

        // --------------------------------------------------------
        // SPHERE
        // --------------------------------------------------------

        SphereCollider sourceSphere =
            source as SphereCollider;

        if (sourceSphere != null)
        {
            SphereCollider destinationSphere =
                destination.GetComponent<SphereCollider>();

            if (destinationSphere == null)
            {
                destinationSphere =
                    Undo.AddComponent<SphereCollider>(
                        destination.gameObject
                    );
            }

            destinationSphere.center =
                sourceSphere.center;

            destinationSphere.radius =
                sourceSphere.radius;

            CopyColliderBase(
                sourceSphere,
                destinationSphere
            );

            return;
        }

        Debug.LogWarning(
            "[Ragdoll Copier] Collider no soportado: " +
            source.GetType().Name +
            " en " +
            source.name
        );
    }

    // ============================================================
    // COLLIDER BASE
    // ============================================================

    private void CopyColliderBase(
        Collider source,
        Collider destination)
    {
        if (source == null || destination == null)
            return;

        destination.isTrigger =
            source.isTrigger;

        destination.contactOffset =
            source.contactOffset;

        destination.sharedMaterial =
            source.sharedMaterial;

        destination.enabled =
            source.enabled;
    }

    // ============================================================
    // CHARACTER JOINT
    // ============================================================

    private void CopyCharacterJoint(
        CharacterJoint source,
        CharacterJoint destination,
        Dictionary<string, Transform> newBones)
    {
        if (source == null || destination == null)
            return;

        // --------------------------------------------------------
        // CONNECTED BODY
        // --------------------------------------------------------

        destination.connectedBody = null;

        if (source.connectedBody != null)
        {
            string connectedName =
                source.connectedBody.gameObject.name;

            if (newBones.TryGetValue(
                    connectedName,
                    out Transform newConnectedBone))
            {
                Rigidbody newConnectedBody =
                    newConnectedBone.GetComponent<Rigidbody>();

                if (newConnectedBody != null)
                {
                    destination.connectedBody =
                        newConnectedBody;
                }
                else
                {
                    Debug.LogWarning(
                        "[Ragdoll Copier] '" +
                        connectedName +
                        "' no tiene Rigidbody en el ragdoll nuevo."
                    );
                }
            }
            else
            {
                Debug.LogWarning(
                    "[Ragdoll Copier] No se encontró connectedBody: " +
                    connectedName
                );
            }
        }

        // --------------------------------------------------------
        // ANCHORS
        // --------------------------------------------------------

        destination.anchor =
            source.anchor;

        destination.axis =
            source.axis;

        destination.autoConfigureConnectedAnchor =
            source.autoConfigureConnectedAnchor;

        destination.connectedAnchor =
            source.connectedAnchor;

        destination.swingAxis =
            source.swingAxis;

        // --------------------------------------------------------
        // LIMITS
        // --------------------------------------------------------

        destination.lowTwistLimit =
            source.lowTwistLimit;

        destination.highTwistLimit =
            source.highTwistLimit;

        destination.swing1Limit =
            source.swing1Limit;

        destination.swing2Limit =
            source.swing2Limit;

        // --------------------------------------------------------
        // PROJECTION
        // --------------------------------------------------------

        destination.enableProjection =
            source.enableProjection;

        destination.projectionDistance =
            source.projectionDistance;

        destination.projectionAngle =
            source.projectionAngle;

        // --------------------------------------------------------
        // COLLISION / PREPROCESSING
        // --------------------------------------------------------

        destination.enablePreprocessing =
            source.enablePreprocessing;

        destination.enableCollision =
            source.enableCollision;

        // --------------------------------------------------------
        // BREAK
        // --------------------------------------------------------

        destination.breakForce =
            source.breakForce;

        destination.breakTorque =
            source.breakTorque;

        // --------------------------------------------------------
        // MASS
        // --------------------------------------------------------

        destination.massScale =
            source.massScale;

        destination.connectedMassScale =
            source.connectedMassScale;
    }
}

#endif
