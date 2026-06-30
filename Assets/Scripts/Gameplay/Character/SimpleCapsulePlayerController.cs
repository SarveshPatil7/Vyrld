using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class SimpleCapsulePlayerController : MonoBehaviour {
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform cameraPivot;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 9f;
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -25f;
    [SerializeField] private float groundedStickForce = -2f;

    [Header("Look")]
    [SerializeField] private float lookSensitivity = 2f;
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;

    private CharacterController characterController;
    private float pitch;
    private float verticalVelocity;

    private void Awake() {
        characterController = GetComponent<CharacterController>();

        if (playerCamera == null) {
            playerCamera = Camera.main;
        }

        if (cameraPivot == null) {
            GameObject pivotObject = new GameObject("Camera_Pivot");
            pivotObject.transform.SetParent(transform);
            pivotObject.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            pivotObject.transform.localRotation = Quaternion.identity;
            cameraPivot = pivotObject.transform;
        }
    }

    private void OnEnable() {
        if (playerCamera != null && cameraPivot != null) {
            playerCamera.transform.SetParent(cameraPivot);
            playerCamera.transform.localPosition = Vector3.zero;
            playerCamera.transform.localRotation = Quaternion.identity;
        }

        Vector3 currentEuler = transform.eulerAngles;
        pitch = 0f;
        transform.rotation = Quaternion.Euler(0f, currentEuler.y, 0f);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnDisable() {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update() {
        HandleLook();
        HandleMovement();
    }

    private void HandleLook() {
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
        Vector3 inputDirection = Vector3.zero;

        if (Input.GetKey(KeyCode.W)) {
            inputDirection += Vector3.forward;
        }

        if (Input.GetKey(KeyCode.S)) {
            inputDirection += Vector3.back;
        }

        if (Input.GetKey(KeyCode.D)) {
            inputDirection += Vector3.right;
        }

        if (Input.GetKey(KeyCode.A)) {
            inputDirection += Vector3.left;
        }

        if (inputDirection.sqrMagnitude > 1f) {
            inputDirection.Normalize();
        }

        float speed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? sprintSpeed : walkSpeed;
        Vector3 worldMove = transform.TransformDirection(inputDirection) * speed;

        if (characterController.isGrounded && verticalVelocity < 0f) {
            verticalVelocity = groundedStickForce;
        }

        if (characterController.isGrounded && Input.GetKeyDown(KeyCode.Space)) {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        verticalVelocity += gravity * Time.deltaTime;
        worldMove.y = verticalVelocity;

        characterController.Move(worldMove * Time.deltaTime);
    }
}