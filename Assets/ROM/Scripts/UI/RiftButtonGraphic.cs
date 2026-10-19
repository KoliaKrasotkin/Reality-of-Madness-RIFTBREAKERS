using UnityEngine;
using UnityEngine.UI;

namespace ROM.UI.Sprint01
{
    // Resolution-independent ornamental frame; all text remains a separate control.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RiftButtonGraphic : MaskableGraphic
    {
        public bool Ornament;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;float cut=Mathf.Min(18,r.height*.22f);
            var points=new[]{new Vector2(r.xMin+cut,r.yMax),new Vector2(r.xMax-cut,r.yMax),new Vector2(r.xMax,r.center.y),new Vector2(r.xMax-cut,r.yMin),new Vector2(r.xMin+cut,r.yMin),new Vector2(r.xMin,r.yMin+cut),new Vector2(r.xMin,r.yMax-cut)};
            vh.AddVert(r.center,color,Vector2.zero);foreach(var p in points)vh.AddVert(p,color,Vector2.zero);
            for(int i=0;i<points.Length;i++)vh.AddTriangle(0,i+1,(i+1)%points.Length+1);
            for(int i=0;i<points.Length;i++)Line(vh,points[i],points[(i+1)%points.Length],1.2f,RiftWidgets.Gold);
            Line(vh,new Vector2(r.xMin+cut,r.yMax-5),new Vector2(r.xMax-cut,r.yMax-5),1,new Color(.4f,.12f,.15f,.8f));
            Line(vh,new Vector2(r.xMin+cut,r.yMin+5),new Vector2(r.xMax-cut,r.yMin+5),1,new Color(.4f,.12f,.15f,.8f));
            if(Ornament){
                var c=new Vector2(r.xMin+37,r.center.y);float d=14;
                var diamond=new[]{c+Vector2.up*d,c+Vector2.right*d,c+Vector2.down*d,c+Vector2.left*d};
                for(int i=0;i<4;i++)Line(vh,diamond[i],diamond[(i+1)%4],1.4f,RiftWidgets.Red);
                Line(vh,c+Vector2.up*20,c+Vector2.down*20,1.2f,RiftWidgets.Gold);Line(vh,c+Vector2.left*20,c+Vector2.right*20,1.2f,RiftWidgets.Gold);
            }
        }
        static void Line(VertexHelper vh,Vector2 a,Vector2 b,float width,Color tint)
        {
            var n=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;int k=vh.currentVertCount;
            vh.AddVert(a-n,tint,Vector2.zero);vh.AddVert(a+n,tint,Vector2.zero);vh.AddVert(b+n,tint,Vector2.zero);vh.AddVert(b-n,tint,Vector2.zero);vh.AddTriangle(k,k+1,k+2);vh.AddTriangle(k,k+2,k+3);
        }
    }
}
