using UnityEngine;

[System.Serializable]
public class DensityChunkData {
    public readonly int cellCount;
    public readonly int sampleCount;
    public readonly float cellSize;
    public readonly Vector3Int chunkCoord;

    private readonly float[] densities;

    public DensityChunkData(Vector3Int chunkCoord, int cellCount, float cellSize) {
        this.chunkCoord = chunkCoord;
        this.cellCount = cellCount;
        this.cellSize = cellSize;

        sampleCount = cellCount + 1;
        densities = new float[sampleCount * sampleCount * sampleCount];
    }

    public float Get(int x, int y, int z) {
        return densities[Index(x, y, z)];
    }

    public void Set(int x, int y, int z, float value) {
        densities[Index(x, y, z)] = value;
    }

    public float[] GetRawDensityArray() {
        return densities;
    }

    private int Index(int x, int y, int z) {
        return x + sampleCount * (y + sampleCount * z);
    }

    public Vector3 SampleToWorldPosition(int x, int y, int z) {
        float worldX = (chunkCoord.x * cellCount + x) * cellSize;
        float worldY = (chunkCoord.y * cellCount + y) * cellSize;
        float worldZ = (chunkCoord.z * cellCount + z) * cellSize;

        return new Vector3(worldX, worldY, worldZ);
    }

    public Vector3 SampleToLocalPosition(int x, int y, int z) {
        return new Vector3(x * cellSize, y * cellSize, z * cellSize);
    }
}