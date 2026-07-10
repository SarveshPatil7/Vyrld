using UnityEngine;

public abstract class TerrainModeTool : MonoBehaviour {
    public abstract TerrainInteractionMode Mode { get; }

    public virtual void EnterMode() {
        enabled = true;
    }

    public virtual void ExitMode() {
        enabled = false;
    }
}