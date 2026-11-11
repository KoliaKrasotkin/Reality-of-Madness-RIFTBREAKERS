Shader "ROM/Ciudad del Sol/Rift Horizon"
{
    Properties { _Panorama("ROM panorama", 2D) = "black" {} }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Background" }
        Pass
        {
            Cull Front
            ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_Panorama); SAMPLER(sampler_Panorama);
            struct Varyings { float4 position:SV_POSITION; float3 world:TEXCOORD0; };
            Varyings Vert(float4 vertex:POSITION)
            {
                Varyings o;
                o.world=TransformObjectToWorld(vertex.xyz);
                o.position=TransformWorldToHClip(o.world);
                // Keep the panorama behind the map, including on gameplay cameras with a shorter far plane.
                #if UNITY_REVERSED_Z
                    o.position.z=o.position.w*0.000002;
                #else
                    o.position.z=o.position.w*0.999998;
                #endif
                return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                float3 d=normalize(i.world-_WorldSpaceCameraPos);
                float2 uv=float2(frac(atan2(d.z,d.x)*.15915494+.57), asin(d.y)*.31830989+.5);
                half3 clouds=SAMPLE_TEXTURE2D(_Panorama,sampler_Panorama,uv).rgb;
                half horizon=exp(-abs(d.y+.13)*3.0);
                half3 sky=clouds*half3(1.12,.79,1.33) + half3(.14,.038,.24)*horizon + half3(.025,.009,.054);
                return half4(sky,1);
            }
            ENDHLSL
        }
    }
}

