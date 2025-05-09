using UnityEngine;
using UnityEngine.InputSystem;
using SpacetimeDB.Types;

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonController : MonoBehaviour
{
    [Header("Runtime")]
    public PlayerEntity playerEntity;

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
    private bool IsMoving => moveInput.sqrMagnitude > 0.01f;
    private bool IsTurning => Mathf.Abs(CalculateYawDelta()) > playerEntity.animController.maxSpineYaw;
    private bool IsGrounded => controller.isGrounded;
    private bool jumpQueued;

    private void Awake()
    {
        playerEntity = GetComponent<PlayerEntity>();
        controller = GetComponent<CharacterController>();
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
        if (!playerEntity.InputEnabled) return;

        moveInput = context.ReadValue<Vector2>();
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (!playerEntity.InputEnabled) return;

        if (context.ReadValue<float>() > 0.5f && IsGrounded)
        {
            jumpQueued = true;
        }
    }

    private void OnAttack(InputAction.CallbackContext context)
    {
        if (!playerEntity.InputEnabled) return;
        if (BuildingSystem.Instance.IsEnabled) return;

        if (context.ReadValue<float>() > 0.5f)
        {
            playerEntity.animController.TriggerAttack();
        }
    }

    private void OnLook(InputAction.CallbackContext context)
    {
        if (!playerEntity.InputEnabled) return;

        // Skip camera input if cursor is unlocked
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            if (playerEntity.CameraFreeForm != null)
                playerEntity.CameraFreeForm.enabled = false;

            return;
        }

        if (playerEntity.CameraFreeForm != null)
            playerEntity.CameraFreeForm.enabled = true;

        lookInput = context.ReadValue<Vector2>();

        if (playerEntity.CameraFreeForm == null) return;

        float yawDelta = CalculateYawDelta();
        playerEntity.animController.SetTurningState(IsTurning, yawDelta);
    }

    private void OnLookStop(InputAction.CallbackContext context)
    {
        lookInput = Vector2.zero;
        playerEntity.animController.SetTurningState(false, 0f);
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        if (!playerEntity.InputEnabled)
        {
            moveInput = Vector2.zero;
            lookInput = Vector2.zero;
            currentMovement = Vector3.zero;
            playerEntity.animController.SetMovementAnimation(Vector2.zero, false);
            playerEntity.animController.UpdateCombatLayerWeight(false, IsGrounded);
            return;
        }

        HandleLook();
        HandleMove();

        playerEntity.animController.SetMovementAnimation(moveInput, IsMoving);
        playerEntity.animController.UpdateCombatLayerWeight(IsMoving, IsGrounded);
    }

    private void FixedUpdate()
    {
        if (!ConnectionManager.IsConnected()) return;

        if (!playerEntity.IsLocalPlayer()) return;

        float cameraPitch = playerEntity.CameraFreeForm.transform.eulerAngles.x;
        float yawDelta = CalculateYawDelta();

        ConnectionManager.Conn.Reducers.MovePlayer(
            new DbVector3(transform.position.x, transform.position.y, transform.position.z),
            new DbVector3(transform.eulerAngles.x, transform.eulerAngles.y, transform.eulerAngles.z),
            new DbVector2(cameraPitch, yawDelta),
            new DbAnimationState(
                moveInput.x,
                moveInput.y,
                yawDelta,
                IsMoving,
                playerEntity.animController.IsTurning,
                playerEntity.animController.IsJumping,
                playerEntity.animController.IsAttacking
            )
        );
    }

    private void LateUpdate()
    {
        if (playerEntity.CameraFreeForm != null)
        {
            playerEntity.animController.cameraPitch = playerEntity.CameraFreeForm.transform.eulerAngles.x;
            playerEntity.animController.yawDelta = CalculateYawDelta();
        }

        if (!IsGrounded)
        {
            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);
        }
    }

    private float CalculateYawDelta()
    {
        if (playerEntity.CameraFreeForm == null) return 0f;

        float cameraYaw = playerEntity.CameraFreeForm.transform.eulerAngles.y;
        float characterYaw = transform.eulerAngles.y;

        return Mathf.DeltaAngle(characterYaw, cameraYaw);
    }

    private void HandleLook()
    {
        if (playerEntity.CameraFreeForm == null || Cursor.lockState != CursorLockMode.Locked) return;

        // Always rotate to face the camera if moving
        if (IsMoving || IsTurning)
        {
            float yawDelta = CalculateYawDelta();
            float targetYaw = IsMoving
                ? playerEntity.CameraFreeForm.transform.eulerAngles.y
                : playerEntity.CameraFreeForm.transform.eulerAngles.y - Mathf.Sign(yawDelta) * playerEntity.animController.maxSpineYaw;

            float rotationMultiplier = Mathf.Max(1f, Mathf.Abs(yawDelta - (IsMoving ? 0f : playerEntity.animController.maxSpineYaw)));
            float newYaw = Mathf.MoveTowardsAngle(
                transform.eulerAngles.y,
                targetYaw,
                rotationSpeed * Time.deltaTime * rotationMultiplier
            );

            transform.rotation = Quaternion.Euler(0, newYaw, 0);
        }

        // Update spine look
        if (playerEntity.CameraFreeForm != null)
        {
            playerEntity.animController.cameraPitch = playerEntity.CameraFreeForm.transform.eulerAngles.x;
            playerEntity.animController.yawDelta = CalculateYawDelta();
        }
    }

    private void HandleMove()
    {
        if (IsGrounded && velocity.y < 0)
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
            playerEntity.animController.TriggerJump();
            jumpQueued = false;
        }

        controller.Move(velocity * Time.deltaTime);
    }
}