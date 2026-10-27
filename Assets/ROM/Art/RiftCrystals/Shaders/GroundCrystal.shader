Shader "ROM/Rift Crystal Ground Radiance" {
 Properties { _BaseColor("Crystal color and transparency",Color)=(.44,.18,.72,.63) [HDR]_EmissionColor("Core energy",Color)=(3,.8,6,1) [HDR]_RimColor("Facet edge",Color)=(.6,.2,1,1) [Range(0,0.15)]_PulseDepth("Pulse depth",Float)=.035 [Enum(Off,0,On,1)]_ZWrite("Depth write",Float)=0 }
 SubShader { Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent"} Pass {
 Tags {"LightMode"="UniversalForward"} Blend SrcAlpha OneMinusSrcAlpha ZWrite [_ZWrite] Cull Back
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_fog
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 struct A {float4 positionOS:POSITION;float3 normalOS:NORMAL;};
 struct V {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;half3 normalWS:TEXCOORD1;half fog:TEXCOORD2;};
 CBUFFER_START(UnityPerMaterial)
 half4 _BaseColor,_EmissionColor,_RimColor;half _PulseDepth;
 CBUFFER_END
 V vert(A i){V o;o.positionWS=TransformObjectToWorld(i.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.positionWS);o.normalWS=TransformObjectToWorldNormal(i.normalOS);o.fog=ComputeFogFactor(o.positionCS.z);return o;}
 half4 frag(V i):SV_Target {half3 n=normalize(i.normalWS);half3 v=normalize(GetWorldSpaceViewDir(i.positionWS));Light sun=GetMainLight();half lit=.3h+.7h*saturate(dot(n,sun.direction));half rim=pow(1-saturate(dot(n,v)),2);half pulse=1-_PulseDepth+_PulseDepth*sin(_Time.y*.7h);half3 color=_BaseColor.rgb*lit+_EmissionColor.rgb*pulse+_RimColor.rgb*rim;return half4(MixFog(color,i.fog),_BaseColor.a);}
 // Local checkpoint surface: emission is explicit, independent of stripped Lit keywords.
 // Fog remains enabled for ground objects; real spill is provided by the editable scene light.
 ENDHLSL
 } }
}
