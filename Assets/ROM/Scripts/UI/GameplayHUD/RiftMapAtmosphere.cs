using UnityEngine;
using UnityEngine.UI;
namespace ROM.UI.Gameplay {
// Decorative ocean/rift wings only. All navigable pixels and coordinates remain in Content.
public sealed class RiftMapAtmosphere:MonoBehaviour {
 RiftFieldUI map;RectTransform backdrop;Material blend,foreground,previous;bool cartography;
 public void SetCartography(bool active){cartography=active;if(backdrop)backdrop.gameObject.SetActive(!active);if(map&&map.MapImage&&foreground)map.MapImage.material=active?previous:foreground;}
 public void Initialize(RiftFieldUI owner){map=owner;if(map.MapImage.texture&&map.MapImage.texture.width!=map.MapImage.texture.height)return;var texture=Resources.Load<Texture2D>("RiftWorldMap/Atmosphere");if(!texture)return;backdrop=new GameObject("Decorative Rift ocean wings",typeof(RectTransform)).GetComponent<RectTransform>();backdrop.SetParent(map.Viewport,false);backdrop.anchorMin=backdrop.anchorMax=backdrop.pivot=new Vector2(.5f,.5f);backdrop.SetAsFirstSibling();var image=backdrop.gameObject.AddComponent<RawImage>();image.texture=texture;image.raycastTarget=false;var shader=Shader.Find("ROM/UI/Map ocean wing blend");if(shader){blend=new Material(shader){hideFlags=HideFlags.HideAndDontSave};blend.SetTexture("_OriginalTex",map.MapImage.texture);image.material=blend;previous=map.MapImage.material;foreground=new Material(blend){hideFlags=HideFlags.HideAndDontSave};foreground.SetFloat("_Foreground",1);map.MapImage.material=foreground;}}
 void LateUpdate(){Refresh();}
 public void Refresh(){if(cartography||!map||!backdrop)return;float h=map.Content.rect.height;backdrop.sizeDelta=new Vector2(Mathf.Max(h*1.5f,map.Viewport.rect.width/map.Content.localScale.x),h);backdrop.localScale=map.Content.localScale;backdrop.anchoredPosition=map.Content.anchoredPosition;if(blend)blend.SetFloat("_SquareWidth",h/backdrop.rect.width);}
 void OnDestroy(){if(map&&map.MapImage)map.MapImage.material=previous;if(foreground)Destroy(foreground);if(blend)Destroy(blend);}
}
}
