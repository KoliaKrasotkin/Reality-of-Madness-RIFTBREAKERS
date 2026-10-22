Shader "ROM/Quest Highlight" {
 Properties { _GlowColor("Quest glow",Color)=(0.95,0.7,0.2,1) _Opacity("Opacity",Range(0,1))=0.65 }
 SubShader {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+10" "RenderType"="Transparent" }
  Pass {
   Tags { "LightMode"="SRPDefaultUnlit" }
   Blend SrcAlpha One
   ZWrite Off
   ZTest LEqual
   Cull Back
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   struct Attributes {float4 positionOS:POSITION;float3 normalOS:NORMAL;};
   struct Varyings {float4 positionCS:SV_POSITION;float3 normalWS:TEXCOORD0;float3 positionWS:TEXCOORD1;};
   CBUFFER_START(UnityPerMaterial)
   half4 _GlowColor;half _Opacity;
   CBUFFER_END
   Varyings vert(Attributes i){Varyings o;o.positionWS=TransformObjectToWorld(i.positionOS.xyz+i.normalOS*.012);o.normalWS=TransformObjectToWorldNormal(i.normalOS);o.positionCS=TransformWorldToHClip(o.positionWS);return o;}
   half4 frag(Varyings i):SV_Target {half rim=pow(saturate(1-dot(normalize(i.normalWS),normalize(GetWorldSpaceViewDir(i.positionWS)))),2);return half4(_GlowColor.rgb,(rim*.8+.08)*_Opacity);}
   ENDHLSL
  }
 }
}
