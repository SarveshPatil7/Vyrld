using UnityEngine;

public class CpuTerrainEditTester : MonoBehaviour {
    public Camera targetCamera;
    public CpuTerrainChunkManager chunkManager;

    [Header("Edit Settings")]
    public float editRadius = 6f;
    public float editStrength = 12f;
    public float rayDistance = 200f;

    private void Awake() {
        if (targetCamera == null) {
            targetCamera = Camera.main;
        }

        if (chunkManager == null) {
            chunkManager = FindAnyObjectByType<CpuTerrainChunkManager>();
        }
    }
    private void Update() {
        HandleKeyboardControls();
        HandleMouseEditing();
    }

    private void HandleKeyboardControls() {
        if (chunkManager == null) {
            return;
        }

        if (Input.GetKeyDown(KeyCode.S)) {
            chunkManager.SaveAllChunks();
        }

        if (Input.GetKeyDown(KeyCode.L)) {
            chunkManager.LoadAllChunks();
        }

        if (Input.GetKeyDown(KeyCode.R)) {
            chunkManager.ResetAllChunksToSeed();
        }
    }

    private void HandleMouseEditing() {
        if (targetCamera == null) {
            return;
        }

        if (Input.GetMouseButtonDown(0)) {
            TryEdit(removeTerrain: true);
        }

        if (Input.GetMouseButtonDown(1)) {
            TryEdit(removeTerrain: false);
        }
    }

    private void TryEdit(bool removeTerrain) {
        Ray ray = targetCamera.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, rayDistance)) {
            Debug.Log("Edit raycast did not hit terrain.");
            return;
        }

        if (chunkManager == null) {
            Debug.LogError("Cannot edit terrain. Chunk manager is not assigned.");
            return;
        }

        float signedStrength = removeTerrain ? -editStrength : editStrength;

        chunkManager.ApplySphereEdit(hit.point, editRadius, signedStrength);
    }
}