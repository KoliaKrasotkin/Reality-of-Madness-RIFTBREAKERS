using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace ROM.UI.Gameplay {
 // Rounded surfaces and small glow rings are UI geometry. No textures, blur pass or global material changes.
 [RequireComponent(typeof(CanvasRenderer))]public sealed class RiftMapSurface:MaskableGraphic {
  public Color Fill=new Color(.1f,.1f,.15f,.8f),Border=new Color(.5f,.46f,.65f);public float Radius=8,Glow;
  List<Vector2> Ring(Rect r,float radius){var points=new List<Vector2>();float rad=Mathf.Min(radius,Mathf.Min(r.width,r.height)*.5f);var centers=new[]{new Vector2(r.xMax-rad,r.yMin+rad),new Vector2(r.xMax-rad,r.yMax-rad),new Vector2(r.xMin+rad,r.yMax-rad),new Vector2(r.xMin+rad,r.yMin+rad)};for(int k=0;k<4;k++)for(int j=0;j<=6;j++){float a=(-90+k*90+j*15)*Mathf.Deg2Rad;points.Add(centers[k]+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*rad);}return points;}
  void Band(VertexHelper vh,List<Vector2> a,List<Vector2> b,Color tint){for(int i=0;i<a.Count;i++){int j=(i+1)%a.Count,n=vh.currentVertCount;vh.AddVert(a[i],tint*color,Vector2.zero);vh.AddVert(a[j],tint*color,Vector2.zero);vh.AddVert(b[j],tint*color,Vector2.zero);vh.AddVert(b[i],tint*color,Vector2.zero);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);}}
  protected override void OnPopulateMesh(VertexHelper vh){vh.Clear();var r=rectTransform.rect;if(r.width<2||r.height<2)return;var edge=Ring(r,Radius);var innerRect=new Rect(r.x+1,r.y+1,r.width-2,r.height-2);var inner=Ring(innerRect,Mathf.Max(0,Radius-1));int center=vh.currentVertCount;vh.AddVert(r.center,Fill*color,Vector2.zero);foreach(var p in inner)vh.AddVert(p,Fill*color,Vector2.zero);for(int i=0;i<inner.Count;i++)vh.AddTriangle(center,center+1+i,center+1+(i+1)%inner.Count);Band(vh,edge,inner,Border);if(Glow>0)for(int i=0;i<4;i++){float d=(i+1)*Glow/4;var expanded=new Rect(r.x-d,r.y-d,r.width+d*2,r.height+d*2);var next=Ring(expanded,Radius+d);var tint=Border;tint.a*=.19f*(1-i*.2f);Band(vh,next,edge,tint);edge=next;}}
 }
}
