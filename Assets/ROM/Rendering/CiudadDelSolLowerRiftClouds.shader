Shader "ROM/Ciudad del Sol/Lower Rift Clouds"
{
    Properties { _CloudColor("Cloud color", Color) = (.5,.22,.8,1) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-30" "RenderType"="Transparent" }
        Pass
        {
            Cull Front
            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _CloudColor;
            CBUFFER_END
            struct Varyings { float4 position:SV_POSITION; float3 world:TEXCOORD0; };
            Varyings Vert(float4 vertex:POSITION)
            {
                Varyings o;
                o.world=TransformObjectToWorld(vertex.xyz);
                o.position=TransformWorldToHClip(o.world);
                return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                float3 eye=_WorldSpaceCameraPos;
                float3 ray=normalize(i.world-eye);
                float3 origin=TransformWorldToObject(eye);
                float3 dir=mul((float3x3)GetWorldToObjectMatrix(),ray);
                float3 signDir=step(0,dir)*2-1;
                float3 inv=signDir/max(abs(dir),1e-6);
                float3 a=(-.5-origin)*inv, b=(.5-origin)*inv;
                float3 enter=min(a,b), leave=max(a,b);
                float start=max(0,max(enter.x,max(enter.y,enter.z)));
                float stop=min(leave.x,min(leave.y,leave.z));
                float raw=SampleSceneDepth(GetNormalizedScreenSpaceUV(i.position));
                float eyeDepth=LinearEyeDepth(raw,_ZBufferParams);
                stop=min(stop,eyeDepth/max(.05,-TransformWorldToViewDir(ray).z));
                float segment=max(0,stop-start);
                if (segment<.01) return 0;
                float opacity=0;
                float luminance=0;
                [unroll] for(int j=0;j<6;j++)
                {
                    float3 p=eye+ray*(start+segment*(j+.5)/6.0);
                    float3 q=TransformWorldToObject(p);
                    float fade=saturate((.5-abs(q.x))*7)*saturate((.5-abs(q.z))*7);
                    float layers=saturate((.5-abs(q.y))*5);
                    float time=_Time.y*.035;
                    float shape=sin(p.x*.015+sin(p.z*.014+time)*2.1+p.y*.046)+
                        sin(p.z*.023-p.y*.061+time+sin(p.x*.019))*.68;
                    float detail=sin(p.x*.058+p.z*.037+p.y*.14+shape)*.24;
                    float cloud=smoothstep(-.64,1.42,shape+detail)*fade*layers;
                    opacity+=cloud;
                    luminance+=cloud*saturate(.55+shape*.25);
                }
                float alpha=min(.65,1-exp(-opacity*segment*.00094));
                half3 color=_CloudColor.rgb*(.57+.72*luminance/max(.001,opacity));
                return half4(color,alpha);
            }
            ENDHLSL
        }
    }
}
