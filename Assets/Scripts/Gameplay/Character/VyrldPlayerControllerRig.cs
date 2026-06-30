using UnityEngine;

public class VyrldPlayerControllerRig : MonoBehaviour {
    [Header("References")]
    [SerializeField] private Camera controlledCamera;
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private GroundedCharacterMotor groundedMotor;
    [SerializeField] private FreecamCharacterMotor freecamMotor;

    [Header("Switching")]
    [SerializeField] private KeyCode switchKey = KeyCode.LeftShift;
    [SerializeField] private float doubleTapWindow = 0.3f;

    private PlayerControllerMode currentMode = PlayerControllerMode.Grounded;
    private bool allowControllerSwitching;
    private float lastSwitchTapTime = -999f;

    public PlayerControllerMode CurrentMode => currentMode;

    private void Awake() {
        EnsureReferences();
        DisableRig();
    }

    private void Update() {
        if (!allowControllerSwitching) {
            return;
        }

        if (!Input.GetKeyDown(switchKey) && !Input.GetKeyDown(KeyCode.RightShift)) {
            return;
        }

        if (Time.time - lastSwitchTapTime <= doubleTapWindow) {
            ToggleControllerMode();
            lastSwitchTapTime = -999f;
            return;
        }

        lastSwitchTapTime = Time.time;
    }

    public void EnableRig(PlayerControllerMode startingMode, bool allowSwitching) {
        EnsureReferences();

        enabled = true;
        allowControllerSwitching = allowSwitching;
        ApplyControllerMode(startingMode);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void DisableRig() {
        allowControllerSwitching = false;

        if (groundedMotor != null) {
            groundedMotor.enabled = false;
        }

        if (freecamMotor != null) {
            freecamMotor.enabled = false;
        }

        if (characterController != null) {
            characterController.enabled = false;
        }

        enabled = false;
    }

    public void ApplyControllerMode(PlayerControllerMode newMode) {
        EnsureReferences();

        currentMode = newMode;

        bool useGrounded = currentMode == PlayerControllerMode.Grounded;
        bool useFreecam = currentMode == PlayerControllerMode.Freecam;

        if (controlledCamera != null && cameraPivot != null) {
            controlledCamera.transform.SetParent(cameraPivot);
            controlledCamera.transform.localPosition = Vector3.zero;
            controlledCamera.transform.localRotation = Quaternion.identity;
        }

        if (visualRoot != null) {
            visualRoot.gameObject.SetActive(useGrounded);
        }

        if (characterController != null) {
            characterController.enabled = useGrounded;
        }

        if (groundedMotor != null) {
            groundedMotor.enabled = useGrounded;
        }

        if (freecamMotor != null) {
            freecamMotor.enabled = useFreecam;
        }

        Debug.Log($"Player controller mode changed to: {currentMode}");
    }

    private void ToggleControllerMode() {
        if (currentMode == PlayerControllerMode.Grounded) {
            ApplyControllerMode(PlayerControllerMode.Freecam);
        }
        else {
            ApplyControllerMode(PlayerControllerMode.Grounded);
        }
    }

    private void EnsureReferences() {
        if (controlledCamera == null) {
            controlledCamera = Camera.main;
        }

        if (cameraPivot == null) {
            Transform existingPivot = transform.Find("Camera_Pivot");

            if (existingPivot != null) {
                cameraPivot = existingPivot;
            }
        }

        if (characterController == null) {
            characterController = GetComponent<CharacterController>();
        }

        if (groundedMotor == null) {
            groundedMotor = GetComponent<GroundedCharacterMotor>();
        }

        if (freecamMotor == null) {
            freecamMotor = GetComponent<FreecamCharacterMotor>();
        }
    }
}