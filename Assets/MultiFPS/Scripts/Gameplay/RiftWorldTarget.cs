using System;
using UnityEngine;
namespace MultiFPS.Gameplay
{
    // Friendly scene actors do not need fake Health or a networked combat body.
    public sealed class RiftWorldTarget:MonoBehaviour
    {
        public string Id,DisplayName,Role;public Transform Head;public float InteractionRange=4.5f;
        public event Action InteractRequested;
        public void Interact(){InteractRequested?.Invoke();}
    }
    public enum RiftProgressEvent { TrainingHit,Talk,Kill,Checkpoint,MiniBoss,Encounter,PotionBought,PotionUsed,QuestTurnedIn }
    public static class RiftWorldEvents
    {
        public static event Action<Health> TrainingHit;
        public static void ReportTrainingHit(Health attacker){TrainingHit?.Invoke(attacker);}
    }
}
