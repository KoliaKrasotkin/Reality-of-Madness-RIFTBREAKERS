using System;
using UnityEngine;
namespace MultiFPS.Gameplay
{
    public interface IRiftLocalPickup { bool LocalVisible {get;} void RequestPickup(bool automatic=false); }
    // Friendly scene actors do not need fake Health or a networked combat body.
    public sealed class RiftWorldTarget:MonoBehaviour
    {
        public string Id,DisplayName,Role;public Transform Head;public float InteractionRange=4.5f;
        public event Action InteractRequested;
        public void Interact(){InteractRequested?.Invoke();}
    }
    public enum RiftProgressEvent { TrainingHit,Talk,Kill,Checkpoint,MiniBoss,Encounter,PotionBought,PotionUsed,QuestTurnedIn,Collect }
    public static class RiftWorldEvents
    {
        public static event Action<Health> TrainingHit;
        public static void ReportTrainingHit(Health attacker){TrainingHit?.Invoke(attacker);}
    }
}
