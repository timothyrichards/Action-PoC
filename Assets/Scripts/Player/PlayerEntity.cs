using SpacetimeDB;
using SpacetimeDB.Types;
using ThirdPersonCamera;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerEntity : Entity
{
    private FreeForm _cameraFreeForm;

    [Header("Runtime")]
    public Identity ownerIdentity;
    public bool Initialized { get; private set; }

    [Header("References")]
    public PlayerInput input;
    public ThirdPersonController controller;
    public AnimationController animController;
    public GameObject nameplate;

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
            // Update transform
            transform.position = new Vector3(newData.Position.X, newData.Position.Y, newData.Position.Z);
            transform.rotation = Quaternion.Euler(newData.Rotation.X, newData.Rotation.Y, newData.Rotation.Z);

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

            // Update spine look values
            animController.cameraPitch = newData.LookDirection.X;
            animController.yawDelta = newData.LookDirection.Y;

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
