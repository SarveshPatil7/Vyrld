using UnityEngine;

public class CpuTerrainDebugOverlay : MonoBehaviour {
    public CpuTerrainChunkManager chunkManager;
    public CpuTerrainEditTester editTester;
    public bool showOverlay = true;

    private void Awake() {
        if (chunkManager == null) {
            chunkManager = FindAnyObjectByType<CpuTerrainChunkManager>();
        }

        if (editTester == null) {
            editTester = FindAnyObjectByType<CpuTerrainEditTester>();
        }
    }

    private void OnGUI() {
        if (!showOverlay) {
            return;
        }

        float width = 420f;
        float height = 285f;

        GUILayout.BeginArea(new Rect(15f, 15f, width, height), GUI.skin.box);

        GUILayout.Label("CPU Terrain Dev Controls");
        GUILayout.Space(6f);

        GUILayout.Label("Left Click: Remove terrain");
        GUILayout.Label("Right Click: Add terrain");
        GUILayout.Label("Mouse Wheel: Adjust edit radius");
        GUILayout.Label("- / =: Decrease / increase edit strength");
        GUILayout.Label("1: SmoothSphere");
        GUILayout.Label("2: HardSphere");
        GUILayout.Label("3: RoughSphere");
        GUILayout.Label("4: Flatten");
        GUILayout.Label("5: Toggle flatten mode");
        GUILayout.Label("S: Save loaded chunks");
        GUILayout.Label("L: Load saved chunks");
        GUILayout.Label("R: Reset visible chunks to seed");

        GUILayout.Space(6f);

        if (editTester != null) {
            GUILayout.Label($"Brush: {editTester.brushType}");
            GUILayout.Label($"Flatten Mode: {editTester.flattenMode}");
            GUILayout.Label($"Edit Radius: {editTester.editRadius}");
            GUILayout.Label($"Edit Strength: {editTester.editStrength}");
            GUILayout.Label($"Roughness Scale: {editTester.roughnessScale}");
            GUILayout.Label($"Roughness Amount: {editTester.roughnessAmount}");
        }

        if (chunkManager != null) {
            GUILayout.Label($"Chunk Radius: X {chunkManager.radiusX}, Y {chunkManager.radiusY}, Z {chunkManager.radiusZ}");
            GUILayout.Label($"Auto-load Saved: {chunkManager.loadSavedChunksOnStart}");
        }

        GUILayout.EndArea();
    }
}