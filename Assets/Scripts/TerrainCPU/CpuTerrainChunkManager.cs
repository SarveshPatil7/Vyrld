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

    [Header("Noise Presets")]
    [SerializeField] private int defaultNoisePresetIndex = 0;
    [SerializeField] private List<TerrainNoisePreset> noisePresets = new List<TerrainNoisePreset> {
    new TerrainNoisePreset {
        presetName = "Default",
        seed = 12345,
        numOctaves = 4,
        lacunarity = 2f,
        persistence = 0.5f,
        noiseScale = 24f,
        noiseWeight = 8f,
        floorOffset = 8f,
        weightMultiplier = 1f,
        hardFloorHeight = -32f,
        hardFloorWeight = 0f
    },
    new TerrainNoisePreset {
        presetName = "Flatlands",
        seed = 12345,
        numOctaves = 3,
        lacunarity = 2f,
        persistence = 0.4f,
        noiseScale = 36f,
        noiseWeight = 2f,
        floorOffset = 7f,
        weightMultiplier = 1f,
        hardFloorHeight = -32f,
        hardFloorWeight = 0f
    },
    new TerrainNoisePreset {
        presetName = "Mountains",
        seed = 12345,
        numOctaves = 5,
        lacunarity = 2.1f,
        persistence = 0.55f,
        noiseScale = 28f,
        noiseWeight = 18f,
        floorOffset = 10f,
        weightMultiplier = 1f,
        hardFloorHeight = -32f,
        hardFloorWeight = 0f
    },
    new TerrainNoisePreset {
        presetName = "Ocean",
        seed = 12345,
        numOctaves = 4,
        lacunarity = 2f,
        persistence = 0.45f,
        noiseScale = 40f,
        noiseWeight = 4f,
        floorOffset = 1f,
        weightMultiplier = 1f,
        hardFloorHeight = -32f,
        hardFloorWeight = 0f
    }
};

    [Header("Vertical Generation Limits")]
    [SerializeField] private int minGeneratedChunkY = -2;
    [SerializeField] private int maxGeneratedChunkY = 4;

    [Header("Vertical Regeneration")]
    [SerializeField] private int regeneratePaddingY = 2;
    [SerializeField] private bool createMissingChunksDuringRegenerate = true;

    [Header("Seam Stitching")]
    [SerializeField] private int stitchIterations = 40;
    [SerializeField] [Range(0f, 1f)] private float stitchNoiseInfluence = 0.25f;
    [SerializeField] private bool stitchSideBoundaries = true;
    [SerializeField] private bool stitchTopBottomBoundaries = false;

    private class TerrainStitchSample {
    public Vector3Int globalSampleCoord;
    public Vector3 worldPosition;
    public float baseDensity;
    public float smoothDensity;
    public float correction;
    public bool isBoundaryConstraint;
}

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

    private CpuTerrainChunk CreateChunk(Vector3Int chunkCoord) {
        return CreateChunk(chunkCoord, defaultNoisePresetIndex, loadSavedChunksOnStart);
    }

    private CpuTerrainChunk CreateChunk(Vector3Int chunkCoord, int noisePresetIndex) {
        return CreateChunk(chunkCoord, noisePresetIndex, loadSavedChunksOnStart);
    }

    private CpuTerrainChunk CreateChunk(Vector3Int chunkCoord, int noisePresetIndex, bool loadSavedChunk) {
        if (chunks.TryGetValue(chunkCoord, out CpuTerrainChunk existingChunk)) {
            return existingChunk;
        }

        if (chunkPrefab == null) {
            Debug.LogError("Chunk prefab is not assigned.");
            return null;
        }

        TerrainNoisePreset noisePreset = GetNoisePresetByIndex(noisePresetIndex);

        CpuTerrainChunk chunk = Instantiate(chunkPrefab, transform);

        chunk.name = $"CPU_Terrain_Chunk_{chunkCoord.x}_{chunkCoord.y}_{chunkCoord.z}";
        chunk.Initialize(chunkCoord, cellCount, cellSize, seed, loadSavedChunk, worldName, noisePreset);

        chunks.Add(chunkCoord, chunk);

        return chunk;
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

    public Vector3Int WorldToChunkCoord(Vector3 worldPosition) {
        float chunkWorldSize = cellCount * cellSize;

        return new Vector3Int(
            Mathf.FloorToInt(worldPosition.x / chunkWorldSize),
            Mathf.FloorToInt(worldPosition.y / chunkWorldSize),
            Mathf.FloorToInt(worldPosition.z / chunkWorldSize)
        );
    }

    public List<CpuTerrainChunk> GenerateChunksAroundChunkCoord(Vector3Int centerChunkCoord, int chunkRadiusX, int chunkRadiusY, int chunkRadiusZ, int noisePresetIndex) {
        List<CpuTerrainChunk> createdChunks = new List<CpuTerrainChunk>();

        for (int x = -chunkRadiusX; x <= chunkRadiusX; x++) {
            for (int y = -chunkRadiusY; y <= chunkRadiusY; y++) {
                for (int z = -chunkRadiusZ; z <= chunkRadiusZ; z++) {
                    Vector3Int chunkCoord = centerChunkCoord + new Vector3Int(x, y, z);

                    if (chunks.ContainsKey(chunkCoord)) {
                        continue;
                    }

                    CpuTerrainChunk createdChunk = CreateChunk(chunkCoord, noisePresetIndex);

                    if (createdChunk != null) {
                        createdChunks.Add(createdChunk);
                    }
                }
            }
        }

        if (createdChunks.Count > 0) {
            Debug.Log($"Generated {createdChunks.Count} new chunks around chunk {centerChunkCoord} using noise preset {GetNoisePresetDisplayName(noisePresetIndex)}.");
        }

        return createdChunks;
    }

    public List<CpuTerrainChunk> GenerateChunksAroundChunkSelection(IEnumerable<CpuTerrainChunk> sourceChunks, int chunkRadiusX, int chunkRadiusY, int chunkRadiusZ, int noisePresetIndex) {
        List<CpuTerrainChunk> createdChunks = new List<CpuTerrainChunk>();

        if (sourceChunks == null) {
            Debug.LogWarning("Cannot generate chunks from selection. Source chunk collection is null.");
            return createdChunks;
        }

        HashSet<Vector2Int> targetColumns = new HashSet<Vector2Int>();

        foreach (CpuTerrainChunk sourceChunk in sourceChunks) {
            if (sourceChunk == null) {
                continue;
            }

            Vector3Int sourceCoord = sourceChunk.ChunkCoord;

            for (int x = -chunkRadiusX; x <= chunkRadiusX; x++) {
                for (int z = -chunkRadiusZ; z <= chunkRadiusZ; z++) {
                    targetColumns.Add(new Vector2Int(sourceCoord.x + x, sourceCoord.z + z));
                }
            }
        }

        int minY = GetMinGeneratedChunkY();
        int maxY = GetMaxGeneratedChunkY();

        foreach (Vector2Int columnCoord in targetColumns) {
            for (int y = minY; y <= maxY; y++) {
                Vector3Int chunkCoord = new Vector3Int(columnCoord.x, y, columnCoord.y);

                if (chunks.ContainsKey(chunkCoord)) {
                    continue;
                }

                CpuTerrainChunk createdChunk = CreateChunk(chunkCoord, noisePresetIndex, loadSavedChunk: false);

                if (createdChunk != null) {
                    createdChunks.Add(createdChunk);
                }
            }
        }

        Debug.Log($"Generated {createdChunks.Count} missing vertical chunks across {targetColumns.Count} columns using noise preset {GetNoisePresetDisplayName(noisePresetIndex)}.");
        return createdChunks;
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

    public void SaveChunks(IEnumerable<CpuTerrainChunk> chunksToSave) {
        if (chunksToSave == null) {
            Debug.LogWarning("Cannot save selected chunks. Chunk collection is null.");
            return;
        }

        SaveWorldMetadata();

        int savedCount = 0;

        foreach (CpuTerrainChunk chunk in chunksToSave) {
            if (chunk == null) {
                continue;
            }

            chunk.SaveChunk();
            savedCount++;
        }

        Debug.Log($"Saved {savedCount} selected CPU terrain chunks to world: {worldName}");
    }

    public void ResetChunksWithNoisePreset(IEnumerable<CpuTerrainChunk> chunksToReset, int noisePresetIndex) {
        if (chunksToReset == null) {
            Debug.LogWarning("Cannot reset selected chunks. Chunk collection is null.");
            return;
        }

        TerrainNoisePreset noisePreset = GetNoisePresetByIndex(noisePresetIndex);

        HashSet<Vector2Int> targetColumns = new HashSet<Vector2Int>();
        int minSelectedY = int.MaxValue;
        int maxSelectedY = int.MinValue;

        foreach (CpuTerrainChunk chunk in chunksToReset) {
            if (chunk == null) {
                continue;
            }

            Vector3Int chunkCoord = chunk.ChunkCoord;

            targetColumns.Add(new Vector2Int(chunkCoord.x, chunkCoord.z));

            minSelectedY = Mathf.Min(minSelectedY, chunkCoord.y);
            maxSelectedY = Mathf.Max(maxSelectedY, chunkCoord.y);
        }

        if (targetColumns.Count == 0) {
            Debug.Log("No valid selected chunks to reset.");
            return;
        }

        int minY = Mathf.Max(GetMinGeneratedChunkY(), minSelectedY - regeneratePaddingY);
        int maxY = Mathf.Min(GetMaxGeneratedChunkY(), maxSelectedY + regeneratePaddingY);

        int resetCount = 0;
        int createdCount = 0;

        foreach (Vector2Int columnCoord in targetColumns) {
            for (int y = minY; y <= maxY; y++) {
                Vector3Int chunkCoord = new Vector3Int(columnCoord.x, y, columnCoord.y);

                CpuTerrainChunk chunk = GetChunk(chunkCoord);

                if (chunk == null && createMissingChunksDuringRegenerate) {
                    chunk = CreateChunk(chunkCoord, noisePresetIndex, loadSavedChunk: false);

                    if (chunk != null) {
                        createdCount++;
                    }
                }

                if (chunk == null) {
                    continue;
                }

                chunk.SetNoisePreset(noisePreset, regenerate: true);
                resetCount++;
            }
        }

        ClearUndoHistory();

        Debug.Log($"Regenerated {resetCount} chunks across {targetColumns.Count} selected columns using noise preset {noisePreset.GetDisplayName()}. Created {createdCount} missing chunks. Y range: {minY} to {maxY}.");
    }

    public void BlendRegenerateChunksWithNoisePreset(IEnumerable<CpuTerrainChunk> chunksToBlend, int noisePresetIndex) {
        if (chunksToBlend == null) {
            Debug.LogWarning("Cannot stitch regenerate chunks. Chunk collection is null.");
            return;
        }

        TerrainNoisePreset noisePreset = GetNoisePresetByIndex(noisePresetIndex);
        List<CpuTerrainChunk> selectedChunkList = new List<CpuTerrainChunk>();
        HashSet<Vector3Int> selectedChunkCoords = new HashSet<Vector3Int>();

        foreach (CpuTerrainChunk chunk in chunksToBlend) {
            if (chunk == null || chunk.DensityData == null) {
                continue;
            }

            if (!selectedChunkCoords.Add(chunk.ChunkCoord)) {
                continue;
            }

            selectedChunkList.Add(chunk);
        }

        if (selectedChunkList.Count == 0) {
            Debug.Log("No valid selected chunks to stitch regenerate.");
            return;
        }

        Dictionary<Vector3Int, TerrainStitchSample> sampleByGlobalCoord = new Dictionary<Vector3Int, TerrainStitchSample>();

        BuildStitchSampleMap(selectedChunkList, noisePreset, sampleByGlobalCoord);

        int boundaryConstraintCount = ApplyExteriorStitchBoundaryConstraints(selectedChunkList, selectedChunkCoords, sampleByGlobalCoord);

        if (boundaryConstraintCount == 0) {
            Debug.LogWarning("No exterior boundary constraints found for stitch regeneration. Select a transition strip that touches existing non-selected chunks.");
            return;
        }

        RunStitchRelaxation(sampleByGlobalCoord);
        WriteStitchSamplesToChunks(selectedChunkList, sampleByGlobalCoord);

        for (int i = 0; i < selectedChunkList.Count; i++) {
            selectedChunkList[i].RebuildMesh();
        }

        ClearUndoHistory();

        Debug.Log($"Stitch regenerated {selectedChunkList.Count} chunks using noise preset {noisePreset.GetDisplayName()}. Boundary constraints: {boundaryConstraintCount}. Iterations: {stitchIterations}. Noise influence: {stitchNoiseInfluence}.");
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
        EnsureNoisePresets();
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

    public List<CpuTerrainChunk> GetChunksInCoordArea(Vector3Int firstCoord, Vector3Int secondCoord) {
        List<CpuTerrainChunk> areaChunks = new List<CpuTerrainChunk>();

        Vector3Int minCoord = GetMinChunkCoord(firstCoord, secondCoord);
        Vector3Int maxCoord = GetMaxChunkCoord(firstCoord, secondCoord);

        for (int x = minCoord.x; x <= maxCoord.x; x++) {
            for (int y = minCoord.y; y <= maxCoord.y; y++) {
                for (int z = minCoord.z; z <= maxCoord.z; z++) {
                    Vector3Int chunkCoord = new Vector3Int(x, y, z);

                    if (!chunks.TryGetValue(chunkCoord, out CpuTerrainChunk chunk)) {
                        continue;
                    }

                    if (chunk == null) {
                        continue;
                    }

                    areaChunks.Add(chunk);
                }
            }
        }

        return areaChunks;
    }

    public Bounds GetChunkCoordAreaWorldBounds(Vector3Int firstCoord, Vector3Int secondCoord) {
        Vector3Int minCoord = GetMinChunkCoord(firstCoord, secondCoord);
        Vector3Int maxCoord = GetMaxChunkCoord(firstCoord, secondCoord);

        float chunkWorldSize = cellCount * cellSize;

        Vector3 minWorld = new Vector3(
        minCoord.x * chunkWorldSize,
        minCoord.y * chunkWorldSize,
        minCoord.z * chunkWorldSize
    );

        Vector3 maxWorld = new Vector3(
        (maxCoord.x + 1) * chunkWorldSize,
        (maxCoord.y + 1) * chunkWorldSize,
        (maxCoord.z + 1) * chunkWorldSize
    );

        Vector3 center = (minWorld + maxWorld) * 0.5f;
        Vector3 size = maxWorld - minWorld;

        return new Bounds(center, size);
    }

    private Vector3Int GetMinChunkCoord(Vector3Int firstCoord, Vector3Int secondCoord) {
        return new Vector3Int(
            Mathf.Min(firstCoord.x, secondCoord.x),
            Mathf.Min(firstCoord.y, secondCoord.y),
            Mathf.Min(firstCoord.z, secondCoord.z)
        );
    }

    private Vector3Int GetMaxChunkCoord(Vector3Int firstCoord, Vector3Int secondCoord) {
        return new Vector3Int(
            Mathf.Max(firstCoord.x, secondCoord.x),
            Mathf.Max(firstCoord.y, secondCoord.y),
            Mathf.Max(firstCoord.z, secondCoord.z)
        );
    }

    private void EnsureNoisePresets() {
        if (noisePresets == null) {
            noisePresets = new List<TerrainNoisePreset>();
        }

        if (noisePresets.Count == 0) {
            noisePresets.Add(TerrainNoisePreset.CreateDefault());
        }
    }

    public int NoisePresetCount {
        get {
            EnsureNoisePresets();
            return noisePresets.Count;
        }
    }

    public TerrainNoisePreset GetNoisePresetByIndex(int noisePresetIndex) {
        EnsureNoisePresets();

        if (noisePresets.Count == 0) {
            return TerrainNoisePreset.CreateDefault();
        }

        int safeIndex = Mathf.Clamp(noisePresetIndex, 0, noisePresets.Count - 1);
        return noisePresets[safeIndex];
    }

    public string GetNoisePresetDisplayName(int noisePresetIndex) {
        TerrainNoisePreset noisePreset = GetNoisePresetByIndex(noisePresetIndex);
        return noisePreset != null ? noisePreset.GetDisplayName() : "None";
    }

    private int GetMinGeneratedChunkY() {
        return Mathf.Min(minGeneratedChunkY, maxGeneratedChunkY);
    }

    private int GetMaxGeneratedChunkY() {
        return Mathf.Max(minGeneratedChunkY, maxGeneratedChunkY);
    }


    private void BuildStitchSampleMap(List<CpuTerrainChunk> selectedChunkList, TerrainNoisePreset noisePreset, Dictionary<Vector3Int, TerrainStitchSample> sampleByGlobalCoord) {
        for (int i = 0; i < selectedChunkList.Count; i++) {
            CpuTerrainChunk chunk = selectedChunkList[i];
            DensityChunkData densityData = chunk.DensityData;

            for (int x = 0; x < densityData.sampleCount; x++) {
                for (int y = 0; y < densityData.sampleCount; y++) {
                    for (int z = 0; z < densityData.sampleCount; z++) {
                        Vector3Int globalSampleCoord = GetGlobalSampleCoord(chunk.ChunkCoord, x, y, z);

                        if (sampleByGlobalCoord.ContainsKey(globalSampleCoord)) {
                            continue;
                        }

                        Vector3 sampleWorldPosition = GlobalSampleToWorldPosition(globalSampleCoord);
                        float baseDensity = DensityInitializer.EvaluateDensity(sampleWorldPosition, noisePreset);

                        sampleByGlobalCoord.Add(globalSampleCoord, new TerrainStitchSample {
                            globalSampleCoord = globalSampleCoord,
                            worldPosition = sampleWorldPosition,
                            baseDensity = baseDensity,
                            smoothDensity = baseDensity,
                            correction = 0f,
                            isBoundaryConstraint = false
                        });
                    }
                }
            }
        }
    }

    private int ApplyExteriorStitchBoundaryConstraints(List<CpuTerrainChunk> selectedChunkList, HashSet<Vector3Int> selectedChunkCoords, Dictionary<Vector3Int, TerrainStitchSample> sampleByGlobalCoord) {
        Dictionary<Vector3Int, float> boundaryDensitySumByGlobalSample = new Dictionary<Vector3Int, float>();
        Dictionary<Vector3Int, int> boundaryDensityCountByGlobalSample = new Dictionary<Vector3Int, int>();

        for (int i = 0; i < selectedChunkList.Count; i++) {
            CpuTerrainChunk chunk = selectedChunkList[i];
            DensityChunkData densityData = chunk.DensityData;

            for (int x = 0; x < densityData.sampleCount; x++) {
                for (int y = 0; y < densityData.sampleCount; y++) {
                    for (int z = 0; z < densityData.sampleCount; z++) {
                        Vector3Int globalSampleCoord = GetGlobalSampleCoord(chunk.ChunkCoord, x, y, z);

                        if (stitchSideBoundaries) {
                            if (x == 0) {
                                TryAddStitchBoundaryConstraint(globalSampleCoord, chunk.ChunkCoord + Vector3Int.left, selectedChunkCoords, cellCount, y, z, boundaryDensitySumByGlobalSample, boundaryDensityCountByGlobalSample);
                            }

                            if (x == cellCount) {
                                TryAddStitchBoundaryConstraint(globalSampleCoord, chunk.ChunkCoord + Vector3Int.right, selectedChunkCoords, 0, y, z, boundaryDensitySumByGlobalSample, boundaryDensityCountByGlobalSample);
                            }

                            if (z == 0) {
                                TryAddStitchBoundaryConstraint(globalSampleCoord, chunk.ChunkCoord + new Vector3Int(0, 0, -1), selectedChunkCoords, x, y, cellCount, boundaryDensitySumByGlobalSample, boundaryDensityCountByGlobalSample);
                            }

                            if (z == cellCount) {
                                TryAddStitchBoundaryConstraint(globalSampleCoord, chunk.ChunkCoord + new Vector3Int(0, 0, 1), selectedChunkCoords, x, y, 0, boundaryDensitySumByGlobalSample, boundaryDensityCountByGlobalSample);
                            }
                        }

                        if (stitchTopBottomBoundaries) {
                            if (y == 0) {
                                TryAddStitchBoundaryConstraint(globalSampleCoord, chunk.ChunkCoord + Vector3Int.down, selectedChunkCoords, x, cellCount, z, boundaryDensitySumByGlobalSample, boundaryDensityCountByGlobalSample);
                            }

                            if (y == cellCount) {
                                TryAddStitchBoundaryConstraint(globalSampleCoord, chunk.ChunkCoord + Vector3Int.up, selectedChunkCoords, x, 0, z, boundaryDensitySumByGlobalSample, boundaryDensityCountByGlobalSample);
                            }
                        }
                    }
                }
            }
        }

        int boundaryConstraintCount = 0;

        foreach (KeyValuePair<Vector3Int, float> entry in boundaryDensitySumByGlobalSample) {
            Vector3Int globalSampleCoord = entry.Key;

            if (!sampleByGlobalCoord.TryGetValue(globalSampleCoord, out TerrainStitchSample sample)) {
                continue;
            }

            int count = boundaryDensityCountByGlobalSample[globalSampleCoord];

            if (count <= 0) {
                continue;
            }

            float boundaryDensity = entry.Value / count;

            sample.smoothDensity = boundaryDensity;
            sample.correction = boundaryDensity - sample.baseDensity;
            sample.isBoundaryConstraint = true;

            boundaryConstraintCount++;
        }

        return boundaryConstraintCount;
    }

    private void TryAddStitchBoundaryConstraint(Vector3Int globalSampleCoord, Vector3Int exteriorChunkCoord, HashSet<Vector3Int> selectedChunkCoords, int exteriorSampleX, int exteriorSampleY, int exteriorSampleZ, Dictionary<Vector3Int, float> boundaryDensitySumByGlobalSample, Dictionary<Vector3Int, int> boundaryDensityCountByGlobalSample) {
        if (selectedChunkCoords.Contains(exteriorChunkCoord)) {
            return;
        }

        if (!chunks.TryGetValue(exteriorChunkCoord, out CpuTerrainChunk exteriorChunk)) {
            return;
        }

        if (exteriorChunk == null || exteriorChunk.DensityData == null) {
            return;
        }

        DensityChunkData exteriorDensityData = exteriorChunk.DensityData;

        if (exteriorSampleX < 0 || exteriorSampleX >= exteriorDensityData.sampleCount || exteriorSampleY < 0 || exteriorSampleY >= exteriorDensityData.sampleCount || exteriorSampleZ < 0 || exteriorSampleZ >= exteriorDensityData.sampleCount) {
            return;
        }

        float exteriorDensity = exteriorDensityData.Get(exteriorSampleX, exteriorSampleY, exteriorSampleZ);

        if (!boundaryDensitySumByGlobalSample.ContainsKey(globalSampleCoord)) {
            boundaryDensitySumByGlobalSample.Add(globalSampleCoord, 0f);
            boundaryDensityCountByGlobalSample.Add(globalSampleCoord, 0);
        }

        boundaryDensitySumByGlobalSample[globalSampleCoord] += exteriorDensity;
        boundaryDensityCountByGlobalSample[globalSampleCoord]++;
    }

    private void RunStitchRelaxation(Dictionary<Vector3Int, TerrainStitchSample> sampleByGlobalCoord) {
        int iterationCount = Mathf.Max(1, stitchIterations);
        Dictionary<Vector3Int, float> nextSmoothDensityByCoord = new Dictionary<Vector3Int, float>();
        Dictionary<Vector3Int, float> nextCorrectionByCoord = new Dictionary<Vector3Int, float>();

        for (int iteration = 0; iteration < iterationCount; iteration++) {
            nextSmoothDensityByCoord.Clear();
            nextCorrectionByCoord.Clear();

            foreach (KeyValuePair<Vector3Int, TerrainStitchSample> entry in sampleByGlobalCoord) {
                Vector3Int globalSampleCoord = entry.Key;
                TerrainStitchSample sample = entry.Value;

                if (sample.isBoundaryConstraint) {
                    nextSmoothDensityByCoord[globalSampleCoord] = sample.smoothDensity;
                    nextCorrectionByCoord[globalSampleCoord] = sample.correction;
                    continue;
                }

                float smoothDensitySum = 0f;
                float correctionSum = 0f;
                int neighborCount = 0;

                AddNeighborStitchValues(sampleByGlobalCoord, globalSampleCoord + Vector3Int.left, ref smoothDensitySum, ref correctionSum, ref neighborCount);
                AddNeighborStitchValues(sampleByGlobalCoord, globalSampleCoord + Vector3Int.right, ref smoothDensitySum, ref correctionSum, ref neighborCount);
                AddNeighborStitchValues(sampleByGlobalCoord, globalSampleCoord + Vector3Int.down, ref smoothDensitySum, ref correctionSum, ref neighborCount);
                AddNeighborStitchValues(sampleByGlobalCoord, globalSampleCoord + Vector3Int.up, ref smoothDensitySum, ref correctionSum, ref neighborCount);
                AddNeighborStitchValues(sampleByGlobalCoord, globalSampleCoord + new Vector3Int(0, 0, -1), ref smoothDensitySum, ref correctionSum, ref neighborCount);
                AddNeighborStitchValues(sampleByGlobalCoord, globalSampleCoord + new Vector3Int(0, 0, 1), ref smoothDensitySum, ref correctionSum, ref neighborCount);

                if (neighborCount <= 0) {
                    nextSmoothDensityByCoord[globalSampleCoord] = sample.smoothDensity;
                    nextCorrectionByCoord[globalSampleCoord] = sample.correction;
                    continue;
                }

                nextSmoothDensityByCoord[globalSampleCoord] = smoothDensitySum / neighborCount;
                nextCorrectionByCoord[globalSampleCoord] = correctionSum / neighborCount;
            }

            foreach (KeyValuePair<Vector3Int, TerrainStitchSample> entry in sampleByGlobalCoord) {
                TerrainStitchSample sample = entry.Value;

                sample.smoothDensity = nextSmoothDensityByCoord[entry.Key];
                sample.correction = nextCorrectionByCoord[entry.Key];
            }
        }
    }

    private void AddNeighborStitchValues(Dictionary<Vector3Int, TerrainStitchSample> sampleByGlobalCoord, Vector3Int neighborSampleCoord, ref float smoothDensitySum, ref float correctionSum, ref int neighborCount) {
        if (!sampleByGlobalCoord.TryGetValue(neighborSampleCoord, out TerrainStitchSample neighborSample)) {
            return;
        }

        smoothDensitySum += neighborSample.smoothDensity;
        correctionSum += neighborSample.correction;
        neighborCount++;
    }

    private void WriteStitchSamplesToChunks(List<CpuTerrainChunk> selectedChunkList, Dictionary<Vector3Int, TerrainStitchSample> sampleByGlobalCoord) {
        float noiseInfluence = Mathf.Clamp01(stitchNoiseInfluence);

        for (int i = 0; i < selectedChunkList.Count; i++) {
            CpuTerrainChunk chunk = selectedChunkList[i];
            DensityChunkData densityData = chunk.DensityData;

            for (int x = 0; x < densityData.sampleCount; x++) {
                for (int y = 0; y < densityData.sampleCount; y++) {
                    for (int z = 0; z < densityData.sampleCount; z++) {
                        Vector3Int globalSampleCoord = GetGlobalSampleCoord(chunk.ChunkCoord, x, y, z);

                        if (!sampleByGlobalCoord.TryGetValue(globalSampleCoord, out TerrainStitchSample sample)) {
                            continue;
                        }

                        float noiseDensity = sample.baseDensity + sample.correction;
                        float finalDensity = sample.isBoundaryConstraint ? sample.smoothDensity : Mathf.Lerp(sample.smoothDensity, noiseDensity, noiseInfluence);

                        densityData.Set(x, y, z, finalDensity);
                    }
                }
            }
        }
    }

    private Vector3Int GetGlobalSampleCoord(Vector3Int chunkCoord, int sampleX, int sampleY, int sampleZ) {
        return new Vector3Int(chunkCoord.x * cellCount + sampleX, chunkCoord.y * cellCount + sampleY, chunkCoord.z * cellCount + sampleZ);
    }

    private Vector3 GlobalSampleToWorldPosition(Vector3Int globalSampleCoord) {
        return new Vector3(globalSampleCoord.x * cellSize, globalSampleCoord.y * cellSize, globalSampleCoord.z * cellSize);
    }
}