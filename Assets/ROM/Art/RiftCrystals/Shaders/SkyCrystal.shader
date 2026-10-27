Shader "ROM/Rift Crystal Sky" {
 Properties { _BaseColor("Amethyst",Color)=(.42,.17,.72,.62) [HDR]_EmissionColor("Inner energy",Color)=(.055,.012,.12,1) [HDR]_RimColor("Facet rim",Color)=(.3,.1,.5,1) [Enum(Off,0,On,1)] _ZWrite("Depth write",Float)=0 }
 SubShader { Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent"} Pass {
 Tags {"LightMode"="UniversalForward"} Blend SrcAlpha OneMinusSrcAlpha ZWrite [_ZWrite] Cull Back
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 struct A {float4 positionOS:POSITION;float3 normalOS:NORMAL;};
 struct V {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;float3 normalWS:TEXCOORD1;};
 CBUFFER_START(UnityPerMaterial)
 half4 _BaseColor,_EmissionColor,_RimColor;
 CBUFFER_END
 V vert(A i){V o;o.positionWS=TransformObjectToWorld(i.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.positionWS);o.normalWS=TransformObjectToWorldNormal(i.normalOS);return o;}
 half4 frag(V i):SV_Target {half3 n=normalize(i.normalWS);half3 v=normalize(GetWorldSpaceViewDir(i.positionWS));Light sun=GetMainLight();half lit=.3h+.7h*saturate(dot(n,sun.direction));half rim=pow(1-saturate(dot(n,v)),2);half pulse=.96h+.04h*sin(_Time.y*.7h);half3 color=_BaseColor.rgb*lit+_EmissionColor.rgb*pulse+_RimColor.rgb*rim;return half4(color,_BaseColor.a);}
 // Deliberately no distance fog: this is the distant celestial landmark, not terrain.
 ENDHLSL
 } }
}
