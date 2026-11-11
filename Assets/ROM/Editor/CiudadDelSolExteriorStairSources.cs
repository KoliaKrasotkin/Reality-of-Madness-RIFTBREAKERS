using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace ROM.EditorTools
{
    internal static class CiudadDelSolExteriorStairSources
    {
        const string StairGroup = "02 - Escalinata monumental - 8 peldanos";
        const float Tolerance = .0001f;

        internal static int Normalize(List<NavMeshBuildSource> sources, Transform entrance, HashSet<Collider> walkable)
        {
            var stair = entrance.Find(StairGroup);
            if (!stair) throw new InvalidOperationException("Falta la escalinata de la Salida Meridional.");
            var originals = sources.Where(source => source.component is Collider collider &&
                collider.transform.IsChildOf(stair) && walkable.Contains(collider)).ToArray();
            if (originals.Length != 32 || originals.Any(source => !(source.component is BoxCollider box) ||
                source.shape != NavMeshBuildSourceShape.Box || !HasWorldAxes(box.transform)))
                throw new InvalidOperationException("La escalinata meridional difiere de los 32 bloques alineados aprobados.");

            // Partition the exact union of the physical boxes. Internal faces between
            // overlapping treads are not submitted to the voxelizer.
            var boxes = originals.Select(source => ((Collider)source.component).bounds).ToArray();
            var xEdges = Edges(boxes, 0);
            var zEdges = Edges(boxes, 2);
            var replacement = new List<NavMeshBuildSource>();
            for (int x = 1; x < xEdges.Length; x++)
            for (int z = 1; z < zEdges.Length; z++)
            {
                float cx = (xEdges[x - 1] + xEdges[x]) * .5f;
                float cz = (zEdges[z - 1] + zEdges[z]) * .5f;
                var column = boxes.Where(b => cx > b.min.x && cx < b.max.x && cz > b.min.z && cz < b.max.z)
                    .OrderBy(b => b.min.y).ToArray();
                if (column.Length == 0) continue;
                float bottom = column[0].min.y, top = column[0].max.y;
                foreach (var box in column.Skip(1))
                {
                    if (box.min.y > top + Tolerance)
                        throw new InvalidOperationException("Los bloques de la escalinata contienen niveles superpuestos con un hueco.");
                    top = Mathf.Max(top, box.max.y);
                }
                replacement.Add(new NavMeshBuildSource
                {
                    shape = NavMeshBuildSourceShape.Box,
                    transform = Matrix4x4.Translate(new Vector3(cx, (bottom + top) * .5f, cz)),
                    size = new Vector3(xEdges[x] - xEdges[x - 1], top - bottom, zEdges[z] - zEdges[z - 1]),
                    area = 0
                });
            }
            if (replacement.Count == 0) throw new InvalidOperationException("La escalinata no produjo volúmenes de horneado.");
            var components = new HashSet<Component>(originals.Select(source => source.component));
            sources.RemoveAll(source => source.component && components.Contains(source.component));
            sources.AddRange(replacement);
            return replacement.Count;
        }

        static float[] Edges(Bounds[] boxes, int axis)
        {
            var edges = new List<float>();
            foreach (var edge in boxes.SelectMany(b => new[] { b.min[axis], b.max[axis] }).OrderBy(value => value))
            {
                if (edges.Count == 0 || edge - edges[edges.Count - 1] > Tolerance) edges.Add(edge);
            }
            return edges.ToArray();
        }

        internal static bool HasWorldAxes(Transform transform)
        {
            var matrix = transform.localToWorldMatrix;
            var right = matrix.MultiplyVector(Vector3.right).normalized;
            var up = matrix.MultiplyVector(Vector3.up).normalized;
            var forward = matrix.MultiplyVector(Vector3.forward).normalized;
            return Aligned(right) && Aligned(up) && Aligned(forward) &&
                Mathf.Abs(Vector3.Dot(right, up)) < .001f &&
                Mathf.Abs(Vector3.Dot(right, forward)) < .001f &&
                Mathf.Abs(Vector3.Dot(up, forward)) < .001f;
        }

        static bool Aligned(Vector3 axis) =>
            Mathf.Max(Mathf.Abs(axis.x), Mathf.Abs(axis.y), Mathf.Abs(axis.z)) >= .999999f;
    }
}
