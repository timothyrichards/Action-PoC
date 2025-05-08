using UnityEngine;
using System.Collections.Generic;
using SpacetimeDB;
using SpacetimeDB.Types;
using ThirdPersonCamera;

public class PlayerSync : MonoBehaviour
{
    private static PlayerSync _instance;
    public static PlayerSync Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<PlayerSync>();
            }

            return _instance;
        }
    }

    [Header("References")]
    public GameObject playerPrefab;
    public CameraController playerCamera;
    public HealthDisplay playerHealthDisplay;

    private Dictionary<Identity, GameObject> playerObjects = new();

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
    }

    private void HandlePlayerJoined(EventContext context, Player player)
    {
        if (playerObjects.ContainsKey(player.Identity))
            return;

        // Instantiate the player object
        var position = new Vector3(player.Position.X, player.Position.Y, player.Position.Z);
        var rotation = Quaternion.Euler(player.Rotation.X, player.Rotation.Y, player.Rotation.Z);
        var playerObject = Instantiate(playerPrefab, position, rotation);

        // Set the owner identity
        var playerEntity = playerObject.GetComponent<PlayerEntity>();
        playerEntity.ownerIdentity = player.Identity;

        // Configure the player object based on whether it's the local player
        playerEntity.Configure(player, playerCamera, playerHealthDisplay);

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

    private void HandlePlayerUpdated(EventContext context, Player oldData, Player newData)
    {
        if (playerObjects.TryGetValue(newData.Identity, out GameObject playerObject))
        {
            var player = playerObject.GetComponent<PlayerEntity>();
            player.UpdateFromPlayer(oldData, newData);
        }
    }
}
