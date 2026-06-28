using System.Collections.Generic;
using UnityEngine;

public class CpuTerrainChunkManager : MonoBehaviour {
    [Header("Chunk Prefab")]
    public CpuTerrainChunk chunkPrefab;

    [Header("Generation Settings")]
    public int cellCount = 16;
    public float cellSize = 1f;
    public int seed = 12345;

    [Header("Test Grid")]
    public int radiusX = 1;
    public int radiusY = 1;
    public int radiusZ = 1;

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
        chunk.Initialize(chunkCoord, cellCount, cellSize, seed);

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
        foreach (CpuTerrainChunk chunk in chunks.Values) {
            if (chunk != null) {
                chunk.SaveChunk();
            }
        }

        Debug.Log($"Saved {chunks.Count} CPU terrain chunks.");
    }

    public void LoadAllChunks() {
        foreach (CpuTerrainChunk chunk in chunks.Values) {
            if (chunk != null) {
                chunk.LoadChunk();
            }
        }

        Debug.Log($"Loaded {chunks.Count} CPU terrain chunks.");
    }

    public void ResetAllChunksToSeed() {
        foreach (CpuTerrainChunk chunk in chunks.Values) {
            if (chunk != null) {
                chunk.ResetChunkToSeed();
            }
        }

        Debug.Log($"Reset {chunks.Count} CPU terrain chunks to seed default.");
    }

    public void ApplySphereEdit(Vector3 worldCenter, float radius, float strength) {
        int editedChunkCount = 0;

        foreach (CpuTerrainChunk chunk in chunks.Values) {
            if (chunk == null) {
                continue;
            }

            if (!DoesSphereOverlapChunk(worldCenter, radius, chunk)) {
                continue;
            }

            chunk.ApplySphereEdit(worldCenter, radius, strength);
            editedChunkCount++;
        }

        Debug.Log($"Applied sphere edit to {editedChunkCount} chunks.");
    }

    private bool DoesSphereOverlapChunk(Vector3 worldCenter, float radius, CpuTerrainChunk chunk) {
        Vector3 chunkMin = new Vector3(
        chunk.chunkCoord.x * cellCount * cellSize,
        chunk.chunkCoord.y * cellCount * cellSize,
        chunk.chunkCoord.z * cellCount * cellSize
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
}