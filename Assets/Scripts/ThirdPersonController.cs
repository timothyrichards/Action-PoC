using ThirdPersonCamera;
using UnityEngine;
using UnityEngine.InputSystem;
using SpacetimeDB.Types;

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonController : MonoBehaviour
{
    [Header("Runtime")]
    public FreeForm cameraFreeForm;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;
    public float jumpHeight = 2f;
    public float gravity = -9.81f;
    public float acceleration = 10f;
    public float deceleration = 8f;
    public float layerTransitionSpeed = 5f;
    public Transform cameraTransform;
    public Animator animator;

    [Header("Look Up/Down Settings")]
    public Transform spineBone;
    public float spineRotationMultiplier = 1.0f;
    public float maxSpinePitch = 45f;
    public float maxSpineYaw = 30f;

    private int noMaskCombatLayerIndex;
    private int maskCombatLayerIndex;
    private CharacterController controller;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private Vector3 velocity;
    private Vector3 currentMovement;
    private bool isMoving;
    private bool isTurning;
    private bool isGrounded;
    private bool jumpQueued;

    private PlayerInputActions inputActions;
    private readonly int isWalkingHash = Animator.StringToHash("Walking");
    private readonly int horizontalHash = Animator.StringToHash("Horizontal");
    private readonly int verticalHash = Animator.StringToHash("Vertical");
    private readonly int isTurningHash = Animator.StringToHash("Turning");
    private readonly int lookYawHash = Animator.StringToHash("LookYaw");
    private readonly int meleeHash = Animator.StringToHash("Melee");
    private readonly int jumpHash = Animator.StringToHash("Jump");

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();

        noMaskCombatLayerIndex = animator.GetLayerIndex("No Mask Combat Layer");
        maskCombatLayerIndex = animator.GetLayerIndex("Mask Combat Layer");

        inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        inputActions.Player.Move.performed += OnMove;
        inputActions.Player.Move.canceled += OnMove;
        inputActions.Player.Jump.performed += OnJump;
        inputActions.Player.Attack.performed += OnAttack;
        inputActions.Player.Look.performed += OnLook;
        inputActions.Player.Look.canceled += OnLookStop;
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Player.Move.performed -= OnMove;
        inputActions.Player.Move.canceled -= OnMove;
        inputActions.Player.Jump.performed -= OnJump;
        inputActions.Player.Attack.performed -= OnAttack;
        inputActions.Player.Look.performed -= OnLook;
        inputActions.Player.Look.canceled -= OnLookStop;
        inputActions.Disable();
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
        isMoving = moveInput.sqrMagnitude > 0.01f;
        animator.SetBool(isWalkingHash, isMoving);
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (context.ReadValue<float>() > 0.5f && isGrounded)
        {
            jumpQueued = true;
            animator.SetTrigger(jumpHash);
        }
    }

    private void OnAttack(InputAction.CallbackContext context)
    {
        if (context.ReadValue<float>() > 0.5f)
        {
            animator.SetTrigger(meleeHash);
        }
    }

    private void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
        if (cameraFreeForm == null) return;
        float cameraYaw = cameraFreeForm.transform.eulerAngles.y;
        float characterYaw = transform.eulerAngles.y;
        float yawDelta = Mathf.DeltaAngle(characterYaw, cameraYaw);
        bool turning = Mathf.Abs(yawDelta) > maxSpineYaw;
        animator.SetBool(isTurningHash, turning);
        animator.SetFloat(lookYawHash, yawDelta < 0 ? 0f : 1f);
    }

    private void OnLookStop(InputAction.CallbackContext context)
    {
        lookInput = Vector2.zero;
        animator.SetBool(isTurningHash, false);
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void FixedUpdate()
    {
        if (!SpacetimeConnectionManager.IsConnected()) return;

        SpacetimeConnectionManager.Conn.Reducers.MovePlayer(
            new DbVector3(transform.position.x, transform.position.y, transform.position.z),
            new DbVector3(transform.eulerAngles.x, transform.eulerAngles.y, transform.eulerAngles.z)
        );
    }

    private void Update()
    {
        HandleLook();
        HandleMove();
        UpdateAnimatorMovement();
        UpdateCombatLayerWeight();
    }

    private void HandleLook()
    {
        if (cameraFreeForm == null) return;

        float cameraYaw = cameraFreeForm.transform.eulerAngles.y;
        float characterYaw = transform.eulerAngles.y;
        float yawDelta = Mathf.DeltaAngle(characterYaw, cameraYaw);
        isTurning = Mathf.Abs(yawDelta) > maxSpineYaw;

        // Always rotate to face the camera if moving
        if (isMoving || isTurning)
        {
            // If not moving, clamp the rotation so the spine twist stays at maxSpineYaw
            float targetYaw = isMoving ? cameraYaw : cameraYaw - Mathf.Sign(yawDelta) * maxSpineYaw;
            float newYaw = Mathf.MoveTowardsAngle(characterYaw, targetYaw, rotationSpeed * Time.deltaTime * Mathf.Max(1f, Mathf.Abs(yawDelta - (isMoving ? 0f : maxSpineYaw))));
            transform.rotation = Quaternion.Euler(0, newYaw, 0);

            // Set LookYaw animator parameter only
            animator.SetFloat(lookYawHash, yawDelta < 0 ? 0f : 1f);
        }
    }

    private void HandleMove()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        Vector3 targetMovement = new Vector3(moveInput.x, 0, moveInput.y);
        if (targetMovement.magnitude > 0.01f)
        {
            targetMovement = Quaternion.Euler(0, cameraTransform ? cameraTransform.eulerAngles.y : transform.eulerAngles.y, 0) * targetMovement;
            targetMovement *= moveSpeed;
        }

        float accelerationToUse = targetMovement.magnitude > 0.01f ? acceleration : deceleration;
        currentMovement = Vector3.Lerp(currentMovement, targetMovement, accelerationToUse * Time.deltaTime);

        controller.Move(currentMovement * Time.deltaTime);

        if (jumpQueued)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpQueued = false;
        }

        if (!isGrounded)
        {
            velocity.y += gravity * Time.deltaTime;
        }

        animator.SetBool(isWalkingHash, currentMovement.magnitude > 0.1f);
        controller.Move(velocity * Time.deltaTime);
    }

    private void UpdateAnimatorMovement()
    {
        // Convert world space movement to local space relative to character's forward direction
        Vector3 localMovement = transform.InverseTransformDirection(currentMovement);

        // Normalize the movement values and apply them to the animator
        float normalizedSpeed = moveSpeed > 0 ? 1f / moveSpeed : 1f;

        // Clean up tiny values that are effectively zero
        float threshold = 0.01f;
        float horizontalValue = Mathf.Abs(localMovement.x) < threshold ? 0f : localMovement.x * normalizedSpeed;
        float verticalValue = Mathf.Abs(localMovement.z) < threshold ? 0f : localMovement.z * normalizedSpeed;

        animator.SetFloat(horizontalHash, horizontalValue, 0.05f, Time.deltaTime);
        animator.SetFloat(verticalHash, verticalValue, 0.05f, Time.deltaTime);
    }

    private void UpdateCombatLayerWeight()
    {
        float targetWeight = (currentMovement.magnitude > 0.1f || !isGrounded) ? 1f : 0f;

        float currentNoMaskWeight = animator.GetLayerWeight(noMaskCombatLayerIndex);
        float currentMaskWeight = animator.GetLayerWeight(maskCombatLayerIndex);

        float newNoMaskWeight = Mathf.Lerp(currentNoMaskWeight, 1f - targetWeight, layerTransitionSpeed * Time.deltaTime);
        float newMaskWeight = Mathf.Lerp(currentMaskWeight, targetWeight, layerTransitionSpeed * Time.deltaTime);

        animator.SetLayerWeight(noMaskCombatLayerIndex, newNoMaskWeight);
        animator.SetLayerWeight(maskCombatLayerIndex, newMaskWeight);
    }

    private void LateUpdate()
    {
        ApplySpineLook();
    }

    private void ApplySpineLook()
    {
        if (spineBone != null && cameraFreeForm != null)
        {
            float cameraPitch = cameraFreeForm.transform.eulerAngles.x;
            if (cameraPitch > 180f) cameraPitch -= 360f;
            float clampedPitch = Mathf.Clamp(cameraPitch, -maxSpinePitch, maxSpinePitch);

            // Calculate yaw difference between camera and character
            float cameraYaw = cameraFreeForm.transform.eulerAngles.y;
            float characterYaw = transform.eulerAngles.y;
            float yawDelta = Mathf.DeltaAngle(characterYaw, cameraYaw);
            float clampedYaw = Mathf.Clamp(yawDelta, -maxSpineYaw, maxSpineYaw);

            // Only rotate on X (pitch), keep Y/Z as is
            Vector3 localEuler = spineBone.localEulerAngles;
            localEuler.x = clampedPitch * spineRotationMultiplier;
            localEuler.y = clampedYaw;
            spineBone.localEulerAngles = localEuler;
        }
    }
}