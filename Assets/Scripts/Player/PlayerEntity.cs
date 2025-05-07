using SpacetimeDB;
using SpacetimeDB.Types;
using ThirdPersonCamera;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerEntity : Entity
{
    [Header("Runtime")]
    public Identity ownerIdentity;

    [Header("References")]
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
    public PlayerInput input;
    public ThirdPersonController controller;
    public AnimationController animController;
    public GameObject nameplate;

    protected override void Awake()
    {
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
            playerHealthDisplay.damageableObject = this;
            playerHealthDisplay.enabled = true;
        }
        else
        {
            // Disable input and third person controller for other players
            input.enabled = false;
            controller.enabled = false;
        }
    }

    public void ToggleInput()
    {
        input.enabled = !input.enabled;
        controller.enabled = !controller.enabled;
    }

    public override void ResetHealth()
    {
        if (IsLocalPlayer())
        {
            ConnectionManager.Conn.Reducers.ResetPlayerHealth(ownerIdentity);
        }
    }

    public override void TakeDamage(float damage)
    {
        if (IsLocalPlayer())
        {
            ConnectionManager.Conn.Reducers.ApplyDamage(ownerIdentity, damage);
        }
    }

    public override void Die()
    {
        base.Die();

        ResetHealth();
    }
}
