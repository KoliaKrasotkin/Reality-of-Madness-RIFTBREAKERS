using System.Collections.Generic;
using UnityEngine;

public class DebugBoneMapping : MonoBehaviour
{
    public SkinnedMeshRenderer redHeadRenderer;
    public SkinnedMeshRenderer multiFPSTppRenderer;

    private void Start()
    {
        CompareUsedBones();
    }

    private void CompareUsedBones()
    {
        if (redHeadRenderer == null)
        {
            Debug.LogError("DebugBoneMapping: falta Red Head Renderer.");
            return;
        }

        if (multiFPSTppRenderer == null)
        {
            Debug.LogError("DebugBoneMapping: falta Multi FPS TPP Renderer.");
            return;
        }

        Transform[] redBones = redHeadRenderer.bones;
        Transform[] multiBones = multiFPSTppRenderer.bones;

        Dictionary<string, Transform> multiByName =
            new Dictionary<string, Transform>();

        foreach (Transform bone in multiBones)
        {
            if (bone != null && !multiByName.ContainsKey(bone.name))
                multiByName.Add(bone.name, bone);
        }

        Mesh mesh = redHeadRenderer.sharedMesh;

        if (mesh == null)
        {
            Debug.LogError("DebugBoneMapping: RedHead no tiene Mesh.");
            return;
        }

        BoneWeight[] weights = mesh.boneWeights;

        bool[] used = new bool[redBones.Length];

        foreach (BoneWeight w in weights)
        {
            if (w.weight0 > 0 && w.boneIndex0 < used.Length)
                used[w.boneIndex0] = true;

            if (w.weight1 > 0 && w.boneIndex1 < used.Length)
                used[w.boneIndex1] = true;

            if (w.weight2 > 0 && w.boneIndex2 < used.Length)
                used[w.boneIndex2] = true;

            if (w.weight3 > 0 && w.boneIndex3 < used.Length)
                used[w.boneIndex3] = true;
        }

        string result = "===== REDHEAD USADOS -> MULTIFPS =====\n\n";

        int found = 0;
        int missing = 0;

        for (int i = 0; i < redBones.Length; i++)
        {
            if (!used[i])
                continue;

            Transform redBone = redBones[i];

            if (redBone == null)
                continue;

            string redName = redBone.name;

            // El rig de RedHead tiene este typo.
            string searchName = redName == "lowerReg.R"
                ? "lowerLeg.R"
                : redName;

            if (multiByName.TryGetValue(searchName, out Transform multiBone))
            {
                result += "[OK]    "
                    + i + " "
                    + redName
                    + " -> "
                    + multiBone.name
                    + "\n";

                found++;
            }
            else
            {
                result += "[FALTA] "
                    + i + " "
                    + redName
                    + "\n";

                missing++;
            }
        }

        result += "\n===== RESUMEN =====\n";
        result += "Encontrados: " + found + "\n";
        result += "Faltantes: " + missing + "\n";

        Debug.Log(result);
    }
}