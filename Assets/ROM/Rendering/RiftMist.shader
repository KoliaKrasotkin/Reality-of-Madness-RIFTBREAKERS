Shader "ROM/Drifting Mist"
{
    Properties { _Color("Mist tint", Color) = (.14,.17,.23,.16) _Softness("Intersection softness", Float) = 5 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _Color; float _Softness;
            CBUFFER_END
            struct Input { float4 positionOS:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; float4 screen:TEXCOORD1; float eye:TEXCOORD2; };
            float Hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            float Noise(float2 p) { float2 a=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(Hash(a),Hash(a+float2(1,0)),f.x),lerp(Hash(a+float2(0,1)),Hash(a+1),f.x),f.y); }
            Varyings Vert(Input v) { Varyings o;float3 w=TransformObjectToWorld(v.positionOS.xyz);o.positionCS=TransformWorldToHClip(w);o.screen=ComputeScreenPos(o.positionCS);o.eye=-TransformWorldToView(w).z;o.uv=v.uv;o.color=v.color;return o; }
            half4 Frag(Varyings i):SV_Target
            {
                float2 p=i.uv*2-1;float edge=saturate(1-dot(p,p));
                float2 drift=float2(_Time.y*.009,_Time.y*.003);
                float cloud=Noise(i.uv*4+drift)*.65+Noise(i.uv*9-drift)*.35;
                float depth=LinearEyeDepth(SampleSceneDepth(i.screen.xy/i.screen.w),_ZBufferParams);
                float fade=saturate((depth-i.eye)/max(_Softness,.01));
                float nearFade=saturate((i.eye-12)/35);
                return half4(_Color.rgb*i.color.rgb,_Color.a*i.color.a*edge*edge*cloud*fade*nearFade);
            }
            ENDHLSL
        }
    }
}
