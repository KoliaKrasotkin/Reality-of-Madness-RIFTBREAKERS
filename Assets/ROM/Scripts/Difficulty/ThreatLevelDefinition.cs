using UnityEngine;

namespace ROM.Difficulty
{
    /// <summary>
    /// Parámetros de un Threat Level. Un asset por nivel.
    ///
    /// Regla de diseño del GDD: los Threat Levels NO son un multiplicador de daño
    /// ni de vida. Suben la presión y la complejidad, y bajan el margen de error.
    /// No agregar acá escalado de stats: si un nivel se vuelve más difícil porque
    /// los enemigos pegan más fuerte, se rompió el principio.
    /// </summary>
    [CreateAssetMenu(fileName = "threat_XXX", menuName = "ROM/Threat Level Definition")]
    public class ThreatLevelDefinition : ScriptableObject
    {
        [Header("Identidad")]
        public ThreatLevel Level = ThreatLevel.I;

        [Tooltip("Nombre visible en UI, por ejemplo \"Threat II\" o \"Madness\".")]
        public string DisplayName = "Threat I";

        [Header("Caído")]
        [Tooltip("Segundos que un Redhead aguanta caído antes de quedar fuera definitivamente.")]
        [Min(1f)] public float BleedOutSeconds = 60f;

        [Tooltip("Si el jugador puede levantarse solo, sin ayuda de un compañero.")]
        public bool AllowSelfRevive = false;

        [Header("Revivir")]
        [Tooltip("Segundos que un compañero tiene que mantener la interacción para completar el revive.")]
        [Min(0.1f)] public float ReviveHoldSeconds = 4f;

        [Tooltip("Porcentaje de la vida máxima con el que vuelve el revivido.")]
        [Range(1, 100)] public int ReviveHealthPercent = 35;

        [Header("Presupuesto de revividas")]
        public ReviveBudgetMode BudgetMode = ReviveBudgetMode.SquadPool;

        [Tooltip("Revividas que comparte toda la escuadra. Solo se usa con BudgetMode = SquadPool.")]
        [Min(0)] public int SquadReviveBudget = 5;

        [Tooltip("Revividas que recibe cada jugador por separado. Solo se usa con BudgetMode = PerPlayer, que todavía no está implementado.")]
        [Min(0)] public int RevivesPerPlayer = 2;

        [Header("Fallo de incursión")]
        [Tooltip("La incursión falla cuando la escuadra ya no puede continuar.")]
        public bool FailWhenSquadCannotContinue = true;

        /// <summary>
        /// Vida absoluta con la que vuelve un revivido, a partir de la vida máxima
        /// del personaje. Siempre devuelve al menos 1: revivir a alguien en 0 lo
        /// dejaría caído de nuevo en el mismo frame.
        /// </summary>
        public int GetReviveHealth(int maxHealth)
        {
            return Mathf.Max(1, Mathf.RoundToInt(maxHealth * (ReviveHealthPercent / 100f)));
        }
    }

    /// <summary>
    /// Niveles de amenaza del GDD. Byte para que viaje barato por red.
    /// None existe para detectar un nivel sin asignar, no es un nivel jugable.
    /// </summary>
    public enum ThreatLevel : byte
    {
        None = 0,
        I = 1,
        II = 2,
        III = 3,
        IV = 4,
        V = 5,
        Madness = 6,
    }

    /// <summary>
    /// Cómo se contabilizan las revividas disponibles.
    /// El consumidor no distingue el modo: le pregunta al presupuesto si puede
    /// revivir y le avisa cuando consumió una. Agregar un modo nuevo no lo toca.
    /// </summary>
    public enum ReviveBudgetMode : byte
    {
        /// <summary>Sin límite. Útil para Threat I y para desarrollo.</summary>
        Unlimited = 0,

        /// <summary>Un pool compartido por toda la escuadra.</summary>
        SquadPool = 1,

        /// <summary>Un cupo por jugador. TODAVÍA NO IMPLEMENTADO.</summary>
        PerPlayer = 2,
    }
}
