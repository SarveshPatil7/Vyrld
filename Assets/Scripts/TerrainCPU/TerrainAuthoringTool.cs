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
    }

    private void Update() {
        if (targetCamera == null) {
            return;
        }

        HandleLook();
        HandleMovement();
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
}