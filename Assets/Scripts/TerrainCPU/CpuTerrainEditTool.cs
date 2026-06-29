using UnityEngine;

public class CpuTerrainEditTool : MonoBehaviour {
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

    private GameObject brushPreviewObject;
    private MeshFilter brushPreviewFilter;
    private MeshRenderer brushPreviewRenderer;
    private Mesh spherePreviewMesh;
    private Mesh diskPreviewMesh;

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

    private void CreateBrushPreview() {
        brushPreviewObject = new GameObject("CPU Terrain Brush Preview");

        brushPreviewFilter = brushPreviewObject.AddComponent<MeshFilter>();
        brushPreviewRenderer = brushPreviewObject.AddComponent<MeshRenderer>();

        spherePreviewMesh = CreateSpherePreviewMesh();
        diskPreviewMesh = CreateDiskPreviewMesh(64);

        brushPreviewFilter.sharedMesh = spherePreviewMesh;

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

        if (previewMaterial.HasProperty("_Cull")) {
            previewMaterial.SetInt("_Cull", (int) UnityEngine.Rendering.CullMode.Off);
        }

        brushPreviewRenderer.material = previewMaterial;
        brushPreviewObject.SetActive(false);

    }

    private Mesh CreateSpherePreviewMesh() {
        GameObject temporarySphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Mesh sourceMesh = temporarySphere.GetComponent<MeshFilter>().sharedMesh;
        Mesh previewMesh = Instantiate(sourceMesh);
        previewMesh.name = "CPU Terrain Brush Sphere Preview Mesh";
        Destroy(temporarySphere);
        return previewMesh;
    }

    private Mesh CreateDiskPreviewMesh(int segmentCount) {
        segmentCount = Mathf.Max(8, segmentCount);

        Vector3[] vertices = new Vector3[segmentCount + 1];
        Vector3[] normals = new Vector3[segmentCount + 1];
        Vector2[] uvs = new Vector2[segmentCount + 1];
        int[] triangles = new int[segmentCount * 6];

        vertices[0] = Vector3.zero;
        normals[0] = Vector3.up;
        uvs[0] = new Vector2(0.5f, 0.5f);

        for (int i = 0; i < segmentCount; i++) {
            float angle = (float) i / segmentCount * Mathf.PI * 2f;
            float x = Mathf.Cos(angle);
            float z = Mathf.Sin(angle);

            vertices[i + 1] = new Vector3(x, 0f, z);
            normals[i + 1] = Vector3.up;
            uvs[i + 1] = new Vector2(x * 0.5f + 0.5f, z * 0.5f + 0.5f);
        }

        int triangleIndex = 0;

        for (int i = 0; i < segmentCount; i++) {
            int current = i + 1;
            int next = i == segmentCount - 1 ? 1 : i + 2;

            triangles[triangleIndex++] = 0;
            triangles[triangleIndex++] = current;
            triangles[triangleIndex++] = next;

            triangles[triangleIndex++] = 0;
            triangles[triangleIndex++] = next;
            triangles[triangleIndex++] = current;
        }

        Mesh mesh = new Mesh();
        mesh.name = "CPU Terrain Brush Disk Preview Mesh";
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();

        return mesh;
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

        UpdateBrushPreviewMesh();

        if (brushType == CpuTerrainBrushType.Flatten) {
            brushPreviewObject.transform.position = brushHitPoint;
            brushPreviewObject.transform.rotation = Quaternion.identity;
            brushPreviewObject.transform.localScale = Vector3.one * editRadius;
        }
        else {
            brushPreviewObject.transform.position = brushHitPoint;
            brushPreviewObject.transform.rotation = Quaternion.identity;
            brushPreviewObject.transform.localScale = Vector3.one * editRadius * 2f;
        }

        if (brushPreviewRenderer != null) {
            SetBrushPreviewMaterialColor(brushPreviewRenderer.material, brushPreviewColor);
        }
    }

    private void UpdateBrushPreviewMesh() {
        if (brushPreviewFilter == null) {
            return;
        }

        Mesh targetMesh = brushType == CpuTerrainBrushType.Flatten ? diskPreviewMesh : spherePreviewMesh;

        if (brushPreviewFilter.sharedMesh != targetMesh) {
            brushPreviewFilter.sharedMesh = targetMesh;
        }
    }

}