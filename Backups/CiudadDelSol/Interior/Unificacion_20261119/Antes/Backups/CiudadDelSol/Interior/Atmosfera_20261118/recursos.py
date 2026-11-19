from pathlib import Path
import re,numpy as np
from PIL import Image
r=Path.cwd();p=r/'Library/PackageCache/com.unity.render-pipelines.universal@7865b6b91f8a';a=r/'Assets/ROM/Art/CiudadDelSol/Interior/Atmosfera';a.mkdir(parents=True,exist_ok=True)
shader=(p/'Shaders/SimpleLit.shader').read_text()
shader=shader.replace('Shader "Universal Render Pipeline/Simple Lit"','Shader "ROM/CiudadDelSol/PiedraInterior"',1)
shader=shader.replace('Properties\n    {','Properties\n    {\n        _InteriorGrime("Deterioro local",2D)="white"{}\n        _InteriorFloor("Piso interior",Float)=-16\n        _InteriorMaxY("Limite superior local",Float)=-0.05',1)
shader=shader.replace('_InteriorFloor("Piso interior",Float)=-16','_InteriorFloor("Piso interior",Float)=-16\n        _InteriorStrength("Tratamiento local",Range(0,1))=1',1)
shader=shader.replace('"LightMode" = "UniversalForward"','"LightMode" = "UniversalForwardOnly"').replace('"LightMode" = "UniversalGBuffer"','"LightMode" = "CityUnusedGBuffer"')
shader=shader.replace('Packages/com.unity.render-pipelines.universal/Shaders/SimpleLitInput.hlsl','Assets/ROM/Art/CiudadDelSol/Interior/Atmosfera/InteriorInput.hlsl').replace('Packages/com.unity.render-pipelines.universal/Shaders/SimpleLitForwardPass.hlsl','Assets/ROM/Art/CiudadDelSol/Interior/Atmosfera/InteriorForward.hlsl')
(a/'PiedraInterior.shader').write_text(shader)
inp=(p/'Shaders/SimpleLitInput.hlsl').read_text().replace('CBUFFER_END','    float _InteriorFloor;\n    float _InteriorMaxY;\nCBUFFER_END',1)
inp=inp.replace('float _InteriorFloor;','float _InteriorFloor;\n    float _InteriorStrength;',1)
(a/'InteriorInput.hlsl').write_text(inp)
lighting=(p/'ShaderLibrary/Lighting.hlsl').read_text();start=lighting.index('half4 UniversalFragmentBlinnPhong(InputData inputData, SurfaceData surfaceData)');end=lighting.index('// Deprecated:',start)
fn=lighting[start:end].replace('UniversalFragmentBlinnPhong','CityInteriorLighting')
fn=fn.replace('Light mainLight = GetMainLight(inputData, shadowMask, aoFactor);','Light mainLight = GetMainLight(inputData, shadowMask, aoFactor);\n    mainLight.color *= lerp(1.0h,0.025h,cityMask);')
fn=fn.replace('half4 CityInteriorLighting(InputData inputData, SurfaceData surfaceData)','half4 CityInteriorLighting(InputData inputData, SurfaceData surfaceData, half cityMask)')
fn=fn.replace('inputData.bakedGI *= surfaceData.albedo;','inputData.bakedGI *= surfaceData.albedo * lerp(1.0h,0.10h,cityMask);')
fn=fn.replace('Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);','Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);\n        light = CityFilterLight(light,lightIndex,cityMask);')
fn=fn.replace('inputData.vertexLighting * surfaceData.albedo','inputData.vertexLighting * surfaceData.albedo * lerp(1.0h,0.12h,cityMask)')
helper='''
TEXTURE2D(_InteriorGrime); SAMPLER(sampler_InteriorGrime);
Light CityFilterLight(Light light,uint index,half mask)
{
 #if USE_CLUSTER_LIGHT_LOOP
 int dataIndex=index;
 #else
 int dataIndex=GetPerObjectLightIndex(index);
 #endif
 #if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
 float positional=_AdditionalLightsBuffer[dataIndex].position.w;
 #else
 float positional=_AdditionalLightsPosition[dataIndex].w;
 #endif
 half warm=step(light.color.b*1.15h,light.color.r)*step(light.color.b,light.color.g);
 half factor=positional<0.5?0.035h:lerp(1.0h,0.09h,warm);
 light.color*=lerp(1.0h,factor,mask);return light;
}
half CityAgeStone(inout InputData data,inout SurfaceData surface)
{
 // Local materials only. Upper exposed faces keep their original treatment.
 half mask=(1-smoothstep(_InteriorMaxY-.15,_InteriorMaxY,data.positionWS.y))*_InteriorStrength;
 if(data.normalWS.y>.55 && data.positionWS.y>_InteriorFloor+7)mask=0;
 float3 n=abs(data.normalWS);float2 coord=n.y>.6?data.positionWS.xz:(n.x>n.z?data.positionWS.zy:data.positionWS.xy);
 half4 dirt=SAMPLE_TEXTURE2D(_InteriorGrime,sampler_InteriorGrime,coord*.105);
 half depth=saturate((-data.positionWS.y-16)/22);
 half luminance=dot(surface.albedo,half3(.2126,.7152,.0722));
 half stain=lerp(.88h,.24h,smoothstep(.22h,.8h,dirt.r));
 half fissure=lerp(1.0h,.38h,dirt.g);
 half3 aged=lerp(luminance.xxx,surface.albedo,.32h)*half3(.48h,.47h,.78h)*stain*fissure*lerp(1.0h,.85h,depth);
 // A03/R21 already use dark local stone: avoid applying the full darkening twice.
 half basePeak=max(_BaseColor.r,max(_BaseColor.g,_BaseColor.b));
 aged*=basePeak<.35h?min(3.2h,.50h/max(.05h,basePeak)):1.0h;
 surface.albedo=lerp(surface.albedo,aged,mask);
 surface.specular*=lerp(1.0h,.18h,mask);surface.smoothness*=lerp(1.0h,.25h,mask);
 // Very faint local bounce prevents black silhouettes when a per-object light budget is full.
 surface.emission=lerp(surface.emission,aged*half3(.12h,.025h,.22h)*lerp(.55h,1.0h,dirt.b),mask);
 // A faint upward-facing floor bounce preserves route legibility, without lighting the whole room.
 half floorGuide=step(.7h,data.normalWS.y)*(1-smoothstep(_InteriorFloor+.2,_InteriorFloor+1.0,data.positionWS.y));
 surface.emission+=aged*half3(.12h,.08h,.26h)*floorGuide*mask;
 return mask;
}
'''
forward=(p/'Shaders/SimpleLitForwardPass.hlsl').read_text()
forward=forward.replace('struct Attributes',helper+'\n'+fn+'\nstruct Attributes',1)
forward=forward.replace('half4 color = UniversalFragmentBlinnPhong(inputData, surfaceData);','half cityMask=CityAgeStone(inputData,surfaceData);\n    half4 color = CityInteriorLighting(inputData, surfaceData,cityMask);')
(a/'InteriorForward.hlsl').write_text(forward)
rng=np.random.default_rng(16033);size=256;noise=np.zeros((size,size))
for grid,weight in [(5,.5),(13,.3),(32,.2)]:
    cell=(rng.random((grid,grid))*255).astype('uint8');noise+=np.asarray(Image.fromarray(cell).resize((size,size),Image.Resampling.BICUBIC))/255*weight
y,x=np.mgrid[:size,:size];cracks=np.exp(-np.abs(np.sin(x*.19+np.sin(y*.08)*1.4)+np.sin(y*.15+x*.014)) *27)
rgba=np.stack([np.clip(noise*1.3-.12,0,1),cracks,noise,np.ones_like(noise)],axis=-1)
Image.fromarray((rgba*255).astype('uint8'),'RGBA').save(a/'Deterioro.png')
(a/'Procedencia.txt').write_text('PiedraInterior/InteriorInput/InteriorForward derivados de URP 17.3 (com.unity.render-pipelines.universal@7865b6b91f8a), solo recursos locales; paquete original intacto.\n')
license=p/'LICENSE.md'
if license.exists():(a/'LICENSE_URP.md').write_text(license.read_text(encoding='utf-8'),encoding='utf-8')
print('Shader local Simple Lit y textura de deterioro generados; ninguna configuración global modificada.')
