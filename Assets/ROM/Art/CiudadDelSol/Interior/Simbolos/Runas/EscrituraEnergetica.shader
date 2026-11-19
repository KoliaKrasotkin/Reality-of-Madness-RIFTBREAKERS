Shader "ROM/CiudadDelSol/EscrituraEnergetica"
{
 Properties { [HDR] _Color("Color",Color)=(.55,.05,1,1) _Energy("Energia",Float)=7 }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
  Pass
  {
   Tags { "LightMode"="SRPDefaultUnlit" }
   Blend One OneMinusSrcAlpha
   ZWrite Off
   Cull Off
   Offset -1,-1
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   CBUFFER_START(UnityPerMaterial)
    half4 _Color;
    float _Energy;
   CBUFFER_END
   struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
   struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };
   Varyings vert(Attributes v) { Varyings o;o.positionCS=TransformObjectToHClip(v.positionOS.xyz);o.uv=v.uv;return o; }
   half4 frag(Varyings i):SV_Target
   {
    float d=abs(i.uv.x),aa=max(fwidth(d),.015);
    float core=1-smoothstep(.16-aa,.16+aa,d);
    float outline=(1-smoothstep(.27,.38,d))*(1-core);
    float halo=pow(saturate(1-d),3)*.22*(1-core);
    // Premultiplied HDR core, a fine dark boundary and a soft local halo.
    // Remains readable without Bloom and does not require any scene light per rune.
    float pulse=.98+.02*sin(_Time.y*.7);
    return half4(_Color.rgb*_Energy*pulse*(core+halo*.3),saturate(core+outline*.82+halo*.2));
   }
   ENDHLSL
  }
 }
}
