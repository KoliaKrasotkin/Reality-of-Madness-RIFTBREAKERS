using System;using UnityEngine;
namespace ROM.UI.Gameplay {
// Calibrated full-map presentation only. The HUD minimap retains its literal atlas and rectangular crop.
public sealed class RiftCartographicProjection {
 public const bool IllustratedMapApproved=true;
 [Serializable]public sealed class Route {public string Name;public float[] FromWorld,ToWorld;public Vector2 ArtStart,ArtEnd;public float Width;}
 [Serializable]public sealed class Calibration {public Vector2 Center,WorldSize;public Vector2[] From,To;public int[] Triangles;public string[] AnchorNames;public Route[] Routes;public int ArtSize;public float MaxHoldoutErrorPixels;}
 public readonly Calibration Data;public readonly Texture2D Texture;public int TriangleCount=>Data.Triangles.Length/3;
 RiftCartographicProjection(Calibration data,Texture2D texture){Data=data;Texture=texture;}
 public static RiftCartographicProjection Load(RiftMinimapAtlas atlas){if(!IllustratedMapApproved&&Array.IndexOf(Environment.GetCommandLineArgs(),"-riftIllustratedMapReview")<0)return null;var source=Resources.Load<TextAsset>("RiftWorldMap/Projection");var texture=Resources.Load<Texture2D>("RiftWorldMap/Illustrated");if(!source||!texture)return null;var data=JsonUtility.FromJson<Calibration>(source.text);if(data==null||data.From==null||data.To==null||data.From.Length!=data.To.Length||data.Triangles==null||data.Triangles.Length%3!=0||Vector2.Distance(data.Center,atlas.Center)>.001f||Vector2.Distance(data.WorldSize,atlas.WorldSize)>.001f){Debug.LogError("World-map calibration does not match the literal atlas; using literal fallback.");return null;}for(int i=0;i<data.Triangles.Length;i+=3){int a=data.Triangles[i],b=data.Triangles[i+1],c=data.Triangles[i+2];if(a<0||b<0||c<0||Mathf.Max(a,Mathf.Max(b,c))>=data.From.Length||Cross(data.From[a],data.From[b],data.From[c])<=1e-9f||Cross(data.To[a],data.To[b],data.To[c])<=1e-9f){Debug.LogError("Folded or invalid world-map calibration; using literal fallback.");return null;}}return new RiftCartographicProjection(data,texture);}
 static float Cross(Vector2 a,Vector2 b,Vector2 c)=>(b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x);
 bool Project(Vector2 value,Vector2[] from,Vector2[] to,out Vector2 result){for(int i=0;i<Data.Triangles.Length;i+=3){int a=Data.Triangles[i],b=Data.Triangles[i+1],c=Data.Triangles[i+2];float den=Cross(from[a],from[b],from[c]);float x=Cross(value,from[b],from[c])/den,y=Cross(from[a],value,from[c])/den,z=1-x-y;if(x>=-.00001f&&y>=-.00001f&&z>=-.00001f){result=to[a]*x+to[b]*y+to[c]*z;return true;}}result=default;return false;}
 public bool TryMap(Vector2 uv,out Vector2 mapped)=>Project(uv,Data.From,Data.To,out mapped);
 public bool TryInverse(Vector2 uv,out Vector2 mapped)=>Project(uv,Data.To,Data.From,out mapped);
 public Vector2 WorldUV(Vector3 world)=>new Vector2((world.x-Data.Center.x)/Data.WorldSize.x+.5f,(world.z-Data.Center.y)/Data.WorldSize.y+.5f);
 }
}
