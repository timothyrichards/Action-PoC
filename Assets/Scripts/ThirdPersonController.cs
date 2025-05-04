using UnityEngine;
using UnityEngine.InputSystem;
using SpacetimeDB.Types;
using ThirdPersonCamera;

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonController : MonoBehaviour
{
    [Header("Runtime")]
    public FreeForm cameraFreeForm;
    public AnimationController animController;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;
    public float jumpHeight = 2f;
    public float gravity = -9.81f;
    public float acceleration = 10f;
    public float deceleration = 8f;
    public Transform cameraTransform;

    private CharacterController controller;
    private PlayerInputActions inputActions;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private Vector3 velocity;
    private Vector3 currentMovement;
    private bool isMoving;
    private bool isTurning;
    private bool isGrounded;
    private bool jumpQueued;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        animController = GetComponentInChildren<AnimationController>();
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
        animController.SetWalkingState(isMoving);
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (context.ReadValue<float>() > 0.5f && isGrounded)
        {
            jumpQueued = true;
            animController.TriggerJump();
        }
    }

    private void OnAttack(InputAction.CallbackContext context)
    {
        if (context.ReadValue<float>() > 0.5f)
        {
            animController.TriggerAttack();
        }
    }

    private void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();

        if (cameraFreeForm == null) return;

        float yawDelta = CalculateYawDelta();
        animController.SetTurningState(isTurning, yawDelta);
    }

    private void OnLookStop(InputAction.CallbackContext context)
    {
        lookInput = Vector2.zero;
        animController.SetTurningState(false, 0f);
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        HandleLook();
        HandleMove();

        animController.SetMovementAnimation(moveInput);
        animController.UpdateCombatLayerWeight(currentMovement.magnitude > 0.1f, isGrounded);

        if (!ConnectionManager.IsConnected()) return;
        // if (!ConnectionManager.Conn.Db.Player.Identity.Equals(ConnectionManager.LocalIdentity)) return;

        float cameraPitch = cameraFreeForm.transform.eulerAngles.x;
        float yawDelta = CalculateYawDelta();

        ConnectionManager.Conn.Reducers.MovePlayer(
            new DbVector3(transform.position.x, transform.position.y, transform.position.z),
            new DbVector3(transform.eulerAngles.x, transform.eulerAngles.y, transform.eulerAngles.z),
            new DbVector2(cameraPitch, yawDelta),
            new DbAnimationState(
                moveInput.x,
                moveInput.y,
                yawDelta,
                isMoving,
                animController.IsTurning,
                animController.IsJumping,
                animController.IsAttacking
            )
        );
    }

    private float CalculateYawDelta()
    {
        if (cameraFreeForm == null) return 0f;

        float cameraYaw = cameraFreeForm.transform.eulerAngles.y;
        float characterYaw = transform.eulerAngles.y;

        return Mathf.DeltaAngle(characterYaw, cameraYaw);
    }

    private void HandleLook()
    {
        if (cameraFreeForm == null) return;

        float yawDelta = CalculateYawDelta();
        isTurning = Mathf.Abs(yawDelta) > animController.maxSpineYaw;

        // Always rotate to face the camera if moving
        if (isMoving || isTurning)
        {
            float targetYaw = isMoving
                ? cameraFreeForm.transform.eulerAngles.y
                : cameraFreeForm.transform.eulerAngles.y - Mathf.Sign(yawDelta) * animController.maxSpineYaw;

            float rotationMultiplier = Mathf.Max(1f, Mathf.Abs(yawDelta - (isMoving ? 0f : animController.maxSpineYaw)));
            float newYaw = Mathf.MoveTowardsAngle(
                transform.eulerAngles.y,
                targetYaw,
                rotationSpeed * Time.deltaTime * rotationMultiplier
            );

            transform.rotation = Quaternion.Euler(0, newYaw, 0);
        }

        // Update spine look
        if (cameraFreeForm != null)
        {
            animController.cameraPitch = cameraFreeForm.transform.eulerAngles.x;
            animController.yawDelta = CalculateYawDelta();
        }
    }

    private void HandleMove()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        Vector3 targetMovement = new(moveInput.x, 0, moveInput.y);
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

        controller.Move(velocity * Time.deltaTime);
    }

    private void LateUpdate()
    {
        if (cameraFreeForm != null)
        {
            animController.cameraPitch = cameraFreeForm.transform.eulerAngles.x;
            animController.yawDelta = CalculateYawDelta();
        }

        if (!isGrounded)
        {
            velocity.y += gravity * Time.deltaTime;
        }
    }
}