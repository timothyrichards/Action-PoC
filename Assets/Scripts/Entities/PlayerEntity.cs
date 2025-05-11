using System;
using System.Collections.Generic;
using QFSW.QC;
using SpacetimeDB;
using ThirdPersonCamera;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerEntity : Entity
{
    [Header("Runtime")]
    private Vector3 currentVelocity;
    private FreeForm _cameraFreeForm;
    public Identity ownerIdentity;
    public bool initialized = false;
    public bool InputEnabled { get; private set; } = true;
    public float CurrentHealth => HealthComponent.CurrentHealth;
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
    public PlayerInput input;
    public ThirdPersonController controller;
    public CreativeMode creativeMode;
    public AnimationController animController;
    public GameObject nameplate;

    [Header("Interpolation Settings")]
    public Vector3 targetPosition;
    public Quaternion targetRotation;
    public float targetCameraPitch;
    public float targetYawDelta;
    public float positionSmoothTime = 0.15f;
    public float rotationLerpSpeed = 8f;
    public float spineLerpSpeed = 8f;

    private QuantumConsole _quantumConsole;
    private HashSet<object> disableInputRequests = new();

    protected override void Awake()
    {
        base.Awake();

        input = GetComponent<PlayerInput>();
        controller = GetComponent<ThirdPersonController>();
        animController = GetComponent<AnimationController>();
        creativeMode = GetComponent<CreativeMode>();
        _quantumConsole = QuantumConsole.Instance ?? FindFirstObjectByType<QuantumConsole>();
    }

    void OnEnable()
    {
        _quantumConsole.OnActivate += OnActivate;
        _quantumConsole.OnDeactivate += OnDeactivate;
    }

    void OnDisable()
    {
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
        if (ConnectionManager.LocalIdentity == null)
        {
            Debug.LogWarning("Could not determine if player is local because no local identity found in ConnectionManager.");
            return true;
        }

        return ownerIdentity.Equals(ConnectionManager.LocalIdentity);
    }

    public override void TakeDamage(float damage)
    {
        if (IsLocalPlayer())
        {
            ConnectionManager.Conn.Reducers.PlayerApplyDamage(ownerIdentity, damage);
        }
    }

    public override void ResetHealth()
    {
        if (ConnectionManager.IsConnected() && IsLocalPlayer())
        {
            ConnectionManager.Conn.Reducers.PlayerResetHealth(ownerIdentity);
        }
    }

    protected override void Die()
    {
        ResetHealth();
    }
}
