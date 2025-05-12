using UnityEngine;
using System.Collections.Generic;
using SpacetimeDB;
using SpacetimeDB.Types;
using ThirdPersonCamera;

public class PlayerManager : MonoBehaviour
{
    private static PlayerManager _instance;
    public static PlayerManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<PlayerManager>();
            }

            return _instance;
        }
    }

    [Header("Runtime")]
    public readonly Dictionary<Identity, PlayerEntity> playerObjects = new();

    [Header("References")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private CameraController playerCamera;
    [SerializeField] private HealthDisplay playerHealthDisplay;

    private void OnEnable()
    {
        // Subscribe to SpacetimeDB connection events
        ConnectionManager.OnConnected += HandleConnected;

        // Subscribe to player table events
        if (ConnectionManager.Conn != null)
        {
            ConnectionManager.Conn.Db.Player.OnInsert += HandlePlayerJoined;
            ConnectionManager.Conn.Db.Player.OnDelete += HandlePlayerLeft;
            ConnectionManager.Conn.Db.Player.OnUpdate += HandlePlayerUpdated;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from SpacetimeDB connection events
        ConnectionManager.OnConnected -= HandleConnected;

        if (ConnectionManager.Conn != null)
        {
            ConnectionManager.Conn.Db.Player.OnInsert -= HandlePlayerJoined;
            ConnectionManager.Conn.Db.Player.OnDelete -= HandlePlayerLeft;
            ConnectionManager.Conn.Db.Player.OnUpdate -= HandlePlayerUpdated;
        }
    }

    private void HandleConnected()
    {
        // Subscribe to table events now that we're connected
        ConnectionManager.Conn.Db.Player.OnInsert += HandlePlayerJoined;
        ConnectionManager.Conn.Db.Player.OnDelete += HandlePlayerLeft;
        ConnectionManager.Conn.Db.Player.OnUpdate += HandlePlayerUpdated;

        // Add subscription for online players
        ConnectionManager.Instance.AddSubscription("select * from player where online = true");

        // Send a player connected reducer to the server
        ConnectionManager.Conn.Reducers.PlayerConnected();
    }

    private void HandlePlayerJoined(EventContext context, Player playerData)
    {
        if (playerObjects.ContainsKey(playerData.Identity))
            return;

        // Instantiate the player object
        var position = new Vector3(playerData.Position.X, playerData.Position.Y, playerData.Position.Z);
        var rotation = Quaternion.Euler(playerData.Rotation.X, playerData.Rotation.Y, playerData.Rotation.Z);
        var playerObject = Instantiate(playerPrefab, position, rotation);
        var playerEntity = playerObject.GetComponent<PlayerEntity>();

        // Store the player object reference
        playerObjects[playerData.Identity] = playerEntity;

        // Set the owner identity
        playerEntity.ownerIdentity = playerData.Identity;

        // Configure the player object based on whether it's the local player
        Configure(playerEntity, playerData, playerCamera, playerHealthDisplay);
    }

    private void HandlePlayerLeft(EventContext context, Player player)
    {
        if (playerObjects.TryGetValue(player.Identity, out PlayerEntity playerEntity))
        {
            Destroy(playerEntity.gameObject);
            playerObjects.Remove(player.Identity);
        }
    }

    private void HandlePlayerUpdated(EventContext context, Player oldData, Player newData)
    {
        if (playerObjects.TryGetValue(newData.Identity, out PlayerEntity playerEntity))
        {
            UpdateFromPlayer(playerEntity, oldData, newData);
        }
    }

    public void Configure(PlayerEntity player, Player playerData, CameraController playerCamera = null, HealthDisplay playerHealthDisplay = null)
    {
        // If the player is the local player, enable input and camera
        if (player.IsLocalPlayer())
        {
            PlayerEntity.LocalPlayer = player;

            // Enable input and third person controller for local player
            player.SetInputState(true);
            player.input.enabled = true;
            player.controller.enabled = true;

            // Configure the camera
            playerCamera.target = player.transform;
            player.CameraFreeForm = playerCamera.GetComponent<FreeForm>();
            player.CameraFreeForm.transform.eulerAngles = new Vector3(playerData.LookDirection.X, playerData.Rotation.Y, playerData.Rotation.Z);

            // Configure the health display
            player.nameplate.SetActive(false);
            playerHealthDisplay.healthComponent = player.HealthComponent;
            playerHealthDisplay.enabled = true;
        }
        else
        {
            // Disable input and third person controller for other players
            player.SetInputState(false);
            player.input.enabled = false;
            player.controller.enabled = false;

            // Initialize target values for interpolation
            player.targetPosition = new Vector3(playerData.Position.X, playerData.Position.Y, playerData.Position.Z);
            player.targetRotation = Quaternion.Euler(playerData.Rotation.X, playerData.Rotation.Y, playerData.Rotation.Z);
            player.targetCameraPitch = playerData.LookDirection.X;
            player.targetYawDelta = playerData.LookDirection.Y;

            // Set initial transform values to match targets
            player.transform.position = player.targetPosition;
            player.transform.rotation = player.targetRotation;
            player.animController.cameraPitch = player.targetCameraPitch;
            player.animController.yawDelta = player.targetYawDelta;
        }

        // Update the health
        if (playerData.Health != player.CurrentHealth)
        {
            player.HealthComponent.SetHealth(playerData.Health, playerData.MaxHealth);
        }

        player.initialized = true;
    }

    public void UpdateFromPlayer(PlayerEntity player, Player oldData, Player newData)
    {
        // Only update position and rotation for non-local players
        if (!player.IsLocalPlayer())
        {
            // Set target transform values
            player.targetPosition = new Vector3(newData.Position.X, newData.Position.Y, newData.Position.Z);
            player.targetRotation = Quaternion.Euler(newData.Rotation.X, newData.Rotation.Y, newData.Rotation.Z);

            // Set movement animation values
            player.animController.SetMovementAnimation(
                new Vector2(
                    newData.AnimationState.HorizontalMovement,
                    newData.AnimationState.VerticalMovement
                ),
                newData.AnimationState.IsMoving
            );

            // Set turning animation values
            player.animController.SetTurningState(newData.AnimationState.IsTurning, newData.AnimationState.LookYaw);

            // Set target spine look values
            player.targetCameraPitch = newData.LookDirection.X;
            player.targetYawDelta = newData.LookDirection.Y;

            // Handle one-time triggers
            if (newData.AnimationState.IsJumping && !oldData.AnimationState.IsJumping)
            {
                player.animController.TriggerJump();
            }
            if (newData.AnimationState.IsAttacking && !oldData.AnimationState.IsAttacking)
            {
                player.animController.TriggerAttack();
            }
        }

        // Update the health
        if (player.CurrentHealth != newData.Health)
        {
            player.HealthComponent.SetHealth(newData.Health, newData.MaxHealth);
        }
    }
}
