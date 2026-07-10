#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CpuTerrainChunkManager))]
public class CpuTerrainChunkManagerEditor : Editor {
    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        CpuTerrainChunkManager manager = (CpuTerrainChunkManager) target;

        EditorGUILayout.Space(12f);
        EditorGUILayout.LabelField("World Building", EditorStyles.boldLabel);

        if (GUILayout.Button("Load Saved World Into Scene")) {
            manager.LoadSavedWorldIntoScene();
            EditorUtility.SetDirty(manager);
        }

        if (GUILayout.Button("Generate Base Grid Into Scene")) {
            bool confirmed = EditorUtility.DisplayDialog("Generate Base Grid", "This will clear existing visible terrain chunks and generate a new base grid. Continue?", "Generate", "Cancel");

            if (confirmed) {
                manager.GenerateBaseGridIntoScene();
                EditorUtility.SetDirty(manager);
            }
        }

        if (GUILayout.Button("Register Existing Scene Chunks")) {
            manager.RegisterExistingSceneChunksForEditing();
            EditorUtility.SetDirty(manager);
        }

        if (GUILayout.Button("Clear Terrain Chunks From Scene")) {
            bool confirmed = EditorUtility.DisplayDialog("Clear Terrain Chunks", "This will remove visible terrain chunk GameObjects from the scene. Saved density files will not be deleted. Continue?", "Clear", "Cancel");

            if (confirmed) {
                manager.ClearTerrainChunksFromScene();
                EditorUtility.SetDirty(manager);
            }
        }
    }
}
#endif