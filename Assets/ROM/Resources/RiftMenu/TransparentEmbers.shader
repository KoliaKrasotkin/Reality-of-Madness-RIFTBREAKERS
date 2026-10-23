Shader "ROM/Menu/Transparent embers" {
 Properties { _BaseMap("Owned ember texture",2D)="white"{} }
 SubShader { Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+50"} Pass { Tags {"LightMode"="SRPDefaultUnlit"} Cull Off ZWrite Off ZTest LEqual Blend SrcAlpha One, One OneMinusSrcAlpha
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct Attributes {float4 positionOS:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};struct Varyings {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
 Varyings vert(Attributes i){Varyings o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.uv=i.uv;o.color=i.color;return o;}
 half4 frag(Varyings i):SV_Target {half3 rgb=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb;half alpha=max(rgb.r,max(rgb.g,rgb.b))*i.color.a;return half4(rgb*i.color.rgb*1.8,alpha);}
 ENDHLSL
 } }
}
