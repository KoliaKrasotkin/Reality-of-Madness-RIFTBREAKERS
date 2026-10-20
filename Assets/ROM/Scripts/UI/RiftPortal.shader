Shader "ROM/Rift Portal"
{
    Properties { [HDR] _Tint("Dimensional energy", Color)=(.7,.06,2.8,1) _Intensity("Intensity",Float)=2 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend One One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };
            CBUFFER_START(UnityPerMaterial)
            float4 _Tint;
            float _Intensity;
            CBUFFER_END
            Varyings vert(Attributes input) { Varyings o; o.positionCS=TransformObjectToHClip(input.positionOS.xyz);o.uv=input.uv;return o; }
            half4 frag(Varyings input):SV_Target
            {
                float2 p=(input.uv-.5)*2;float r=length(p);float a=atan2(p.y,p.x);
                float edge=1-smoothstep(.88,1,r);
                float ring=pow(saturate(1-abs(r-.77)*18),2);
                float swirls=pow(saturate(sin(a*5+r*19-_Time.y*1.8)*.5+.5),5);
                float inner=(1-smoothstep(.1,.78,r))*.18;
                float energy=(ring*(.65+swirls)+swirls*.15+inner)*edge;
                return half4(_Tint.rgb*energy*_Intensity,1);
            }
            ENDHLSL
        }
    }
}
