using System.Collections.Generic;
using UnityEngine;
namespace MultiFPS.Gameplay.RiftCombat {
public sealed class RiftPortal:MonoBehaviour {public PortalDefinition Definition;public Transform Destination;public static readonly List<RiftPortal> All=new List<RiftPortal>();void OnEnable(){All.Add(this);}void OnDisable(){All.Remove(this);}}
}
