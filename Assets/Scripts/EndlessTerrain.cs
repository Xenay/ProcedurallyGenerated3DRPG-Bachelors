using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour
{
    
    const float viewerMoveTresholdForChunkUpdate = 25f;
    const float sqrviewerMoveTresholdForChunkUpdate = viewerMoveTresholdForChunkUpdate * viewerMoveTresholdForChunkUpdate;
    public LODInfo[] detailLevels;
    public static float maxViewDst;
    public Transform viewer;
    public Material mapMaterial;

    public static Vector2 viewerPosition;
    Vector2 viewerPositionOld;
    static MapGenerator mapGenerator;
    int chunkSize;
    int chunksVisibleInViewDst;

    Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    static List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

    void Start()
    {
        mapGenerator = FindObjectOfType<MapGenerator> ();

        maxViewDst = detailLevels[detailLevels.Length - 1].visibleDistanceThreshold;
        chunkSize = MapGenerator.mapChunkSize - 1;
        chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / chunkSize);

        UpdateVisibleChunks();

    }
    void Update()
    {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z)/mapGenerator.terrainData.uniformScale;

        if((viewerPositionOld-viewerPosition).sqrMagnitude > sqrviewerMoveTresholdForChunkUpdate) {
            viewerPositionOld = viewerPosition;
            UpdateVisibleChunks();
        }
       
    }
    void UpdateVisibleChunks()  
    {
        for (int i = 0; i<terrainChunksVisibleLastUpdate.Count; i++)
        {
            terrainChunksVisibleLastUpdate[i].SetVisible(false);
        }
        terrainChunksVisibleLastUpdate.Clear();


        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

        for (int yOffset = -chunksVisibleInViewDst;yOffset<=chunksVisibleInViewDst;yOffset++ )
        {
            for (int xOffset = -chunksVisibleInViewDst; xOffset <= chunksVisibleInViewDst; xOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);
                if (terrainChunkDictionary.ContainsKey(viewedChunkCoord)) {
                    terrainChunkDictionary[viewedChunkCoord].UpdateTerainChunk();
                    
                } else
                {
                    terrainChunkDictionary.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, detailLevels, transform, mapMaterial));
                }
            }
        }
    }
    public class TerrainChunk
    {
        GameObject meshObject;
        Vector2 position;
        Bounds bounds;

        

        MeshRenderer meshRenderer;
        MeshFilter meshFilter;
        MeshCollider meshCollider;

        LODInfo[] detailLevels;
        LODMesh[] lODMeshes;
        LODMesh collisionLODMEsh;

        MapData mapData;
        bool mapDataReceived;
        int previousLODIndex = -1;
        public TerrainChunk(Vector2 coord, int size, LODInfo[] detailLevels, Transform parent, Material material)
        {
            this.detailLevels = detailLevels;

            position = coord * size;
            bounds = new Bounds(position, Vector2.one * size);
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);

            meshObject = new GameObject("Terrain chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshCollider = meshObject.AddComponent<MeshCollider>();
            meshRenderer.material = material;
            meshObject.transform.localScale = Vector3.one * mapGenerator.terrainData.uniformScale;

            meshObject.transform.position = positionV3* mapGenerator.terrainData.uniformScale;
            meshObject.transform.parent = parent;
            SetVisible(false);


            lODMeshes = new LODMesh[detailLevels.Length];
            for (int i=0; i< detailLevels.Length; i++)
            {
                lODMeshes[i] = new LODMesh(detailLevels[i].lod, UpdateTerainChunk);
                if(detailLevels[i].useForCollider)
                {
                    collisionLODMEsh = lODMeshes[i];
                }
            }

            mapGenerator.RequestMapData(position,OnMapDataRecieved);

        }

        void OnMapDataRecieved(MapData mapData)
        {
            this.mapData = mapData;
            mapDataReceived = true;

            
            UpdateTerainChunk();
        }

        
        public void UpdateTerainChunk()
        {
            if (mapDataReceived)
            {
                float viewerDistanceFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
                bool visible = viewerDistanceFromNearestEdge <= maxViewDst;

                if (visible)
                {
                    int lodIndex = 0;

                    for (int i = 0; i < detailLevels.Length - 1; i++)
                    {
                        if (viewerDistanceFromNearestEdge > detailLevels[i].visibleDistanceThreshold)
                        {
                            lodIndex = i + 1;
                        }
                        else
                        {
                            break;
                        }
                    }
                    if (lodIndex != previousLODIndex)
                    {
                        LODMesh lODMesh = lODMeshes[lodIndex];
                        if (lODMesh.hasMesh)
                        {
                            previousLODIndex = lodIndex;
                            meshFilter.mesh = lODMesh.mesh;
                            
                        }
                        else if (!lODMesh.hasRequestedMesh)
                        {
                            lODMesh.RequestMesh(mapData);
                        }
                    }
                    if(lodIndex==0)
                    {
                        if(collisionLODMEsh.hasMesh)
                        {
                            meshCollider.sharedMesh = collisionLODMEsh.mesh;
                        }
                        else if(!collisionLODMEsh.hasRequestedMesh)
                        {
                            collisionLODMEsh.RequestMesh(mapData);
                        }
                    }

                    terrainChunksVisibleLastUpdate.Add(this);

                }

                SetVisible(visible);
            }
        }
        public void SetVisible(bool visible)
        {
            meshObject.SetActive(visible);
        }
        public bool IsVisible()
        {
            return meshObject.activeSelf;
        }
    }

    class LODMesh
    {
        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;
        int lod;
        System.Action updateCallback;

        public LODMesh(int lod, System.Action updateCallback)
        {
            this.lod = lod;
            this.updateCallback = updateCallback;
        }

        void onMeshDataRecieved(MeshData meshData)
        {
            mesh = meshData.CreateMesh();
            hasMesh = true;

            updateCallback();
        }

        public void RequestMesh(MapData mapData)
        {
            hasRequestedMesh = true;
            mapGenerator.RequestMeshData(mapData, lod, onMeshDataRecieved);
        }

    }

    [System.Serializable]
    public struct LODInfo
    {
        public int lod;
        public float visibleDistanceThreshold;
        public bool useForCollider;
    }
}
