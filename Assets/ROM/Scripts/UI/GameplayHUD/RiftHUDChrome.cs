using UnityEngine;
using UnityEngine.UI;
namespace ROM.UI.Gameplay {
// Native angular charcoal/violet frame, shared only by the gameplay HUD.
[RequireComponent(typeof(CanvasRenderer))]public sealed class RiftHUDChrome:MaskableGraphic {
 public Color Fill=new Color(.025f,.026f,.041f,.86f),Border=new Color(.48f,.31f,.66f,.95f);
 public float Cut=12;public bool Accents=true;
 void Quad(VertexHelper v,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color color){int i=v.currentVertCount;v.AddVert(a,color,Vector2.zero);v.AddVert(b,color,Vector2.zero);v.AddVert(c,color,Vector2.zero);v.AddVert(d,color,Vector2.zero);v.AddTriangle(i,i+1,i+2);v.AddTriangle(i,i+2,i+3);}
 void Line(VertexHelper v,Vector2 a,Vector2 b,float w,Color c){var d=(b-a).normalized;var n=new Vector2(-d.y,d.x)*w*.5f;Quad(v,a+n,b+n,b-n,a-n,c);}
 protected override void OnPopulateMesh(VertexHelper v){v.Clear();var r=rectTransform.rect;float c=Mathf.Min(Cut,Mathf.Min(r.width,r.height)*.2f);var p=new[]{new Vector2(r.xMin+c,r.yMax),new Vector2(r.xMax-c,r.yMax),new Vector2(r.xMax,r.yMax-c),new Vector2(r.xMax,r.yMin+c),new Vector2(r.xMax-c,r.yMin),new Vector2(r.xMin+c,r.yMin),new Vector2(r.xMin,r.yMin+c),new Vector2(r.xMin,r.yMax-c)};int first=v.currentVertCount;foreach(var point in p)v.AddVert(point,Fill,Vector2.zero);for(int i=1;i<7;i++)v.AddTriangle(first,first+i,first+i+1);for(int i=0;i<8;i++)Line(v,p[i],p[(i+1)%8],1.2f,Border);if(Accents){var accent=new Color(.73f,.45f,1,.94f);foreach(int i in new[]{1,3,5,7}){Line(v,p[i],p[(i+1)%8],2,accent);Line(v,p[i],Vector2.Lerp(p[i],p[(i+7)%8],.07f),1,accent);}}}
}
}
