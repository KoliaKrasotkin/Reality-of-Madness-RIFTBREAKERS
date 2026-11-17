using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

namespace ROM.CiudadDelSol
{
    public sealed partial class CiudadDelSolPopulation
    {
        readonly Queue<Post[]> validationQueue = new Queue<Post[]>();
        int approvedGroups, rejectedGroups;
        readonly Dictionary<string, string> sectorIssues = new Dictionary<string, string>();

        void PrepareEncounterValidation()
        {
            if (!configuration.ValidateExteriorEncounters) return;
            foreach (var sector in configuration.Sectors)
                foreach (var group in posts.Where(p => p.Definition.SectorId == sector.Id).GroupBy(p => p.Definition.EncounterId))
                    validationQueue.Enqueue(group.ToArray());
            Debug.Log($"CIUDAD_EXTERIOR VALIDACION_PENDIENTE: {validationQueue.Count} encuentros, {posts.Count} puestos. " +
                "Comprobación por sectores antes de habilitar apariciones; no se hornea navegación.", this);
        }

        bool ValidateNextEncounter()
        {
            if (validationQueue.Count == 0) return false;
            var group = validationQueue.Dequeue(); string error = null;
            var sector = configuration.Sectors.Single(s => s.Id == group[0].Definition.SectorId);
            if (!sectorIssues.TryGetValue(sector.Id, out error))
            {
                var recovery = configuration.RecoveryAreas?.SingleOrDefault(a => a.Id == sector.RecoveryAreaId);
                if (recovery == null) error = "Sector sin punto de recuperación configurado";
                else if (!CiudadDelSolEncounterValidation.RecoveryArea(gameObject.scene, configuration, recovery, out var issue))
                    error = "Recuperación " + recovery.Id + ": " + issue;
                else error = null;
                sectorIssues.Add(sector.Id, error);
            }
            if (error == null) foreach (var post in group)
            {
                try
                {
                    if (!CiudadDelSolEncounterValidation.Validate(gameObject.scene, configuration, post.Definition, post.Reference,
                        post.Occupant ? post.Occupant.transform : null, out post.Patrol, out var issue))
                    { error = post.Definition.Id + " @ " + post.Reference.ToString("F3") + ": " + issue; break; }
                }
                catch (Exception exception) { error = post.Definition.Id + ": " + exception.Message; break; }
            }
            if (error == null)
                for (int i = 0; i < group.Length; i++) for (int j = i + 1; j < group.Length; j++)
                {
                    float minimum = group[i].Definition.Enemy.Prefab.Body.radius + group[j].Definition.Enemy.Prefab.Body.radius + .5f;
                    if (Vector3.Distance(group[i].Reference, group[j].Reference) < minimum)
                        error = "Puestos superpuestos: " + group[i].Definition.Id + " / " + group[j].Definition.Id;
                }
            foreach (var post in group) { post.Approved = error == null; if (!post.Approved) post.Patrol = Array.Empty<Vector3>(); }
            var first = group[0].Definition;
            if (error != null)
            {
                rejectedGroups++;
                Debug.LogWarning($"CIUDAD_EXTERIOR GRUPO_BLOQUEADO {first.SectorId}/{first.EncounterId}: {error}. " +
                    "No se desplaza el spawn. Los actores adoptados se conservan; no habrá nuevas apariciones de este grupo.", this);
            }
            else
            {
                approvedGroups++;
                foreach (var post in group) InstallPatrol(post);
                Debug.Log($"CIUDAD_EXTERIOR GRUPO_VALIDADO {first.SectorId}/{first.EncounterId}: {group.Length} puestos, " +
                    $"{group.Count(p => p.Patrol.Length > 0)} patrullas, acceso completo en ambos sentidos y holgura física. Combate pendiente de prueba.", this);
            }
            if (validationQueue.Count == 0)
                Debug.Log($"CIUDAD_EXTERIOR PREFLIGHT_TERMINADO: grupos aprobados={approvedGroups}, bloqueados={rejectedGroups}; " +
                    $"puestos habilitados={posts.Count(p => p.Approved)}/{posts.Count}. Solo los aprobados pueden aparecer. " +
                    "No equivale a validación funcional ni de rendimiento.", this);
            return true;
        }

        void InstallPatrol(Post post)
        {
            if (!post.Occupant || post.Occupant.Dead || !post.Approved || post.Patrol.Length < 2 || post.Occupant.GetComponent<CiudadDelSolPatrol>()) return;
            post.Occupant.gameObject.AddComponent<CiudadDelSolPatrol>().Configure(post.Occupant,
                post.Definition.Id, post.Patrol, post.Definition.PatrolWaitSeconds);
        }

        bool EncounterFighting(string id) => posts.Any(p => p.Definition.EncounterId == id && p.Occupant &&
            !p.Occupant.Dead && (p.Occupant.Target || p.Occupant.Attacking || p.Occupant.IsSkillCasting));

        bool SpawnInitialEncounter(Post first)
        {
            var group = posts.Where(p => p.Definition.EncounterId == first.Definition.EncounterId && p.Approved &&
                !p.Occupant && p.Schedule.Phase == CiudadDelSolRespawnSchedule.State.Initial).ToArray();
            foreach (var post in group) post.RetryAt = NetworkTime.time + configuration.RetrySeconds;
            if (!group.Any(p => CiudadDelSolSpawnSafety.HasHumanNear(p.Reference, p.Definition.ActivationRadius))) return false;
            if (EncounterFighting(first.Definition.EncounterId)) { Deferred(first, "encuentro en combate"); return false; }
            if (group.Any(p => !CiudadDelSolSpawnSafety.PlayersClear(p.Reference, configuration)))
            { Deferred(first, "jugador próximo al grupo inicial"); return false; }
            int sectorCount = posts.Count(p => p.Occupant && p.Definition.SectorId == first.Definition.SectorId);
            int sectorMaximum = configuration.Sectors.Single(s => s.Id == first.Definition.SectorId).Maximum;
            if (OccupiedCount + group.Length > configuration.GlobalMaximum || sectorCount + group.Length > sectorMaximum)
            { Deferred(first, "presupuesto insuficiente para el grupo completo"); return false; }
            var positions = new Vector3[group.Length];
            for (int i = 0; i < group.Length; i++)
                if (!CiudadDelSolSpawnSafety.Placement(gameObject.scene, group[i].Definition, group[i].Reference, out positions[i], out var reason))
                { Deferred(first, group[i].Definition.Id + ": " + reason); return false; }
            for (int i = 0; i < group.Length; i++)
            {
                Spawn(group[i], positions[i]);
                if (faulted) break;
            }
            return true;
        }
    }
}
