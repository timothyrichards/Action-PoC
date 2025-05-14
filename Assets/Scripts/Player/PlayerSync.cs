using UnityEngine;
using System.Collections.Generic;
using SpacetimeDB;
using SpacetimeDB.Types;
using ThirdPersonCamera;

public class PlayerSync : MonoBehaviour
{
    [Header("Runtime")]
    public static readonly Dictionary<Identity, PlayerEntity> playerObjects = new();

    [Header("References")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private CameraController playerCamera;
    [SerializeField] private HealthDisplay playerHealthDisplay;

    private void OnEnable()
    {
        // Subscribe to SpacetimeDB connection events
        SpacetimeManager.OnConnected += HandleConnected;

        // Subscribe to player table events
        if (SpacetimeManager.Conn != null)
        {
            SpacetimeManager.Conn.Db.Player.OnInsert += HandlePlayerJoined;
            SpacetimeManager.Conn.Db.Player.OnDelete += HandlePlayerLeft;
            SpacetimeManager.Conn.Db.Player.OnUpdate += HandlePlayerUpdated;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from SpacetimeDB connection events
        SpacetimeManager.OnConnected -= HandleConnected;

        if (SpacetimeManager.Conn != null)
        {
            SpacetimeManager.Conn.Db.Player.OnInsert -= HandlePlayerJoined;
            SpacetimeManager.Conn.Db.Player.OnDelete -= HandlePlayerLeft;
            SpacetimeManager.Conn.Db.Player.OnUpdate -= HandlePlayerUpdated;
        }
    }

    private void HandleConnected()
    {
        // Subscribe to table events now that we're connected
        SpacetimeManager.Conn.Db.Player.OnInsert += HandlePlayerJoined;
        SpacetimeManager.Conn.Db.Player.OnDelete += HandlePlayerLeft;
        SpacetimeManager.Conn.Db.Player.OnUpdate += HandlePlayerUpdated;

        // Add subscription for online players
        SpacetimeManager.Instance.AddSubscription("select * from player where online = true");

        // Send a player connected reducer to the server
        SpacetimeManager.Conn.Reducers.PlayerConnected();
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

        // Set the inventory
        playerEntity.inventory = InventorySync.GetInventory(playerEntity).Items;

        // Configure the player object based on whether it's the local player
        playerEntity.Configure(playerData, playerCamera, playerHealthDisplay);
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
            playerEntity.UpdateFromPlayerData(oldData, newData);
        }
    }
}
