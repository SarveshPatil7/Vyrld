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

    [Header("Terrain Regions")]
    [SerializeField] private int defaultRegionId = 0;
    [SerializeField]
    private List<TerrainRegionDefinition> terrainRegions = new List<TerrainRegionDefinition> {
    new TerrainRegionDefinition(0, "Default", 0, 8f, 8f, 0.06f),
    new TerrainRegionDefinition(1, "Flatlands", 1000, 7f, 2f, 0.035f),
    new TerrainRegionDefinition(2, "Mountains", 2000, 10f, 18f, 0.08f),
    new TerrainRegionDefinition(3, "Ocean", 3000, 1f, 4f, 0.045f)
    };

    [SerializeField] private int regionBlendSearchRadiusX = 1;
    [SerializeField] private int regionBlendSearchRadiusY = 1;
    [SerializeField] private int regionBlendSearchRadiusZ = 1;
    [SerializeField] private float regionBlendDistanceInChunks = 1.5f;

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
        return CreateChunk(chunkCoord, defaultRegionId);
    }

    private CpuTerrainChunk CreateChunk(Vector3Int chunkCoord, int regionId) {
        if (chunks.TryGetValue(chunkCoord, out CpuTerrainChunk existingChunk)) {
            return existingChunk;
        }

        if (chunkPrefab == null) {
            Debug.LogError("Chunk prefab is not assigned.");
            return null;
        }

        TerrainRegionDefinition terrainRegion = GetRegionById(regionId);

        CpuTerrainChunk chunk = Instantiate(chunkPrefab, transform);

        chunk.name = $"CPU_Terrain_Chunk_{chunkCoord.x}_{chunkCoord.y}_{chunkCoord.z}";
        chunks.Add(chunkCoord, chunk);

        chunk.SetSeedDensityEvaluator(EvaluateBlendedRegionDensity);
        chunk.Initialize(chunkCoord, cellCount, cellSize, seed, loadSavedChunksOnStart, worldName, terrainRegion);

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

    public List<CpuTerrainChunk> GenerateChunksAroundChunkCoord(Vector3Int centerChunkCoord, int chunkRadiusX, int chunkRadiusY, int chunkRadiusZ, int sourceRegionId) {
        List<CpuTerrainChunk> createdChunks = new List<CpuTerrainChunk>();

        for (int x = -chunkRadiusX; x <= chunkRadiusX; x++) {
            for (int y = -chunkRadiusY; y <= chunkRadiusY; y++) {
                for (int z = -chunkRadiusZ; z <= chunkRadiusZ; z++) {
                    Vector3Int chunkCoord = centerChunkCoord + new Vector3Int(x, y, z);

                    if (chunks.ContainsKey(chunkCoord)) {
                        continue;
                    }

                    CpuTerrainChunk createdChunk = CreateChunk(chunkCoord, sourceRegionId);

                    if (createdChunk != null) {
                        createdChunks.Add(createdChunk);
                    }
                }
            }
        }

        if (createdChunks.Count > 0) {
            TerrainRegionDefinition region = GetRegionById(sourceRegionId);
            string regionName = region != null ? region.GetDisplayName() : "Unknown";
            Debug.Log($"Generated {createdChunks.Count} new chunks around chunk {centerChunkCoord} using region {regionName}.");
        }

        return createdChunks;
    }

    public List<CpuTerrainChunk> GenerateChunksAroundChunkSelection(IEnumerable<CpuTerrainChunk> sourceChunks, int chunkRadiusX, int chunkRadiusY, int chunkRadiusZ) {
        List<CpuTerrainChunk> createdChunks = new List<CpuTerrainChunk>();

        if (sourceChunks == null) {
            Debug.LogWarning("Cannot generate chunks from selection. Source chunk collection is null.");
            return createdChunks;
        }

        List<CpuTerrainChunk> sourceChunkList = new List<CpuTerrainChunk>();

        foreach (CpuTerrainChunk sourceChunk in sourceChunks) {
            if (sourceChunk != null) {
                sourceChunkList.Add(sourceChunk);
            }
        }

        for (int i = 0; i < sourceChunkList.Count; i++) {
            CpuTerrainChunk sourceChunk = sourceChunkList[i];
            List<CpuTerrainChunk> newlyCreatedChunks = GenerateChunksAroundChunkCoord(sourceChunk.ChunkCoord, chunkRadiusX, chunkRadiusY, chunkRadiusZ, sourceChunk.RegionId);
            createdChunks.AddRange(newlyCreatedChunks);
        }

        Debug.Log($"Generated {createdChunks.Count} total chunks from selected chunk expansion.");
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

    public void ResetChunksToSeed(IEnumerable<CpuTerrainChunk> chunksToReset) {
        if (chunksToReset == null) {
            Debug.LogWarning("Cannot reset selected chunks. Chunk collection is null.");
            return;
        }

        HashSet<Vector3Int> chunkCoordsToReset = new HashSet<Vector3Int>();
        int selectedCount = 0;

        int safeSearchRadiusX = Mathf.Max(0, regionBlendSearchRadiusX);
        int safeSearchRadiusY = Mathf.Max(0, regionBlendSearchRadiusY);
        int safeSearchRadiusZ = Mathf.Max(0, regionBlendSearchRadiusZ);

        foreach (CpuTerrainChunk chunk in chunksToReset) {
            if (chunk == null) {
                continue;
            }

            selectedCount++;

            for (int x = -safeSearchRadiusX; x <= safeSearchRadiusX; x++) {
                for (int y = -safeSearchRadiusY; y <= safeSearchRadiusY; y++) {
                    for (int z = -safeSearchRadiusZ; z <= safeSearchRadiusZ; z++) {
                        Vector3Int resetCoord = chunk.ChunkCoord + new Vector3Int(x, y, z);
                        chunkCoordsToReset.Add(resetCoord);
                    }
                }
            }
        }

        int resetCount = 0;

        foreach (Vector3Int chunkCoord in chunkCoordsToReset) {
            if (!chunks.TryGetValue(chunkCoord, out CpuTerrainChunk chunk)) {
                continue;
            }

            if (chunk == null) {
                continue;
            }

            chunk.ResetChunkToSeed();
            resetCount++;
        }

        ClearUndoHistory();

        Debug.Log($"Reset {resetCount} CPU terrain chunks using blended regions from {selectedCount} selected chunks.");
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
        EnsureTerrainRegions();
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

    private void EnsureTerrainRegions() {
        if (terrainRegions == null) {
            terrainRegions = new List<TerrainRegionDefinition>();
        }

        if (terrainRegions.Count == 0) {
            terrainRegions.Add(TerrainRegionDefinition.CreateDefault());
        }
    }

    public int RegionCount {
        get {
            EnsureTerrainRegions();
            return terrainRegions.Count;
        }
    }

    public TerrainRegionDefinition GetRegionByIndex(int regionIndex) {
        EnsureTerrainRegions();

        if (terrainRegions.Count == 0) {
            return TerrainRegionDefinition.CreateDefault();
        }

        int safeIndex = Mathf.Clamp(regionIndex, 0, terrainRegions.Count - 1);
        return terrainRegions[safeIndex];
    }

    public TerrainRegionDefinition GetRegionById(int targetRegionId) {
        EnsureTerrainRegions();

        for (int i = 0; i < terrainRegions.Count; i++) {
            TerrainRegionDefinition region = terrainRegions[i];

            if (region != null && region.RegionId == targetRegionId) {
                return region;
            }
        }

        return terrainRegions[0] ?? TerrainRegionDefinition.CreateDefault();
    }

    public string GetRegionDisplayName(int regionIndex) {
        TerrainRegionDefinition region = GetRegionByIndex(regionIndex);
        return region != null ? region.GetDisplayName() : "None";
    }

    public void AssignRegionToChunks(IEnumerable<CpuTerrainChunk> chunksToAssign, int regionId) {
        if (chunksToAssign == null) {
            Debug.LogWarning("Cannot assign terrain region. Chunk collection is null.");
            return;
        }

        TerrainRegionDefinition terrainRegion = GetRegionById(regionId);
        int assignedCount = 0;

        foreach (CpuTerrainChunk chunk in chunksToAssign) {
            if (chunk == null) {
                continue;
            }

            chunk.SetTerrainRegion(terrainRegion, regenerate: false);
            assignedCount++;
        }

        Debug.Log($"Assigned terrain region {terrainRegion.GetDisplayName()} to {assignedCount} selected chunks.");
    }

    public float EvaluateBlendedRegionDensity(Vector3 worldPosition) {
        EnsureTerrainRegions();

        Vector3Int centerChunkCoord = WorldToChunkCoord(worldPosition);

        int safeSearchRadiusX = Mathf.Max(0, regionBlendSearchRadiusX);
        int safeSearchRadiusY = Mathf.Max(0, regionBlendSearchRadiusY);
        int safeSearchRadiusZ = Mathf.Max(0, regionBlendSearchRadiusZ);

        float chunkWorldSize = cellCount * cellSize;
        float blendDistance = Mathf.Max(0.001f, chunkWorldSize * regionBlendDistanceInChunks);

        float weightedDensitySum = 0f;
        float totalWeight = 0f;

        for (int x = -safeSearchRadiusX; x <= safeSearchRadiusX; x++) {
            for (int y = -safeSearchRadiusY; y <= safeSearchRadiusY; y++) {
                for (int z = -safeSearchRadiusZ; z <= safeSearchRadiusZ; z++) {
                    Vector3Int sampleChunkCoord = centerChunkCoord + new Vector3Int(x, y, z);

                    if (!chunks.TryGetValue(sampleChunkCoord, out CpuTerrainChunk chunk)) {
                        continue;
                    }

                    if (chunk == null) {
                        continue;
                    }

                    Vector3 chunkCenter = GetChunkWorldCenter(sampleChunkCoord);
                    float distance = Vector3.Distance(worldPosition, chunkCenter);

                    float weight = 1f - Mathf.Clamp01(distance / blendDistance);
                    weight = weight * weight * (3f - 2f * weight);

                    if (weight <= 0.0001f) {
                        continue;
                    }

                    TerrainRegionDefinition terrainRegion = GetRegionById(chunk.RegionId);
                    float density = DensityInitializer.EvaluateDensity(worldPosition, seed, terrainRegion);

                    weightedDensitySum += density * weight;
                    totalWeight += weight;
                }
            }
        }

        if (totalWeight <= 0.0001f) {
            TerrainRegionDefinition fallbackRegion = GetRegionById(defaultRegionId);
            return DensityInitializer.EvaluateDensity(worldPosition, seed, fallbackRegion);
        }

        return weightedDensitySum / totalWeight;
    }

    private Vector3 GetChunkWorldCenter(Vector3Int chunkCoord) {
        float chunkWorldSize = cellCount * cellSize;

        return new Vector3(
            chunkCoord.x * chunkWorldSize + chunkWorldSize * 0.5f,
            chunkCoord.y * chunkWorldSize + chunkWorldSize * 0.5f,
            chunkCoord.z * chunkWorldSize + chunkWorldSize * 0.5f
        );
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
}