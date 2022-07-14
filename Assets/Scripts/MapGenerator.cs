using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;


public class MapGenerator : MonoBehaviour
{
    public enum DrawMode
    {
        NoiseMap, Mesh, FalloffMap, ColorMap
    };
    public DrawMode drawMode;

    public TerrainData terrainData;
    public noisedata noiseData;
    public TextureData textureData;
    public Material terrainMaterial;

    public const int mapChunkSize = 239;

    
    [Range(0, 6)]
    public int editorPreviewLOD;
   

    public bool autoUpdate;
    
    public TerrainType[] regions;

    
   
    float[,] falloffMap;

    Queue<MapThreadInfo<MapData>> mapDataInfoQueue = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();
   


    private void Awake()
    {
        textureData.UpdateMeshHeights(terrainMaterial, terrainData.minHeight, terrainData.maxHeight);
        falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize);
    }
    void OnValuesUpdated() {
        textureData.ApplyToMaterial(terrainMaterial);
        if (!Application.isPlaying)
        {
            DrawMapInEditor();
        }
    }
    void OnTextureValuesUpdated()
    {
       
    }

    public void DrawMapInEditor()
    {
        textureData.UpdateMeshHeights(terrainMaterial, terrainData.minHeight, terrainData.maxHeight);
        MapData mapData = GenerateMapData(Vector2.zero);
        MapDisplay display = FindObjectOfType<MapDisplay>();
        Color[] colorMap = new Color[mapChunkSize * mapChunkSize];
        if (drawMode == DrawMode.NoiseMap)
        {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
        }
        else if (drawMode == DrawMode.Mesh)
        {
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, terrainData.meshHeightMultiplier, terrainData.meshHeightCurve, editorPreviewLOD, terrainData.useFlatShading));
        }
        else if (drawMode == DrawMode.FalloffMap)
        {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(mapChunkSize)));
        }
        else if (drawMode == DrawMode.ColorMap)
        {
            display.DrawTexture(TextureGenerator.TextureFromColorMap(colorMap, mapChunkSize, mapChunkSize));
        }
    }
    public void RequestMapData(Vector2 center, Action<MapData> callback)
    {
        textureData.UpdateMeshHeights(terrainMaterial, terrainData.minHeight, terrainData.maxHeight);
        ThreadStart threadStart = delegate
        {
            MapDataThread(center,callback);
        };

        new Thread(threadStart).Start();
    }

    void MapDataThread(Vector2 center,Action<MapData> callback)
    {
        MapData mapData = GenerateMapData(center);
        lock (mapDataInfoQueue)
        {
            mapDataInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
    }

    public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MeshDataThread(mapData, lod,callback);
        };
        new Thread(threadStart).Start();
    }

    void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback)
    {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, terrainData.meshHeightMultiplier, terrainData.meshHeightCurve, lod, terrainData.useFlatShading);
        lock (meshDataThreadInfoQueue)
        {
            meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }

    private void Update()
    {
       if(mapDataInfoQueue.Count > 0)
        {
            for(int i=0; i<mapDataInfoQueue.Count; i++)
            {
                MapThreadInfo<MapData> threadInfo = mapDataInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }     
       
       if(meshDataThreadInfoQueue.Count>0)
        {
            for(int i = 0; i<meshDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }

       

    }


    MapData GenerateMapData(Vector2 center)
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize+2, mapChunkSize+2, noiseData.seed, noiseData.noiseScale, noiseData.octaves, noiseData.persistance, noiseData.lacunarity, center + noiseData.offset, noiseData.normalizeMode);

       
            for (int y = 0; y < mapChunkSize; y++)
            {
            for (int x = 0; x < mapChunkSize; x++)
            {
                if(terrainData.useFalloff)
                {
                    noiseMap[x, y] =Mathf.Clamp01(noiseMap[x, y] - falloffMap[x, y]);
                }
               
            }
        }


      
        return new MapData(noiseMap);
        
    }
    private void OnValidate()
    { 
        if(terrainData != null)
        {
            terrainData.OnValuesUpdated -= OnValuesUpdated;
            terrainData.OnValuesUpdated += OnValuesUpdated;
        }
        if (noiseData != null)
        {
            noiseData.OnValuesUpdated -= OnValuesUpdated;
            noiseData.OnValuesUpdated += OnValuesUpdated;
        }
        if (textureData != null)
        {
            textureData.OnValuesUpdated -= OnTextureValuesUpdated;
            textureData.OnValuesUpdated += OnTextureValuesUpdated;
        }

        falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize);
    }

    struct MapThreadInfo<T>
    {
        public readonly Action<T> callback;
        public readonly T parameter;

        public MapThreadInfo(Action<T> callback, T parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }


}

[System.Serializable]
public struct TerrainType
{
    public string name;
    public float height;
    public Color color;

}

public struct MapData
{
    public readonly float[,] heightMap;
   

    public MapData(float[,] heightMap)
    {
        this.heightMap = heightMap;
        

    }
}
