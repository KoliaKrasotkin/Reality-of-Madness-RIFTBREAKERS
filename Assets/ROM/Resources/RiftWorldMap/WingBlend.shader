Shader "ROM/UI/Map ocean wing blend" {
 Properties { [PerRendererData] _MainTex("Decorative side art",2D)="white"{} _OriginalTex("Calibrated edge reference",2D)="white"{} _SquareWidth("Calibrated square fraction",Float)=.666667 _Foreground("Calibrated foreground",Float)=0 _StencilComp("Stencil Comparison",Float)=8 _Stencil("Stencil ID",Float)=0 _StencilOp("Stencil Operation",Float)=0 _StencilWriteMask("Stencil Write Mask",Float)=255 _StencilReadMask("Stencil Read Mask",Float)=255 _ColorMask("Color Mask",Float)=15 }
 SubShader { Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="False"} Stencil {Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask]} Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode] Blend SrcAlpha OneMinusSrcAlpha ColorMask [_ColorMask]
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
 #include "UnityCG.cginc"
 #include "UnityUI.cginc"
 struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
 struct v2f {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;float4 local:TEXCOORD1;};
 sampler2D _MainTex;sampler2D _OriginalTex;float _SquareWidth;float _Foreground;float4 _ClipRect;
 v2f vert(appdata v){v2f o;o.local=v.vertex;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
 fixed4 frag(v2f i):SV_Target {
  fixed4 art;
  if(_Foreground>.5){
   art=tex2D(_OriginalTex,i.uv);
   // Only the non-traversable outer ocean margin feathers; source pixels and mapping are unchanged.
   art.a*=smoothstep(0,.035,min(i.uv.x,1-i.uv.x));
  }else{
   float square=clamp(_SquareWidth,.01,1);float left=(1-square)*.5;float right=1-left;
   float x=(i.uv.x-left)/square;
   if(x>=0&&x<=1){
    fixed4 original=tex2D(_OriginalTex,float2(x,i.uv.y));
    fixed4 sea=tex2D(_MainTex,float2(x<.5?1.0/6:5.0/6,i.uv.y));
    art=lerp(sea,original,smoothstep(0,.035,min(x,1-x)));
   }else{
    // Strictly the generated side wings, never the reinterpreted center.
    float u=x<0?saturate(i.uv.x/max(left,.0001))/6:5.0/6+saturate((i.uv.x-right)/max(left,.0001))/6;
    art=tex2D(_MainTex,float2(u,i.uv.y));
   }
  }
  art*=i.color;
 #ifdef UNITY_UI_CLIP_RECT
  art.a*=UnityGet2DClipping(i.local.xy,_ClipRect);
 #endif
  return art;
 }
 ENDCG }
 }
}
