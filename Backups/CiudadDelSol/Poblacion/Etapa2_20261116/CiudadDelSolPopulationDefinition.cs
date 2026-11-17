using System;
using MultiFPS.Gameplay.RiftEnemies;
using UnityEngine;

namespace ROM.CiudadDelSol
{
    public enum CiudadEnemyRank { Normal, Elite, MiniBoss, Boss }

    [Serializable] public sealed class CiudadSectorBudget
    {
        public string Id;
        [Min(1)] public int Maximum = 4;
    }

    [Serializable] public sealed class CiudadRecoveryArea
    {
        public string Id;
        public Vector3 Position;
        [Min(1)] public float Radius = 22;
    }

    [Serializable] public sealed class CiudadEnemyPost
    {
        public string Id, SectorId, EncounterId;
        public RiftEnemyDefinition Enemy;
        public CiudadEnemyRank Rank;
        public Vector3 Position;
        // Optional exact scene anchor, resolved once. Never searches or changes the position on failure.
        public string AnchorPath;
        public string PlayableRoot, PlayableAncestor;
        public float Yaw = 180;
        [Min(1)] public int Level = 1;
        public bool Respawn = true;
        [Min(1)] public float RespawnSeconds = 90;
        [Min(1)] public float ActivationRadius = 150;
        [Min(1)] public float HealthMultiplier = 1, DamageMultiplier = 1;
        public bool AutoRespawn => Respawn && Rank != CiudadEnemyRank.Boss && !Enemy.IsBoss;
    }

    [CreateAssetMenu(menuName = "ROM/Ciudad del Sol/Population")]
    public sealed class CiudadDelSolPopulationDefinition : ScriptableObject
    {
        public const string ScenePath = "Assets/Scenes/Dungeon_Isla_del_Sol.unity";
        public string Stage = "Etapa 1 - infraestructura";
        public RiftEnemyDefinition[] CompatibleEnemies;
        public CiudadEnemyPost[] Posts;
        public CiudadSectorBudget[] Sectors;
        public CiudadRecoveryArea[] RecoveryAreas;
        [Min(1)] public int GlobalMaximum = 2;
        [Min(.25f)] public float TickSeconds = .5f;
        [Min(1)] public float RetrySeconds = 5, PlayerExclusionRadius = 25, CombatExclusionRadius = 40;
        [Min(1)] public float CombatQuietSeconds = 10;
        public bool ScaleForHumanPlayers = true;
    }
}
