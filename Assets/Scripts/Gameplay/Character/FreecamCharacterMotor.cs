using UnityEngine;

public class FreecamCharacterMotor : MonoBehaviour {
    [Header("References")]
    [SerializeField] private Transform cameraPivot;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 18f;
    [SerializeField] private float fastMoveMultiplier = 4f;
    [SerializeField] private float verticalMoveSpeed = 14f;

    [Header("Look")]
    [SerializeField] private float lookSensitivity = 2f;
    [SerializeField] private bool holdMiddleMouseToLook = true;
    [SerializeField] private float minPitch = -89f;
    [SerializeField] private float maxPitch = 89f;

    private float pitch;

    private void Awake() {
        if (cameraPivot == null) {
            Transform existingPivot = transform.Find("Camera_Pivot");

            if (existingPivot != null) {
                cameraPivot = existingPivot;
            }
        }
    }

    private void OnEnable() {
        if (cameraPivot != null) {
            pitch = cameraPivot.localEulerAngles.x;
        }
    }

    private void Update() {
        HandleLook();
        HandleMovement();
    }

    private void HandleLook() {
        bool canLook = !holdMiddleMouseToLook || Input.GetMouseButton(2);

        if (!canLook) {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        float mouseX = Input.GetAxis("Mouse X") * lookSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * lookSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        if (cameraPivot != null) {
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
    }

    private void HandleMovement() {
        Vector3 moveDirection = Vector3.zero;

        if (cameraPivot != null) {
            if (Input.GetKey(KeyCode.W)) {
                moveDirection += cameraPivot.forward;
            }

            if (Input.GetKey(KeyCode.S)) {
                moveDirection -= cameraPivot.forward;
            }

            if (Input.GetKey(KeyCode.D)) {
                moveDirection += cameraPivot.right;
            }

            if (Input.GetKey(KeyCode.A)) {
                moveDirection -= cameraPivot.right;
            }
        }
        else {
            if (Input.GetKey(KeyCode.W)) {
                moveDirection += transform.forward;
            }

            if (Input.GetKey(KeyCode.S)) {
                moveDirection -= transform.forward;
            }

            if (Input.GetKey(KeyCode.D)) {
                moveDirection += transform.right;
            }

            if (Input.GetKey(KeyCode.A)) {
                moveDirection -= transform.right;
            }
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

        float speed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? moveSpeed * fastMoveMultiplier : moveSpeed;
        transform.position += moveDirection * speed * Time.deltaTime;
    }
}