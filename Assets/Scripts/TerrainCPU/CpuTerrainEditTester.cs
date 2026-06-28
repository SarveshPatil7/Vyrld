using UnityEngine;

public class CpuTerrainEditTester : MonoBehaviour {
    public Camera targetCamera;
    public CpuTerrainChunkManager chunkManager;

    [Header("Edit Settings")]
    public float editRadius = 6f;
    public float editStrength = 12f;
    public float rayDistance = 200f;

    [Header("Edit Adjustment")]
    public float radiusScrollStep = 0.5f;
    public float strengthStep = 1f;
    public float minEditRadius = 0.5f;
    public float maxEditRadius = 20f;
    public float minEditStrength = 0.5f;
    public float maxEditStrength = 50f;

    [Header("Continuous Editing")]
    public bool continuousEditing = true;
    public float editsPerSecond = 12f;

    private float nextEditTime = 0f;

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
        HandleEditAdjustmentControls();
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

        if (continuousEditing) {
            HandleContinuousMouseEditing();
        }
        else {
            HandleSingleClickMouseEditing();
        }
    }

    private void HandleContinuousMouseEditing() {
        if (Time.time < nextEditTime) {
            return;
        }

        float editInterval = 1f / Mathf.Max(1f, editsPerSecond);

        if (Input.GetMouseButton(0)) {
            TryEdit(removeTerrain: true);
            nextEditTime = Time.time + editInterval;
        }
        else if (Input.GetMouseButton(1)) {
            TryEdit(removeTerrain: false);
            nextEditTime = Time.time + editInterval;
        }
    }

    private void HandleSingleClickMouseEditing() {
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

    private void HandleEditAdjustmentControls() {
        float scroll = Input.mouseScrollDelta.y;

        if (Mathf.Abs(scroll) > 0.01f) {
            editRadius += scroll * radiusScrollStep;
            editRadius = Mathf.Clamp(editRadius, minEditRadius, maxEditRadius);
            Debug.Log($"Edit radius: {editRadius}");
        }

        if (Input.GetKeyDown(KeyCode.Minus)) {
            editStrength -= strengthStep;
            editStrength = Mathf.Clamp(editStrength, minEditStrength, maxEditStrength);
            Debug.Log($"Edit strength: {editStrength}");
        }

        if (Input.GetKeyDown(KeyCode.Equals)) {
            editStrength += strengthStep;
            editStrength = Mathf.Clamp(editStrength, minEditStrength, maxEditStrength);
            Debug.Log($"Edit strength: {editStrength}");
        }
    }
}