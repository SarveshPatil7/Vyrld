using System;
using UnityEngine;

[Serializable]
public class TerrainWorldSaveMetadata {
    public int metadataVersion = 1;
    public string worldName;
    public int seed;
    public int cellCount;
    public float cellSize;
    public string createdUtc;
    public string lastSavedUtc;

    public TerrainWorldSaveMetadata(string worldName, int seed, int cellCount, float cellSize) {
        this.worldName = worldName;
        this.seed = seed;
        this.cellCount = cellCount;
        this.cellSize = cellSize;

        string now = DateTime.UtcNow.ToString("O");
        createdUtc = now;
        lastSavedUtc = now;
    }

    public void MarkSavedNow() {
        lastSavedUtc = DateTime.UtcNow.ToString("O");
    }
}