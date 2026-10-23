Shader "ROM/Menu/Living Rift" {
 Properties { _FlowTime("Unscaled flow time",Float)=0 }
 SubShader { Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent"} Pass { Tags {"LightMode"="SRPDefaultUnlit"} Cull Off ZWrite Off ZTest LEqual Blend SrcAlpha One, One OneMinusSrcAlpha
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct Attributes {float4 positionOS:POSITION;float2 uv:TEXCOORD0;};struct Varyings {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;};float _FlowTime;
 Varyings vert(Attributes i){Varyings o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.uv=i.uv;return o;}
 float hash21(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float noise21(float2 p){float2 q=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash21(q),hash21(q+float2(1,0)),f.x),lerp(hash21(q+float2(0,1)),hash21(q+1),f.x),f.y);}
 float4 frag(Varyings i):SV_Target {
  float2 p=(i.uv-.5)*2;float t=_FlowTime*.13;float r=length(p);
  float n=noise21(p*3.7+float2(t*.3,-t));
  float fine=noise21(p*10.5+float2(-t,t*.6));
  float warped=r+(n-.5)*.16+(fine-.5)*.035;
  float a=atan2(p.y,p.x);
  float width=lerp(.008,.045,noise21(p*5+float2(t,-t*.2)));
  float strand=exp(-abs(warped-.77)/width);
  float broken=smoothstep(.43,.73,noise21(p*4.6+float2(-t*.5,t*.7)));
  float directional=smoothstep(.08,.65,abs(p.x))*(1-smoothstep(.58,.94,p.y));
  float wisps=exp(-abs(warped-.64-(n-.5)*.12)/.026)*smoothstep(.62,.86,fine)*.28;
  float edge=1-smoothstep(.87,.99,r);
  float energy=(strand*broken*directional+wisps)*edge;
  float haze=saturate(1-r)*smoothstep(.52,.8,n)*.045;
  float intensity=.55+.45*noise21(float2(a*2+t,p.y*3-t*.4));
  float3 col=lerp(float3(.16,.035,.38),float3(.42,.25,.85),fine);
  return float4(col*(energy*intensity*1.45+haze),saturate(energy*.65+haze));
 }
 ENDHLSL
 } }
}
