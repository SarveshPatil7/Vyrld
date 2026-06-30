using System.IO;
using UnityEngine;
using System.Collections.Generic;

public static class DensityChunkSaveLoad {
    private const int SaveVersion = 1;
    private const string Header = "WOK_DENSITY_CHUNK";
    private const string RootFolderName = "WorldOfKamish";
    private const string SavesFolderName = "Saves";
    private const string DensityChunksFolderName = "DensityChunks";
    private const string MetadataFileName = "world_metadata.json";

    public static void Save(DensityChunkData data, string worldName) {
        string path = GetChunkPath(worldName, data.chunkCoord);
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

    public static DensityChunkData Load(string worldName, Vector3Int chunkCoord, int expectedCellCount, float expectedCellSize) {
        string path = GetChunkPath(worldName, chunkCoord);

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

        Vector3Int savedCoord = new Vector3Int(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
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

    public static bool SaveExists(string worldName, Vector3Int chunkCoord) {
        string path = GetChunkPath(worldName, chunkCoord);
        return File.Exists(path);
    }

    public static void SaveMetadata(TerrainWorldSaveMetadata metadata) {
        if (metadata == null) {
            Debug.LogError("Cannot save terrain world metadata. Metadata is null.");
            return;
        }

        string path = GetMetadataPath(metadata.worldName);
        string directory = Path.GetDirectoryName(path);

        if (!Directory.Exists(directory)) {
            Directory.CreateDirectory(directory);
        }

        metadata.MarkSavedNow();

        string json = JsonUtility.ToJson(metadata, true);
        File.WriteAllText(path, json);

        Debug.Log($"Saved terrain world metadata to: {path}");
    }

    public static TerrainWorldSaveMetadata LoadMetadata(string worldName) {
        string path = GetMetadataPath(worldName);

        if (!File.Exists(path)) {
            return null;
        }

        string json = File.ReadAllText(path);
        return JsonUtility.FromJson<TerrainWorldSaveMetadata>(json);
    }

    public static List<Vector3Int> GetSavedChunkCoords(string worldName) {
        List<Vector3Int> savedChunkCoords = new List<Vector3Int>();
        string folder = Path.Combine(GetWorldFolder(worldName), DensityChunksFolderName);

        if (!Directory.Exists(folder)) {
            return savedChunkCoords;
        }

        string[] files = Directory.GetFiles(folder, "chunk_*_*_*.wokdensity");

        for (int i = 0; i < files.Length; i++) {
            string fileName = Path.GetFileNameWithoutExtension(files[i]);
            string[] parts = fileName.Split('_');

            if (parts.Length != 4) {
                continue;
            }

            if (!int.TryParse(parts[1], out int x)) {
                continue;
            }

            if (!int.TryParse(parts[2], out int y)) {
                continue;
            }

            if (!int.TryParse(parts[3], out int z)) {
                continue;
            }

            savedChunkCoords.Add(new Vector3Int(x, y, z));
        }

        return savedChunkCoords;
    }

    public static string GetWorldFolder(string worldName) {
        string safeWorldName = GetSafeWorldName(worldName);

        return Path.Combine(GetSaveRootFolder(), RootFolderName, SavesFolderName, safeWorldName);
    }

    private static string GetSaveRootFolder() {
    #if UNITY_EDITOR
        DirectoryInfo assetsDirectory = Directory.GetParent(Application.dataPath);

        if (assetsDirectory != null) {
            return assetsDirectory.FullName;
        }

        return Application.dataPath;
        #else
    return Application.persistentDataPath;
    #endif
    }

    private static string GetChunkPath(string worldName, Vector3Int chunkCoord) {
        string folder = Path.Combine(GetWorldFolder(worldName), DensityChunksFolderName);
        string fileName = $"chunk_{chunkCoord.x}_{chunkCoord.y}_{chunkCoord.z}.wokdensity";
        return Path.Combine(folder, fileName);
    }

    private static string GetMetadataPath(string worldName) {
        return Path.Combine(GetWorldFolder(worldName), MetadataFileName);
    }

    private static string GetSafeWorldName(string worldName) {
        if (string.IsNullOrWhiteSpace(worldName)) {
            return "DevWorld";
        }

        foreach (char invalidChar in Path.GetInvalidFileNameChars()) {
            worldName = worldName.Replace(invalidChar, '_');
        }

        return worldName.Trim();
    }
}