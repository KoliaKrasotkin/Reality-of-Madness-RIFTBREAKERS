Shader "ROM/Fractured Heart" {
Properties {_BaseColor("Energy",Color)=(.3,.02,1,1)}
SubShader {Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"} Pass {HLSLPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
struct A{float4 positionOS:POSITION;float3 normalOS:NORMAL;};struct V{float4 pos:SV_POSITION;float3 normal:TEXCOORD0;float3 world:TEXCOORD1;};
CBUFFER_START(UnityPerMaterial)
float4 _BaseColor;
CBUFFER_END
V vert(A i){V o;o.pos=TransformObjectToHClip(i.positionOS.xyz);o.world=TransformObjectToWorld(i.positionOS.xyz);o.normal=TransformObjectToWorldNormal(i.normalOS);return o;}
half4 frag(V i):SV_Target {float rim=pow(1-saturate(abs(dot(normalize(i.normal),normalize(_WorldSpaceCameraPos-i.world)))),2);float pulse=.9+.1*sin(_Time.y*.35+i.world.y*.08);return half4(_BaseColor.rgb*(1+rim*3)*pulse+float3(.05,.22,.4)*rim,1);}
ENDHLSL}}}
