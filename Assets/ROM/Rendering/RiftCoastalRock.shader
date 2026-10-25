Shader "ROM/Coastal Rock Triplanar"
{
 Properties { _BaseMap("Existing rock texture",2D)="white"{} _BaseColor("Tint",Color)=(.52,.56,.59,1) _WorldScale("Texture repeats per metre",Float)=.19 }
 SubShader {
 Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
 Pass {
 Tags {"LightMode"="UniversalForward"}
 HLSLPROGRAM
 #pragma vertex Vert
 #pragma fragment Frag
 #pragma multi_compile_fog
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
 CBUFFER_START(UnityPerMaterial)
 half4 _BaseColor;float _WorldScale;
 CBUFFER_END
 struct A{float4 p:POSITION;float3 n:NORMAL;};
 struct V{float4 p:SV_POSITION;float3 w:TEXCOORD0;float3 n:TEXCOORD1;half fog:TEXCOORD2;};
 V Vert(A a){V o;o.w=TransformObjectToWorld(a.p.xyz);o.p=TransformWorldToHClip(o.w);o.n=TransformObjectToWorldNormal(a.n);o.fog=ComputeFogFactor(o.p.z);return o;}
 half4 Frag(V i):SV_Target {half3 n=normalize(i.n);half3 weight=pow(abs(n),4);weight/=max(dot(weight,1),.001);float3 p=i.w*_WorldScale;
 half3 tex=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,p.zy).rgb*weight.x+SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,p.xz).rgb*weight.y+SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,p.xy).rgb*weight.z;
 Light light=GetMainLight();half3 lit=SampleSH(n)+light.color*saturate(dot(n,light.direction));half wet=lerp(.7,1,smoothstep(-1,2,i.w.y));return half4(MixFog(tex*_BaseColor.rgb*lit*wet,i.fog),1);}
 ENDHLSL
 }
 UsePass "Universal Render Pipeline/Lit/ShadowCaster"
 UsePass "Universal Render Pipeline/Lit/DepthOnly"
 }
}
