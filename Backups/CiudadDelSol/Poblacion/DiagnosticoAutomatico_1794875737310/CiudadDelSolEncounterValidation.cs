using System;
using MultiFPS;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Unity.Collections;
using UnityEngine.Experimental.AI;

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

        // Only the low contact of the observed Santuario arch may borrow
        // provenance from the real modular floor underneath. The arch remains solid.
        static bool SantuarioArchOnFloor(PhysicsScene physics, CiudadDelSolPopulationDefinition config,
            RaycastHit contact, RaycastHit[] witnesses, float climb, out string error)
        {
            error = ""; var t = contact.collider.transform;
            if (t.name != "Entrance_Arch_01" || !t.parent || t.parent.name != "Arquitectura monumental - fuera de los pasos" ||
                t.root.name != "Dungeon_Ciudad_del_Sol_Generated")
            { error = "El apoyo no pertenece al exterior autorizado"; return false; }
            string sectorName = null;
            foreach (var sector in config.Sectors) if (sector.Id == "06-santuario") sectorName = sector.PlayableAncestor;
            if (string.IsNullOrEmpty(sectorName))
            { error = "Falta la referencia física del Santuario"; return false; }
            var sectorRoot = t.root.Find(sectorName);
            if (!sectorRoot)
            { error = "No se encuentra la raíz física del Santuario"; return false; }
            // The witness is the sector's modular pavement, not a plane following the
            // inclined arch. Keep its 3cm query band; validate the arch's actual rise
            // separately against the unchanged agent climb and all following physics checks.
            const float contactBand = .03f;
            var pavementProbe = new Vector3(contact.point.x, sectorRoot.position.y + contactBand, contact.point.z);
            int count = physics.Raycast(pavementProbe, Vector3.down, witnesses,
                2 * contactBand, GameManager.environmentLayer, QueryTriggerInteraction.Ignore);
            if (count == witnesses.Length)
            { error = "Consulta de pavimento bajo arco saturada"; return false; }
            for (int i = 0; i < count; i++)
            {
                var floor = witnesses[i];
                if (floor.collider.name != "Platform_Base_01" || floor.collider.transform.root.name != "Dungeon_Ciudad_del_Sol_Generated" ||
                    Mathf.Abs(floor.point.y - sectorRoot.position.y) > contactBand || Vector3.Angle(floor.normal, Vector3.up) > 5) continue;
                bool modular = false, santuario = false;
                for (var parent = floor.collider.transform.parent; parent; parent = parent.parent)
                {
                    modular |= parent.name.StartsWith("Suelo modular - superficie Y=", StringComparison.Ordinal);
                    santuario |= parent.name == sectorName;
                }
                if (!modular || !santuario) continue;
                if (Mathf.Abs(contact.point.y - floor.point.y) > climb)
                { error = $"Base de arco fuera del escalón autorizado: contacto={contact.point:F6}, pavimento={floor.point:F6}, máximo={climb:F3}"; return false; }
                return true;
            }
            error = $"Sin pavimento modular en su plano físico: consulta={pavementProbe:F6}, banda=0.030m; no se autoriza el arco";
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
        // Never skip it by distance: demand either an unobstructed reciprocal ray or a
        // native ground-polygon proof, plus complete straight local paths both ways.
        // Restricted to an original corner or measured edge; no tolerance is increased.
        static bool ConnectedStep(Vector3 from, Vector3 to, Vector3 cornerFrom, Vector3 cornerTo,
            NavMeshQueryFilter filter, out bool cornerVerified, out bool polygonVerified, out string error)
        {
            cornerVerified = polygonVerified = false; error = "";
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
            {
                // A reverse hit at the same original source corner is an endpoint hit.
                // Require a real source edge, native ground movement both ways and
                // straight local paths. The other sample may be inside its polygon.
                bool reverseAtOrigin = Vector3.Distance(to, reverseHit.position) <= precision;
                bool reverseAtSourceCorner = sourceCorner && Vector3.Distance(from, reverseHit.position) <= precision;
                if (startHit > precision || (!reverseAtOrigin && !reverseAtSourceCorner))
                { error = $"RUTA_NAVMESH_INTERRUMPIDA (doble impacto fuera de los orígenes, límiteInverso={reverseHit.position:F6}): " + context; return false; }
                if (!NativePolygonStep(from, to, filter, precision, out var proofIssue, reverseAtSourceCorner))
                { error = $"RUTA_CONTINUIDAD_NO_DEMOSTRADA (raycasts bloqueados; {proofIssue}): " + context; return false; }
                polygonVerified = true;
            }
            if (!StraightLocalPath(from, to, filter, precision, out var reason) ||
                !StraightLocalPath(to, from, filter, precision, out reason))
            { error = "RUTA_NAVMESH_INTERRUMPIDA (esquina sin prueba local: " + reason + "): " + context; return false; }
            cornerVerified = true;
            return true;
        }

        // Read-only proof on the existing native polygon graph. MoveLocation stays on
        // connected ground and does not traverse NavMeshLinks. Kept local because this
        // Unity 6000.3 API is obsolete without a replacement; no AI or game flow uses it.
        static bool NativePolygonStep(Vector3 from, Vector3 to, NavMeshQueryFilter filter, float precision, out string error, bool reverseEndsAtSourceCorner = false)
        {
            error = "";
            if (!NavMesh.FindClosestEdge(from, out var edgeFrom, filter) || !NavMesh.FindClosestEdge(to, out var edgeTo, filter) ||
                edgeFrom.distance > precision || Vector3.Distance(from, edgeFrom.position) > precision ||
                (!reverseEndsAtSourceCorner && (edgeTo.distance > precision || Vector3.Distance(to, edgeTo.position) > precision)))
            { error = "los extremos requeridos no acreditan aristas reales dentro de 0.001m"; return false; }
            try
            {
                using (var query = new NavMeshQuery(NavMeshWorld.GetDefaultWorld(), Allocator.Temp, 256))
                {
                    var extent = new Vector3(precision, precision, precision);
                    var origin = query.MapLocation(from, extent, filter.agentTypeID, filter.areaMask);
                    var goal = query.MapLocation(to, extent, filter.agentTypeID, filter.areaMask);
                    bool Ground(NavMeshLocation location) => query.IsValid(location.polygon) &&
                        query.GetAgentTypeIdForPolygon(location.polygon) == filter.agentTypeID &&
                        query.GetPolygonType(location.polygon) == NavMeshPolyTypes.Ground;
                    bool SameOrPortal(NavMeshLocation a, NavMeshLocation b) => a.polygon.Equals(b.polygon) ||
                        query.GetPortalPoints(a.polygon, b.polygon, out _, out _);
                    if (!Ground(origin) || !Ground(goal) || Vector3.Distance(origin.position, from) > precision ||
                        Vector3.Distance(goal.position, to) > precision)
                    { error = "extremos sin polígono de suelo del mismo agente/nivel dentro de 0.001m"; return false; }
                    var forward = query.MoveLocation(origin, to, filter.areaMask);
                    if (!Ground(forward) || Vector3.Distance(forward.position, to) > precision || !SameOrPortal(forward, goal))
                    { error = $"el movimiento nativo queda limitado o sin portal: alcanzado={forward.position:F6}, destino={to:F6}"; return false; }
                    var backward = query.MoveLocation(forward, from, filter.areaMask);
                    if (!Ground(backward) || Vector3.Distance(backward.position, from) > precision || !SameOrPortal(backward, origin))
                    { error = $"el retorno nativo queda limitado o sin portal: alcanzado={backward.position:F6}, origen={from:F6}"; return false; }
                    return true;
                }
            }
            catch (Exception exception)
            { error = "consulta nativa no disponible; rechazo seguro: " + exception.GetType().Name + ": " + exception.Message; return false; }
        }

        static string PhysicalWitness(PhysicsScene physics, Vector3 point, float climb)
        {
            if (!physics.Raycast(point + Vector3.up * (climb + .03f), Vector3.down, out var hit, 2 * (climb + .03f),
                GameManager.environmentLayer, QueryTriggerInteraction.Ignore)) return $"[{point:F3}: sin apoyo en la ventana del agente]";
            return $"[{point:F3}: diferenciaNavSuelo={Mathf.Abs(point.y - hit.point.y):F6}; " +
                CiudadDelSolSpawnSafety.SupportDescription(hit) + "]";
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
            var corners = path.corners; int verifiedCorners = 0, verifiedPolygons = 0; string verifiedAt = "";
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
                    if (!ConnectedStep(previousNav, p, a, b, filter, out bool cornerVerified, out bool polygonVerified, out error))
                    {
                        error += $", tramo={i}/{corners.Length - 1}; sondas físicas solo diagnósticas: " +
                            PhysicalWitness(physics, previousNav, step) + " " + PhysicalWitness(physics, Vector3.Lerp(previousNav, p, .5f), step) +
                            " " + PhysicalWitness(physics, p, step);
                        return false;
                    }
                    if (cornerVerified)
                    { verifiedCorners++; verifiedAt += " " + previousNav.ToString("F3") + " -> " + p.ToString("F3"); }
                    if (polygonVerified) verifiedPolygons++;
                    // Start above the navigation surface, so overlapping foundation blocks cannot
                    // hide the upper tread by putting the ray origin inside that solid block.
                    var origin = p + Vector3.up * (step + .03f);
                    if (!physics.Raycast(origin, Vector3.down, out var floor, 2 * (step + .03f),
                        GameManager.environmentLayer, QueryTriggerInteraction.Ignore))
                    { error = $"RUTA_SIN_APOYO nav={p:F3}, sueloPrevio={previousFloor.point:F3}, escalón={step:F3}, tramo={i}/{corners.Length - 1}"; return false; }
                    if (!Exterior(floor.collider, config))
                    {
                        if (!SantuarioArchOnFloor(physics, config, floor, floorWitnesses, step, out var supportIssue))
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
                    $"{verifiedCorners} esquinas/aristas verificadas; {verifiedPolygons} con prueba nativa de polígonos bidireccional sin enlaces. " +
                    $"Caminos locales completos sin desvío y física aprobada. Puntos:{verifiedAt}");
            if (verifiedPolygons > 0)
                Debug.Log($"CIUDAD_EXTERIOR TOPOLOGIA_NATIVA_VERIFICADA {post.Id}: {verifiedPolygons} pasos con suelo, agente, portales y continuidad nativa acreditados; " +
                    "no se aceptaron huecos por distancia ni se modificó el NavMesh.");
            if (archContacts > 0)
                Debug.Log($"CIUDAD_EXTERIOR ARCO_APOYO_VERIFICADO {post.Id}: {archContacts} muestras de base dentro del escalón autorizado sobre pavimento real del Santuario; " +
                    "NavMesh, pendiente, altura y cápsula aprobados sin excluir el arco de colisiones.");
            return true;
        }
    }
}
