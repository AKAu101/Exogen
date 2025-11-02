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
    private float cameraPitch;

    // Components
    private CharacterController controller;
    private bool isGrounded;
    private bool isSprinting;
    private bool jumpPressed;
    private Vector2 lookInput;

    // Input values
    private Vector2 moveInput;
    private IUIStateManagement uiStateManagement;

    // Movement variables
    private Vector3 velocity;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        // Lock and hide cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Find camera if not assigned
        if (cameraTransform == null) cameraTransform = Camera.main.transform;
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

        var currentSpeed = isSprinting ? sprintSpeed : walkSpeed;

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

    public void OnSprint(InputAction.CallbackContext context)
    {
        isSprinting = context.performed || context.started;
    }
}