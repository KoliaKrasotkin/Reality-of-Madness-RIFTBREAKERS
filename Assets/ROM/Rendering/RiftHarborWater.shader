Shader "ROM/Harbor Water"
{
Properties { _Color("Deep water",Color)=(.03,.16,.19,.8) _ShallowColor("Shallow water",Color)=(.09,.27,.25,.42) _DepthDistance("Depth transition metres",Float)=4 _FlowSpeed("Wave speed",Float)=1 _WaveStrength("Wave strength",Float)=1 }
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
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
CBUFFER_START(UnityPerMaterial)
half4 _Color;half4 _ShallowColor;float _DepthDistance;float _FlowSpeed;float _WaveStrength;
CBUFFER_END
struct A {float4 p:POSITION;};
struct V {float4 p:SV_POSITION;float3 w:TEXCOORD0;half fog:TEXCOORD1;float4 screen:TEXCOORD2;float eye:TEXCOORD3;};
V Vert(A a){V o;o.w=TransformObjectToWorld(a.p.xyz);o.w.y+=sin(o.w.x*.4+_Time.y*.5*_FlowSpeed)*.025*_WaveStrength+cos(o.w.z*.32-_Time.y*.4*_FlowSpeed)*.025*_WaveStrength;o.p=TransformWorldToHClip(o.w);o.fog=ComputeFogFactor(o.p.z);o.screen=ComputeScreenPos(o.p);o.eye=-TransformWorldToView(o.w).z;return o;}
half4 Frag(V i):SV_Target {
half3 normal=normalize(half3(-cos(i.w.x*.4+_Time.y*.5*_FlowSpeed)*.08,1,sin(i.w.z*.32-_Time.y*.4*_FlowSpeed)*.06));
half3 view=GetWorldSpaceNormalizeViewDir(i.w);Light light=GetMainLight();
half fresnel=pow(1-saturate(dot(normal,view)),4);half spec=pow(saturate(dot(normal,normalize(view+light.direction))),96);
float sceneDepth=LinearEyeDepth(SampleSceneDepth(i.screen.xy/i.screen.w),_ZBufferParams);float depth=max(0,sceneDepth-i.eye);
half foam=(1-smoothstep(.02,.2,depth))*(.3+.15*sin(i.w.z*1.7+i.w.x*.9+_Time.y))*.15;
half depthBlend=1-exp(-depth/max(.1,_DepthDistance));
half4 tint=lerp(_ShallowColor,_Color,depthBlend);
half3 color=tint.rgb*(SampleSH(normal)+.35)+fresnel*half3(.12,.16,.24)+light.color*spec*.6+foam*half3(.32,.37,.38);
half shore=smoothstep(0,.12,depth);
return half4(MixFog(color,i.fog),tint.a*shore);
}
ENDHLSL
}}}
