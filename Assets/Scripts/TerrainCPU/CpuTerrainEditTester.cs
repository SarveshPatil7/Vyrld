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

    [Header("Brush Preview")]
    public bool showBrushPreview = true;
    public Color brushPreviewColor = new Color(1f, 1f, 1f, 0.25f);

    private bool hasBrushHit;
    private Vector3 brushHitPoint;

    private GameObject brushPreviewObject;
    private MeshRenderer brushPreviewRenderer;

    private void Awake() {
        if (targetCamera == null) {
            targetCamera = Camera.main;
        }

        if (chunkManager == null) {
            chunkManager = FindAnyObjectByType<CpuTerrainChunkManager>();
        }

        CreateBrushPreview();
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

    private void CreateBrushPreview() {
        brushPreviewObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        brushPreviewObject.name = "CPU Terrain Brush Preview";

        Collider previewCollider = brushPreviewObject.GetComponent<Collider>();
        if (previewCollider != null) {
            Destroy(previewCollider);
        }

        brushPreviewRenderer = brushPreviewObject.GetComponent<MeshRenderer>();

        Shader previewShader = Shader.Find("Universal Render Pipeline/Unlit");

        if (previewShader == null) {
            previewShader = Shader.Find("Unlit/Transparent");
        }

        if (previewShader == null) {
            previewShader = Shader.Find("Sprites/Default");
        }

        Material previewMaterial = new Material(previewShader);

        SetBrushPreviewMaterialColor(previewMaterial, brushPreviewColor);

        previewMaterial.SetInt("_SrcBlend", (int) UnityEngine.Rendering.BlendMode.SrcAlpha);
        previewMaterial.SetInt("_DstBlend", (int) UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        previewMaterial.SetInt("_ZWrite", 0);
        previewMaterial.renderQueue = 3000;

        if (previewMaterial.HasProperty("_Surface")) {
            previewMaterial.SetFloat("_Surface", 1f);
        }

        brushPreviewRenderer.material = previewMaterial;

        brushPreviewObject.SetActive(false);
    }

    private void SetBrushPreviewMaterialColor(Material material, Color color) {
        if (material == null) {
            return;
        }

        if (material.HasProperty("_BaseColor")) {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color")) {
            material.SetColor("_Color", color);
        }

        material.color = color;
    }

    private void UpdateBrushPreviewVisual() {
        bool shouldShow = showBrushPreview && hasBrushHit;

        if (brushPreviewObject == null) {
            return;
        }

        brushPreviewObject.SetActive(shouldShow);

        if (!shouldShow) {
            return;
        }

        brushPreviewObject.transform.position = brushHitPoint;
        brushPreviewObject.transform.localScale = Vector3.one * editRadius * 2f;

        if (brushPreviewRenderer != null) {
            SetBrushPreviewMaterialColor(brushPreviewRenderer.material, brushPreviewColor);
        }
    }
}