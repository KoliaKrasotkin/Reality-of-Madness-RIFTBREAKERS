Shader "ROM/Ciudad del Sol/Ceremonial Fire"
{
    Properties
    {
        _MainTex("Flame atlas", 2D) = "white" {}
        [HDR] _Tint("Emission", Color) = (1.8,.8,.2,1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
            half4 _Tint;
            CBUFFER_END
            struct Varyings { float4 position:SV_POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            Varyings Vert(float4 vertex:POSITION,float2 uv:TEXCOORD0,half4 color:COLOR)
            {
                Varyings o;
                o.position=TransformObjectToHClip(vertex.xyz);
                o.uv=uv;
                o.color=color;
                return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                half4 atlas=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv);
                half heat=max(atlas.r,max(atlas.g,atlas.b));
                half3 flame=lerp(_Tint.rgb*.68,_Tint.rgb+half3(.4,.37,.30),heat);
                return half4(flame,atlas.a*heat*i.color.a*.85);
            }
            ENDHLSL
        }
    }
}
