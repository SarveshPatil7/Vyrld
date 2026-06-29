using UnityEngine;

public class TerraformTool : TerrainModeTool {
    public override TerrainInteractionMode Mode => TerrainInteractionMode.Terraform;

    [SerializeField] private Camera targetCamera;
    [SerializeField] private CpuTerrainChunkManager chunkManager;

    [Header("Edit Settings")]
    [SerializeField] private float editRadius = 6f;
    [SerializeField] private float editStrength = 12f;
    [SerializeField] private float rayDistance = 200f;

    [Header("Edit Adjustment")]
    [SerializeField] private float radiusScrollStep = 0.5f;
    [SerializeField] private float strengthStep = 1f;
    [SerializeField] private float minEditRadius = 0.5f;
    [SerializeField] private float maxEditRadius = 20f;
    [SerializeField] private float minEditStrength = 0.5f;
    [SerializeField] private float maxEditStrength = 50f;

    [Header("Continuous Editing")]
    [SerializeField] private bool continuousEditing = true;
    [SerializeField] private float editsPerSecond = 12f;
    private float nextEditTime = 0f;
    private TerrainEditUndoSnapshot activeStrokeUndoSnapshot;
    private bool isUndoStrokeActive;
    private bool activeStrokeHadSuccessfulEdit;

    [Header("Brush Preview")]
    [SerializeField] private bool showBrushPreview = true;
    [SerializeField] private Color brushPreviewColor = new Color(1f, 1f, 1f, 0.25f);

    private bool hasBrushHit;
    private Vector3 brushHitPoint;

    private TerrainBrushPreview brushPreview;

    [Header("Brush")]
    [SerializeField] private CpuTerrainBrushType brushType = CpuTerrainBrushType.SmoothSphere;

    [Header("Rough Brush")]
    [SerializeField] private float roughnessScale = 3.0f;
    [Range(0f, 5f)]
    [SerializeField] private float roughnessAmount = 2.0f;

    [Header("Flatten Brush")]
    [SerializeField] private CpuTerrainFlattenMode flattenMode = CpuTerrainFlattenMode.Horizontal;

    private void Awake() {
        if (targetCamera == null) {
            targetCamera = Camera.main;
        }

        if (chunkManager == null) {
            chunkManager = FindAnyObjectByType<CpuTerrainChunkManager>();
        }

        brushPreview = new TerrainBrushPreview();
        brushPreview.Initialize(brushPreviewColor);
    }
    private void Update() {
        HandleKeyboardControls();
        HandleEditAdjustmentControls();
        UpdateBrushPreview();
        UpdateBrushPreviewVisual();
        HandleMouseEditing();
    }

    private void UpdateBrushPreview() {
        hasBrushHit = false;

        if (targetCamera == null) {
            return;
        }

        Ray ray = targetCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance)) {
            hasBrushHit = true;
            brushHitPoint = hit.point;
        }
    }

    private void HandleKeyboardControls() {
        HandleBrushSwitchingControls();

        if (chunkManager == null) {
            return;
        }

        HandleUndoControls();

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

    private void HandleBrushSwitchingControls() {
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) {
            SetBrushType(CpuTerrainBrushType.SmoothSphere);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) {
            SetBrushType(CpuTerrainBrushType.HardSphere);
        }

        if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) {
            SetBrushType(CpuTerrainBrushType.RoughSphere);
        }

        if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) {
            SetBrushType(CpuTerrainBrushType.Flatten);
        }

        if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5)) {
            ToggleFlattenMode();
        }
    }

    private void HandleUndoControls() {
        bool controlHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        if (controlHeld && Input.GetKeyDown(KeyCode.Z)) {
            EndUndoStroke();
            chunkManager.UndoLastEdit();
        }
    }

    private void SetBrushType(CpuTerrainBrushType newBrushType) {
        brushType = newBrushType;
        Debug.Log($"Brush type: {brushType}");
    }

    private void ToggleFlattenMode() {
        flattenMode = flattenMode == CpuTerrainFlattenMode.Horizontal ? CpuTerrainFlattenMode.AveragePlane : CpuTerrainFlattenMode.Horizontal;
        Debug.Log($"Flatten mode: {flattenMode}");
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
        bool removeHeld = Input.GetMouseButton(0);
        bool addHeld = Input.GetMouseButton(1);

        if (!removeHeld && !addHeld) {
            EndUndoStroke();
            return;
        }

        BeginUndoStroke();

        if (Time.time < nextEditTime) {
            return;
        }

        float editInterval = 1f / Mathf.Max(1f, editsPerSecond);

        if (removeHeld) {
            if (TryEdit(removeTerrain: true)) {
                activeStrokeHadSuccessfulEdit = true;
                nextEditTime = Time.time + editInterval;
            }
        }
        else if (addHeld) {
            if (TryEdit(removeTerrain: false)) {
                activeStrokeHadSuccessfulEdit = true;
                nextEditTime = Time.time + editInterval;
            }
        }
    }

    private void HandleSingleClickMouseEditing() {
        if (Input.GetMouseButtonDown(0)) {
            BeginUndoStroke();

            if (TryEdit(removeTerrain: true)) {
                activeStrokeHadSuccessfulEdit = true;
            }

            EndUndoStroke();
        }

        if (Input.GetMouseButtonDown(1)) {
            BeginUndoStroke();

            if (TryEdit(removeTerrain: false)) {
                activeStrokeHadSuccessfulEdit = true;
            }

            EndUndoStroke();
        }
    }

    private bool TryEdit(bool removeTerrain) {
        Ray ray = targetCamera.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, rayDistance)) {
            Debug.Log("Edit raycast did not hit terrain.");
            return false;
        }

        if (chunkManager == null) {
            Debug.LogError("Cannot edit terrain. Chunk manager is not assigned.");
            return false;
        }

        if (activeStrokeUndoSnapshot != null) {
            chunkManager.AddBrushAreaToUndoSnapshot(activeStrokeUndoSnapshot, hit.point, editRadius);
        }

        float signedStrength = removeTerrain ? -editStrength : editStrength;

        chunkManager.ApplyBrushEdit(hit.point, editRadius, signedStrength, brushType, flattenMode, roughnessScale, roughnessAmount);
        return true;
    }

    private void BeginUndoStroke() {
        if (isUndoStrokeActive) {
            return;
        }

        activeStrokeUndoSnapshot = new TerrainEditUndoSnapshot();
        activeStrokeHadSuccessfulEdit = false;
        isUndoStrokeActive = true;
    }

    private void EndUndoStroke() {
        if (!isUndoStrokeActive) {
            return;
        }

        if (chunkManager != null && activeStrokeHadSuccessfulEdit && activeStrokeUndoSnapshot != null && !activeStrokeUndoSnapshot.IsEmpty) {
            chunkManager.PushUndoSnapshot(activeStrokeUndoSnapshot);
        }

        activeStrokeUndoSnapshot = null;
        activeStrokeHadSuccessfulEdit = false;
        isUndoStrokeActive = false;
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

    private void UpdateBrushPreviewVisual() {
        if (brushPreview == null) {
            return;
        }

        bool shouldShow = showBrushPreview && hasBrushHit;
        brushPreview.UpdatePreview(shouldShow, brushHitPoint, editRadius, brushType, brushPreviewColor);
    }

    private void OnDestroy() {
        if (brushPreview != null) {
            brushPreview.Dispose();
            brushPreview = null;
        }
    }

    public override void EnterMode() {
        base.EnterMode();
    }

    public override void ExitMode() {
        EndUndoStroke();

        if (brushPreview != null) {
            brushPreview.Hide();
        }

        base.ExitMode();
    }

}