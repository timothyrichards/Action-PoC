using UnityEngine;
using System.Collections.Generic;
using SpacetimeDB;
using SpacetimeDB.Types;
using ThirdPersonCamera;

public class PlayerSpawner : MonoBehaviour
{
    [Header("References")]
    public GameObject playerPrefab;
    public CameraController playerCamera;
    public HealthDisplay playerHealthDisplay;

    private Dictionary<Identity, GameObject> playerObjects = new Dictionary<Identity, GameObject>();
    private Identity localPlayerId;

    private void OnEnable()
    {
        // Subscribe to SpacetimeDB connection events
        ConnectionManager.OnConnected += HandleConnected;
        ConnectionManager.OnSubscriptionApplied += HandleSubscriptionApplied;

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
        ConnectionManager.OnSubscriptionApplied -= HandleSubscriptionApplied;

        if (ConnectionManager.Conn != null)
        {
            ConnectionManager.Conn.Db.Player.OnInsert -= HandlePlayerJoined;
            ConnectionManager.Conn.Db.Player.OnDelete -= HandlePlayerLeft;
            ConnectionManager.Conn.Db.Player.OnUpdate -= HandlePlayerUpdated;
        }
    }

    private void HandleConnected()
    {
        // Store the local player's identity
        localPlayerId = ConnectionManager.LocalIdentity;

        // Subscribe to table events now that we're connected
        ConnectionManager.Conn.Db.Player.OnInsert += HandlePlayerJoined;
        ConnectionManager.Conn.Db.Player.OnDelete += HandlePlayerLeft;
        ConnectionManager.Conn.Db.Player.OnUpdate += HandlePlayerUpdated;
    }

    private void HandleSubscriptionApplied()
    {
        // No need to do anything here - OnInsert events will be triggered for existing players
        Debug.Log("Subscription applied, waiting for player data...");
    }

    private void HandlePlayerJoined(EventContext context, Player player)
    {
        if (playerObjects.ContainsKey(player.Identity))
            return;

        // Instantiate the player object
        Vector3 position = new(player.Position.X, player.Position.Y, player.Position.Z);
        Quaternion rotation = Quaternion.Euler(player.Rotation.X, player.Rotation.Y, player.Rotation.Z);
        GameObject playerObject = Instantiate(playerPrefab, position, rotation);

        // Set the owner identity
        var playerEntity = playerObject.GetComponent<PlayerEntity>();
        playerEntity.ownerIdentity = player.Identity;

        // Configure the player object based on whether it's the local player
        playerEntity.Configure(player, playerCamera, playerHealthDisplay);

        // Set the health
        playerEntity.health = player.Health;
        playerEntity.maxHealth = player.MaxHealth;
        playerEntity.OnHealthChanged?.Invoke(player.Health);

        // Store the player object reference
        playerObjects[player.Identity] = playerObject;
    }

    private void HandlePlayerLeft(EventContext context, Player player)
    {
        if (playerObjects.TryGetValue(player.Identity, out GameObject playerObject))
        {
            Destroy(playerObject);
            playerObjects.Remove(player.Identity);
        }
    }

    private void HandlePlayerUpdated(EventContext context, Player oldPlayer, Player newPlayer)
    {
        if (playerObjects.TryGetValue(newPlayer.Identity, out GameObject playerObject))
        {
            var player = playerObject.GetComponent<PlayerEntity>();

            // Only update position and rotation for non-local players
            if (!newPlayer.Identity.Equals(localPlayerId))
            {

                // Update transform
                playerObject.transform.position = new Vector3(newPlayer.Position.X, newPlayer.Position.Y, newPlayer.Position.Z);
                playerObject.transform.rotation = Quaternion.Euler(newPlayer.Rotation.X, newPlayer.Rotation.Y, newPlayer.Rotation.Z);

                // Update animation state
                var animController = player.animController;

                // Set basic movement states
                animController.SetWalkingState(newPlayer.AnimationState.IsMoving);
                animController.SetTurningState(newPlayer.AnimationState.IsTurning, newPlayer.AnimationState.LookYaw);

                // Set movement animation values
                animController.SetMovementAnimation(
                    new Vector2(
                        newPlayer.AnimationState.HorizontalMovement,
                        newPlayer.AnimationState.VerticalMovement
                    )
                );

                // Update spine look values
                animController.cameraPitch = newPlayer.LookDirection.X;
                animController.yawDelta = newPlayer.LookDirection.Y;

                // Handle one-time triggers
                if (newPlayer.AnimationState.IsJumping && !oldPlayer.AnimationState.IsJumping)
                {
                    animController.TriggerJump();
                }
                if (newPlayer.AnimationState.IsAttacking && !oldPlayer.AnimationState.IsAttacking)
                {
                    animController.TriggerAttack();
                }
            }

            if (player.health != newPlayer.Health)
            {
                player.health = newPlayer.Health;
                player.maxHealth = newPlayer.MaxHealth;
                player.OnHealthChanged?.Invoke(player.health);
            }

            if (player.health <= 0)
            {
                player.Die();
            }
        }
    }
}
