using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FPSPlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 4f;
    [SerializeField] private float runSpeed = 7f;
    [SerializeField] private float crouchSpeed = 2f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float jumpHeight = 1.2f;

    [Header("Crouch")]
    [SerializeField] private float standingHeight = 2f;
    [SerializeField] private float crouchingHeight = 1.2f;
    [SerializeField] private float standingCameraY = 1.65f;
    [SerializeField] private float crouchingCameraY = 1f;
    [SerializeField] private float crouchTransitionSpeed = 8f;
    [SerializeField] private float ceilingCheckRadius = 0.35f;
    [SerializeField] private float ceilingCheckDistance = 0.1f;
    [SerializeField] private LayerMask obstacleMask = ~0;

    [Header("Mouse Look")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float minLookX = -80f;
    [SerializeField] private float maxLookX = 80f;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    private CharacterController controller;
    private Vector3 velocity;
    private Vector3 standingControllerCenter;
    private float cameraPitch;
    private bool isCrouching;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (cameraTransform == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
                cameraTransform = mainCamera.transform;
        }

        InitializeCrouchDimensions();

        // Asegurar que el jugador siempre cuente con sus componentes esenciales
        if (GetComponent<PlayerCombat>() == null)
        {
            gameObject.AddComponent<PlayerCombat>();
        }

        if (GetComponent<PlayerHealth>() == null)
        {
            gameObject.AddComponent<PlayerHealth>();
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        Look();
        Move();
    }

    private void Look()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, minLookX, maxLookX);

        cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
    }

    private void Move()
    {
        bool isGrounded = controller.isGrounded;

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        float inputX = Input.GetAxisRaw("Horizontal");
        float inputZ = Input.GetAxisRaw("Vertical");

        Vector3 move = transform.right * inputX + transform.forward * inputZ;
        move = Vector3.ClampMagnitude(move, 1f);

        UpdateCrouch();
        bool isRunning = Input.GetKey(KeyCode.LeftShift) && !isCrouching && inputZ > 0;

        float currentSpeed = walkSpeed;

        if (isCrouching)
            currentSpeed = crouchSpeed;
        else if (isRunning)
            currentSpeed = runSpeed;

        controller.Move(move * currentSpeed * Time.deltaTime);

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

            if (animator != null)
                animator.SetTrigger("Jump");
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        UpdateAnimator(inputX, inputZ, isRunning, isGrounded);
    }

    private void InitializeCrouchDimensions()
    {
        standingHeight = Mathf.Max(standingHeight, controller.radius * 2f);
        crouchingHeight = Mathf.Clamp(crouchingHeight, controller.radius * 2f, standingHeight);

        float controllerBottom = controller.center.y - controller.height * 0.5f;
        standingControllerCenter = controller.center;
        standingControllerCenter.y = controllerBottom + standingHeight * 0.5f;

        controller.height = standingHeight;
        controller.center = standingControllerCenter;

        if (cameraTransform != null)
        {
            Vector3 cameraPosition = cameraTransform.localPosition;
            cameraPosition.y = standingCameraY;
            cameraTransform.localPosition = cameraPosition;
        }
    }

    private void UpdateCrouch()
    {
        bool crouchRequested = Input.GetKey(KeyCode.LeftControl);

        if (crouchRequested)
        {
            isCrouching = true;
        }
        else if (controller.height < standingHeight - 0.01f)
        {
            isCrouching = !CanStandUp();
        }
        else
        {
            isCrouching = false;
        }

        float targetHeight = isCrouching ? crouchingHeight : standingHeight;
        float transitionStep = Mathf.Max(0.01f, crouchTransitionSpeed) * Time.deltaTime;
        float nextHeight = Mathf.MoveTowards(controller.height, targetHeight, transitionStep);

        controller.height = nextHeight;

        Vector3 nextCenter = standingControllerCenter;
        nextCenter.y -= (standingHeight - nextHeight) * 0.5f;
        controller.center = nextCenter;

        if (cameraTransform != null)
        {
            float targetCameraY = isCrouching ? crouchingCameraY : standingCameraY;
            Vector3 cameraPosition = cameraTransform.localPosition;
            cameraPosition.y = Mathf.MoveTowards(cameraPosition.y, targetCameraY, transitionStep);
            cameraTransform.localPosition = cameraPosition;
        }
    }

    private bool CanStandUp()
    {
        float checkRadius = Mathf.Clamp(ceilingCheckRadius, 0.01f, controller.radius);
        Vector3 up = transform.up;

        Vector3 currentCenter = transform.TransformPoint(controller.center);
        Vector3 currentTop = currentCenter
            + up * Mathf.Max(0f, controller.height * 0.5f - checkRadius);

        Vector3 standingCenter = transform.TransformPoint(standingControllerCenter);
        Vector3 standingTop = standingCenter
            + up * Mathf.Max(0f, standingHeight * 0.5f - checkRadius)
            + up * Mathf.Max(0f, ceilingCheckDistance);

        Collider[] obstacles = Physics.OverlapCapsule(
            currentTop,
            standingTop,
            checkRadius,
            obstacleMask,
            QueryTriggerInteraction.Ignore
        );

        foreach (Collider obstacle in obstacles)
        {
            if (obstacle.transform == transform || obstacle.transform.IsChildOf(transform))
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private void UpdateAnimator(float inputX, float inputZ, bool isRunning, bool isGrounded)
    {
        if (animator == null) return;

        bool isMoving = Mathf.Abs(inputX) > 0.1f || Mathf.Abs(inputZ) > 0.1f;

        float speedY = 0f;

        if (isMoving)
            speedY = isRunning ? 2f : 1f;

        animator.SetFloat("SpeedX", 0f);
        animator.SetFloat("SpeedY", speedY);
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsCrouching", isCrouching);
    }
}
