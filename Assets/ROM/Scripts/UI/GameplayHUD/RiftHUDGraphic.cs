using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace ROM.UI.Gameplay
{
    // Small native UI meshes: only the four painted ability icons and portrait are raster assets.
    public enum HUDShape { Panel,Slot,Bar,Hex,Portrait,Diamond,Disc,Ring,Line,Ornament,Crystal,Arrow,Helmet,Bag,Skill,Gear,Lock,Target }
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RiftHUDGraphic:MaskableGraphic
    {
        public HUDShape Shape;
        public Color Border=new Color(.7f,.5f,.25f),Bottom=new Color(.025f,.03f,.045f,.65f);
        public float Thickness=1,Fill=1;
        public bool OutlineOnly;
        Vector2 P(float x,float y){var r=rectTransform.rect;return new Vector2(r.xMin+x*r.width,r.yMin+y*r.height);}
        void Polygon(VertexHelper vh,IList<Vector2> points,Color top,Color bottom)
        {
            if(points.Count<3)return;int start=vh.currentVertCount;var rect=rectTransform.rect;
            for(int i=0;i<points.Count;i++){float y=Mathf.InverseLerp(rect.yMin,rect.yMax,points[i].y);vh.AddVert(points[i],Color.Lerp(bottom,top,y),Vector2.zero);}
            for(int i=1;i<points.Count-1;i++)vh.AddTriangle(start,start+i,start+i+1);
        }
        void Stroke(VertexHelper vh,Vector2 a,Vector2 b,float width,Color c)
        {
            var delta=b-a;if(delta.sqrMagnitude<.00001f)return;Vector2 n=new Vector2(-delta.y,delta.x).normalized*width*.5f;
            Polygon(vh,new[]{a+n,b+n,b-n,a-n},c,c);
        }
        void Loop(VertexHelper vh,IList<Vector2> points,float width,Color c){for(int i=0;i<points.Count;i++)Stroke(vh,points[i],points[(i+1)%points.Count],width,c);}
        List<Vector2> Circle(float radius=1,int count=72){var list=new List<Vector2>(count);for(int i=0;i<count;i++){float a=i*Mathf.PI*2/count;list.Add(P(.5f+Mathf.Cos(a)*radius*.49f,.5f+Mathf.Sin(a)*radius*.49f));}return list;}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;float w=r.width,h=r.height;if(w<=0||h<=0)return;
            var gold=Border*color;var ink=Bottom*color;
            if(Shape==HUDShape.Line){Stroke(vh,P(0,.5f),P(1,.5f),Thickness,color);return;}
            if(Shape==HUDShape.Disc){Polygon(vh,Circle(),color,color);return;}
            if(Shape==HUDShape.Ring){Loop(vh,Circle(),Thickness,color);return;}
            if(Shape==HUDShape.Crystal){
                Polygon(vh,new[]{P(.5f,1),P(.08f,.48f),P(.5f,0),P(.9f,.48f)},new Color(.66f,.26f,1,.3f),new Color(.24f,.03f,.65f,.1f));
                Polygon(vh,new[]{P(.49f,.88f),P(.25f,.47f),P(.49f,.46f)},new Color(.9f,.8f,1),new Color(.56f,.15f,1));
                Polygon(vh,new[]{P(.51f,.88f),P(.51f,.46f),P(.74f,.47f)},new Color(.76f,.3f,1),new Color(.26f,.03f,.6f));
                Polygon(vh,new[]{P(.25f,.44f),P(.49f,.08f),P(.49f,.44f)},new Color(.65f,.3f,1),new Color(.3f,.03f,.85f));
                Polygon(vh,new[]{P(.51f,.44f),P(.51f,.08f),P(.74f,.44f)},Color.white,new Color(.5f,.1f,1));return;
            }
            if(Shape==HUDShape.Arrow){Polygon(vh,new[]{P(.5f,.96f),P(.1f,.03f),P(.5f,.3f),P(.9f,.03f)},color,color*.7f);return;}
            if(Shape==HUDShape.Ornament){Stroke(vh,P(0,.5f),P(.44f,.5f),Thickness,color);Stroke(vh,P(.56f,.5f),P(1,.5f),Thickness,color);Loop(vh,new[]{P(.5f,.96f),P(.56f,.5f),P(.5f,.04f),P(.44f,.5f)},Thickness,color);Stroke(vh,P(.5f,.22f),P(.5f,.78f),Thickness*.65f,color);return;}
            if(Shape==HUDShape.Target){Loop(vh,new[]{P(.5f,.96f),P(.84f,.5f),P(.5f,.04f),P(.16f,.5f)},Thickness,color);return;}
            if(Shape==HUDShape.Gear||Shape==HUDShape.Skill){
                int count=Shape==HUDShape.Gear?10:6;
                for(int i=0;i<count;i++){float a=i*Mathf.PI*2/count;Vector2 d=new Vector2(Mathf.Sin(a),Mathf.Cos(a));Stroke(vh,P(.5f+d.x*.19f,.5f+d.y*.19f),P(.5f+d.x*.44f,.5f+d.y*.44f),Shape==HUDShape.Gear?w*.12f:Thickness*1.6f,color);if(Shape==HUDShape.Skill){Vector2 t=new Vector2(d.y,-d.x);Stroke(vh,P(.5f+d.x*.3f,.5f+d.y*.3f),P(.5f+d.x*.15f+t.x*.12f,.5f+d.y*.15f+t.y*.12f),Thickness,color);}}
                Loop(vh,Circle(Shape==HUDShape.Gear?.55f:.34f,36),Shape==HUDShape.Gear?w*.12f:Thickness,color);return;
            }
            if(Shape==HUDShape.Helmet){Polygon(vh,new[]{P(.5f,.95f),P(.19f,.78f),P(.13f,.24f),P(.37f,.05f),P(.4f,.47f),P(.24f,.62f),P(.44f,.57f),P(.5f,.12f),P(.56f,.57f),P(.76f,.62f),P(.6f,.47f),P(.63f,.05f),P(.87f,.24f),P(.81f,.78f)},color,color);return;}
            if(Shape==HUDShape.Bag){Polygon(vh,new[]{P(.29f,.89f),P(.71f,.89f),P(.62f,.73f),P(.86f,.21f),P(.71f,.08f),P(.28f,.08f),P(.13f,.21f),P(.37f,.73f)},color,color);Stroke(vh,P(.28f,.7f),P(.72f,.7f),Thickness*2,ink.a>0?ink:new Color(.1f,.06f,.02f));return;}
            if(Shape==HUDShape.Lock){
                Polygon(vh,new[]{P(.23f,.08f),P(.77f,.08f),P(.77f,.58f),P(.23f,.58f)},color,color);var arc=new List<Vector2>();for(int i=0;i<=20;i++){float a=i*Mathf.PI/20;arc.Add(P(.5f+Mathf.Cos(a)*.22f,.58f+Mathf.Sin(a)*.29f));}for(int i=1;i<arc.Count;i++)Stroke(vh,arc[i-1],arc[i],w*.11f,color);Stroke(vh,P(.5f,.2f),P(.5f,.42f),w*.1f,new Color(.025f,.025f,.035f));return;
            }
            List<Vector2> points;
            if(Shape==HUDShape.Hex)points=new List<Vector2>{P(.5f,1),P(.96f,.75f),P(.96f,.25f),P(.5f,0),P(.04f,.25f),P(.04f,.75f)};
            else if(Shape==HUDShape.Portrait)points=new List<Vector2>{P(.5f,1),P(.96f,.75f),P(.96f,.25f),P(.5f,0),P(.04f,.25f),P(.04f,.75f)};
            else if(Shape==HUDShape.Diamond)points=new List<Vector2>{P(.5f,1),P(1,.5f),P(.5f,0),P(0,.5f)};
            else if(Shape==HUDShape.Bar)points=new List<Vector2>{P(0,.5f),P(.035f,1),P(.965f,1),P(1,.5f),P(.965f,0),P(.035f,0)};
            else points=new List<Vector2>{P(0,.94f),P(.06f,1),P(.94f,1),P(1,.94f),P(1,.06f),P(.94f,0),P(.06f,0),P(0,.06f)};
            if(Shape==HUDShape.Bar&&Fill<.999f){
                float edge=r.xMin+w*Mathf.Clamp01(Fill);var clipped=new List<Vector2>();for(int i=0;i<points.Count;i++){var a=points[i];var b=points[(i+1)%points.Count];bool inside=a.x<=edge,next=b.x<=edge;if(inside)clipped.Add(a);if(inside!=next)clipped.Add(Vector2.Lerp(a,b,(edge-a.x)/(b.x-a.x)));}if(Fill>0)Polygon(vh,clipped,color,Bottom);return;
            }
            if(!OutlineOnly)Polygon(vh,points,Shape==HUDShape.Bar&&Thickness==0?color:ink,Bottom);
            if(Thickness>0){Loop(vh,points,Thickness,gold);if(Shape==HUDShape.Hex||Shape==HUDShape.Portrait){var inset=points.SelectInset(P(.5f,.5f),.92f);Loop(vh,inset,Thickness*.55f,gold*.7f);}}
        }
    }
    static class HUDPoints {public static List<Vector2> SelectInset(this List<Vector2> points,Vector2 center,float scale){var result=new List<Vector2>(points.Count);foreach(var p in points)result.Add(center+(p-center)*scale);return result;}}
}
