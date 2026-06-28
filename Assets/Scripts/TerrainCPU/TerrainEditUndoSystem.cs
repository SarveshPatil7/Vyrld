using System.Collections.Generic;
using UnityEngine;

public class TerrainEditUndoSnapshot {
    private readonly Dictionary<Vector3Int, float[]> chunkDensitySnapshots = new();

    public IReadOnlyDictionary<Vector3Int, float[]> ChunkDensitySnapshots => chunkDensitySnapshots;
    public bool IsEmpty => chunkDensitySnapshots.Count == 0;

    public bool ContainsChunk(Vector3Int chunkCoord) {
        return chunkDensitySnapshots.ContainsKey(chunkCoord);
    }

    public void AddChunk(Vector3Int chunkCoord, float[] densitySnapshot) {
        if (densitySnapshot == null) {
            return;
        }

        chunkDensitySnapshots[chunkCoord] = densitySnapshot;
    }
}

public class TerrainEditUndoSystem {
    private readonly List<TerrainEditUndoSnapshot> undoStack = new();
    private readonly int maxUndoSteps;

    public int UndoCount => undoStack.Count;

    public TerrainEditUndoSystem(int maxUndoSteps) {
        this.maxUndoSteps = Mathf.Max(1, maxUndoSteps);
    }

    public void Push(TerrainEditUndoSnapshot snapshot) {
        if (snapshot == null || snapshot.IsEmpty) {
            return;
        }

        undoStack.Add(snapshot);

        while (undoStack.Count > maxUndoSteps) {
            undoStack.RemoveAt(0);
        }
    }

    public bool TryPop(out TerrainEditUndoSnapshot snapshot) {
        snapshot = null;

        if (undoStack.Count == 0) {
            return false;
        }

        int lastIndex = undoStack.Count - 1;
        snapshot = undoStack[lastIndex];
        undoStack.RemoveAt(lastIndex);
        return true;
    }

    public void Clear() {
        undoStack.Clear();
    }
}