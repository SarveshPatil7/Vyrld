using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]

public class CpuTerrainChunk : MonoBehaviour {
    [Header("Chunk Settings")]
    public Vector3Int chunkCoord = Vector3Int.zero;
    public int cellCount = 16;
    public float cellSize = 1f;
    public int seed = 12345;

    [Header("Debug")]
    public bool generateOnStart = true;
    public bool logDensityRange = true;

    [Header("Gizmos")]
    public bool showChunkBounds = true;
    public bool showChunkLabel = true;
    public Color chunkBoundsColor = new Color(0.5f, 0.5f, 0.5f, 0.25f);

    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    private DensityChunkData densityData;

    public DensityChunkData DensityData => densityData;

    private void Awake() {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
    }

    private void Start() {
        if (generateOnStart) {
            GenerateNewChunk();
        }
    }

    public void GenerateNewChunk() {
        UpdateChunkTransformPosition();

        densityData = new DensityChunkData(chunkCoord, cellCount, cellSize);
        DensityInitializer.FillFromSeed(densityData, seed);

        if (logDensityRange) {
            LogDensityRange();
        }

        RebuildMesh();

    }

    public void RebuildMesh() {
        if (densityData == null) {
            Debug.LogError("Cannot rebuild mesh. Density data is null.");
            return;
        }

        Mesh mesh = MarchingCubesMesher.GenerateMesh(densityData);
        mesh.name = $"CPU_Terrain_Chunk_{chunkCoord.x}_{chunkCoord.y}_{chunkCoord.z}";

        meshFilter.sharedMesh = mesh;

        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = mesh;


    }

    private void UpdateChunkTransformPosition() {
        float worldX = chunkCoord.x * cellCount * cellSize;
        float worldY = chunkCoord.y * cellCount * cellSize;
        float worldZ = chunkCoord.z * cellCount * cellSize;

        transform.position = new Vector3(worldX, worldY, worldZ);
    }

    private void LogDensityRange() {
        float min = float.MaxValue;
        float max = float.MinValue;

        for (int x = 0; x < densityData.sampleCount; x++) {
            for (int y = 0; y < densityData.sampleCount; y++) {
                for (int z = 0; z < densityData.sampleCount; z++) {
                    float d = densityData.Get(x, y, z);

                    if (d < min) {
                        min = d;
                    }

                    if (d > max) {
                        max = d;
                    }
                }
            }
        }

        Debug.Log($"Density range for chunk {chunkCoord}: min {min}, max {max}");
    }

    public void ApplySphereEdit(Vector3 worldCenter, float radius, float strength) {
        if (densityData == null) {
            Debug.LogError("Cannot edit terrain. Density data is null.");
            return;
        }

        for (int x = 0; x < densityData.sampleCount; x++) {
            for (int y = 0; y < densityData.sampleCount; y++) {
                for (int z = 0; z < densityData.sampleCount; z++) {
                    Vector3 worldPos = densityData.SampleToWorldPosition(x, y, z);
                    float distance = Vector3.Distance(worldPos, worldCenter);

                    if (distance > radius) {
                        continue;
                    }

                    float falloff = 1f - distance / radius;

                    float oldDensity = densityData.Get(x, y, z);
                    float newDensity = oldDensity + strength * falloff;

                    densityData.Set(x, y, z, newDensity);
                }
            }
        }

        RebuildMesh();

    }

    public void SaveChunk() {
        if (densityData == null) {
            Debug.LogError("Cannot save chunk. Density data is null.");
            return;
        }

        DensityChunkSaveLoad.Save(densityData);
    }

    public void LoadChunk() {
        DensityChunkData loadedData = DensityChunkSaveLoad.Load(
        chunkCoord,
        cellCount,
        cellSize
        );

        if (loadedData == null) {
            return;
        }

        densityData = loadedData;
        RebuildMesh();

    }

    public void ResetChunkToSeed() {
        densityData = new DensityChunkData(chunkCoord, cellCount, cellSize);
        DensityInitializer.FillFromSeed(densityData, seed);

        RebuildMesh();

    }

    public void Initialize(Vector3Int newChunkCoord, int newCellCount, float newCellSize, int newSeed, bool loadSavedIfAvailable) {
        chunkCoord = newChunkCoord;
        cellCount = newCellCount;
        cellSize = newCellSize;
        seed = newSeed;

        UpdateChunkTransformPosition();

        if (loadSavedIfAvailable && DensityChunkSaveLoad.SaveExists(chunkCoord)) {
            LoadChunk();
            Debug.Log($"Chunk {chunkCoord} loaded from saved density file.");
        }
        else {
            GenerateNewChunk();

            if (loadSavedIfAvailable) {
                Debug.Log($"Chunk {chunkCoord} generated from seed because no save file exists.");
            }
            else {
                Debug.Log($"Chunk {chunkCoord} generated from seed because auto-load is disabled.");
            }
        }
    }

    private void OnDrawGizmos() {
        if (!showChunkBounds) {
            return;
        }

        float size = cellCount * cellSize;

        Vector3 chunkOrigin = new Vector3(
        chunkCoord.x * size,
        chunkCoord.y * size,
        chunkCoord.z * size
    );

        Vector3 center = chunkOrigin + Vector3.one * size * 0.5f;

        Gizmos.color = chunkBoundsColor;
        Gizmos.DrawWireCube(center, Vector3.one * size);

#if UNITY_EDITOR
        if (showChunkLabel) {
            Handles.Label(center, $"Chunk {chunkCoord}");
        }
#endif
    }
}