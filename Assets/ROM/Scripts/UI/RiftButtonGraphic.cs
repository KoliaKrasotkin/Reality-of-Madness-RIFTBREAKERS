using UnityEngine;
using UnityEngine.UI;

namespace ROM.UI.Sprint01
{
    // All ornament is mesh geometry: labels and hit targets stay independent.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RiftButtonGraphic : MaskableGraphic
    {
        public bool Ornament;
        public bool Active;
        float emphasis, targetEmphasis;
        bool pressed, disabled;
        public void SetVisualState(int state, bool instant)
        {
            disabled=state==4;pressed=state==2;
            targetEmphasis=disabled?0:(Active||state==1||state==2||state==3?1:0);
            if(instant)emphasis=targetEmphasis;
            SetVerticesDirty();
        }
        void Update()
        {
            float target=disabled?0:Mathf.Max(Active?1:0,targetEmphasis);
            if(Mathf.Approximately(emphasis,target))return;
            emphasis=Mathf.MoveTowards(emphasis,target,Time.unscaledDeltaTime*7);
            SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;
            float cut=Mathf.Min(21,r.height*.29f),fade=disabled?.32f:1;
            float e=pressed?emphasis*.7f:emphasis;
            var bronze=new Color(.49f,.40f,.34f,fade);
            var silver=new Color(.77f,.69f,.59f,.8f*fade);
            var ember=new Color(.9f,.69f,.36f,fade);
            var edge=Color.Lerp(bronze,ember,e);
            var points=Outline(r,cut,0);
            Fill(vh,points,new Color(.025f+.025f*e,.023f+.017f*e,.02f+.01f*e,.98f*fade),new Color(.06f+.045f*e,.05f+.025f*e,.035f+.01f*e,.98f*fade));
            // Broad translucent passes emulate the amber edge glow without a texture or bloom requirement.
            if(e>.01f)for(int j=4;j>=1;j--)Stroke(vh,points,j*2+1,new Color(.8f,.52f,.22f,.012f*e));
            Stroke(vh,points,1.25f,edge);
            var bevel=Outline(r,cut,3);Stroke(vh,bevel,.8f,Color.Lerp(new Color(.19f,.15f,.14f,fade),new Color(.76f,.56f,.29f,fade),e));
            var inner=Outline(r,cut,6);Stroke(vh,inner,.7f,new Color(.39f,.30f,.25f,.65f*fade));
            // A lit upper lip and dark lower lip give the frame its metal thickness.
            Line(vh,points[0]+Vector2.down*1.5f,points[1]+Vector2.down*1.5f,.7f,Color.Lerp(silver,new Color(1,.86f,.61f,fade),e));
            Line(vh,points[3]+Vector2.up*2,points[4]+Vector2.up*2,1.7f,new Color(0,0,0,.8f*fade));
            float cy=r.center.y,cx=r.center.x;
            float jewel=Mathf.Min(5,r.height*.08f);
            // Small crown and bottom notch, as on the reference's engraved double border.
            foreach(float sign in new[]{-1f,1f}){
                float y=sign>0?r.yMax-3:r.yMin+3;
                var c=new Vector2(cx,y);
                Line(vh,c+new Vector2(-21,0),c+new Vector2(-8,0),.8f,edge);
                Line(vh,c+new Vector2(8,0),c+new Vector2(21,0),.8f,edge);
                Diamond(vh,c,jewel,edge,.9f);
                Line(vh,new Vector2(r.xMax-cut-11,y),new Vector2(r.xMax-cut+3,y-sign*7),1,bronze);
            }
            var tip=new Vector2(r.xMax-10,cy);Diamond(vh,tip,Mathf.Min(6,r.height*.1f),edge,1);
            Line(vh,tip+Vector2.left*15,tip+Vector2.right*6,1,silver);
            // Fine diagonal engravings at the left corners remain readable on compact buttons.
            Line(vh,new Vector2(r.xMin+4,r.yMax-cut),new Vector2(r.xMin+cut,r.yMax-4),1,bronze);
            Line(vh,new Vector2(r.xMin+4,r.yMin+cut),new Vector2(r.xMin+cut,r.yMin+4),1,bronze);
            if(Ornament){
                var c=new Vector2(r.xMin+r.height*.47f,cy);float radius=r.height*.31f;
                Ring(vh,c,radius+4,1.2f,edge,32);Ring(vh,c,radius,.7f,silver,32);
                for(int j=0;j<4;j++){
                    float a=j*Mathf.PI*.5f;var dir=new Vector2(Mathf.Cos(a),Mathf.Sin(a));var side=new Vector2(-dir.y,dir.x);
                    Line(vh,c+dir*(radius-2)+side*4,c+dir*(radius+9),1,edge);
                    Line(vh,c+dir*(radius+9),c+dir*(radius-2)-side*4,1,edge);
                }
                Diamond(vh,c,radius*.72f,Color.Lerp(silver,ember,e),1.6f);
                Line(vh,c+Vector2.up*(radius*.95f),c-Vector2.up*(radius*.95f),2,Color.Lerp(silver,Color.white,e));
                Line(vh,c-Vector2.right*(radius*.64f),c+Vector2.right*(radius*.64f),.9f,edge);
                float x=r.xMin+r.height*.95f;
                Line(vh,new Vector2(x,r.yMin+13),new Vector2(x,cy-6),.7f,bronze);
                Line(vh,new Vector2(x,cy+6),new Vector2(x,r.yMax-13),.7f,bronze);
            }
        }
        static Vector2[] Outline(Rect r,float cut,float inset)
        {
            float l=r.xMin+inset,t=r.yMax-inset,b=r.yMin+inset,rr=r.xMax-inset;
            return new[]{new Vector2(l+cut*.55f,t),new Vector2(rr-cut,t),new Vector2(rr,r.center.y),new Vector2(rr-cut,b),new Vector2(l+cut*.55f,b),new Vector2(l,b+cut*.55f),new Vector2(l,t-cut*.55f)};
        }
        static void Fill(VertexHelper vh,Vector2[] p,Color edge,Color center)
        {
            Vector2 c=Vector2.zero;foreach(var v in p)c+=v;c/=p.Length;
            int k=vh.currentVertCount;vh.AddVert(c,center,Vector2.zero);foreach(var v in p)vh.AddVert(v,edge,Vector2.zero);
            for(int j=0;j<p.Length;j++)vh.AddTriangle(k,k+j+1,k+(j+1)%p.Length+1);
        }
        static void Stroke(VertexHelper vh,Vector2[] p,float width,Color tint){for(int j=0;j<p.Length;j++)Line(vh,p[j],p[(j+1)%p.Length],width,tint);}
        static void Ring(VertexHelper vh,Vector2 c,float radius,float width,Color tint,int steps){for(int j=0;j<steps;j++){float a=j*Mathf.PI*2/steps,b=(j+1)*Mathf.PI*2/steps;Line(vh,c+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,c+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,width,tint);}}
        static void Diamond(VertexHelper vh,Vector2 c,float d,Color tint,float width){Stroke(vh,new[]{c+Vector2.up*d,c+Vector2.right*d,c+Vector2.down*d,c+Vector2.left*d},width,tint);}
        static void Line(VertexHelper vh,Vector2 a,Vector2 b,float width,Color tint)
        {
            var n=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;int k=vh.currentVertCount;
            vh.AddVert(a-n,tint,Vector2.zero);vh.AddVert(a+n,tint,Vector2.zero);vh.AddVert(b+n,tint,Vector2.zero);vh.AddVert(b-n,tint,Vector2.zero);vh.AddTriangle(k,k+1,k+2);vh.AddTriangle(k,k+2,k+3);
        }
    }
}
