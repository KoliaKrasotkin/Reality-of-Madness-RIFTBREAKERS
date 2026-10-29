Shader "ROM/Pilots/Violet Crystal Facets" {
 Properties {_BaseColor("Crystal color",Color)=(.24,.04,.48,1) [HDR]_EmissionColor("Violet energy",Color)=(.9,.14,2.4,1) [HDR]_RimColor("Edges",Color)=(.36,.1,.75,1) [Range(0,.15)]_PulseDepth("Pulse",Float)=.035 [Enum(Off,0,On,1)]_ZWrite("Depth write",Float)=0}
 SubShader {Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent"} Pass {Tags {"LightMode"="UniversalForward"} Blend SrcAlpha OneMinusSrcAlpha ZWrite [_ZWrite] Cull Back
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_fog
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 struct A{float4 positionOS:POSITION;float3 normalOS:NORMAL;};struct V{float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;half3 normalWS:TEXCOORD1;half fog:TEXCOORD2;};
 CBUFFER_START(UnityPerMaterial)
 half4 _BaseColor,_EmissionColor,_RimColor;half _PulseDepth;
 CBUFFER_END
 V vert(A i){V o;o.positionWS=TransformObjectToWorld(i.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.positionWS);o.normalWS=TransformObjectToWorldNormal(i.normalOS);o.fog=ComputeFogFactor(o.positionCS.z);return o;}
 half4 frag(V i):SV_Target{half3 n=normalize(i.normalWS);half3 view=normalize(GetWorldSpaceViewDir(i.positionWS));Light key=GetMainLight();half facet=saturate(dot(n,normalize(half3(.3,.7,.5))));half lit=.28h+.72h*saturate(dot(n,key.direction));half edge=pow(1-saturate(dot(n,view)),3);half pulse=1-_PulseDepth+_PulseDepth*sin(_Time.y*.7h);half3 color=_BaseColor.rgb*lit*key.color+_EmissionColor.rgb*(.3h+.7h*facet)*pulse+_RimColor.rgb*edge;return half4(MixFog(color,i.fog),_BaseColor.a);}
 ENDHLSL
 } }
}
