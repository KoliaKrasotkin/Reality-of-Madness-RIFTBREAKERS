using UnityEngine;
namespace MultiFPS.Gameplay.RiftCombat {
[CreateAssetMenu(menuName="ROM/Progression/Playtest and world caps")]
public sealed class RiftProgressionCaps:ScriptableObject {
 [Range(1,60)]public int DesignMaximumLevel=60,WorldMaximumLevel=60,PlayerLevelCap=20;
 public int DesignCap=>Mathf.Clamp(DesignMaximumLevel,1,60);
 public int WorldCap=>Mathf.Clamp(WorldMaximumLevel,1,DesignCap);
 public int ActivePlayerCap=>Mathf.Clamp(PlayerLevelCap,1,DesignCap);
 static RiftProgressionCaps cached;public static RiftProgressionCaps Load{get{if(!cached)cached=Resources.Load<RiftProgressionCaps>("RiftProgressionCaps");if(!cached)cached=CreateInstance<RiftProgressionCaps>();return cached;}}
}}
