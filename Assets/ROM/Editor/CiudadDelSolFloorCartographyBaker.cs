using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using ROM.UI.Gameplay;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ROM.EditorTools
{
    // Offline cartography from a disposable prefab-content scene. Never opens/saves a gameplay scene.
    public static class CiudadDelSolFloorCartographyBaker
    {
        const string SourceGuid="450b132d8394ca54f86ff67fca648d8a";
        const string SourceHash="5C0B4E4E8D0888EACD18A677AAC1CEB1425DEAAC49D8E34717A87E5DCC435154";
        const string Original="Assets/Scenes/Dungeon_Isla_del_Sol.unity";
        const string GlobalAtlas="Assets/ROM/UI/GameplayHUD/Resources/RiftMinimapAtlas.asset";
        const string GlobalImage="Assets/ROM/UI/GameplayHUD/Resources/RiftMinimap.png";
        const int W=2048,H=1576;
        static readonly Rect Extent=Rect.MinMaxRect(-36,-60,198,120);
        static readonly Color32 Background=new Color32(15,22,31,255),FloorColor=new Color32(218,202,156,255),Deep38=new Color32(88,182,203,255),StairColor=new Color32(231,151,75,255),WallColor=new Color32(47,68,79,255),Ink=new Color32(243,237,211,255);
        static readonly Vector2[] Route={new Vector2(156,99),new Vector2(180,99),new Vector2(180,63),new Vector2(135,63),new Vector2(135,105),new Vector2(120,105),new Vector2(120,12),new Vector2(63,12),new Vector2(63,-15),new Vector2(57,-15),new Vector2(45,-15)};
        sealed class Geometry { public Vector3[] Vertices;public int[] Indices; }
        sealed class Part { public Collider Collider;public Vector3[] Vertices;public int[] Indices;public Bounds Bounds;public bool Floor,Stair,Access;public string Path; }
        [Serializable] sealed class PlaneInfo { public string id,texture,atlas;public float[] heights;public int floorModules,stairs,obstacleSections;public float surfaceAreaApprox; }
        [Serializable] sealed class StairInfo { public string path;public Vector3 center;public float bottom,top;public string[] planes; }
        [Serializable] sealed class Report { public string sourcePrefab,sourceSHA256,coordinates="Design coordinates, no QA runtime offset. North=+Z, east=+X. Surfaces from upward mesh triangles; obstacles sliced 1m above each floor. Static cartography, not runtime navigation certification.";public Vector2 center,worldSize;public int width=W,height=H;public PlaneInfo[] planes;public StairInfo[] stairs; }
        static string Hash(string path){using(var sha=SHA256.Create())using(var stream=File.OpenRead(path))return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","");}
        static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        static Vector2 Pixel(Vector3 p)=>new Vector2((p.x-Extent.xMin)/Extent.width*(W-1),(p.z-Extent.yMin)/Extent.height*(H-1));
        static Vector2 Pixel(Vector2 p)=>Pixel(new Vector3(p.x,0,p.y));
        static Bounds MeshBounds(Collider c)
        {
            var b=c is MeshCollider mc?mc.sharedMesh.bounds:new Bounds(((BoxCollider)c).center,((BoxCollider)c).size);var m=c.transform.localToWorldMatrix;var x=m.MultiplyVector(Vector3.right*b.extents.x);var y=m.MultiplyVector(Vector3.up*b.extents.y);var z=m.MultiplyVector(Vector3.forward*b.extents.z);
            return new Bounds(m.MultiplyPoint3x4(b.center),new Vector3(Mathf.Abs(x.x)+Mathf.Abs(y.x)+Mathf.Abs(z.x),Mathf.Abs(x.y)+Mathf.Abs(y.y)+Mathf.Abs(z.y),Mathf.Abs(x.z)+Mathf.Abs(y.z)+Mathf.Abs(z.z))*2);
        }
        static Geometry BoxGeometry(BoxCollider box)
        {
            var e=box.size*.5f;var c=box.center;
            return new Geometry{Vertices=new[]{c+new Vector3(-e.x,-e.y,-e.z),c+new Vector3(e.x,-e.y,-e.z),c+new Vector3(e.x,e.y,-e.z),c+new Vector3(-e.x,e.y,-e.z),c+new Vector3(-e.x,-e.y,e.z),c+new Vector3(e.x,-e.y,e.z),c+new Vector3(e.x,e.y,e.z),c+new Vector3(-e.x,e.y,e.z)},Indices=new[]{3,7,6,3,6,2,0,1,5,0,5,4,0,3,2,0,2,1,4,5,6,4,6,7,0,4,7,0,7,3,1,2,6,1,6,5}};
        }
        static bool Scope(Vector3 p)
        {
            if(p.x>=-24.1f&&p.x<=60.1f&&p.z>=-24.1f&&p.z<=60.1f||p.x>=78&&p.x<=132&&p.z>=30&&p.z<=96||p.x>=66&&p.x<=132&&p.z>=-48&&p.z<=30)return true;
            for(int i=1;i<Route.Length;i++){var a=Route[i-1];var d=Route[i]-a;float t=Mathf.Clamp01(Vector2.Dot(new Vector2(p.x,p.z)-a,d)/d.sqrMagnitude);if(Vector2.Distance(new Vector2(p.x,p.z),a+d*t)<=4.3f)return true;}
            return false;
        }
        static bool StairsOn(Part p,int plane)
        {
            if(p.Access)return plane==0;
            float lo=p.Bounds.min.y,hi=p.Bounds.max.y;
            return plane==0?lo>=-24.1f&&hi<=-15.9f:plane==1?lo>=-30.1f&&hi<=-15.9f:plane==2?lo>=-38.1f&&hi<=-23.9f:lo>=-38.1f&&hi<=-29.9f;
        }
        static byte FloorOn(Part p,int plane)
        {
            if(p.Access)return plane==0?(byte)1:(byte)0;
            float y=p.Bounds.max.y;
            if(plane==0&&Mathf.Abs(y+16)<.1f||plane==1&&Mathf.Abs(y+24)<.1f||plane==2&&Mathf.Abs(y+30)<.1f||plane==3&&Mathf.Abs(y+34)<.1f)return 1;
            return plane==3&&Mathf.Abs(y+38)<.1f?(byte)3:(byte)0;
        }
        static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
        static void Triangle(byte[] raster,Vector3 a,Vector3 b,Vector3 c,byte kind)
        {
            if(Vector3.Cross(b-a,c-a).normalized.y<.55f)return;
            var x=Pixel(a);var y=Pixel(b);var z=Pixel(c);float area=Cross(y-x,z-x);if(Mathf.Abs(area)<.01f)return;
            int minX=Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(x.x,y.x,z.x)),0,W-1),maxX=Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(x.x,y.x,z.x)),0,W-1);
            int minY=Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(x.y,y.y,z.y)),0,H-1),maxY=Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(x.y,y.y,z.y)),0,H-1);
            for(int row=minY;row<=maxY;row++)for(int col=minX;col<=maxX;col++){var q=new Vector2(col+.5f,row+.5f);float u=Cross(y-q,z-q)/area,v=Cross(z-q,x-q)/area;if(u>=-.001f&&v>=-.001f&&u+v<=1.001f)raster[row*W+col]=kind;}
        }
        static void Line(Color32[] pixels,Vector2 a,Vector2 b,Color32 color,int width=1)
        {
            int n=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(a,b)));for(int i=0;i<=n;i++){var q=Vector2.Lerp(a,b,i/(float)n);int x=Mathf.RoundToInt(q.x),y=Mathf.RoundToInt(q.y);for(int dy=-width;dy<=width;dy++)for(int dx=-width;dx<=width;dx++)if(x+dx>=0&&x+dx<W&&y+dy>=0&&y+dy<H)pixels[(y+dy)*W+x+dx]=color;}
        }
        static int Slice(Part p,float height,byte kind,byte[] surface,Color32[] pixels)
        {
            if(p.Bounds.min.y>=height||p.Bounds.max.y<=height)return 0;
            var edges=new List<(Vector2 a,Vector2 b)>();
            for(int i=0;i<p.Indices.Length;i+=3)
            {
                var a=p.Vertices[p.Indices[i]];var b=p.Vertices[p.Indices[i+1]];var c=p.Vertices[p.Indices[i+2]];
                var points=new List<Vector2>(3);
                void Cut(Vector3 u,Vector3 v){if((u.y<=height&&v.y>height)||(v.y<=height&&u.y>height))points.Add(Pixel(Vector3.LerpUnclamped(u,v,(height-u.y)/(v.y-u.y))));}
                Cut(a,b);Cut(b,c);Cut(c,a);if(points.Count==2)edges.Add((points[0],points[1]));
            }
            if(edges.Count==0)return 0;
            int lo=Mathf.Clamp(Mathf.FloorToInt(edges.Min(e=>Mathf.Min(e.a.y,e.b.y))),0,H-1),hi=Mathf.Clamp(Mathf.CeilToInt(edges.Max(e=>Mathf.Max(e.a.y,e.b.y))),0,H-1);
            var intersections=new List<float>();
            for(int row=lo;row<=hi;row++)
            {
                intersections.Clear();float y=row+.5f;
                foreach(var e in edges)if((e.a.y<=y&&e.b.y>y)||(e.b.y<=y&&e.a.y>y))intersections.Add(e.a.x+(y-e.a.y)*(e.b.x-e.a.x)/(e.b.y-e.a.y));
                intersections.Sort();for(int k=1;k<intersections.Count;k+=2)for(int x=Mathf.Max(0,Mathf.CeilToInt(intersections[k-1]));x<=Mathf.Min(W-1,Mathf.FloorToInt(intersections[k]));x++)if(surface[row*W+x]==kind)pixels[row*W+x]=WallColor;
            }
            // Thin/open meshes still retain their actual section outline, without treating their bounding box as a wall.
            foreach(var e in edges){int n=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(e.a,e.b)));for(int k=0;k<=n;k++){var q=Vector2.Lerp(e.a,e.b,k/(float)n);int x=Mathf.RoundToInt(q.x),y=Mathf.RoundToInt(q.y);if(x>=0&&x<W&&y>=0&&y<H&&surface[y*W+x]==kind)pixels[y*W+x]=WallColor;}}
            return 1;
        }
        static void StairSymbol(Part p,Color32[] pixels)
        {
            var tops=new List<Vector3>();for(int i=0;i<p.Indices.Length;i+=3){var a=p.Vertices[p.Indices[i]];var b=p.Vertices[p.Indices[i+1]];var c=p.Vertices[p.Indices[i+2]];if(Vector3.Cross(b-a,c-a).normalized.y>.55f)tops.Add((a+b+c)/3);}
            if(tops.Count==0)return;float low=tops.Min(v=>v.y),high=tops.Max(v=>v.y);
            var bottom=tops.Where(v=>v.y<low+.12f).Aggregate(Vector3.zero,(s,v)=>s+v)/tops.Count(v=>v.y<low+.12f);
            var top=tops.Where(v=>v.y>high-.12f).Aggregate(Vector3.zero,(s,v)=>s+v)/tops.Count(v=>v.y>high-.12f);
            var d=new Vector2(top.x-bottom.x,top.z-bottom.z).normalized;if(d.sqrMagnitude<.5f)return;
            var center=new Vector2(p.Bounds.center.x,p.Bounds.center.z);var side=new Vector2(-d.y,d.x);
            for(float t=-2;t<=2;t+=.7f)Line(pixels,Pixel(center+d*t-side*2),Pixel(center+d*t+side*2),new Color32(128,76,43,255),0);
            var a2=Pixel(center-d*1.6f);var b2=Pixel(center+d*1.6f);var unit=(b2-a2).normalized;var wing=new Vector2(-unit.y,unit.x);
            Line(pixels,a2,b2,Ink,1);Line(pixels,b2,b2-unit*8+wing*5,Ink);Line(pixels,b2,b2-unit*8-wing*5,Ink);
        }
        static readonly Dictionary<char,string> Glyphs=new Dictionary<char,string>{
            {'A',"01110100011000111111100011000110001"},{'B',"11110100011000111110100011000111110"},{'C',"01111100001000010000100001000001111"},{'D',"11110100011000110001100011000111110"},{'E',"11111100001000011110100001000011111"},{'F',"11111100001000011110100001000010000"},{'G',"01111100001000010111100011000101111"},{'H',"10001100011000111111100011000110001"},{'I',"11111001000010000100001000010011111"},{'J',"00111000100001000010000101001001100"},{'L',"10000100001000010000100001000011111"},{'M',"10001110111010110101100011000110001"},{'N',"10001110011010110011100011000110001"},{'O',"01110100011000110001100011000101110"},{'P',"11110100011000111110100001000010000"},{'R',"11110100011000111110101001001010001"},{'S',"01111100001000001110000010000111110"},{'T',"11111001000010000100001000010000100"},{'U',"10001100011000110001100011000101110"},{'V',"10001100011000110001100010101000100"},{'X',"10001100010101000100010101000110001"},{'Y',"10001100010101000100001000010000100"},{'Z',"11111000010001000100010001000011111"},
            {'0',"01110100011001110101110011000101110"},{'1',"00100011000010000100001000010001110"},{'2',"01110100010000100010001000100011111"},{'3',"11110000010000101110000010000111110"},{'4',"10010100101001011111000100001000010"},{'5',"11111100001000011110000010000111110"},{'6',"01110100001000011110100011000101110"},{'7',"11111000010001000100010000100001000"},{'8',"01110100011000101110100011000101110"},{'9',"01110100011000101111000010000101110"},{'-',"00000000000000011111000000000000000"},{'/',"00001000010001000100010001000010000"},{'.',"00000000000000000000000000011000110"},{':',"00000011000110000000011000110000000"}};
        static void Text(Color32[] pixels,string text,int x,int top,int size=2)
        {
            foreach(char c in text){if(Glyphs.TryGetValue(c,out var glyph))for(int row=0;row<7;row++)for(int col=0;col<5;col++)if(glyph[row*5+col]=='1')for(int dy=0;dy<size;dy++)for(int dx=0;dx<size;dx++){int px=x+col*size+dx,py=top-row*size-dy;if(px>=0&&px<W&&py>=0&&py<H)pixels[py*W+px]=Ink;}x+=6*size;}
        }
        [MenuItem("ROM/Dungeon Ciudad del Sol/Cartografia por plantas/Generar cuatro planos en copia aislada")]
        public static void Build()
        {
            GameObject work=null;var textures=new List<Texture2D>();
            try
            {
                Require(!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling,"Salir de Play y esperar compilacion antes de generar planos.");
                string path=AssetDatabase.GUIDToAssetPath(SourceGuid);Require(File.Exists(path)&&Hash(path)==SourceHash,"La copia no coincide con el nucleo aprobado.");
                var hashes=new[]{Original,GlobalAtlas,GlobalImage,path}.ToDictionary(p=>p,Hash);
                work=PrefabUtility.LoadPrefabContents(path);Require(work,"No se pudo abrir copia de trabajo aislada.");
                Physics.SyncTransforms();
                var geometry=new Dictionary<Mesh,Geometry>();var parts=new List<Part>();
                foreach(var col in work.GetComponentsInChildren<Collider>())
                {
                    if(!col.enabled||col.isTrigger||!(col is BoxCollider)&&!(col is MeshCollider)||col is MeshCollider empty&&!empty.sharedMesh)continue;var b=MeshBounds(col);
                    if(b.max.x<Extent.xMin||b.min.x>Extent.xMax||b.max.z<Extent.yMin||b.min.z>Extent.yMax||b.max.y< -41||b.min.y>6)continue;
                    string hierarchy=AnimationUtility.CalculateTransformPath(col.transform,work.transform);bool access=hierarchy.Contains("Subsuelo_Acceso_Etapa01");
                    if(!access&&!Scope(b.center))continue;
                    bool floor=false,stair=false;for(var t=col.transform;t&&t!=work.transform;t=t.parent){floor|=t.name.StartsWith("Platform_Base_01",StringComparison.Ordinal)||t.name.StartsWith("Road_01",StringComparison.Ordinal);stair|=t.name.StartsWith("Staircase_01",StringComparison.Ordinal);}
                    Geometry g;
                    if(col is BoxCollider box)g=BoxGeometry(box);
                    else {var mesh=((MeshCollider)col).sharedMesh;if(!geometry.TryGetValue(mesh,out g)){g=new Geometry{Vertices=mesh.vertices,Indices=mesh.triangles};geometry.Add(mesh,g);}}
                    parts.Add(new Part{Collider=col,Vertices=g.Vertices.Select(col.transform.TransformPoint).ToArray(),Indices=g.Indices,Bounds=b,Floor=floor,Stair=stair,Access=access,Path=hierarchy});
                }
                Require(parts.Any(p=>p.Floor)&&parts.Count(p=>p.Stair)>=29,"Faltan pisos o escaleras del nucleo aprobado: pisos="+parts.Count(p=>p.Floor)+" escaleras="+parts.Count(p=>p.Stair)+" partes="+parts.Count);
                string folder="Assets/ROM/Cartography/CiudadDelSol/Resources/CiudadDelSolNucleo/Revision_"+DateTime.Now.ToString("yyyyMMdd_HHmmss");Directory.CreateDirectory(folder);
                var ids=new[]{"01_Superior_Y16","02_Intermedia_Y24","03_Intermedia_Y30","04_Profunda_Y34_Y38"};var heights=new[]{new[]{-16f},new[]{-24f},new[]{-30f},new[]{-34f,-38f}};
                var report=new Report{sourcePrefab=path,sourceSHA256=SourceHash,center=Extent.center,worldSize=Extent.size,planes=new PlaneInfo[4],stairs=parts.Where(p=>p.Stair).Select(p=>new StairInfo{path=p.Path,center=p.Bounds.center,bottom=p.Bounds.min.y,top=p.Bounds.max.y,planes=Enumerable.Range(0,4).Where(i=>StairsOn(p,i)).Select(i=>ids[i]).ToArray()}).ToArray()};
                for(int plane=0;plane<4;plane++)
                {
                    EditorUtility.DisplayProgressBar("Ciudad del Sol - cartografia aislada",ids[plane],plane/4f);
                    var raster=new byte[W*H];var info=new PlaneInfo{id=ids[plane],heights=heights[plane]};report.planes[plane]=info;
                    foreach(var p in parts.Where(p=>p.Floor&&!p.Stair))
                    {
                        byte kind=FloorOn(p,plane);if(kind==0)continue;info.floorModules++;for(int k=0;k<p.Indices.Length;k+=3)Triangle(raster,p.Vertices[p.Indices[k]],p.Vertices[p.Indices[k+1]],p.Vertices[p.Indices[k+2]],kind);
                    }
                    var stairs=parts.Where(p=>p.Stair&&StairsOn(p,plane)).ToArray();info.stairs=stairs.Length;
                    foreach(var p in stairs)for(int k=0;k<p.Indices.Length;k+=3)Triangle(raster,p.Vertices[p.Indices[k]],p.Vertices[p.Indices[k+1]],p.Vertices[p.Indices[k+2]],2);
                    Require(info.floorModules>0&&stairs.Length>0,"Planta vacia: "+ids[plane]);
                    var pixels=raster.Select(k=>k==1?FloorColor:k==2?StairColor:k==3?Deep38:Background).ToArray();
                    foreach(var p in parts.Where(p=>!p.Floor&&!p.Stair))foreach(float y in heights[plane])info.obstacleSections+=Slice(p,y+1,plane==3&&y== -38?(byte)3:(byte)1,raster,pixels);
                    for(int y=1;y<H-1;y++)for(int x=1;x<W-1;x++)if(raster[y*W+x]!=0&&(raster[y*W+x-1]==0||raster[y*W+x+1]==0||raster[(y-1)*W+x]==0||raster[(y+1)*W+x]==0))pixels[y*W+x]=new Color32(164,153,121,255);
                    foreach(var p in stairs)StairSymbol(p,pixels);
                    Text(pixels,"CIUDAD DEL SOL / NUCLEO MONUMENTAL",24,H-24,3);
                    Text(pixels,plane==0?"PLANTA Y-16 / ACCESO Y4":plane==1?"PLANTA Y-24":plane==2?"PLANTA Y-30":"PLANTA PROFUNDA / OCRE Y-34 / AZUL Y-38",24,H-56,3);
                    Text(pixels,"N",W-43,H-26,3);Line(pixels,new Vector2(W-36,H-88),new Vector2(W-36,H-50),Ink,1);Line(pixels,new Vector2(W-36,H-50),new Vector2(W-43,H-62),Ink);Line(pixels,new Vector2(W-36,H-50),new Vector2(W-29,H-62),Ink);
                    float length=20/Extent.width*(W-1);Line(pixels,new Vector2(24,34),new Vector2(24+length,34),Ink,2);Text(pixels,"20 M",24,22,2);Text(pixels,"NARANJA: ESCALERAS / FLECHA: SUBIDA / OSCURO: MUROS Y PILARES",260,37,2);
                    info.surfaceAreaApprox=raster.Count(k=>k!=0)*Extent.width*Extent.height/(W*H);
                    var texture=new Texture2D(W,H,TextureFormat.RGB24,false);texture.SetPixels32(pixels);texture.Apply();textures.Add(texture);info.texture=folder+"/"+ids[plane]+".png";File.WriteAllBytes(info.texture,texture.EncodeToPNG());
                    Debug.Log("CIUDAD_CARTOGRAFIA_PLANTA: "+info.id+" pisos="+info.floorModules+" escaleras="+info.stairs+" cortes="+info.obstacleSections+" superficie proyectada="+info.surfaceAreaApprox.ToString("F1"));
                }
                var sheet=new Texture2D(W*2,H*2,TextureFormat.RGB24,false);textures.Add(sheet);for(int i=0;i<4;i++)sheet.SetPixels((i%2)*W,(1-i/2)*H,W,H,textures[i].GetPixels());sheet.Apply();File.WriteAllBytes(folder+"/Revision_Cuatro_Plantas.png",sheet.EncodeToPNG());
                AssetDatabase.Refresh();
                foreach(var info in report.planes)
                {
                    var importer=(TextureImporter)AssetImporter.GetAtPath(info.texture);importer.textureType=TextureImporterType.Default;importer.mipmapEnabled=false;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
                    var atlas=ScriptableObject.CreateInstance<RiftMinimapAtlas>();atlas.Map=AssetDatabase.LoadAssetAtPath<Texture2D>(info.texture);atlas.Center=Extent.center;atlas.WorldSize=Extent.size;info.atlas=folder+"/CiudadDelSol_"+info.id+"_Atlas.asset";AssetDatabase.CreateAsset(atlas,info.atlas);
                }
                File.WriteAllText(folder+"/Cartografia.json",JsonUtility.ToJson(report,true));
                foreach(var pair in hashes)Require(Hash(pair.Key)==pair.Value,"Un origen protegido cambio: "+pair.Key);
                Debug.Log("CIUDAD_CARTOGRAFIA_LISTA: "+folder+";4 planos 2048x1576; X-36..198 Z-60..120; norte+Z; mismos centro/escala; copia aislada descartada; original, prefab y atlas general intactos. Sin conexion al HUD.");
                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Texture2D>(report.planes[0].texture));
            }
            catch(Exception e){Debug.LogException(e);}
            finally{EditorUtility.ClearProgressBar();if(work)PrefabUtility.UnloadPrefabContents(work);foreach(var texture in textures)if(texture)Object.DestroyImmediate(texture);}
        }
    }
}
