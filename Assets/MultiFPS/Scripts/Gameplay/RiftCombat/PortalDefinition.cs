using UnityEngine;
namespace MultiFPS.Gameplay.RiftCombat {
[CreateAssetMenu(menuName="ROM/Combat/Portal")]
public sealed class PortalDefinition:ScriptableObject {public int Id;public string DisplayName;public float InteractRange=4,Cooldown=4;public GameObject Effect;}
}
