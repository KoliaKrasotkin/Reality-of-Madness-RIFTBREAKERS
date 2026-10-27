Shader "ROM/Rift Crystal Halo" {
 Properties { [HDR]_Tint("Radiance",Color)=(2,.6,5,1) _Gain("Gain",Float)=.65 }
 SubShader { Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent"} Pass {
 Blend SrcAlpha One ZWrite Off Cull Off
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct A{float4 positionOS:POSITION;float2 uv:TEXCOORD0;};struct V{float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;};
 CBUFFER_START(UnityPerMaterial)
 half4 _Tint;float _Gain;
 CBUFFER_END
 V vert(A i){V o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.uv=i.uv;return o;}
 half4 frag(V i):SV_Target {float d=length(i.uv*2-1);float a=pow(saturate(1-d),2.7)*.55;float noise=frac(sin(dot(i.positionCS.xy,float2(12.9898,78.233)))*43758.5453)-.5;a=saturate(a+noise/255);return half4(_Tint.rgb*_Gain,a*_Tint.a);}
 ENDHLSL
 } }
}
