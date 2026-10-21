Shader "ROM/World Terrain"
{
Properties {
_BaseMap("Soil",2D)="white"{}
_RockMap("Rock",2D)="white"{}
_BaseColor("Tint",Color)=(.8,.8,.8,1)
_WorldScale("World texture scale",Float)=.12
_Cutoff("Cutoff",Range(0,1))=.5
}
SubShader {
Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
Pass {
Name "ForwardLit"
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
TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
TEXTURE2D(_RockMap);SAMPLER(sampler_RockMap);
CBUFFER_START(UnityPerMaterial)
float4 _BaseColor;float4 _BaseMap_ST;float _WorldScale;float _Cutoff;
CBUFFER_END
struct Attributes {float4 positionOS:POSITION;float3 normalOS:NORMAL;};
struct Varyings {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;half3 normalWS:TEXCOORD1;half fog:TEXCOORD2;};
Varyings Vert(Attributes v){Varyings o;VertexPositionInputs p=GetVertexPositionInputs(v.positionOS.xyz);o.positionCS=p.positionCS;o.positionWS=p.positionWS;o.normalWS=TransformObjectToWorldNormal(v.normalOS);o.fog=ComputeFogFactor(p.positionCS.z);return o;}
half4 Frag(Varyings i):SV_Target {
half3 n=NormalizeNormalPerPixel(i.normalWS);half3 w=pow(abs(n),4);w/=max(w.x+w.y+w.z,.001);
float3 p=i.positionWS*_WorldScale;
half3 soil=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,p.xz).rgb;
half3 rock=SAMPLE_TEXTURE2D(_RockMap,sampler_RockMap,p.zy).rgb*w.x+SAMPLE_TEXTURE2D(_RockMap,sampler_RockMap,p.xz).rgb*w.y+SAMPLE_TEXTURE2D(_RockMap,sampler_RockMap,p.xy).rgb*w.z;
half slope=1-smoothstep(.45,.85,n.y);
SurfaceData surface=(SurfaceData)0;surface.albedo=lerp(soil,rock,slope)*_BaseColor.rgb;surface.alpha=1;surface.smoothness=.08;surface.occlusion=1;surface.normalTS=half3(0,0,1);
InputData input=(InputData)0;input.positionWS=i.positionWS;input.normalWS=n;input.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.positionWS);input.shadowCoord=TransformWorldToShadowCoord(i.positionWS);input.bakedGI=SampleSH(n);input.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);input.shadowMask=half4(1,1,1,1);
half4 color=UniversalFragmentPBR(input,surface);color.rgb=MixFog(color.rgb,i.fog);return color;
}
ENDHLSL
}
UsePass "Universal Render Pipeline/Lit/ShadowCaster"
UsePass "Universal Render Pipeline/Lit/DepthOnly"
UsePass "Universal Render Pipeline/Lit/DepthNormals"
}
}
