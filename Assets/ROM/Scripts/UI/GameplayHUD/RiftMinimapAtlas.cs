using UnityEngine;
namespace ROM.UI.Gameplay {
// Static environment capture; player position/orientation stay live in the HUD.
public sealed class RiftMinimapAtlas:ScriptableObject {
 public Texture2D Map;public Vector2 Center,WorldSize;
 public Rect View(Vector3 point,float radius)=>new Rect((point.x-Center.x-radius)/WorldSize.x+.5f,(point.z-Center.y-radius)/WorldSize.y+.5f,2*radius/WorldSize.x,2*radius/WorldSize.y);
}}
