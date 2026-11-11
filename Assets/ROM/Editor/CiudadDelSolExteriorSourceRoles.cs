using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ROM.EditorTools
{
    internal static class CiudadDelSolExteriorSourceRoles
    {
        // These groups use the same platform prefab as the floor, but their generators
        // place it as a lintel, statue plinth, canopy or column cap.
        static readonly Dictionary<string, string[]> StructuralPlatforms = new Dictionary<string, string[]>
        {
            ["01 - Entrada Portico Solar"] = new[] { "02 - Gran puerta transitable", "06 - Procesion y esculturas" },
            ["04 - Archivos Solares"] = new[] { "02 - Fachada monumental" },
            ["05 - Claustro del Eclipse"] = new[] { "04 - Portico monumental", "06 - Columnata del claustro" },
            ["06 - Santuario Profano"] = new[] { "05 - Fachada monumental", "16 - Baldaquino del altar" },
            ["07 - El Sol Corrompido"] = new[] { "05 - Pabellones monumentales", "07 - Santuario solar destruido" }
        };

        internal static bool IsStructuralPlatform(Collider collider, Transform group)
        {
            StructuralPlatforms.TryGetValue(group.name, out var structuralGroups);
            bool platform = false, structure = false;
            for (var node = collider.transform; node && node != group; node = node.parent)
            {
                platform |= node.name.StartsWith("Platform_Base_01", StringComparison.Ordinal);
                structure |= node.name == "Perimetro y basamento" ||
                    (structuralGroups != null && Array.IndexOf(structuralGroups, node.name) >= 0);
            }
            return platform && structure;
        }

        internal static bool IsCrossableCurbCandidate(BoxCollider box)
        {
            if (!box.name.StartsWith("Curb_01", StringComparison.Ordinal)) return false;
            // These generators use native curb boxes on the pavement, separate from railings.
            for (var node = box.transform.parent; node; node = node.parent)
            {
                if (node.name == "03 - Patios vegetales" || node.name == "01 - Pavimento ceremonial e incrustaciones")
                    return true;
            }
            return false;
        }

        internal static Bounds GardenPondInterior(Transform garden)
        {
            var pool = garden.GetComponentsInChildren<Transform>().Single(
                node => node.name == "02 - Estanque ornamental somero - sin simulacion");
            // Two sides of the ornamental frame were explicitly created as visual-only.
            var edges = pool.GetComponentsInChildren<MeshRenderer>()
                .Where(renderer => renderer.enabled && renderer.name.StartsWith("Curb_01", StringComparison.Ordinal))
                .Select(renderer => renderer.bounds).ToArray();
            var x = edges.Where(edge => edge.size.x < edge.size.z).ToArray();
            var z = edges.Where(edge => edge.size.z < edge.size.x).ToArray();
            float centerX = PoolBoundsCenter(x, 0), centerZ = PoolBoundsCenter(z, 2);
            float left = x.Where(edge => edge.center.x < centerX).Max(edge => edge.max.x);
            float right = x.Where(edge => edge.center.x > centerX).Min(edge => edge.min.x);
            float near = z.Where(edge => edge.center.z < centerZ).Max(edge => edge.max.z);
            float far = z.Where(edge => edge.center.z > centerZ).Min(edge => edge.min.z);
            if (right - left < 22f || right - left > 24f || far - near < 22f || far - near > 24f)
                throw new InvalidOperationException("Los bordillos del estanque no delimitan el recinto ornamental esperado.");
            var result = new Bounds();
            result.SetMinMax(new Vector3(left, garden.position.y - .25f, near),
                new Vector3(right, garden.position.y + .25f, far));
            return result;
        }

        static float PoolBoundsCenter(Bounds[] edges, int axis) =>
            (edges.Min(edge => edge.center[axis]) + edges.Max(edge => edge.center[axis])) * .5f;
    }
}
