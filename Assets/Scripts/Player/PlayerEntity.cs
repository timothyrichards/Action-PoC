using System.Collections.Generic;
using QFSW.QC;
using SpacetimeDB;
using SpacetimeDB.Types;
using ThirdPersonCamera;
using UnityEngine;

public class PlayerEntity : Entity
{
    public static PlayerEntity LocalPlayer;

    [Header("Runtime")]
    public bool initialized = false;
    public Identity ownerIdentity;
    public bool InputEnabled { get; private set; } = true;
    public float CurrentHealth => HealthComponent.CurrentHealth;
    public List<ItemRef> inventory = new();
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

    [Header("References")]
    public ThirdPersonController controller;
    public CreativeMode creativeMode;
    public AnimationController animController;
    public GameObject nameplate;

    [Header("Interpolation Settings")]
    public Vector3 targetPosition;
    public Quaternion targetRotation;
    public float positionSmoothTime = 0.15f;
    public float rotationLerpSpeed = 8f;

    private QuantumConsole _quantumConsole;
    private readonly HashSet<object> disableInputRequests = new();

    protected override void Awake()
    {
        base.Awake();

        controller = GetComponent<ThirdPersonController>();
        animController = GetComponent<AnimationController>();
        creativeMode = GetComponent<CreativeMode>();
        _quantumConsole = QuantumConsole.Instance ?? FindFirstObjectByType<QuantumConsole>();
    }

    private void OnEnable()
    {
        HealthComponent.OnHealthChanged += OnHealthChanged;
        _quantumConsole.OnActivate += OnActivate;
        _quantumConsole.OnDeactivate += OnDeactivate;
    }

    private void OnDisable()
    {
        HealthComponent.OnHealthChanged -= OnHealthChanged;
        _quantumConsole.OnActivate -= OnActivate;
        _quantumConsole.OnDeactivate -= OnDeactivate;
    }

    private void Update()
    {
        if (!initialized || IsLocalPlayer()) return;

        // Smoothly interpolate position using SmoothDamp
        var distance = Vector3.Distance(transform.position, targetPosition);
        transform.position = distance > 5f ? targetPosition : Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, positionSmoothTime);

        // Smoothly interpolate rotation using Slerp
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationLerpSpeed * Time.deltaTime);
    }

    public void Configure(Player playerData, CameraController playerCamera = null, HealthDisplay playerHealthDisplay = null)
    {
        var isLocalPlayer = IsLocalPlayer();

        // Set input and third person controller
        SetInputState(isLocalPlayer);
        controller.enabled = isLocalPlayer;

        // If the player is the local player, enable input and camera
        if (isLocalPlayer)
        {
            LocalPlayer = this;

            // Configure the camera
            playerCamera.target = transform;
            CameraFreeForm = playerCamera.GetComponent<FreeForm>();
            CameraFreeForm.transform.eulerAngles = new Vector3(playerData.LookDirection.X, playerData.Rotation.Y, playerData.Rotation.Z);

            // Configure the health display
            nameplate.SetActive(false);
            playerHealthDisplay.healthComponent = HealthComponent;
            playerHealthDisplay.enabled = true;
        }
        else
        {
            // Initialize target values for interpolation
            targetPosition = new Vector3(playerData.Position.X, playerData.Position.Y, playerData.Position.Z);
            targetRotation = Quaternion.Euler(playerData.Rotation.X, playerData.Rotation.Y, playerData.Rotation.Z);

            // Set initial transform values to match targets
            transform.position = targetPosition;
            transform.rotation = targetRotation;
        }

        // Update the health
        if (playerData.Health != CurrentHealth)
        {
            HealthComponent.SetHealth(playerData.Health, playerData.MaxHealth);
        }

        initialized = true;
    }

    public void UpdateFromPlayerData(Player oldData, Player newData)
    {
        // Only update position and rotation for non-local players
        if (!IsLocalPlayer())
        {
            // Set target transform values
            targetPosition = new Vector3(newData.Position.X, newData.Position.Y, newData.Position.Z);
            targetRotation = Quaternion.Euler(newData.Rotation.X, newData.Rotation.Y, newData.Rotation.Z);

            // Set movement animation values
            animController.SetMovementAnimation(
                new Vector2(
                    newData.AnimationState.HorizontalMovement,
                    newData.AnimationState.VerticalMovement
                ),
                newData.AnimationState.IsMoving
            );

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
        if (CurrentHealth != newData.Health)
        {
            HealthComponent.SetHealth(newData.Health, newData.MaxHealth);
        }
    }

    public void UpdateFromCreativeCameraData(CreativeCamera creativeCamera)
    {
        if (IsLocalPlayer()) return;

        var flyingCamera = creativeMode.flyingCamera;

        creativeMode.targetPosition = new Vector3(creativeCamera.Position.X, creativeCamera.Position.Y, creativeCamera.Position.Z);
        creativeMode.targetRotation = Quaternion.Euler(creativeCamera.Rotation.X, creativeCamera.Rotation.Y, creativeCamera.Rotation.Z);
        flyingCamera.gameObject.SetActive(creativeCamera.Enabled);
    }

    private void OnHealthChanged(float health)
    {
        if (!IsLocalPlayer()) return;

        if (health <= 0)
        {
            Die();
        }
    }

    private void OnActivate()
    {
        RequestInputDisabled(this);
    }

    private void OnDeactivate()
    {
        RequestInputEnabled(this);
    }

    public void RequestInputDisabled(object requester)
    {
        disableInputRequests.Add(requester);
        UpdateInputState();
    }

    public void RequestInputEnabled(object requester)
    {
        disableInputRequests.Remove(requester);
        UpdateInputState();
    }

    private void UpdateInputState()
    {
        SetInputState(disableInputRequests.Count == 0);
    }

    public void SetInputState(bool enabled)
    {
        InputEnabled = enabled;
    }

    public bool IsLocalPlayer()
    {
        if (SpacetimeManager.LocalIdentity == null)
        {
            Debug.LogWarning("Could not determine if player is local because no local identity found in ConnectionManager.");
            return true;
        }

        return ownerIdentity.Equals(SpacetimeManager.LocalIdentity);
    }

    public override void Attack(Entity target, float damage)
    {
        if (IsLocalPlayer() && target is PlayerEntity playerTarget)
        {
            SpacetimeManager.Conn.Reducers.PlayerApplyDamage(playerTarget.ownerIdentity, damage);
            return;
        }

        base.Attack(target, damage);
    }

    public override void TakeDamage(float damage)
    {
        Debug.Log($"PlayerEntity {ownerIdentity} took {damage} damage");
    }

    public override void ResetHealth()
    {
        if (IsLocalPlayer())
        {
            SpacetimeManager.Conn.Reducers.PlayerResetHealth(ownerIdentity);
        }
    }

    protected override void Die()
    {
        ResetHealth();
    }
}
