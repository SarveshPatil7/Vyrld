using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]

public class CpuTerrainChunk : MonoBehaviour {
    [SerializeField] private string worldName = "DevWorld";

    [Header("Chunk Settings")]
    [SerializeField] private Vector3Int chunkCoord = Vector3Int.zero;
    [SerializeField] private int cellCount = 16;
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private int seed = 12345;

    [Header("Debug")]
    [SerializeField] private bool generateOnStart = true;
    [SerializeField] private bool logDensityRange = true;

    [Header("Gizmos")]
    [SerializeField] private bool showChunkBounds = true;
    [SerializeField] private bool showChunkLabel = true;
    [SerializeField] private Color chunkBoundsColor = new Color(0.5f, 0.5f, 0.5f, 0.25f);

    [SerializeField] private TerrainNoisePreset noisePreset;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;
    private DensityChunkData densityData;
    public Vector3Int ChunkCoord => chunkCoord;
    public DensityChunkData DensityData => densityData;
    public TerrainChunkDensityState DensityState { get; private set; } = TerrainChunkDensityState.Dense;
    public string NoisePresetName => noisePreset != null ? noisePreset.presetName : "None";

    private void Awake() {
        EnsureComponents();
    }

    private void Start() {
        if (generateOnStart) {
            GenerateNewChunk();
        }
    }

    public void GenerateNewChunk() {
        EnsureNoisePreset();

        UpdateChunkTransformPosition();

        densityData = new DensityChunkData(chunkCoord, cellCount, cellSize);
        FillDensityFromSeed();

        if (logDensityRange) {
            LogDensityRange();
        }

        RebuildMesh();
    }

    public void RebuildMesh() {
        EnsureComponents();

        if (densityData == null) {
            Debug.LogError("Cannot rebuild mesh. Density data is null.");
            return;
        }

        if (meshFilter == null || meshRenderer == null || meshCollider == null) {
            Debug.LogError($"Cannot rebuild mesh for chunk {chunkCoord}. MeshFilter, MeshRenderer, or MeshCollider is missing.");
            return;
        }

        DensityState = EvaluateDensityState();

        if (DensityState != TerrainChunkDensityState.Dense) {
            ClearChunkMesh();
            return;
        }

        Mesh mesh = MarchingCubesMesher.GenerateMesh(densityData);
        mesh.name = $"CPU_Terrain_Chunk_{chunkCoord.x}_{chunkCoord.y}_{chunkCoord.z}";

        if (mesh.vertexCount == 0) {
            ClearChunkMesh();
            DensityState = TerrainChunkDensityState.Air;
            return;
        }

        meshFilter.sharedMesh = mesh;

        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = mesh;

        meshRenderer.enabled = true;
        meshCollider.enabled = true;
    }

    private TerrainChunkDensityState EvaluateDensityState() {
        const float epsilon = 0.0001f;

        bool hasAir = false;
        bool hasSolid = false;

        for (int x = 0; x < densityData.sampleCount; x++) {
            for (int y = 0; y < densityData.sampleCount; y++) {
                for (int z = 0; z < densityData.sampleCount; z++) {
                    float density = densityData.Get(x, y, z);

                    if (density < -epsilon) {
                        hasAir = true;
                    }

                    if (density > epsilon) {
                        hasSolid = true;
                    }

                    if (hasAir && hasSolid) {
                        return TerrainChunkDensityState.Dense;
                    }
                }
            }
        }

        if (hasSolid) {
            return TerrainChunkDensityState.Filled;
        }

        if (hasAir) {
            return TerrainChunkDensityState.Air;
        }

        return TerrainChunkDensityState.Dense;
    }

    private void ClearChunkMesh() {
        if (meshFilter != null) {
            meshFilter.sharedMesh = null;
        }

        if (meshCollider != null) {
            meshCollider.sharedMesh = null;
            meshCollider.enabled = false;
        }

        if (meshRenderer != null) {
            meshRenderer.enabled = false;
        }
    }

    public void SetChunkRuntimeVisibility(bool visible, bool colliderEnabled) {
        EnsureComponents();

        bool hasSurfaceMesh = DensityState == TerrainChunkDensityState.Dense && meshFilter != null && meshFilter.sharedMesh != null && meshFilter.sharedMesh.vertexCount > 0;

        if (meshRenderer != null) {
            meshRenderer.enabled = visible && hasSurfaceMesh;
        }

        if (meshCollider != null) {
            meshCollider.enabled = colliderEnabled && hasSurfaceMesh;
        }
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

    public void ApplyBrushEdit(Vector3 worldCenter, float radius, float strength, CpuTerrainBrushType brushType, float roughnessScale, float roughnessAmount, 
                                CpuTerrainBrushContext brushContext) {
        ApplyBrushEditToDensity(worldCenter, radius, strength, brushType, roughnessScale, roughnessAmount, brushContext);
        RebuildMesh();
    }

    public void ApplyBrushEditToDensity(Vector3 worldCenter, float radius, float strength, CpuTerrainBrushType brushType, float roughnessScale, float roughnessAmount, CpuTerrainBrushContext brushContext) {
        if (densityData == null) {
            Debug.LogError("Cannot edit terrain. Density data is null.");
            return;
        }

        for (int x = 0; x < densityData.sampleCount; x++) {
            for (int y = 0; y < densityData.sampleCount; y++) {
                for (int z = 0; z < densityData.sampleCount; z++) {
                    Vector3 sampleWorldPosition = densityData.SampleToWorldPosition(x, y, z);
                    float oldDensity = densityData.Get(x, y, z);

                    float densityDelta = CpuTerrainBrush.EvaluateDensityDelta(sampleWorldPosition, oldDensity, worldCenter, radius, strength, brushType, roughnessScale, roughnessAmount, brushContext);

                    if (Mathf.Approximately(densityDelta, 0f)) {
                        continue;
                    }

                    densityData.Set(x, y, z, oldDensity + densityDelta);
                }
            }
        }
    }

    public void SaveChunk() {
        if (densityData == null) {
            Debug.LogError("Cannot save chunk. Density data is null.");
            return;
        }

        DensityChunkSaveLoad.Save(densityData, worldName);
    }

    public void LoadChunk() {
        DensityChunkData loadedData = DensityChunkSaveLoad.Load(worldName, chunkCoord, cellCount, cellSize);

        if (loadedData == null) {
            return;
        }

        densityData = loadedData;
        RebuildMesh();

    }

    public void ResetChunkToSeed() {
        EnsureNoisePreset();

        densityData = new DensityChunkData(chunkCoord, cellCount, cellSize);
        FillDensityFromSeed();

        RebuildMesh();
    }

    public void Initialize(Vector3Int chunkCoord, int cellCount, float cellSize, int seed, bool loadSavedChunkOnStart, string worldName,
                            TerrainNoisePreset noisePreset) {
        this.worldName = worldName;
        this.chunkCoord = chunkCoord;
        this.cellCount = cellCount;
        this.cellSize = cellSize;
        this.seed = seed;

        SetNoisePreset(noisePreset, regenerate: false);

        UpdateChunkTransformPosition();

        if (loadSavedChunkOnStart && DensityChunkSaveLoad.SaveExists(worldName, chunkCoord)) {
            LoadChunk();
            Debug.Log($"Chunk {chunkCoord} loaded from saved density file.");
        }
        else {
            GenerateNewChunk();

            if (loadSavedChunkOnStart) {
                Debug.Log($"Chunk {chunkCoord} generated from seed because no save file exists.");
            }
            else {
                Debug.Log($"Chunk {chunkCoord} generated from seed because saved load is disabled.");
            }
        }
    }

    public void RestoreDensitySnapshot(float[] densitySnapshot) {
        if (densityData == null) {
            Debug.LogError("Cannot restore terrain chunk. Density data is null.");
            return;
        }

        if (densitySnapshot == null) {
            Debug.LogError("Cannot restore terrain chunk. Snapshot is null.");
            return;
        }

        float[] targetDensityArray = densityData.GetRawDensityArray();

        if (targetDensityArray.Length != densitySnapshot.Length) {
            Debug.LogError($"Cannot restore terrain chunk {chunkCoord}. Snapshot length does not match density array length.");
            return;
        }

        Array.Copy(densitySnapshot, targetDensityArray, targetDensityArray.Length);
        RebuildMesh();
    }

    private void OnDrawGizmos() {
        if (!showChunkBounds) {
            return;
        }

        float chunkWorldSize = cellCount * cellSize;
        Vector3 chunkCenter = transform.position + Vector3.one * chunkWorldSize * 0.5f;
        Vector3 chunkSize = Vector3.one * chunkWorldSize;

        Color previousColor = Gizmos.color;
        Gizmos.color = chunkBoundsColor;
        Gizmos.DrawWireCube(chunkCenter, chunkSize);
        Gizmos.color = previousColor;

    #if UNITY_EDITOR
        if (showChunkLabel) {
            Handles.Label(chunkCenter, $"Chunk {chunkCoord}");
        }
    #endif
    }

    public Bounds WorldBounds {
        get {
            float chunkWorldSize = cellCount * cellSize;
            Vector3 center = transform.position + Vector3.one * chunkWorldSize * 0.5f;
            Vector3 size = Vector3.one * chunkWorldSize;
            return new Bounds(center, size);
        }
    }

    public void SetNoisePreset(TerrainNoisePreset newNoisePreset, bool regenerate) {
        noisePreset = newNoisePreset ?? TerrainNoisePreset.CreateDefault();

        if (regenerate) {
            ResetChunkToSeed();
        }
    }

    private void EnsureNoisePreset() {
        if (noisePreset != null) {
            return;
        }

        noisePreset = TerrainNoisePreset.CreateDefault();
    }

    private void FillDensityFromSeed() {
        if (densityData == null) {
            Debug.LogError("Cannot fill density from seed. Density data is null.");
            return;
        }

        for (int x = 0; x < densityData.sampleCount; x++) {
            for (int y = 0; y < densityData.sampleCount; y++) {
                for (int z = 0; z < densityData.sampleCount; z++) {
                    Vector3 sampleWorldPosition = densityData.SampleToWorldPosition(x, y, z);
                    float density = EvaluateSeedDensity(sampleWorldPosition);
                    densityData.Set(x, y, z, density);
                }
            }
        }
    }

    private float EvaluateSeedDensity(Vector3 worldPosition) {
        EnsureNoisePreset();
        return DensityInitializer.EvaluateDensity(worldPosition, noisePreset);
    }

    private void EnsureComponents() {
        if (meshFilter == null) {
            meshFilter = GetComponent<MeshFilter>();
        }

        if (meshRenderer == null) {
            meshRenderer = GetComponent<MeshRenderer>();
        }

        if (meshCollider == null) {
            meshCollider = GetComponent<MeshCollider>();
        }
    }
}