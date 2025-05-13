using UnityEngine;
using UnityEngine.InputSystem;
using SpacetimeDB.Types;
using QFSW.QC;

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonController : MonoBehaviour
{
    [Header("Runtime")]
    [SerializeField] private PlayerEntity playerEntity;
    [SerializeField] private CharacterController controller;
    public Transform cameraTransform;
    [SerializeField] private bool movingPlayer = false;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float deceleration = 8f;

    private PlayerInputActions inputActions;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private Vector3 velocity;
    private Vector3 currentMovement;
    private bool IsMoving => moveInput.sqrMagnitude > 0.01f;
    private bool IsTurning => Mathf.Abs(CalculateYawDelta()) > playerEntity.animController.maxSpineYaw;
    private bool IsGrounded => controller.isGrounded;
    private bool jumpQueued;
    private DbAnimationState lastAnimationState;

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
        // Skip camera input if cursor is unlocked
        if (playerEntity.CameraFreeForm != null)
            playerEntity.CameraFreeForm.enabled = Cursor.lockState == CursorLockMode.Locked;

        if (!playerEntity.InputEnabled) return;

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

    private void Update()
    {
        if (!playerEntity.InputEnabled)
        {
            moveInput = Vector2.zero;
            lookInput = Vector2.zero;
            currentMovement = Vector3.zero;
            playerEntity.animController.SetMovementAnimation(Vector2.zero, false);
            playerEntity.animController.UpdateCombatLayerWeight(false, IsGrounded);
            controller.Move(new Vector3(0, velocity.y, 0) * Time.deltaTime);
            return;
        }

        HandleLook();
        HandleMove();

        playerEntity.animController.SetMovementAnimation(moveInput, IsMoving);
        playerEntity.animController.UpdateCombatLayerWeight(IsMoving, IsGrounded);

        if (movingPlayer) return;
        if (!ConnectionManager.IsConnected()) return;
        if (!playerEntity.IsLocalPlayer()) return;

        float cameraPitch = playerEntity.CameraFreeForm.transform.eulerAngles.x;
        float yawDelta = CalculateYawDelta();

        var position = new DbVector3(transform.position.x, transform.position.y, transform.position.z);
        var rotation = new DbVector3(transform.eulerAngles.x, transform.eulerAngles.y, transform.eulerAngles.z);
        var lookDirection = new DbVector2(cameraPitch, yawDelta);
        var animState = new DbAnimationState(
            moveInput.x,
            moveInput.y,
            yawDelta,
            IsMoving,
            playerEntity.animController.IsTurning,
            playerEntity.animController.IsJumping,
            playerEntity.animController.IsAttacking,
            (uint)playerEntity.animController.ComboCount
        );

        ReducerMiddleware.Instance.CallReducer<object[]>(
            "PlayerUpdate",
            _ => ConnectionManager.Conn.Reducers.PlayerUpdate(position, rotation, lookDirection, animState),
            position, rotation, lookDirection, animState
        );
    }

    private void LateUpdate()
    {
        if (playerEntity.CameraFreeForm != null)
        {
            playerEntity.animController.cameraPitch = playerEntity.CameraFreeForm.transform.eulerAngles.x;
            playerEntity.animController.yawDelta = CalculateYawDelta();
        }

        // Apply gravity regardless of input state
        if (!IsGrounded)
        {
            velocity.y += gravity * Time.deltaTime;
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

        if (jumpQueued)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            playerEntity.animController.TriggerJump();
            jumpQueued = false;
        }

        // Combine horizontal movement and vertical velocity into a single movement vector
        Vector3 finalMovement = currentMovement + velocity;
        controller.Move(finalMovement * Time.deltaTime);
    }

    [Command]
    public void ForceMove(float x, float y, float z)
    {
        movingPlayer = true;
        controller.enabled = false;
        transform.position = new Vector3(x, y, z);
        controller.enabled = true;
        movingPlayer = false;
    }
}