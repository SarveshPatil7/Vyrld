using UnityEngine;

public class PlayerControllerModeTool : TerrainModeTool {
    [Header("Mode")]
    [SerializeField] private TerrainInteractionMode mode = TerrainInteractionMode.Playtest;

    [Header("Player Controller")]
    [SerializeField] private VyrldPlayerControllerRig playerControllerRig;
    [SerializeField] private PlayerControllerMode startingControllerMode = PlayerControllerMode.Grounded;
    [SerializeField] private bool allowControllerSwitching = true;

    public override TerrainInteractionMode Mode => mode;

    public override void EnterMode() {
        base.EnterMode();

        if (playerControllerRig != null) {
            playerControllerRig.EnableRig(startingControllerMode, allowControllerSwitching);
        }

        Debug.Log($"Entered {mode} player controller mode. Starting controller: {startingControllerMode}. Switching allowed: {allowControllerSwitching}");
    }

    public override void ExitMode() {
        if (playerControllerRig != null) {
            playerControllerRig.DisableRig();
        }

        Debug.Log($"Exited {mode} player controller mode.");
        base.ExitMode();
    }
}