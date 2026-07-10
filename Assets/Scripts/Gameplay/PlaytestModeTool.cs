using UnityEngine;

public class PlaytestModeTool : TerrainModeTool {
    public override TerrainInteractionMode Mode => TerrainInteractionMode.Playtest;

    [Header("Player Rig")]
    [SerializeField] private VyrldPlayerControllerRig playerRig;

    [Header("Start Mode")]
    [SerializeField] private PlayerControllerMode startingControllerMode = PlayerControllerMode.Grounded;
    [SerializeField] private bool allowControllerSwitching = true;

    public override void EnterMode() {
        base.EnterMode();

        if (playerRig != null) {
            playerRig.EnableRig(startingControllerMode, allowControllerSwitching);
        }

        Debug.Log("Entered Playtest mode.");
    }

    public override void ExitMode() {
        if (playerRig != null) {
            playerRig.DisableRig();
        }

        Debug.Log("Exited Playtest mode.");
        base.ExitMode();
    }
}