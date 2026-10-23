using UnityEngine;
namespace ROM.UI.Sprint01 {
// Immutable atlas prevents hover/size changes from invalidating another label's glyph UVs.
public sealed class RiftInventoryFontScope:MonoBehaviour {
 static Font fixedFont;Font fallback;
 public Font Font {get{if(!fixedFont)fixedFont=Resources.Load<Font>("RiftInventoryStatic");return fixedFont?fixedFont:fallback;}set{fallback=value;}}
}
}
