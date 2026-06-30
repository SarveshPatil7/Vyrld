using UnityEngine;

public class PlaytestModeTool : TerrainModeTool {
    public override TerrainInteractionMode Mode => TerrainInteractionMode.Playtest;

    [Header("Player")]
    [SerializeField] private SimpleCapsulePlayerController playerController;

    public override void EnterMode() {
        base.EnterMode();

        if (playerController != null) {
            playerController.enabled = true;
        }

        Debug.Log("Entered Playtest mode.");
    }

    public override void ExitMode() {
        if (playerController != null) {
            playerController.enabled = false;
        }

        Debug.Log("Exited Playtest mode.");
        base.ExitMode();
    }
}