using UnityEngine;

public class CpuTerrainEditTester : MonoBehaviour {
    public Camera targetCamera;
    public CpuTerrainChunk chunk;

    [Header("Edit Settings")]
    public float editRadius = 2.5f;
    public float editStrength = 4f;
    public float rayDistance = 200f;

    private void Update() {
        if (targetCamera == null || chunk == null) {
            return;
        }

        if (Input.GetMouseButtonDown(0)) {
            TryEdit(removeTerrain: true);
        }

        if (Input.GetMouseButtonDown(1)) {
            TryEdit(removeTerrain: false);
        }

        if (Input.GetKeyDown(KeyCode.S)) {
            chunk.SaveChunk();
        }

        if (Input.GetKeyDown(KeyCode.L)) {
            chunk.LoadChunk();
        }

        if (Input.GetKeyDown(KeyCode.R)) {
            chunk.ResetChunkToSeed();
        }

        if (targetCamera == null) {
            return;
        }
    }

    private void TryEdit(bool removeTerrain) {
        Ray ray = targetCamera.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, rayDistance)) {
            Debug.Log("Edit raycast did not hit terrain.");
            return;
        }

        float signedStrength = removeTerrain ? -editStrength : editStrength;

        chunk.ApplySphereEdit(hit.point, editRadius, signedStrength);
    }
}