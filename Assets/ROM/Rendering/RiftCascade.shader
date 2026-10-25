Shader "ROM/Coastal Cascade"
{
 Properties { _BaseColor("Water tint",Color)=(.23,.38,.43,.72) _Mist("Impact spray",Float)=0 _FlowSpeed("Flow speed",Float)=1.2 }
 SubShader {
 Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
 Pass {
 Tags {"LightMode"="UniversalForward"}
 Blend SrcAlpha OneMinusSrcAlpha
 ZWrite Off
 Cull Off
 HLSLPROGRAM
 #pragma vertex Vert
 #pragma fragment Frag
 #pragma multi_compile_fog
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 CBUFFER_START(UnityPerMaterial)
 half4 _BaseColor; float _Mist; float _FlowSpeed;
 CBUFFER_END
 struct A {float4 p:POSITION;float3 n:NORMAL;float2 uv:TEXCOORD0;half4 color:COLOR;};
 struct V {float4 p:SV_POSITION;float3 w:TEXCOORD0;float3 n:TEXCOORD1;float2 uv:TEXCOORD2;half fog:TEXCOORD3;half4 color:COLOR;};
 V Vert(A a){V o; o.w=TransformObjectToWorld(a.p.xyz);o.p=TransformWorldToHClip(o.w);o.n=TransformObjectToWorldNormal(a.n);o.uv=a.uv;o.color=a.color;o.fog=ComputeFogFactor(o.p.z);return o;}
 half4 Frag(V i):SV_Target {
  if(_Mist>.5){float radius=length(i.uv-.5)*2;half alpha=pow(saturate(1-radius),2)*.24*i.color.a;return half4(MixFog(half3(.55,.67,.69),i.fog),alpha);}
  float flow=i.w.y*2+_Time.y*_FlowSpeed;
  float streak=.5+.5*sin(i.uv.x*137+sin(i.w.y*.8+_Time.y*2)*.45);
  float ripple=.6+.4*sin(flow*2.1+i.uv.x*7);
  float edgeWidth=.12+.025*sin(flow*1.4);
  half edge=smoothstep(0,edgeWidth,i.uv.x)*(1-smoothstep(1-edgeWidth,1,i.uv.x));
  Light light=GetMainLight();half illumination=.36+saturate(dot(normalize(i.n),light.direction))*.32;
  half3 water=_BaseColor.rgb*illumination+half3(.07,.1,.11)*pow(streak,9)*ripple;
  return half4(MixFog(water,i.fog),_BaseColor.a*edge*(.62+.18*streak));
 }
 ENDHLSL
 }
 }
}
