using System;

namespace ROM.CiudadDelSol
{
    // Pure timing state, independent of Unity and Mirror. Owning a corpse is handled by the population.
    public sealed class CiudadDelSolRespawnSchedule
    {
        public enum State { Initial, Occupied, CoolingDown, Defeated }
        public State Phase { get; private set; }
        public double ReadyAt { get; private set; }
        public bool Due(double now) => Phase == State.Initial || Phase == State.CoolingDown && now >= ReadyAt;

        public void Spawned()
        {
            if (Phase == State.Occupied || Phase == State.Defeated)
                throw new InvalidOperationException("El puesto está ocupado o derrotado permanentemente.");
            Phase = State.Occupied;
        }

        public void Lost(double now, double seconds, bool respawns)
        {
            if (Phase != State.Occupied) return; // Duplicate death callbacks cannot extend or duplicate a reservation.
            if (double.IsNaN(now) || double.IsInfinity(now) || double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 1)
                throw new ArgumentOutOfRangeException(nameof(seconds));
            Phase = respawns ? State.CoolingDown : State.Defeated;
            ReadyAt = respawns ? now + seconds : double.PositiveInfinity;
        }
    }
}
