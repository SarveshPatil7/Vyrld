using UnityEngine;

public class GameModeManager : MonoBehaviour {
    [Header("Mode")]
    [SerializeField] private TerrainInteractionMode startingMode = TerrainInteractionMode.Terraform;

    [Header("Mode Hotkeys")]
    [SerializeField] private KeyCode playtestModeKey = KeyCode.F1;
    [SerializeField] private KeyCode terraformModeKey = KeyCode.F2;
    [SerializeField] private KeyCode authoringModeKey = KeyCode.F3;

    [Header("Tools")]
    [SerializeField] private TerrainModeTool[] modeTools;

    private TerrainInteractionMode currentMode;
    private bool hasActiveMode;

    public TerrainInteractionMode CurrentMode => currentMode;

    private void Start() {
        DisableAllTools();
        SetMode(startingMode);
    }

    private void Update() {
        HandleUniversalModeHotkeys();
    }

    private void HandleUniversalModeHotkeys() {
        if (Input.GetKeyDown(playtestModeKey)) {
            SetMode(TerrainInteractionMode.Playtest);
        }

        if (Input.GetKeyDown(terraformModeKey)) {
            SetMode(TerrainInteractionMode.Terraform);
        }

        if (Input.GetKeyDown(authoringModeKey)) {
            SetMode(TerrainInteractionMode.Authoring);
        }
    }

    public void SetMode(TerrainInteractionMode newMode) {
        if (hasActiveMode && currentMode == newMode) {
            return;
        }

        if (hasActiveMode) {
            ExitToolsForMode(currentMode);
        }

        currentMode = newMode;
        hasActiveMode = true;

        bool foundTool = EnterToolsForMode(currentMode);

        if (foundTool) {
            Debug.Log($"Terrain mode changed to: {currentMode}");
        }
        else {
            Debug.Log($"Terrain mode changed to: {currentMode}. No tool assigned for this mode yet.");
        }
    }

    private void DisableAllTools() {
        if (modeTools == null) {
            return;
        }

        for (int i = 0; i < modeTools.Length; i++) {
            if (modeTools[i] != null) {
                modeTools[i].ExitMode();
            }
        }
    }

    private bool EnterToolsForMode(TerrainInteractionMode mode) {
        bool foundTool = false;

        if (modeTools == null) {
            return false;
        }

        for (int i = 0; i < modeTools.Length; i++) {
            TerrainModeTool tool = modeTools[i];

            if (tool == null) {
                continue;
            }

            if (tool.Mode == mode) {
                tool.EnterMode();
                foundTool = true;
            }
        }

        return foundTool;
    }

    private void ExitToolsForMode(TerrainInteractionMode mode) {
        if (modeTools == null) {
            return;
        }

        for (int i = 0; i < modeTools.Length; i++) {
            TerrainModeTool tool = modeTools[i];

            if (tool == null) {
                continue;
            }

            if (tool.Mode == mode) {
                tool.ExitMode();
            }
        }
    }
}