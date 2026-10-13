
using System.Collections.Generic;
using UnityEngine;

namespace MultiFPS.Gameplay
{
    public static class SkinBoneMapper
    {
        public static bool TryMapBones(
            SkinContainer skin,
            Transform targetRig,
            out Transform[] mappedBones,
            string logPrefix)
        {
            mappedBones = null;

            if (skin == null || skin.Renderer == null ||
                skin.Mesh == null || targetRig == null)
            {
                Debug.LogError(
                    $"{logPrefix}: Skin, Mesh, Renderer o rig inválido."
                );
                return false;
            }

            // Crear diccionario del rig de destino.
            Dictionary<string, Transform> targetBones =
                new Dictionary<string, Transform>();

            foreach (Transform bone in
                     targetRig.GetComponentsInChildren<Transform>(true))
            {
                if (!targetBones.ContainsKey(bone.name))
                    targetBones.Add(bone.name, bone);
            }

            Transform[] sourceBones = skin.Renderer.bones;
            Transform[] result = new Transform[sourceBones.Length];

            // Obtener los índices utilizados por el mesh.
            HashSet<int> usedIndices = new HashSet<int>();

            foreach (BoneWeight weight in skin.Mesh.boneWeights)
            {
                if (weight.weight0 > 0) usedIndices.Add(weight.boneIndex0);
                if (weight.weight1 > 0) usedIndices.Add(weight.boneIndex1);
                if (weight.weight2 > 0) usedIndices.Add(weight.boneIndex2);
                if (weight.weight3 > 0) usedIndices.Add(weight.boneIndex3);
            }

            bool valid = true;

            for (int i = 0; i < sourceBones.Length; i++)
            {
                Transform sourceBone = sourceBones[i];

                if (sourceBone == null)
                    continue;

                string boneName = sourceBone.name;

                // Compatibilidad con el nombre antiguo de RedHead.
                if (boneName == "lowerReg.R")
                    boneName = "lowerLeg.R";

                if (targetBones.TryGetValue(
                        boneName, out Transform targetBone))
                {
                    result[i] = targetBone;
                }
                else if (usedIndices.Contains(i))
                {
                    Debug.LogError(
                        $"{logPrefix}: FALTA BONE UTILIZADO: {boneName}"
                    );

                    valid = false;
                }
            }

            if (!valid)
                return false;

            mappedBones = result;
            return true;
        }
    }
}
