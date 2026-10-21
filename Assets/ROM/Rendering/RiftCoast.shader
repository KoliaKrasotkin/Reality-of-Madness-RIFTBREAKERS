Shader "ROM/Coastal Ground"
{
Properties { _BaseMap("Soil",2D)="white"{} _RockMap("Rock",2D)="white"{} _SandMap("Sand",2D)="white"{} _BaseColor("Tint",Color)=(.8,.8,.8,1) _WorldScale("Scale",Float)=.12 }
SubShader {
Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
Pass {
Tags {"LightMode"="UniversalForward"}
HLSLPROGRAM
#pragma vertex Vert
#pragma fragment Frag
#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
#pragma multi_compile_fragment _ _SHADOWS_SOFT
#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
#pragma multi_compile_fog
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);TEXTURE2D(_RockMap);SAMPLER(sampler_RockMap);TEXTURE2D(_SandMap);SAMPLER(sampler_SandMap);
CBUFFER_START(UnityPerMaterial)
half4 _BaseColor;float4 _BaseMap_ST;float _WorldScale;
CBUFFER_END
struct A {float4 p:POSITION;float3 n:NORMAL;};
struct V {float4 p:SV_POSITION;float3 w:TEXCOORD0;half3 n:TEXCOORD1;half fog:TEXCOORD2;};
V Vert(A a){V o;o.w=TransformObjectToWorld(a.p.xyz);o.p=TransformWorldToHClip(o.w);o.n=TransformObjectToWorldNormal(a.n);o.fog=ComputeFogFactor(o.p.z);return o;}
half4 Frag(V i):SV_Target {
half3 n=NormalizeNormalPerPixel(i.n);
half sand=smoothstep(84,112,i.w.x)*(1-smoothstep(48,78,abs(i.w.z-450)));
half wet=1-smoothstep(1.6,3.4,i.w.y);
half3 soil=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.w.xz*_WorldScale).rgb*_BaseColor.rgb;
half3 beach=SAMPLE_TEXTURE2D(_SandMap,sampler_SandMap,i.w.xz*.22).rgb*lerp(half3(1,.88,.65),half3(.44,.44,.36),wet);
half3 rock=SAMPLE_TEXTURE2D(_RockMap,sampler_RockMap,i.w.zy*.12).rgb*_BaseColor.rgb;
SurfaceData s=(SurfaceData)0;s.albedo=lerp(lerp(soil,beach,sand),rock,1-smoothstep(.4,.8,n.y));s.alpha=1;s.occlusion=1;s.smoothness=lerp(.08,.3,wet*sand);s.normalTS=half3(0,0,1);
InputData d=(InputData)0;d.positionWS=i.w;d.normalWS=n;d.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.w);d.shadowCoord=TransformWorldToShadowCoord(i.w);d.bakedGI=SampleSH(n);d.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.p);d.shadowMask=half4(1,1,1,1);
half4 c=UniversalFragmentPBR(d,s);c.rgb=MixFog(c.rgb,i.fog);return c;
}
ENDHLSL
}
UsePass "Universal Render Pipeline/Lit/ShadowCaster"
UsePass "Universal Render Pipeline/Lit/DepthOnly"
UsePass "Universal Render Pipeline/Lit/DepthNormals"
}}
