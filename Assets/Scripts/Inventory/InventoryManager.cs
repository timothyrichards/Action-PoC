using UnityEngine;
using System.Collections.Generic;
using SpacetimeDB;
using SpacetimeDB.Types;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    public System.Action<List<ItemRef>> OnInventoryChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        // Subscribe to SpacetimeDB connection events
        ConnectionManager.OnConnected += HandleConnected;

        // Subscribe to player table events
        if (ConnectionManager.Conn != null)
        {
            ConnectionManager.Conn.Db.Inventory.OnInsert += HandleInventoryInserted;
            ConnectionManager.Conn.Db.Inventory.OnUpdate += HandleInventoryUpdated;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from SpacetimeDB connection events
        ConnectionManager.OnConnected -= HandleConnected;

        if (ConnectionManager.Conn != null)
        {
            ConnectionManager.Conn.Db.Inventory.OnInsert -= HandleInventoryInserted;
            ConnectionManager.Conn.Db.Inventory.OnUpdate -= HandleInventoryUpdated;
        }
    }

    private void HandleConnected()
    {
        // Subscribe to table events now that we're connected
        ConnectionManager.Conn.Db.Inventory.OnInsert += HandleInventoryInserted;
        ConnectionManager.Conn.Db.Inventory.OnUpdate += HandleInventoryUpdated;

        // Add subscription for online players
        ConnectionManager.Instance.AddSubscription("select * from inventory");
    }

    private void HandleInventoryInserted(EventContext context, Inventory inventory)
    {
        if (PlayerManager.Instance.playerObjects.TryGetValue(inventory.Identity, out PlayerEntity playerEntity))
        {
            // TODO: this runs before the player entity is initialized
            playerEntity.inventory = inventory.Items;
            OnInventoryChanged?.Invoke(playerEntity.inventory);
        }
    }

    private void HandleInventoryUpdated(EventContext context, Inventory oldData, Inventory newData)
    {
        if (PlayerManager.Instance.playerObjects.TryGetValue(newData.Identity, out PlayerEntity playerEntity))
        {
            playerEntity.inventory = newData.Items;
            OnInventoryChanged?.Invoke(playerEntity.inventory);
        }
    }

    public void AddItem(PlayerEntity playerEntity, uint itemId, uint quantity = 1)
    {
        ConnectionManager.Conn.Reducers.InventoryAddItem(itemId, quantity);

        OnInventoryChanged?.Invoke(playerEntity.inventory);
    }

    public void RemoveItem(PlayerEntity playerEntity, uint itemId, uint quantity = 1)
    {
        ConnectionManager.Conn.Reducers.InventoryRemoveItem(itemId, quantity);

        OnInventoryChanged?.Invoke(playerEntity.inventory);
    }

    public List<ItemRef> GetInventory(PlayerEntity playerEntity)
    {
        return new List<ItemRef>(playerEntity.inventory);
    }

    public ItemRef GetItem(PlayerEntity playerEntity, uint itemId)
    {
        return playerEntity.inventory.Find(i => i.Id == itemId);
    }
}