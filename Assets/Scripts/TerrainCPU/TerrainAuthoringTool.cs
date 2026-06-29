using System.Collections.Generic;
using UnityEngine;

public class TerrainAuthoringTool : TerrainModeTool {
    public override TerrainInteractionMode Mode => TerrainInteractionMode.Authoring;

    [Header("Camera")]
    [SerializeField] private Camera targetCamera;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 20f;
    [SerializeField] private float fastMoveMultiplier = 4f;
    [SerializeField] private float verticalMoveSpeed = 15f;

    [Header("Look")]
    [SerializeField] private float lookSensitivity = 2f;
    [SerializeField] private bool holdRightMouseToLook = true;

    [Header("Chunk Selection")]
    [SerializeField] private LayerMask terrainRaycastMask = ~0;
    [SerializeField] private float rayDistance = 500f;
    [SerializeField] private Color hoverColor = new Color(1f, 1f, 0f, 0.8f);
    [SerializeField] private Color selectedColor = new Color(0f, 1f, 1f, 0.8f);
    [SerializeField] private bool showAuthoringOverlay = true;

    [Header("Chunk Selection Visuals")]
    [SerializeField] private bool showSelectionInGameView = true;
    [SerializeField] private float selectionLineWidth = 0.05f;

    [Header("Terrain")]
    [SerializeField] private CpuTerrainChunkManager chunkManager;

    private TerrainChunkBoxVisual hoverBoxVisual;
    private readonly Dictionary<CpuTerrainChunk, TerrainChunkBoxVisual> selectedBoxVisuals = new();

    private CpuTerrainChunk hoveredChunk;
    private readonly HashSet<CpuTerrainChunk> selectedChunks = new();

    private float yaw;
    private float pitch;

    private void Awake() {
        if (targetCamera == null) {
            targetCamera = Camera.main;
        }

        if (targetCamera != null) {
            Vector3 currentEuler = targetCamera.transform.eulerAngles;
            yaw = currentEuler.y;
            pitch = currentEuler.x;
        }

        if (chunkManager == null) {
            chunkManager = FindAnyObjectByType<CpuTerrainChunkManager>();
        }

        hoverBoxVisual = new TerrainChunkBoxVisual("Hovered Terrain Chunk", hoverColor, selectionLineWidth);
    }

    private void Update() {
        if (targetCamera == null) {
            return;
        }

        HandleLook();
        HandleMovement();
        UpdateHoveredChunk();
        HandleChunkSelection();
        HandleSelectedChunkOperations();
        UpdateSelectionVisuals();
    }

    public override void EnterMode() {
        base.EnterMode();

        if (targetCamera == null) {
            targetCamera = Camera.main;
        }

        if (targetCamera != null) {
            Vector3 currentEuler = targetCamera.transform.eulerAngles;
            yaw = currentEuler.y;
            pitch = currentEuler.x;
        }

        Debug.Log("Entered Terrain Authoring mode.");
    }

    public override void ExitMode() {
        UnlockCursor();
        Debug.Log("Exited Terrain Authoring mode.");
        HideSelectionVisuals();
        hoveredChunk = null;
        base.ExitMode();
    }

    private void HandleLook() {
        bool canLook = !holdRightMouseToLook || Input.GetMouseButton(1);

        if (!canLook) {
            UnlockCursor();
            return;
        }

        LockCursor();

        float mouseX = Input.GetAxis("Mouse X") * lookSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * lookSensitivity;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -89f, 89f);

        targetCamera.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void HandleMovement() {
        float speed = moveSpeed;

        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) {
            speed *= fastMoveMultiplier;
        }

        Vector3 moveDirection = Vector3.zero;

        if (Input.GetKey(KeyCode.W)) {
            moveDirection += targetCamera.transform.forward;
        }

        if (Input.GetKey(KeyCode.S)) {
            moveDirection -= targetCamera.transform.forward;
        }

        if (Input.GetKey(KeyCode.D)) {
            moveDirection += targetCamera.transform.right;
        }

        if (Input.GetKey(KeyCode.A)) {
            moveDirection -= targetCamera.transform.right;
        }

        if (Input.GetKey(KeyCode.E)) {
            moveDirection += Vector3.up * verticalMoveSpeed / moveSpeed;
        }

        if (Input.GetKey(KeyCode.Q)) {
            moveDirection -= Vector3.up * verticalMoveSpeed / moveSpeed;
        }

        if (moveDirection.sqrMagnitude > 1f) {
            moveDirection.Normalize();
        }

        targetCamera.transform.position += moveDirection * speed * Time.deltaTime;
    }

    private void LockCursor() {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void UnlockCursor() {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void UpdateHoveredChunk() {
        hoveredChunk = null;

        Ray ray = targetCamera.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, rayDistance, terrainRaycastMask)) {
            return;
        }

        hoveredChunk = hit.collider.GetComponentInParent<CpuTerrainChunk>();
    }

    private void HandleChunkSelection() {
        if (Input.GetKeyDown(KeyCode.C)) {
            selectedChunks.Clear();
            Debug.Log("Cleared selected terrain chunks.");
            return;
        }

        if (!Input.GetMouseButtonDown(0)) {
            return;
        }

        if (hoveredChunk == null) {
            if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift)) {
                selectedChunks.Clear();
            }

            return;
        }

        bool additiveSelection = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        if (!additiveSelection) {
            selectedChunks.Clear();
            selectedChunks.Add(hoveredChunk);
            Debug.Log($"Selected chunk {hoveredChunk.ChunkCoord}");
            return;
        }

        if (selectedChunks.Contains(hoveredChunk)) {
            selectedChunks.Remove(hoveredChunk);
            Debug.Log($"Deselected chunk {hoveredChunk.ChunkCoord}");
        }
        else {
            selectedChunks.Add(hoveredChunk);
            Debug.Log($"Added chunk {hoveredChunk.ChunkCoord} to selection.");
        }
    }

    private void HandleSelectedChunkOperations() {
        if (chunkManager == null) {
            return;
        }

        if (Input.GetKeyDown(KeyCode.V)) {
            SaveSelectedChunks();
        }

        if (Input.GetKeyDown(KeyCode.R)) {
            ResetSelectedChunksToSeed();
        }
    }

    private void SaveSelectedChunks() {
        if (selectedChunks.Count == 0) {
            Debug.Log("No selected chunks to save.");
            return;
        }

        chunkManager.SaveChunks(selectedChunks);
    }

    private void ResetSelectedChunksToSeed() {
        if (selectedChunks.Count == 0) {
            Debug.Log("No selected chunks to reset.");
            return;
        }

        chunkManager.ResetChunksToSeed(selectedChunks);
    }

    private void OnGUI() {
        if (!showAuthoringOverlay || !enabled) {
            return;
        }

        GUILayout.BeginArea(new Rect(15f, 15f, 380f, 210f), GUI.skin.box);

        GUILayout.Label("Authoring Mode");
        GUILayout.Label("WASD: Move freecam");
        GUILayout.Label("Q / E: Down / Up");
        GUILayout.Label("Right Mouse: Look");
        GUILayout.Label("Left Click: Select chunk");
        GUILayout.Label("Shift + Left Click: Add/remove chunk");
        GUILayout.Label("C: Clear selection");
        GUILayout.Label("S: Save selected chunks");
        GUILayout.Label("R: Reset selected chunks to seed");

        string hoverText = hoveredChunk != null ? hoveredChunk.ChunkCoord.ToString() : "None";
        GUILayout.Label($"Hovered Chunk: {hoverText}");
        GUILayout.Label($"Selected Chunks: {selectedChunks.Count}");

        GUILayout.EndArea();
    }

    private void UpdateSelectionVisuals() {
        if (!showSelectionInGameView) {
            HideSelectionVisuals();
            return;
        }

        UpdateHoverVisual();
        UpdateSelectedChunkVisuals();
    }

    private void UpdateHoverVisual() {
        if (hoverBoxVisual == null) {
            return;
        }

        if (hoveredChunk == null) {
            hoverBoxVisual.SetVisible(false);
            return;
        }

        hoverBoxVisual.SetColor(hoverColor);
        hoverBoxVisual.SetLineWidth(selectionLineWidth);
        hoverBoxVisual.SetBounds(hoveredChunk.WorldBounds);
        hoverBoxVisual.SetVisible(true);
    }

    private void UpdateSelectedChunkVisuals() {
        List<CpuTerrainChunk> chunksToRemove = new List<CpuTerrainChunk>();

        foreach (KeyValuePair<CpuTerrainChunk, TerrainChunkBoxVisual> entry in selectedBoxVisuals) {
            if (entry.Key == null || !selectedChunks.Contains(entry.Key)) {
                chunksToRemove.Add(entry.Key);
            }
        }

        for (int i = 0; i < chunksToRemove.Count; i++) {
            CpuTerrainChunk chunk = chunksToRemove[i];

            if (selectedBoxVisuals.TryGetValue(chunk, out TerrainChunkBoxVisual visual)) {
                visual.Dispose();
                selectedBoxVisuals.Remove(chunk);
            }
        }

        foreach (CpuTerrainChunk chunk in selectedChunks) {
            if (chunk == null) {
                continue;
            }

            if (!selectedBoxVisuals.TryGetValue(chunk, out TerrainChunkBoxVisual visual)) {
                visual = new TerrainChunkBoxVisual($"Selected Terrain Chunk {chunk.ChunkCoord}", selectedColor, selectionLineWidth);
                selectedBoxVisuals.Add(chunk, visual);
            }

            visual.SetColor(selectedColor);
            visual.SetLineWidth(selectionLineWidth);
            visual.SetBounds(chunk.WorldBounds);
            visual.SetVisible(true);
        }
    }

    private void HideSelectionVisuals() {
        if (hoverBoxVisual != null) {
            hoverBoxVisual.SetVisible(false);
        }

        foreach (TerrainChunkBoxVisual visual in selectedBoxVisuals.Values) {
            if (visual != null) {
                visual.SetVisible(false);
            }
        }
    }

    private void OnDestroy() {
        if (hoverBoxVisual != null) {
            hoverBoxVisual.Dispose();
            hoverBoxVisual = null;
        }

        foreach (TerrainChunkBoxVisual visual in selectedBoxVisuals.Values) {
            if (visual != null) {
                visual.Dispose();
            }
        }

        selectedBoxVisuals.Clear();
    }
}