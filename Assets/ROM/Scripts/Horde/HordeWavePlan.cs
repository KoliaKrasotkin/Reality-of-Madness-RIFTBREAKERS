using System;
using UnityEngine;

namespace ROM.Horde
{
    /// <summary>
    /// Anomalías de la grieta: modificadores que pueden tocarle a una oleada para
    /// romper la monotonía. El servidor elige uno al armar la oleada y lo manda a
    /// los clientes junto con el resto del estado.
    /// </summary>
    public enum HordeModifier : byte
    {
        None,
        Blindados,  //más vida
        Frenesi,    //aparecen más rápido y hay más vivos a la vez
        Enjambre,   //muchos más, pero más frágiles
        Artilleria, //los enemigos vienen con armas pesadas
        Cazadores,  //disparan con más frecuencia
    }

    /// <summary>
    /// Todo lo que el gamemode necesita saber para correr una oleada. Se calcula
    /// una sola vez al empezarla, a partir del número de oleada y de los jugadores.
    /// </summary>
    public struct HordeWavePlan
    {
        public int Wave;
        public int EnemyCount;
        public int BossCount;
        public int MaxAlive;
        public float SpawnInterval;
        public float HealthMultiplier;
        public float EliteChance;
        public float MeleeChance;
        public float BotFireTime;
        public float BotWaitForFireTime;
        public int ActivePortals;
        public HordeModifier Modifier;

        public bool IsBossWave => BossCount > 0;
    }

    /// <summary>
    /// Curva de dificultad de la horda. Vive como bloque serializado dentro del
    /// gamemode para poder ajustarla desde el inspector sin tocar código.
    ///
    /// La dificultad sube por cantidad, por ritmo de aparición, por cuántos
    /// enemigos hay vivos a la vez, por vida y por agresividad de la IA. No se
    /// escala el daño de las armas: un enemigo pega lo mismo en la oleada 1 que
    /// en la 20, lo que cambia es cuántos te están apuntando.
    /// </summary>
    [Serializable]
    public class HordeDifficulty
    {
        [Header("Cantidad")]
        [Tooltip("Enemigos de la primera oleada.")]
        public int BaseEnemies = 5;
        [Tooltip("Enemigos que se suman por cada oleada.")]
        public float EnemiesPerWave = 2.5f;
        [Tooltip("Crecimiento cuadrático: hace que las oleadas altas se vuelvan realmente densas.")]
        public float EnemiesQuadratic = 0.12f;
        [Tooltip("Multiplicador de cantidad por cada jugador extra en la partida.")]
        public float ExtraPlayerCountFactor = 0.5f;

        [Header("Cuerpo a cuerpo")]
        [Tooltip("Hasta esta oleada inclusive, todos los enemigos comunes vienen solo con cuchillo.")]
        public int MeleeOnlyUntilWave = 2;
        [Tooltip("Probabilidad de que un enemigo común venga a cuchillo en la primera oleada con armas de fuego.")]
        [Range(0f, 1f)] public float MeleeChanceAfter = 0.6f;
        [Tooltip("Cuánto baja esa probabilidad por cada oleada siguiente.")]
        public float MeleeChanceDecay = 0.12f;
        [Tooltip("Piso de enemigos a cuchillo: siempre queda alguno que viene a buscarte de cerca.")]
        [Range(0f, 1f)] public float MinMeleeChance = 0.15f;

        [Header("Presión simultánea")]
        public int BaseMaxAlive = 4;
        public int MaxAlivePerWave = 1;
        public int MaxAliveExtraPerPlayer = 2;
        [Tooltip("Tope duro de enemigos vivos a la vez. Cada uno es un bot completo con IA y red, no conviene pasarse.")]
        public int MaxAliveCap = 20;

        [Header("Ritmo de aparición")]
        public float BaseSpawnInterval = 2.8f;
        [Tooltip("Cada oleada multiplica el intervalo por este valor.")]
        public float SpawnIntervalDecay = 0.92f;
        public float MinSpawnInterval = 0.4f;

        [Header("Resistencia")]
        [Tooltip("Vida extra por oleada, como fracción de la vida base (0.06 = +6% por oleada).")]
        public float HealthPerWave = 0.06f;
        [Tooltip("Oleada desde la que empiezan a aparecer élites.")]
        public int EliteFromWave = 5;
        public float EliteChancePerWave = 0.06f;
        public float MaxEliteChance = 0.45f;
        public float EliteHealthMultiplier = 2f;

        [Header("Jefes")]
        [Tooltip("Cada cuántas oleadas aparece un Coloso. 0 desactiva los jefes.")]
        public int BossEveryWaves = 5;
        public float BossHealthMultiplier = 8f;

        [Header("Agresividad de la IA")]
        [Tooltip("Ráfaga y pausa de disparo del bot en las primeras oleadas con armas: ráfagas cortas y pausas largas.")]
        public float StartFireTime = 0.7f;
        public float StartWaitForFireTime = 1.6f;
        [Tooltip("Valores a los que llega en la oleada de agresividad máxima (los del kit son 1.55 / 0.75).")]
        public float EndFireTime = 1.4f;
        public float EndWaitForFireTime = 0.6f;
        public int WaveOfMaxAggression = 18;

        [Header("Portales")]
        [Tooltip("Portales activos en la primera oleada. Se abre uno más por oleada hasta usar todos.")]
        public int StartingPortals = 4;

        [Header("Anomalías")]
        [Tooltip("Oleada desde la que pueden aparecer anomalías.")]
        public int ModifiersFromWave = 3;
        [Range(0f, 1f)] public float ModifierChance = 0.4f;

        public HordeWavePlan Build(int wave, int players, int portalCount, HordeModifier modifier)
        {
            wave = Mathf.Max(1, wave);
            players = Mathf.Max(1, players);
            int n = wave - 1;

            float count = BaseEnemies + EnemiesPerWave * n + EnemiesQuadratic * n * n;
            count *= 1f + ExtraPlayerCountFactor * (players - 1);

            int maxAlive = BaseMaxAlive + MaxAlivePerWave * n + MaxAliveExtraPerPlayer * (players - 1);
            float interval = Mathf.Max(MinSpawnInterval, BaseSpawnInterval * Mathf.Pow(SpawnIntervalDecay, n));
            float health = 1f + HealthPerWave * n;

            float eliteChance = wave >= EliteFromWave
                ? Mathf.Min(MaxEliteChance, EliteChancePerWave * (wave - EliteFromWave + 1))
                : 0f;

            float aggression = WaveOfMaxAggression > 1 ? Mathf.Clamp01(n / (float)(WaveOfMaxAggression - 1)) : 1f;

            float melee = wave <= MeleeOnlyUntilWave
                ? 1f
                : Mathf.Max(MinMeleeChance, MeleeChanceAfter - MeleeChanceDecay * (wave - MeleeOnlyUntilWave - 1));

            switch (modifier)
            {
                case HordeModifier.Blindados:
                    health *= 1.5f;
                    break;
                case HordeModifier.Frenesi:
                    interval *= 0.5f;
                    maxAlive += 4;
                    break;
                case HordeModifier.Enjambre:
                    count *= 1.5f;
                    health *= 0.7f;
                    break;
                case HordeModifier.Cazadores:
                    aggression = Mathf.Min(1f, aggression + 0.5f);
                    break;
            }

            return new HordeWavePlan
            {
                Wave = wave,
                EnemyCount = Mathf.Max(1, Mathf.RoundToInt(count)),
                BossCount = BossEveryWaves > 0 && wave % BossEveryWaves == 0 ? wave / BossEveryWaves : 0,
                MaxAlive = Mathf.Clamp(maxAlive, 1, MaxAliveCap),
                SpawnInterval = Mathf.Max(0.15f, interval),
                HealthMultiplier = health,
                EliteChance = eliteChance,
                MeleeChance = modifier == HordeModifier.Artilleria ? 0f : melee,
                BotFireTime = Mathf.Lerp(StartFireTime, EndFireTime, aggression),
                BotWaitForFireTime = Mathf.Lerp(StartWaitForFireTime, EndWaitForFireTime, aggression),
                ActivePortals = Mathf.Clamp(StartingPortals + n, 1, Mathf.Max(1, portalCount)),
                Modifier = modifier,
            };
        }

        public HordeModifier RollModifier(int wave)
        {
            if (wave < ModifiersFromWave || UnityEngine.Random.value > ModifierChance)
                return HordeModifier.None;

            //el primer valor del enum es None, se sortea entre el resto
            int options = Enum.GetValues(typeof(HordeModifier)).Length - 1;
            return (HordeModifier)UnityEngine.Random.Range(1, options + 1);
        }

        public static string DisplayName(HordeModifier modifier)
        {
            switch (modifier)
            {
                case HordeModifier.Blindados: return "BLINDADOS";
                case HordeModifier.Frenesi: return "FRENESÍ";
                case HordeModifier.Enjambre: return "ENJAMBRE";
                case HordeModifier.Artilleria: return "ARTILLERÍA";
                case HordeModifier.Cazadores: return "CAZADORES";
                default: return string.Empty;
            }
        }

        public static string Description(HordeModifier modifier)
        {
            switch (modifier)
            {
                case HordeModifier.Blindados: return "Los enemigos tienen +50% de vida";
                case HordeModifier.Frenesi: return "Aparecen el doble de rápido y en mayor número a la vez";
                case HordeModifier.Enjambre: return "+50% de enemigos, pero más frágiles";
                case HordeModifier.Artilleria: return "Los enemigos traen armas pesadas";
                case HordeModifier.Cazadores: return "Los enemigos disparan con más insistencia";
                default: return string.Empty;
            }
        }
    }
}
