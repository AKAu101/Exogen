using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
///     First-person character controller handling movement, jumping, sprinting, and camera look.
///     Supports input blocking when UI (like inventory) is open.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Movement Settings")] [SerializeField]
    private float walkSpeed = 5f;

    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float gravity = -18f;

    [Header("Mouse Look Settings")] [SerializeField]
    private float mouseSensitivity = 2f;

    [SerializeField] private float maxLookAngle = 80f;
    [SerializeField] private Transform cameraTransform;

    [Header("Ground Check")] [SerializeField]
    private Transform groundCheck;
    [SerializeField] private float groundDistance = 0.2f;
    [SerializeField] private LayerMask groundMask;

    [Header("Head Check")] [SerializeField]
    private float duckCamHeight = 0.12f;
    [SerializeField] private Vector3 duckCenter;
    private Vector3 baseCenter;

    // Duck variables
    private float baseHeight;
    private float cameraBaseHeight;
    private float cameraPitch;

    // Components
    private CharacterController controller;
    private bool isDucking;

    //States
    private bool isGrounded;
    private bool isSprinting;
    private bool jumpPressed;
    private Vector2 lookInput;

    // Input values
    private Vector2 moveInput;
    private IUIStateManagement uiStateManagement;
    private Vector3 velocity;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        // Lock and hide cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Find camera if not assigned
        if (cameraTransform == null) cameraTransform = Camera.main.transform;

        baseHeight = controller.height;
        baseCenter = controller.center;
        cameraBaseHeight = cameraTransform.localPosition.y;
    }

    private void Start()
    {
        // Try to get the UI state service from the ServiceLocator
        // Using Start() instead of Awake() to ensure singletons have registered themselves
        // This is optional - if not available, input won't be blocked by UI
        if (ServiceLocator.Instance.IsRegistered<IUIStateManagement>())
            uiStateManagement = ServiceLocator.Instance.Get<IUIStateManagement>();
    }

    private void Update()
    {
        HandleGroundCheck();
        HandleMovement();
        HandleMouseLook();
        HandleJump();
        HandleDuck();
    }

    private void HandleGroundCheck()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0) velocity.y = -2f;
    }

    private void HandleMovement()
    {
        // Lazy initialization if service wasn't available at Start()
        if (uiStateManagement == null && ServiceLocator.Instance.IsRegistered<IUIStateManagement>())
            uiStateManagement = ServiceLocator.Instance.Get<IUIStateManagement>();

        if (uiStateManagement != null && uiStateManagement.IsInventoryVisible) return;

        var currentSpeed = isSprinting ? sprintSpeed : isDucking ? walkSpeed / 2 : walkSpeed;

        var move = transform.right * moveInput.x + transform.forward * moveInput.y;
        controller.Move(move * currentSpeed * Time.deltaTime);

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private void HandleMouseLook()
    {
        // Lazy initialization if service wasn't available at Start()
        if (uiStateManagement == null && ServiceLocator.Instance.IsRegistered<IUIStateManagement>())
            uiStateManagement = ServiceLocator.Instance.Get<IUIStateManagement>();

        if (uiStateManagement != null && uiStateManagement.IsInventoryVisible) return;

        var mouseX = lookInput.x * mouseSensitivity;
        var mouseY = lookInput.y * mouseSensitivity;

        // Rotate player body left/right
        transform.Rotate(Vector3.up * mouseX);

        // Rotate camera up/down with clamping
        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -maxLookAngle, maxLookAngle);
        cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
    }

    private void HandleJump()
    {
        if (jumpPressed && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpPressed = false;
        }
    }

    private void HandleDuck()
    {
        if (isDucking && isGrounded)
        {
            //set collider height
            //set camera height, if not parented to Head-Bone

            controller.height = baseHeight / 2;
            controller.center = duckCenter;

            //hack till bone parented
            var camPos = cameraTransform.localPosition;
            camPos.y = duckCamHeight;
            cameraTransform.localPosition = camPos;
        }
        else
        {
            //if not ducking unduck

            //check if you can unduck
            var canStandUp = !Physics.Raycast(groundCheck.position, Vector3.up, baseHeight, groundMask,
                QueryTriggerInteraction.Collide);

            if (canStandUp)
            {
                controller.height = baseHeight;
                controller.center = baseCenter;

                //hack till bone parented
                var camPos = cameraTransform.localPosition;
                camPos.y = cameraBaseHeight;
                cameraTransform.localPosition = camPos;
            }
            else
            {
                //give feedback to player
                Debug.Log("Cant stand up! space too tiny");
            }
        }
    }

    // Input System callback methods
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed) jumpPressed = true;
    }

    public void OnDuck(InputAction.CallbackContext context)
    {
        if (context.performed || context.started)
            isDucking = true;
        else if (context.canceled)
            isDucking = false;
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        isSprinting = context.performed || context.started;
    }
}