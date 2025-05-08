using SpacetimeDB;
using SpacetimeDB.Types;
using ThirdPersonCamera;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerEntity : Entity
{
    [Header("Runtime")]
    public Identity ownerIdentity;
    public bool Initialized { get; private set; }

    [Header("References")]
    public PlayerInput input;
    public ThirdPersonController controller;
    public CreativeMode creativeMode;
    public AnimationController animController;
    public GameObject nameplate;

    [Header("Interpolation Settings")]
    public float positionSmoothTime = 0.15f;
    public float rotationLerpSpeed = 8f;
    public float spineLerpSpeed = 8f;

    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private float targetCameraPitch;
    private float targetYawDelta;
    private Vector3 currentVelocity;

    private FreeForm _cameraFreeForm;

    public FreeForm CameraFreeForm
    {
        get => _cameraFreeForm;
        set
        {
            _cameraFreeForm = value;
            if (value != null)
            {
                if (controller != null)
                {
                    controller.cameraTransform = value.transform;
                }
            }
        }
    }

    protected override void Awake()
    {
        base.Awake();

        input = GetComponent<PlayerInput>();
        controller = GetComponent<ThirdPersonController>();
        animController = GetComponent<AnimationController>();
        creativeMode = GetComponent<CreativeMode>();
    }

    public bool IsLocalPlayer()
    {
        return ownerIdentity.Equals(ConnectionManager.LocalIdentity);
    }

    public void Configure(Player player, CameraController playerCamera = null, HealthDisplay playerHealthDisplay = null)
    {
        // If the player is the local player, enable input and camera
        if (IsLocalPlayer())
        {
            // Configure the camera
            CameraFreeForm = playerCamera.GetComponent<FreeForm>();
            CameraFreeForm.transform.eulerAngles = new Vector3(player.LookDirection.X, player.Rotation.Y, player.Rotation.Z);
            playerCamera.target = transform;

            // Configure the health display
            nameplate.SetActive(false);
            playerHealthDisplay.healthComponent = healthComponent;
            playerHealthDisplay.enabled = true;
        }
        else
        {
            // Disable input and third person controller for other players
            input.enabled = false;
            controller.enabled = false;

            // Initialize target values for interpolation
            targetPosition = new Vector3(player.Position.X, player.Position.Y, player.Position.Z);
            targetRotation = Quaternion.Euler(player.Rotation.X, player.Rotation.Y, player.Rotation.Z);
            targetCameraPitch = player.LookDirection.X;
            targetYawDelta = player.LookDirection.Y;

            // Set initial transform values to match targets
            transform.position = targetPosition;
            transform.rotation = targetRotation;
            animController.cameraPitch = targetCameraPitch;
            animController.yawDelta = targetYawDelta;
        }

        // Update the health
        if (player.Health != healthComponent.CurrentHealth)
        {
            healthComponent.SetHealth(player.Health, player.MaxHealth);
        }

        Initialized = true;
    }

    public void UpdateFromPlayer(Player oldData, Player newData)
    {
        // Only update position and rotation for non-local players
        if (!newData.Identity.Equals(ConnectionManager.LocalIdentity))
        {
            // Set target transform values
            targetPosition = new Vector3(newData.Position.X, newData.Position.Y, newData.Position.Z);
            targetRotation = Quaternion.Euler(newData.Rotation.X, newData.Rotation.Y, newData.Rotation.Z);

            // Update animation state
            animController.SetWalkingState(newData.AnimationState.IsMoving);
            animController.SetTurningState(newData.AnimationState.IsTurning, newData.AnimationState.LookYaw);

            // Set movement animation values
            animController.SetMovementAnimation(
                new Vector2(
                    newData.AnimationState.HorizontalMovement,
                    newData.AnimationState.VerticalMovement
                )
            );

            // Set target spine look values
            targetCameraPitch = newData.LookDirection.X;
            targetYawDelta = newData.LookDirection.Y;

            // Handle one-time triggers
            if (newData.AnimationState.IsJumping && !oldData.AnimationState.IsJumping)
            {
                animController.TriggerJump();
            }
            if (newData.AnimationState.IsAttacking && !oldData.AnimationState.IsAttacking)
            {
                animController.TriggerAttack();
            }
        }

        // Update the health
        if (healthComponent.CurrentHealth != newData.Health)
        {
            healthComponent.SetHealth(newData.Health, newData.MaxHealth);
        }

        // Always check for death state, even if health didn't change
        if (healthComponent.CurrentHealth <= 0)
        {
            Die();
        }
    }

    private void Update()
    {
        if (!Initialized || IsLocalPlayer()) return;

        // Smoothly interpolate position using SmoothDamp
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, positionSmoothTime);

        // Smoothly interpolate rotation using Slerp
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationLerpSpeed * Time.deltaTime);

        // Smoothly interpolate spine look values
        float currentPitch = animController.cameraPitch;
        float currentYaw = animController.yawDelta;

        // Normalize angles before interpolation
        if (Mathf.Abs(targetCameraPitch - currentPitch) > 180f)
        {
            if (targetCameraPitch > currentPitch)
                currentPitch += 360f;
            else
                currentPitch -= 360f;
        }

        if (Mathf.Abs(targetYawDelta - currentYaw) > 180f)
        {
            if (targetYawDelta > currentYaw)
                currentYaw += 360f;
            else
                currentYaw -= 360f;
        }

        animController.cameraPitch = Mathf.Lerp(currentPitch, targetCameraPitch, spineLerpSpeed * Time.deltaTime);
        animController.yawDelta = Mathf.Lerp(currentYaw, targetYawDelta, spineLerpSpeed * Time.deltaTime);
    }

    public void ToggleInput()
    {
        input.enabled = !input.enabled;
        controller.enabled = !controller.enabled;
    }

    public override void TakeDamage(float damage)
    {
        if (IsLocalPlayer())
        {
            ConnectionManager.Conn.Reducers.ApplyDamage(ownerIdentity, damage);
        }
    }

    public override void ResetHealth()
    {
        if (IsLocalPlayer())
        {
            ConnectionManager.Conn.Reducers.ResetPlayerHealth(ownerIdentity);
        }
    }

    protected override void Die()
    {
        ResetHealth();
    }
}
