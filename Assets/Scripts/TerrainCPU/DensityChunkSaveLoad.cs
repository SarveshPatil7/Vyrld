using System.IO;
using UnityEngine;

public static class DensityChunkSaveLoad {
    private const int SaveVersion = 1;
    private const string Header = "WOK_DENSITY_CHUNK";

    public static void Save(DensityChunkData data) {
        string path = GetChunkPath(data.chunkCoord);

        string directory = Path.GetDirectoryName(path);

        if (!Directory.Exists(directory)) {
            Directory.CreateDirectory(directory);
        }

        using BinaryWriter writer = new BinaryWriter(File.Open(path, FileMode.Create));

        writer.Write(Header);
        writer.Write(SaveVersion);

        writer.Write(data.chunkCoord.x);
        writer.Write(data.chunkCoord.y);
        writer.Write(data.chunkCoord.z);

        writer.Write(data.cellCount);
        writer.Write(data.cellSize);

        float[] raw = data.GetRawDensityArray();

        writer.Write(raw.Length);

        for (int i = 0; i < raw.Length; i++) {
            writer.Write(raw[i]);
        }

        Debug.Log($"Saved density chunk to: {path}");
    }

    public static DensityChunkData Load(Vector3Int chunkCoord, int expectedCellCount, float expectedCellSize) {
        string path = GetChunkPath(chunkCoord);

        if (!File.Exists(path)) {
            Debug.LogWarning($"No saved density chunk found at: {path}");
            return null;
        }

        using BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open));

        string header = reader.ReadString();

        if (header != Header) {
            Debug.LogError("Invalid density chunk file header.");
            return null;
        }

        int version = reader.ReadInt32();

        if (version != SaveVersion) {
            Debug.LogError($"Unsupported density save version: {version}");
            return null;
        }

        Vector3Int savedCoord = new Vector3Int(
            reader.ReadInt32(),
            reader.ReadInt32(),
            reader.ReadInt32()
        );

        int savedCellCount = reader.ReadInt32();
        float savedCellSize = reader.ReadSingle();

        if (savedCoord != chunkCoord) {
            Debug.LogWarning($"Saved coord {savedCoord} does not match requested coord {chunkCoord}.");
        }

        if (savedCellCount != expectedCellCount) {
            Debug.LogError($"Saved cell count {savedCellCount} does not match expected {expectedCellCount}.");
            return null;
        }

        if (Mathf.Abs(savedCellSize - expectedCellSize) > 0.0001f) {
            Debug.LogError($"Saved cell size {savedCellSize} does not match expected {expectedCellSize}.");
            return null;
        }

        DensityChunkData data = new DensityChunkData(savedCoord, savedCellCount, savedCellSize);

        int savedLength = reader.ReadInt32();
        float[] raw = data.GetRawDensityArray();

        if (savedLength != raw.Length) {
            Debug.LogError($"Saved density length {savedLength} does not match expected {raw.Length}.");
            return null;
        }

        for (int i = 0; i < raw.Length; i++) {
            raw[i] = reader.ReadSingle();
        }

        Debug.Log($"Loaded density chunk from: {path}");

        return data;
    }

    private static string GetChunkPath(Vector3Int chunkCoord) {
        string folder = Path.Combine(
            Application.persistentDataPath,
            "WorldOfKamish",
            "DensityChunks"
        );

        string fileName = $"chunk_{chunkCoord.x}_{chunkCoord.y}_{chunkCoord.z}.wokdensity";

        return Path.Combine(folder, fileName);
    }
}