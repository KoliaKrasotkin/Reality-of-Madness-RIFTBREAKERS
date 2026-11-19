Shader "ROM/CiudadDelSol/NieblaBaja"
{
 Properties { _Color("Violeta gris",Color)=(.39,.28,.47,.24) }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-10" "RenderType"="Transparent" }
  Pass
  {
   Tags { "LightMode"="SRPDefaultUnlit" }
   Blend SrcAlpha OneMinusSrcAlpha
   Cull Off
   ZWrite Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   CBUFFER_START(UnityPerMaterial)
    half4 _Color;
   CBUFFER_END
   struct A{float4 p:POSITION;float2 uv:TEXCOORD0;};
   struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;float3 world:TEXCOORD1;};
   V vert(A a){V v;v.p=TransformObjectToHClip(a.p.xyz);v.world=TransformObjectToWorld(a.p.xyz);v.uv=a.uv;return v;}
   float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
   float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
   half4 frag(V v):SV_Target
   {
    float2 q=(v.uv-.5)*2;float edge=pow(saturate(1-dot(q,q)),1.5);
    float density=noise(v.world.xz*.48+float2(_Time.y*.025,_Time.y*.013));
    density*=.55+.45*noise(v.world.xz*1.3-_Time.y*.015);
    return half4(_Color.rgb,_Color.a*edge*smoothstep(.12,.65,density));
   }
   ENDHLSL
  }
 }
}
