using System;
using MultiFPS;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace ROM.CiudadDelSol
{
    // Read-only checks against the real scene. Never bakes, repairs, changes colliders, or relocates a rejected point.
    public static class CiudadDelSolEncounterValidation
    {
        static readonly string[] Connections = { "01-02", "01-06", "02-03", "03-04", "03-05", "03-06", "04-05", "05-07", "06-07" };

        static bool Exterior(Collider collider, CiudadDelSolPopulationDefinition config)
        {
            if (collider.transform.root.name == "Salida_Meridional_Generated") return true;
            if (collider.transform.root.name != "Dungeon_Ciudad_del_Sol_Generated") return false;
            for (var t = collider.transform; t; t = t.parent)
            {
                foreach (var sector in config.Sectors) if (t.name == sector.PlayableAncestor) return true;
                if (t.name.Contains("ancho util"))
                    foreach (var id in Connections) if (t.name.StartsWith(id + " - ", StringComparison.Ordinal)) return true;
            }
            return false;
        }

        // Only the low, near-coplanar contact of the observed Santuario arch may borrow
        // provenance from the real modular floor underneath. The arch remains solid.
        static bool SantuarioArchOnFloor(PhysicsScene physics, CiudadDelSolPopulationDefinition config,
            RaycastHit contact, RaycastHit[] witnesses, out string error)
        {
            error = ""; var t = contact.collider.transform;
            if (t.name != "Entrance_Arch_01" || !t.parent || t.parent.name != "Arquitectura monumental - fuera de los pasos" ||
                t.root.name != "Dungeon_Ciudad_del_Sol_Generated")
            { error = "El apoyo no pertenece al exterior autorizado"; return false; }
            string sectorName = null;
            foreach (var sector in config.Sectors) if (sector.Id == "06-santuario") sectorName = sector.PlayableAncestor;
            if (string.IsNullOrEmpty(sectorName))
            { error = "Falta la referencia física del Santuario"; return false; }
            // The existing 3cm query margin is used as a narrow coplanarity requirement,
            // not as permission to climb the arch or accept other monumental structures.
            const float contactBand = .03f;
            int count = physics.Raycast(contact.point + Vector3.up * contactBand, Vector3.down, witnesses,
                2 * contactBand, GameManager.environmentLayer, QueryTriggerInteraction.Ignore);
            if (count == witnesses.Length)
            { error = "Consulta de pavimento bajo arco saturada"; return false; }
            for (int i = 0; i < count; i++)
            {
                var floor = witnesses[i];
                if (floor.collider.name != "Platform_Base_01" || floor.collider.transform.root.name != "Dungeon_Ciudad_del_Sol_Generated" ||
                    Mathf.Abs(floor.point.y - contact.point.y) > contactBand || Vector3.Angle(floor.normal, Vector3.up) > 5) continue;
                bool modular = false, santuario = false;
                for (var parent = floor.collider.transform.parent; parent; parent = parent.parent)
                {
                    modular |= parent.name.StartsWith("Suelo modular - superficie Y=", StringComparison.Ordinal);
                    santuario |= parent.name == sectorName;
                }
                if (modular && santuario) return true;
            }
            error = "Base de arco sin pavimento modular del Santuario a menos de 0.030m";
            return false;
        }

        public static bool Validate(Scene scene, CiudadDelSolPopulationDefinition config, CiudadEnemyPost post,
            Vector3 reference, Transform ignore, out Vector3[] patrol, out string error)
        {
            patrol = Array.Empty<Vector3>();
            if (CiudadDelSolSpawnSafety.Recovery(reference, config))
            { error = "Puesto dentro de recuperación"; return false; }
            if (!CiudadDelSolSpawnSafety.Placement(scene, post, reference, out var spawn, out error, ignore, true)) return false;
            var agent = post.Enemy.Prefab.GetComponent<NavMeshAgent>();
            var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
            if (!Project(scene, config, config.AccessibilityOrigin, filter, out var entrance, out error))
            { error = "ORIGEN_ACCESIBILIDAD: " + error; return false; }
            if (!Route(scene, config, post, entrance, spawn, false, out error) ||
                !Route(scene, config, post, spawn, entrance, false, out error))
            { error = "ACCESIBILIDAD_ENTRADA (no es una prueba local del spawn): " + error; return false; }
            if (post.PatrolPoints == null || post.PatrolPoints.Length == 0) return true;
            if (post.Enemy.IsWildlife || post.PatrolPoints.Length < 2)
            { error = "Patrulla inválida: fauna conserva su conducta nativa; se requieren dos puntos"; return false; }
            patrol = new Vector3[post.PatrolPoints.Length];
            for (int i = 0; i < patrol.Length; i++)
            {
                if (CiudadDelSolSpawnSafety.Recovery(post.PatrolPoints[i], config) ||
                    !CiudadDelSolSpawnSafety.Placement(scene, post, post.PatrolPoints[i], out patrol[i], out error, ignore, true))
                { error = "Patrulla " + i + ": recuperación o " + error; return false; }
                if (!Route(scene, config, post, patrol[i], spawn, true, out error) ||
                    !Route(scene, config, post, spawn, patrol[i], true, out error)) return false;
            }
            for (int i = 0; i < patrol.Length; i++)
            {
                var a = patrol[i]; var b = patrol[(i + 1) % patrol.Length];
                if (Vector3.Distance(a, b) < 2) { error = "Puntos consecutivos de patrulla demasiado próximos"; return false; }
                if (!Route(scene, config, post, a, b, true, out error) || !Route(scene, config, post, b, a, true, out error)) return false;
            }
            return true;
        }

        static bool Project(Scene scene, CiudadDelSolPopulationDefinition config, Vector3 point, NavMeshQueryFilter filter,
            out Vector3 navPoint, out string error)
        {
            navPoint = default; error = "";
            if (!scene.GetPhysicsScene().Raycast(point + Vector3.up * .5f, Vector3.down, out var floor, 1,
                GameManager.environmentLayer, QueryTriggerInteraction.Ignore))
            { error = "SIN_SUELO en " + point.ToString("F3"); return false; }
            string support = CiudadDelSolSpawnSafety.SupportDescription(floor);
            if (!Exterior(floor.collider, config))
            { error = "SUELO_NO_EXTERIOR en " + point.ToString("F3") + ": " + support; return false; }
            if (Mathf.Abs(point.y - floor.point.y) > .36f)
            { error = $"COTA_FISICA referencia={point:F3}, suelo={floor.point:F3}, máximo=0.360; {support}"; return false; }
            if (!NavMesh.SamplePosition(floor.point, out var nav, .25f, filter))
            { error = $"SIN_NAVMESH_LOCAL suelo={floor.point:F3}, radioConsulta=0.250, agente={filter.agentTypeID}; {support}"; return false; }
            if (Mathf.Abs(nav.position.y - floor.point.y) > .36f)
            { error = $"COTA_NAVMESH suelo={floor.point:F3}, nav={nav.position:F3}, máximo=0.360; {support}"; return false; }
            navPoint = nav.position; return true;
        }

        public static bool RecoveryArea(Scene scene, CiudadDelSolPopulationDefinition config, CiudadRecoveryArea area, out string error)
        {
            var filter = new NavMeshQueryFilter { agentTypeID = 0, areaMask = NavMesh.AllAreas };
            var hits = new Collider[8]; error = "";
            foreach (var offset in new[] { Vector3.zero, Vector3.right * 2, Vector3.left * 2, Vector3.forward * 2, Vector3.back * 2 })
            {
                if (!Project(scene, config, area.Position + offset, filter, out var p, out error))
                { error = $"centro={area.Position:F3}, muestra={offset:F3}: " + error; return false; }
                if (scene.GetPhysicsScene().OverlapCapsule(p + Vector3.up * .35f, p + Vector3.up * 1.71f,
                    .32f, hits, GameManager.environmentLayer, QueryTriggerInteraction.Ignore) > 0)
                { error = "Recuperación sin despeje para el Warrior en " + p.ToString("F3") + ": " +
                    CiudadDelSolSpawnSafety.ColliderPath(hits[0]); return false; }
            }
            return true;
        }

        // A ray cast from an exact polygon vertex can remap the origin to the incident
        // polygon on the wrong side of that vertex. A hit alone does not prove a gap.
        // Never skip it by distance: demand an unobstructed reciprocal ray AND complete,
        // straight local paths in both directions, restricted to an original path corner
        // or a measured NavMesh edge at the ray's origin. No tolerance is increased.
        static bool ConnectedStep(Vector3 from, Vector3 to, Vector3 cornerFrom, Vector3 cornerTo,
            NavMeshQueryFilter filter, out bool cornerVerified, out string error)
        {
            cornerVerified = false; error = "";
            if (!NavMesh.Raycast(from, to, out var hit, filter)) return true;
            const float precision = .001f; // 1 mm numerical equality, not agent clearance or a permitted gap.
            float startHit = Vector3.Distance(from, hit.position), endHit = Vector3.Distance(to, hit.position);
            bool sourceCorner = Vector3.Distance(from, cornerFrom) <= precision && startHit <= precision;
            bool targetCorner = Vector3.Distance(to, cornerTo) <= precision && endHit <= precision;
            bool edgeFound = false, sourceEdge = false; NavMeshHit edge = default;
            if (!sourceCorner && startHit <= precision)
            {
                edgeFound = NavMesh.FindClosestEdge(from, out edge, filter);
                sourceEdge = edgeFound && edge.distance <= precision && Vector3.Distance(from, edge.position) <= precision &&
                    Vector3.Distance(hit.position, edge.position) <= precision;
            }
            string context = $"origen={from:F6}, destino={to:F6}, límite={hit.position:F6}, máscara={hit.mask}, " +
                $"distanciaOrigenLímite={startHit:F6}, distanciaDestinoLímite={endHit:F6}, " +
                $"esquinaOrigen={cornerFrom:F6}, esquinaDestino={cornerTo:F6}, " +
                $"distanciaAEsquinaOrigen={Vector3.Distance(from, cornerFrom):F6}, distanciaAEsquinaDestino={Vector3.Distance(to, cornerTo):F6}, " +
                $"aristaOrigenVerificada={sourceEdge}, distanciaArista={(edgeFound ? edge.distance : float.PositiveInfinity):F6}";
            if (!sourceCorner && !targetCorner && !sourceEdge)
            { error = "RUTA_NAVMESH_INTERRUMPIDA (no es esquina ni arista de origen verificada): " + context; return false; }
            if (NavMesh.Raycast(to, from, out var reverseHit, filter))
            { error = $"RUTA_NAVMESH_INTERRUMPIDA (raycast inverso también bloqueado, límiteInverso={reverseHit.position:F6}): " + context; return false; }
            if (!StraightLocalPath(from, to, filter, precision, out var reason) ||
                !StraightLocalPath(to, from, filter, precision, out reason))
            { error = "RUTA_NAVMESH_INTERRUMPIDA (esquina sin prueba local: " + reason + "): " + context; return false; }
            cornerVerified = true;
            return true;
        }

        static bool StraightLocalPath(Vector3 from, Vector3 to, NavMeshQueryFilter filter, float precision, out string error)
        {
            error = ""; var path = new NavMeshPath();
            if (!NavMesh.CalculatePath(from, to, filter, path) || path.status != NavMeshPathStatus.PathComplete)
            { error = "camino local " + path.status; return false; }
            var corners = path.corners;
            if (corners.Length < 2 || Vector3.Distance(corners[0], from) > precision ||
                Vector3.Distance(corners[corners.Length - 1], to) > precision)
            { error = "extremos locales no coincidentes"; return false; }
            var direction = to - from; float squaredLength = direction.sqrMagnitude, length = 0;
            if (squaredLength <= precision * precision)
            { error = "paso local degenerado"; return false; }
            for (int i = 0; i < corners.Length; i++)
            {
                float t = Mathf.Clamp01(Vector3.Dot(corners[i] - from, direction) / squaredLength);
                if (Vector3.Distance(corners[i], from + direction * t) > precision)
                { error = "el camino local requiere un desvío"; return false; }
                if (i > 0) length += Vector3.Distance(corners[i - 1], corners[i]);
            }
            if (length > Mathf.Sqrt(squaredLength) + 2 * precision)
            { error = "el camino local rodea un corte"; return false; }
            return true;
        }

        static bool Route(Scene scene, CiudadDelSolPopulationDefinition config, CiudadEnemyPost post,
            Vector3 from, Vector3 to, bool patrol, out string error)
        {
            error = "";
            var agent = post.Enemy.Prefab.GetComponent<NavMeshAgent>();
            var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
            var path = new NavMeshPath();
            if (!NavMesh.CalculatePath(from, to, filter, path) || path.status != NavMeshPathStatus.PathComplete)
            { error = $"Ruta {path.status}: {from:F3} -> {to:F3}"; return false; }
            var body = post.Enemy.Prefab.Body; var physics = scene.GetPhysicsScene(); var hits = new Collider[8];
            var floorWitnesses = new RaycastHit[16]; int archContacts = 0;
            var settings = NavMesh.GetSettingsByID(agent.agentTypeID);
            float step = settings.agentClimb;
            if (step <= 0 || settings.agentSlope <= 0)
            { error = "Configuración de navegación sin escalón/pendiente válida"; return false; }
            // Keep the native corridor, but recover its surface height at every local move.
            // Neither an interpolated corner height nor the lower stair block is the NavMesh.
            if (!Project(scene, config, from, filter, out var previousNav, out error)) return false;
            if (!physics.Raycast(from + Vector3.up * .5f, Vector3.down, out var previousFloor, 1,
                GameManager.environmentLayer, QueryTriggerInteraction.Ignore))
            { error = "Falta suelo en el inicio físico de la ruta"; return false; }
            var corners = path.corners; int verifiedCorners = 0; string verifiedAt = "";
            if (corners.Length < 2 && Vector3.Distance(from, to) > .025f)
            { error = "RUTA_SIN_ESQUINAS: camino completo sin un trazado evaluable"; return false; }
            for (int i = 1; i < corners.Length; i++)
            {
                var a = corners[i - 1]; var b = corners[i];
                float horizontal = Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
                int steps = Mathf.Max(1, Mathf.CeilToInt(horizontal / .1f));
                for (int j = 1; j <= steps; j++)
                {
                    var chord = Vector3.Lerp(a, b, (float)j / steps);
                    var requested = new Vector3(chord.x, previousNav.y, chord.z);
                    float run = Vector2.Distance(new Vector2(previousNav.x, previousNav.z), new Vector2(requested.x, requested.z));
                    // Query reach derives from one horizontal sample and the unchanged climb.
                    // A wider query alone never grants access: XZ, height, native ray and physical support must all agree.
                    float reach = Mathf.Sqrt(step * step + run * run) + .03f;
                    if (!NavMesh.SamplePosition(requested, out var sample, reach, filter))
                    { error = $"RUTA_SIN_NAVMESH anterior={previousNav:F3}, consulta={requested:F3}, alcance={reach:F3}, tramo={i}/{corners.Length - 1}"; return false; }
                    var p = sample.position;
                    float deviation = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(requested.x, requested.z));
                    if (deviation > .025f || Mathf.Abs(p.y - previousNav.y) > step + .001f)
                    { error = $"RUTA_PROYECCION_FUERA_DEL_CORREDOR anterior={previousNav:F3}, consulta={requested:F3}, nav={p:F3}, desviaciónXZ={deviation:F3} (máximo=0.025), escalón={step:F3}"; return false; }
                    if (!ConnectedStep(previousNav, p, a, b, filter, out bool cornerVerified, out error))
                    { error += $", tramo={i}/{corners.Length - 1}"; return false; }
                    if (cornerVerified)
                    { verifiedCorners++; verifiedAt += " " + previousNav.ToString("F3") + " -> " + p.ToString("F3"); }
                    // Start above the navigation surface, so overlapping foundation blocks cannot
                    // hide the upper tread by putting the ray origin inside that solid block.
                    var origin = p + Vector3.up * (step + .03f);
                    if (!physics.Raycast(origin, Vector3.down, out var floor, 2 * (step + .03f),
                        GameManager.environmentLayer, QueryTriggerInteraction.Ignore))
                    { error = $"RUTA_SIN_APOYO nav={p:F3}, sueloPrevio={previousFloor.point:F3}, escalón={step:F3}, tramo={i}/{corners.Length - 1}"; return false; }
                    if (!Exterior(floor.collider, config))
                    {
                        if (!SantuarioArchOnFloor(physics, config, floor, floorWitnesses, out var supportIssue))
                        { error = $"RUTA_APOYO_NO_AUTORIZADO nav={p:F3}, suelo={floor.point:F3}: {supportIssue}; " +
                            CiudadDelSolSpawnSafety.SupportDescription(floor); return false; }
                        archContacts++;
                    }
                    if (Mathf.Abs(floor.point.y - p.y) > .36f ||
                        Mathf.Abs(floor.point.y - previousFloor.point.y) > step + .001f ||
                        Vector3.Angle(floor.normal, Vector3.up) > settings.agentSlope)
                    { error = $"RUTA_APOYO_INCOMPATIBLE nav={p:F3}, sueloAnterior={previousFloor.point:F3}, actual={floor.point:F3}, toleranciaNavSuelo=0.360, escalón={step:F3}, pendienteMáxima={settings.agentSlope:F1}; " +
                        CiudadDelSolSpawnSafety.SupportDescription(floor); return false; }
                    previousFloor = floor;
                    previousNav = p;
                    if (patrol && CiudadDelSolSpawnSafety.Recovery(p, config))
                    { error = "La patrulla cruza recuperación en " + p.ToString("F3"); return false; }
                    // Test the pose used by NavMeshAgent, not a capsule artificially sunk to a lower physical tread.
                    p += Vector3.up * agent.baseOffset;
                    float top = p.y + body.center.y + body.height * .5f + .03f;
                    float bottom = Mathf.Min(p.y + body.center.y - body.height * .5f + step + .03f, top - body.radius * 2);
                    var offset = new Vector3(body.center.x, 0, body.center.z);
                    if (physics.OverlapCapsule(new Vector3(p.x, bottom + body.radius, p.z) + offset,
                        new Vector3(p.x, top - body.radius, p.z) + offset, body.radius, hits,
                        GameManager.environmentLayer, QueryTriggerInteraction.Ignore) > 0)
                    { error = $"RUTA_CAPSULA_OBSTRUIDA pose={p:F3}, nav={previousNav:F3}, suelo={floor.point:F3}, radio={body.radius:F3}, altura={body.height:F3}: " +
                        CiudadDelSolSpawnSafety.ColliderPath(hits[0]); return false; }
                }
            }
            if (Vector3.Distance(previousNav, to) > .1f)
            { error = $"RUTA_DESTINO_NO_ALCANZADO nav={previousNav:F3}, destino={to:F3}, máximo=0.100"; return false; }
            if (Mathf.Abs(previousFloor.point.y - to.y) > .36f)
            { error = $"RUTA_NIVEL_FINAL suelo={previousFloor.point:F3}, destino={to:F3}, máximo=0.360"; return false; }
            if (verifiedCorners > 0)
                Debug.Log($"CIUDAD_EXTERIOR ESQUINAS_VERIFICADAS {post.Id}: {from:F3} -> {to:F3}; " +
                    $"{verifiedCorners} esquinas/aristas con raycast recíproco libre, caminos locales completos sin desvío y física aprobada. Puntos:{verifiedAt}");
            if (archContacts > 0)
                Debug.Log($"CIUDAD_EXTERIOR ARCO_APOYO_VERIFICADO {post.Id}: {archContacts} muestras sobre base coplanar con pavimento del Santuario; " +
                    "NavMesh, pendiente, altura y cápsula aprobados sin excluir el arco de colisiones.");
            return true;
        }
    }
}
