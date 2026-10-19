using System;
using System.IO;
using UnityEditor;
using UnityEngine;
public static class ExportTerrain
{
    public static void Run()
    {
        try
        {
            var data=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/Terrain.asset");
            if(!data) throw new Exception("El terreno antiguo no pudo cargarse.");
            int n=data.heightmapResolution; var heights=data.GetHeights(0,0,n,n);
            using(var writer=new BinaryWriter(File.Create("../sun-terrain-heights.bin")))
            {
                writer.Write(n); writer.Write(data.size.x); writer.Write(data.size.y); writer.Write(data.size.z);
                for(int z=0;z<n;z++) for(int x=0;x<n;x++) writer.Write(heights[z,x]);
            }
            Debug.Log("[Recovery] Exported "+n+" heightmap, size="+data.size);
            EditorApplication.Exit(0);
        }
        catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
