using System;
using UnityEditor;
using UnityEngine;

namespace ROM.EditorTools
{
    public static partial class CiudadDelSolNaveJardinesPreview
    {
        [MenuItem("ROM/Dungeon Ciudad del Sol/Cinemática/Exportar tráiler Warrior V4")]
        public static void ExportWarriorTrailerV4()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-warrior-v4-probes") >= 0)
            {
                ExportWarriorV3(1, true);
                ExportWarriorV3(2, true);
                return;
            }
            ExportWarriorV3(0, true);
        }

        static (Vector3 mark, float yaw, int clip, float phase, Vector3 eye, Vector3 aim, float fov) WarriorV4Pose(float t)
        {
            if (t < 12)
            {
                float a = Mathf.Lerp(120, 185, t / 8) * Mathf.Deg2Rad;
                return (new Vector3(7, 12, -38), 0, 0, t,
                    new Vector3(3.65f * Mathf.Sin(a), 1.5f, 3.65f * Mathf.Cos(a)),
                    new Vector3(0, 1.20f, .44f), 49);
            }
            if (t < 19)
            {
                float d = t - 12;
                return (new Vector3(7, 12, -38 + Mathf.Min(d, 5) * 1.30f), 0, 1, d,
                    Vector3.Lerp(new Vector3(-3.2f, 1.9f, -3.4f), new Vector3(-3.65f, 1.85f, 2.6f), d / 5),
                    new Vector3(0, 1.12f, .35f), 58);
            }
            if (t < 25)
            {
                float d = t - 19;
                return (new Vector3(7, 12, -35 + d * 2.6f), 0, 2, d * .96f,
                    Vector3.Lerp(new Vector3(2.5f, 1.8f, 3.7f), new Vector3(4.1f, 2.2f, 1.5f), d / 6),
                    new Vector3(0, 1.10f, .5f), 61);
            }
            if (t < 30)
            {
                float d = t - 25;
                return (new Vector3(162, 20, -23 + d), 0, 1, d * .90f,
                    new Vector3(-2.7f, 2.45f, -3.2f), new Vector3(0, 1.17f, 1.8f), 60);
            }
            // Ellipses between the stairs compress travel through known landings and galleries.
            if (t < 34)
                return Flight(30, 34, new Vector3(162,20,-18), new Vector3(162,16,-12), new Vector3(-2.45f,2.1f,-2.9f));
            if (t < 38)
                return Flight(34, 38, new Vector3(150,4,36), new Vector3(150,0,42), new Vector3(2.0f,1.6f,2.6f));
            if (t < 43)
                return Flight(38, 43, new Vector3(45,-16,-12), new Vector3(45,-18.5f,-4.5f), new Vector3(-2.1f,2.2f,-3.0f));
            if (t < 48)
                return Flight(43, 48, new Vector3(-9,-35.5f,19.5f), new Vector3(-9,-38,12), new Vector3(2.2f,1.9f,-3.2f));
            if (t < 51)
                return (new Vector3(16, -38, -11 + (t - 48) * 2 / 3), 0, 1, (t - 48) * .64f,
                    new Vector3(-2.8f, 1.95f, -3.7f), new Vector3(.4f, 2.6f, 3.3f), 64);
            if (t < 74.1f)
                return (new Vector3(16, -38, -9), 0, 3, t - 72,
                    Vector3.Lerp(new Vector3(-2.7f, 1.45f, 2.15f), new Vector3(-2.14f, 1.65f, 2.0f), (t - 72) / 2.1f),
                    new Vector3(0, 1.17f, .10f), 47);
            return (new Vector3(16, -38, -9), 0, 0, t - 74.1f,
                Vector3.Lerp(new Vector3(1.7f, 1.9f, -4.4f), new Vector3(2f, 2.8f, -8.5f), (t - 74.1f) / 3.9f),
                new Vector3(2, 12.5f, 28), 68);

            (Vector3, float, int, float, Vector3, Vector3, float) Flight(float start, float end, Vector3 from, Vector3 to, Vector3 eye)
            {
                float a = Mathf.InverseLerp(start, end, t);
                return (Vector3.Lerp(from, to, a), to.z > from.z ? 0 : 180, 1, (t - start) * 1.09f,
                    eye, new Vector3(0, 1.06f, Mathf.Sign(to.z - from.z) * .35f), 58);
            }
        }

        static Vector3 V4Ground(Vector3 point, PhysicsScene physics)
        {
            Require(physics.Raycast(point + Vector3.up * .76f, Vector3.down, out var hit, 1.56f, ~0,
                QueryTriggerInteraction.Ignore) && hit.normal.y > .64f, $"V4 sin escalón en {point:F3}");
            var foot = new Vector3(point.x, hit.point.y + .025f, point.z);
            var contacts = new Collider[24];
            int n = physics.OverlapCapsule(foot + Vector3.up * .60f, foot + Vector3.up * 1.65f, .23f,
                contacts, ~0, QueryTriggerInteraction.Ignore);
            Require(n == 0, $"V4 interferencia en {foot:F3}: {(n == 0 ? "" : contacts[0].name)}");
            return foot;
        }

        static void V4Feet(Animator animator, PhysicsScene physics)
        {
            SetFoot(HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot);
            SetFoot(HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot);

            void SetFoot(HumanBodyBones upperBone, HumanBodyBones lowerBone, HumanBodyBones footBone)
            {
                var upper = animator.GetBoneTransform(upperBone);
                var lower = animator.GetBoneTransform(lowerBone);
                var foot = animator.GetBoneTransform(footBone);
                if (!physics.Raycast(foot.position + Vector3.up * .42f, Vector3.down, out var step, .86f,
                    ~0, QueryTriggerInteraction.Ignore) || step.normal.y < .64f) return;
                float shift = step.point.y + .095f - foot.position.y;
                if (shift <= 0) return;

                var hip = upper.position;
                var knee = lower.position;
                var ankle = foot.position;
                var orientation = foot.rotation;
                var target = ankle + Vector3.up * Mathf.Min(shift, .34f);
                float a = Vector3.Distance(hip, knee);
                float b = Vector3.Distance(knee, ankle);
                var line = (target - hip).normalized;
                float reach = Mathf.Clamp(Vector3.Distance(hip, target), Mathf.Abs(a - b) + .001f, a + b - .001f);
                var bend = Vector3.ProjectOnPlane(knee - hip, line).normalized;
                if (bend.sqrMagnitude < .5f) return;
                float kneeAlong = (reach * reach + a * a - b * b) / (2 * reach);
                var solved = hip + line * kneeAlong + bend * Mathf.Sqrt(Mathf.Max(0, a * a - kneeAlong * kneeAlong));
                upper.rotation = Quaternion.FromToRotation(knee - hip, solved - hip) * upper.rotation;
                lower.rotation = Quaternion.FromToRotation(foot.position - lower.position, hip + line * reach - lower.position) * lower.rotation;
                foot.rotation = orientation;
            }
        }
    }
}

