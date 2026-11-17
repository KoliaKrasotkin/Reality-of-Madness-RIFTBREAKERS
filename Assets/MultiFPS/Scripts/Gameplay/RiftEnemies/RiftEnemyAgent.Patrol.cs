using Mirror;
using UnityEngine;

namespace MultiFPS.Gameplay.RiftEnemies
{
    public sealed partial class RiftEnemyAgent
    {
        [Server] public bool ServerTryIdleRoute(Vector3 point)
        {
            if (!CityRaidPursuitActive || ServerIdleRoute == null || Dead || Target || attacking || skillCasting || returning ||
                NetworkTime.time < staggerUntil) return false;
            return TryCityRaidPath(point);
        }

        [Server] public void ServerStopIdleRoute()
        {
            if (!Target && !attacking && !skillCasting) Stop();
        }

        [Server] public void ServerReturnAfterPatrolCombat()
        {
            if (!Target && !Dead) { Stop(); returning = true; nextPath = 0; }
        }
    }
}
