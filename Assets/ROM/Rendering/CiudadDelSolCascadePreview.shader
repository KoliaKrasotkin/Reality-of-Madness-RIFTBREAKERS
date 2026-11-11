Shader "ROM/Ciudad del Sol/Coastal Cascade Preview"
{
    Properties
    {
        _BaseColor("Water tint", Color) = (.48,.68,.76,.8)
        _FlowSpeed("Flow speed", Float) = .65
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _FlowSpeed;
            CBUFFER_END
            struct A { float4 pos:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; half4 color:COLOR; };
            struct V { float4 pos:SV_POSITION; float3 world:TEXCOORD0; float3 normal:TEXCOORD1;
                float2 uv:TEXCOORD2; half fog:TEXCOORD3; half4 color:COLOR; };
            V Vert(A a)
            {
                V o;
                o.world = TransformObjectToWorld(a.pos.xyz);
                o.pos = TransformWorldToHClip(o.world);
                o.normal = TransformObjectToWorldNormal(a.normal);
                o.uv = a.uv;
                o.color = a.color;
                o.fog = ComputeFogFactor(o.pos.z);
                return o;
            }
            float Hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            float Noise(float2 p)
            {
                float2 i=floor(p), f=frac(p);
                f=f*f*(3-2*f);
                return lerp(lerp(Hash(i),Hash(i+float2(1,0)),f.x),
                    lerp(Hash(i+float2(0,1)),Hash(i+1),f.x),f.y);
            }
            half4 Frag(V i):SV_Target
            {
                float u=i.uv.x, t=i.uv.y;
                float travel = -i.world.y*.17 - _Time.y*_FlowSpeed*(1+i.color.r*.22);
                float phase = i.color.r*19;
                float warp = Noise(float2(phase+u*5,travel*.44));
                float threads = Noise(float2(u*23 + phase + warp*.85,travel*.68));
                float foam = smoothstep(.36,.85,Noise(float2(u*7+phase,travel*1.1))*.7+threads*.44);
                float fringe = .07 + warp*.13;
                float edge = smoothstep(0,fringe,u)*smoothstep(0,fringe,1-u);
                float dissolving = smoothstep(.60+i.color.r*.12,.995,t);
                float breakup = smoothstep(dissolving*.79,dissolving*.79+.19,threads);
                float fade = 1-smoothstep(.73+i.color.r*.08,1,t);
                float air = lerp(1,breakup,dissolving)*fade;
                half diffuse = .62 + .15 * abs(dot(normalize(i.normal),GetMainLight().direction));
                half3 body = _BaseColor.rgb * diffuse;
                half3 whitewater = half3(.81,.91,.98);
                half3 color = lerp(body,whitewater,saturate(foam*.87+threads*.18+i.color.g*.24));
                return half4(MixFog(color,i.fog),_BaseColor.a * edge * air * (.36+.63*foam) * i.color.a);
            }
            ENDHLSL
        }
    }
}
