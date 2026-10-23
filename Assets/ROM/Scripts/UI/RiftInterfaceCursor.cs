using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using MultiFPS;
namespace ROM.UI.Sprint01 {
// Presentation only: cursor visibility and locking stay owned by the existing input manager.
public sealed class RiftInterfaceCursor:MonoBehaviour {
 Texture2D texture;
 public static int InventoryConsumedFrame=-1;
 public static bool Typing=>RiftControls.TextEntryFocused;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Install(){var go=new GameObject("Rift interface cursor");DontDestroyOnLoad(go);go.AddComponent<RiftInterfaceCursor>();}
 void Awake(){
  texture=new Texture2D(32,32,TextureFormat.RGBA32,false);texture.name="Rift ivory cursor";texture.filterMode=FilterMode.Point;
  var pixels=new Color32[1024];
  // An original ivory spear pointer; upper-left tip is also the click hotspot.
  for(int y=2;y<27;y++)for(int x=2;x<20;x++){
   bool head=y<21&&x<=2+(y-2)*.56f;bool stem=y>=14&&y<27&&x>=7+(y-14)*.25f&&x<=10+(y-14)*.25f;
   if(!head&&!stem)continue;
   bool edge=x==2||y==2||head&&x>=1+(y-2)*.56f||y>=25;
   pixels[(31-y)*32+x]=edge?new Color32(28,30,37,255):new Color32(230,205,154,255);
  }
  texture.SetPixels32(pixels);texture.Apply();Cursor.SetCursor(texture,new Vector2(2,2),CursorMode.Auto);
 }
 void OnApplicationFocus(bool focus){if(focus&&texture)Cursor.SetCursor(texture,new Vector2(2,2),CursorMode.Auto);}
 void OnDestroy(){Cursor.SetCursor(null,Vector2.zero,CursorMode.Auto);if(texture)Destroy(texture);}
}
}
