using System;
using UnityEngine;
using UnityEngine.UI;

namespace ROM.UI.Sprint01
{
    // Shared visual primitives. Screens compose these rather than inventing styles.
    public static class RiftWidgets
    {
        public static readonly Color Ink = new Color(.025f,.024f,.034f,.94f), Red = new Color(.87f,.08f,.22f), Gold = new Color(.58f,.45f,.31f), Muted = new Color(.68f,.66f,.68f), White = new Color(.94f,.91f,.87f);
        public static Font Body, Title;
        public static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(parent,false); r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1); r.anchoredPosition=new Vector2(x,-y); r.sizeDelta=new Vector2(w,h); return r;
        }
        public static Image Image(Transform p,string n,float x,float y,float w,float h,Color color,Sprite sprite=null)
        {
            var i=Rect(p,n,x,y,w,h).gameObject.AddComponent<Image>(); i.sprite=sprite; i.color=color; i.raycastTarget=false; return i;
        }
        public static Text Label(Transform p,string value,float x,float y,float w,float h,int size=22,Color? color=null,bool title=false)
        {
            var t=Rect(p,"Text",x,y,w,h).gameObject.AddComponent<Text>(); t.font=title?Title:Body; t.fontSize=size; t.color=color??White; t.text=value;
            t.alignment=TextAnchor.MiddleLeft; t.fontStyle=title?FontStyle.Bold:FontStyle.Normal; t.raycastTarget=false; t.supportRichText=false; t.verticalOverflow=VerticalWrapMode.Truncate; return t;
        }
        public static RectTransform ScrollArea(Transform parent,string name,float x,float y,float width,float height,float contentHeight,bool bottom=false)
        {
            var root=Rect(parent,name,x,y,width,height);var scroll=root.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;
            var viewport=Rect(root,"Viewport",0,0,width,height);viewport.gameObject.AddComponent<RectMask2D>();var surface=viewport.gameObject.AddComponent<Image>();surface.color=new Color(0,0,0,.01f);surface.raycastTarget=true;
            var content=Rect(viewport,"Content",0,0,width,Mathf.Max(height,contentHeight));scroll.viewport=viewport;scroll.content=content;scroll.verticalNormalizedPosition=bottom?0:1;return content;
        }
        public static RectTransform Panel(Transform p,string n,float x,float y,float w,float h)
        {
            var i=Image(p,n,x,y,w,h,Ink); var r=i.rectTransform;
            // Actual framed panel, with fine antique-gold edges and crimson corners.
            Image(r,"Top",0,0,w,1,Gold); Image(r,"Bottom",0,h-1,w,1,Gold); Image(r,"Left",0,0,1,h,Gold); Image(r,"Right",w-1,0,1,h,Gold);
            Image(r,"Accent",0,0,60,3,Red); Image(r,"Accent",w-60,h-3,60,3,Red); return r;
        }
        public static Button Button(Transform p,string value,float x,float y,float w,float h,Action action,bool active=false)
        {
            var r=Rect(p,value,x,y,w,h); var i=r.gameObject.AddComponent<RiftButtonGraphic>();i.Ornament=h>=60&&w>=280;i.raycastTarget=true; i.color=active?new Color(.28f,.025f,.06f,.95f):Ink;
            var b=r.gameObject.AddComponent<Button>(); b.targetGraphic=i;
            var colors=b.colors; colors.highlightedColor=new Color(1,.55f,.62f); colors.pressedColor=new Color(.6f,.2f,.3f); colors.selectedColor=new Color(1,.65f,.7f); b.colors=colors;
            float labelX=i.Ornament?70:22;
            Label(r,value,labelX,0,w-labelX-22,h,22,null,true); b.onClick.AddListener(()=>action?.Invoke()); return b;
        }
        public static InputField Input(Transform p,string placeholder,float x,float y,float w,Action<string> changed)
        {
            var r=Panel(p,"Input",x,y,w,48); r.GetComponent<Image>().raycastTarget=true;
            var field=r.gameObject.AddComponent<InputField>(); var text=Label(r,"",12,0,w-24,48,20); field.textComponent=text; field.targetGraphic=r.GetComponent<Image>();
            var hint=Label(r,placeholder,12,0,w-24,48,20,Muted); field.placeholder=hint; field.onValueChanged.AddListener(v=>changed(v)); return field;
        }
        public static Slider Slider(Transform p,string name,float x,float y,float w,float min,float max,float value,Action<float> changed)
        {
            var r=Rect(p,name,x,y,w,44); var s=r.gameObject.AddComponent<Slider>(); s.minValue=min;s.maxValue=max;
            var bg=Image(r,"Track",0,18,w,8,Gold); bg.raycastTarget=true;
            var handle=Image(r,"Handle",0,10,18,26,Red);handle.raycastTarget=true;
            s.targetGraphic=handle;s.handleRect=handle.rectTransform;s.value=value;s.onValueChanged.AddListener(v=>changed(v));return s;
        }
    }
}
