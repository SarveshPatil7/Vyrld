using System;
using System.Collections.Generic;
using UnityEngine;

public class CpuTerrainChunkManager : MonoBehaviour {
    [Header("Chunk Prefab")]
    [SerializeField] private CpuTerrainChunk chunkPrefab;

    [Header("Generation Settings")]
    [SerializeField] private int cellCount = 16;
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private int seed = 12345;

    [Header("Test Grid")]
    [SerializeField] private int radiusX = 1;
    [SerializeField] private int radiusY = 1;
    [SerializeField] private int radiusZ = 1;

    [Header("Save/Load")]
    [SerializeField] private bool loadSavedChunksOnStart = true;
    [SerializeField] private string worldName = "DevWorld";

    [Header("Undo")]
    [SerializeField] private int maxUndoSteps = 30;

    private TerrainEditUndoSystem undoSystem;

    private readonly Dictionary<Vector3Int, CpuTerrainChunk> chunks = new();

    private void Start() {
        GenerateTestChunks();
    }

    public void GenerateTestChunks() {
        ClearExistingChunks();

        for (int x = -radiusX; x <= radiusX; x++) {
            for (int y = -radiusY; y <= radiusY; y++) {
                for (int z = -radiusZ; z <= radiusZ; z++) {
                    CreateChunk(new Vector3Int(x, y, z));
                }
            }
        }

        Debug.Log($"Generated {chunks.Count} CPU terrain chunks.");
    }

    private void CreateChunk(Vector3Int chunkCoord) {
        if (chunkPrefab == null) {
            Debug.LogError("Chunk prefab is not assigned.");
            return;
        }

        CpuTerrainChunk chunk = Instantiate(
            chunkPrefab,
            transform
        );

        chunk.name = $"CPU_Terrain_Chunk_{chunkCoord.x}_{chunkCoord.y}_{chunkCoord.z}";
        chunk.Initialize(chunkCoord, cellCount, cellSize, seed, loadSavedChunksOnStart, worldName);

        chunks.Add(chunkCoord, chunk);
    }

    private void ClearExistingChunks() {
        foreach (CpuTerrainChunk chunk in chunks.Values) {
            if (chunk != null) {
                Destroy(chunk.gameObject);
            }
        }

        chunks.Clear();
    }

    public CpuTerrainChunk GetChunk(Vector3Int chunkCoord) {
        chunks.TryGetValue(chunkCoord, out CpuTerrainChunk chunk);
        return chunk;
    }

    public void SaveAllChunks() {
        SaveWorldMetadata();

        foreach (CpuTerrainChunk chunk in chunks.Values) {
            if (chunk != null) {
                chunk.SaveChunk();
            }
        }

        Debug.Log($"Saved {chunks.Count} CPU terrain chunks to world: {worldName}");
    }

    private void SaveWorldMetadata() {
        TerrainWorldSaveMetadata existingMetadata = DensityChunkSaveLoad.LoadMetadata(worldName);

        TerrainWorldSaveMetadata metadata = existingMetadata ?? new TerrainWorldSaveMetadata(worldName, seed, cellCount, cellSize);

        metadata.worldName = worldName;
        metadata.seed = seed;
        metadata.cellCount = cellCount;
        metadata.cellSize = cellSize;

        DensityChunkSaveLoad.SaveMetadata(metadata);
    }

    public void LoadAllChunks() {
        foreach (CpuTerrainChunk chunk in chunks.Values) {
            if (chunk != null) {
                chunk.LoadChunk();
            }
        }

        ClearUndoHistory();

        Debug.Log($"Loaded {chunks.Count} CPU terrain chunks.");
    }

    public void ResetAllChunksToSeed() {
        foreach (CpuTerrainChunk chunk in chunks.Values) {
            if (chunk != null) {
                chunk.ResetChunkToSeed();
            }
        }

        ClearUndoHistory();

        Debug.Log($"Reset {chunks.Count} CPU terrain chunks to seed default.");
    }

    public void ApplyBrushEdit(Vector3 worldCenter, float radius, float strength, CpuTerrainBrushType brushType, CpuTerrainFlattenMode flattenMode, float roughnessScale, float roughnessAmount) {
        CpuTerrainBrushContext brushContext = BuildBrushContext(worldCenter, radius, brushType, flattenMode);
        List<CpuTerrainChunk> editedChunks = new List<CpuTerrainChunk>();

        foreach (CpuTerrainChunk chunk in chunks.Values) {
            if (chunk == null || chunk.DensityData == null) {
                continue;
            }

            if (!DoesBrushOverlapChunk(worldCenter, radius, chunk)) {
                continue;
            }

            chunk.ApplyBrushEditToDensity(worldCenter, radius, strength, brushType, roughnessScale, roughnessAmount, brushContext);
            editedChunks.Add(chunk);
        }

        for (int i = 0; i < editedChunks.Count; i++) {
            editedChunks[i].RebuildMesh();
        }
    }

    private CpuTerrainBrushContext BuildBrushContext(Vector3 worldCenter, float radius, CpuTerrainBrushType brushType, CpuTerrainFlattenMode flattenMode) {
        if (brushType != CpuTerrainBrushType.Flatten) {
            return CpuTerrainBrushContext.Horizontal(worldCenter);
        }

        if (flattenMode == CpuTerrainFlattenMode.Horizontal) {
            return CpuTerrainBrushContext.Horizontal(worldCenter);
        }

        if (TryBuildAveragePlaneContext(worldCenter, radius, out CpuTerrainBrushContext context)) {
            return context;
        }

        return CpuTerrainBrushContext.Horizontal(worldCenter);
    }

    private bool TryBuildAveragePlaneContext(Vector3 worldCenter, float radius, out CpuTerrainBrushContext context) {
        context = CpuTerrainBrushContext.Horizontal(worldCenter);

        float sumXX = 0f;
        float sumXZ = 0f;
        float sumX = 0f;

        float sumZZ = 0f;
        float sumZ = 0f;
        float sum = 0f;

        float sumXY = 0f;
        float sumZY = 0f;
        float sumY = 0f;

        foreach (CpuTerrainChunk chunk in chunks.Values) {
            if (chunk == null || chunk.DensityData == null) {
                continue;
            }

            if (!DoesBrushOverlapChunk(worldCenter, radius, chunk)) {
                continue;
            }

            DensityChunkData densityData = chunk.DensityData;

            for (int x = 0; x < densityData.sampleCount; x++) {
                for (int y = 0; y < densityData.sampleCount; y++) {
                    for (int z = 0; z < densityData.sampleCount; z++) {
                        Vector3 sampleWorldPosition = densityData.SampleToWorldPosition(x, y, z);
                        float distance = Vector3.Distance(sampleWorldPosition, worldCenter);

                        if (distance > radius) {
                            continue;
                        }

                        float falloff = 1f - distance / radius;
                        float weight = Mathf.Max(0.001f, falloff);

                        float density = densityData.Get(x, y, z);

                        float localX = sampleWorldPosition.x - worldCenter.x;
                        float localZ = sampleWorldPosition.z - worldCenter.z;

                        float estimatedSurfaceY = sampleWorldPosition.y + density;

                        sumXX += weight * localX * localX;
                        sumXZ += weight * localX * localZ;
                        sumX += weight * localX;

                        sumZZ += weight * localZ * localZ;
                        sumZ += weight * localZ;
                        sum += weight;

                        sumXY += weight * localX * estimatedSurfaceY;
                        sumZY += weight * localZ * estimatedSurfaceY;
                        sumY += weight * estimatedSurfaceY;
                    }
                }
            }
        }

        if (sum <= 0.001f) {
            return false;
        }

        bool solved = Solve3x3(
        sumXX, sumXZ, sumX,
        sumXZ, sumZZ, sumZ,
        sumX,  sumZ,  sum,
        sumXY, sumZY, sumY,
        out float a,
        out float b,
        out float c
    );

        if (!solved) {
            return false;
        }

        Vector3 planeNormal = new Vector3(-a, 1f, -b).normalized;
        Vector3 planePoint = new Vector3(worldCenter.x, c, worldCenter.z);

        context = new CpuTerrainBrushContext {
            planePoint = planePoint,
            planeNormal = planeNormal
        };

        return true;
    }

    private bool Solve3x3(  float a11, float a12, float a13,
                            float a21, float a22, float a23,
                            float a31, float a32, float a33,
                            float b1, float b2, float b3,
                            out float x, out float y, out float z) {
        x = 0f;
        y = 0f;
        z = 0f;

        float det =
        a11 * (a22 * a33 - a23 * a32) -
        a12 * (a21 * a33 - a23 * a31) +
        a13 * (a21 * a32 - a22 * a31);

        if (Mathf.Abs(det) < 0.00001f) {
            return false;
        }

        float detX =
        b1 * (a22 * a33 - a23 * a32) -
        a12 * (b2 * a33 - a23 * b3) +
        a13 * (b2 * a32 - a22 * b3);

        float detY =
        a11 * (b2 * a33 - a23 * b3) -
        b1 * (a21 * a33 - a23 * a31) +
        a13 * (a21 * b3 - b2 * a31);

        float detZ =
        a11 * (a22 * b3 - b2 * a32) -
        a12 * (a21 * b3 - b2 * a31) +
        b1 * (a21 * a32 - a22 * a31);

        x = detX / det;
        y = detY / det;
        z = detZ / det;

        return true;
    }

    private bool DoesBrushOverlapChunk(Vector3 worldCenter, float radius, CpuTerrainChunk chunk) {
        Vector3 chunkMin = new Vector3(
        chunk.ChunkCoord.x * cellCount * cellSize,
        chunk.ChunkCoord.y * cellCount * cellSize,
        chunk.ChunkCoord.z * cellCount * cellSize
    );

        Vector3 chunkMax = chunkMin + new Vector3(
        cellCount * cellSize,
        cellCount * cellSize,
        cellCount * cellSize
    );

        float sqrDistance = 0f;

        if (worldCenter.x < chunkMin.x) {
            float d = chunkMin.x - worldCenter.x;
            sqrDistance += d * d;
        }
        else if (worldCenter.x > chunkMax.x) {
            float d = worldCenter.x - chunkMax.x;
            sqrDistance += d * d;
        }

        if (worldCenter.y < chunkMin.y) {
            float d = chunkMin.y - worldCenter.y;
            sqrDistance += d * d;
        }
        else if (worldCenter.y > chunkMax.y) {
            float d = worldCenter.y - chunkMax.y;
            sqrDistance += d * d;
        }

        if (worldCenter.z < chunkMin.z) {
            float d = chunkMin.z - worldCenter.z;
            sqrDistance += d * d;
        }
        else if (worldCenter.z > chunkMax.z) {
            float d = worldCenter.z - chunkMax.z;
            sqrDistance += d * d;
        }

        return sqrDistance <= radius * radius;
    }

    private void Awake() {
        undoSystem = new TerrainEditUndoSystem(maxUndoSteps);
    }

    public void AddBrushAreaToUndoSnapshot(TerrainEditUndoSnapshot snapshot, Vector3 worldCenter, float radius) {
        if (snapshot == null) {
            return;
        }

        foreach (CpuTerrainChunk chunk in chunks.Values) {
            if (chunk == null || chunk.DensityData == null) {
                continue;
            }

            if (!DoesBrushOverlapChunk(worldCenter, radius, chunk)) {
                continue;
            }

            if (snapshot.ContainsChunk(chunk.ChunkCoord)) {
                continue;
            }

            float[] sourceDensityArray = chunk.DensityData.GetRawDensityArray();
            float[] copiedDensityArray = new float[sourceDensityArray.Length];
            Array.Copy(sourceDensityArray, copiedDensityArray, sourceDensityArray.Length);

            snapshot.AddChunk(chunk.ChunkCoord, copiedDensityArray);
        }
    }

    public void PushUndoSnapshot(TerrainEditUndoSnapshot snapshot) {
        EnsureUndoSystem();

        if (snapshot == null || snapshot.IsEmpty) {
            return;
        }

        undoSystem.Push(snapshot);
        Debug.Log($"Stored terrain undo step. Undo count: {undoSystem.UndoCount}");
    }

    public void UndoLastEdit() {
        EnsureUndoSystem();

        if (!undoSystem.TryPop(out TerrainEditUndoSnapshot snapshot)) {
            Debug.Log("No terrain undo step available.");
            return;
        }

        int restoredChunkCount = 0;

        foreach (KeyValuePair<Vector3Int, float[]> entry in snapshot.ChunkDensitySnapshots) {
            if (!chunks.TryGetValue(entry.Key, out CpuTerrainChunk chunk)) {
                continue;
            }

            if (chunk == null) {
                continue;
            }

            chunk.RestoreDensitySnapshot(entry.Value);
            restoredChunkCount++;
        }

        Debug.Log($"Undid terrain edit. Restored {restoredChunkCount} chunks.");
    }

    public void ClearUndoHistory() {
        EnsureUndoSystem();
        undoSystem.Clear();
    }

    private void EnsureUndoSystem() {
        if (undoSystem == null) {
            undoSystem = new TerrainEditUndoSystem(maxUndoSteps);
        }
    }
}