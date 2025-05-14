using UnityEngine;
using SpacetimeDB.Types;

public class CreativeCameraSync : MonoBehaviour
{
    private void OnEnable()
    {
        // Subscribe to SpacetimeDB connection events
        SpacetimeManager.OnConnected += HandleConnected;

        // Subscribe to player table events
        if (SpacetimeManager.Conn != null)
        {
            SpacetimeManager.Conn.Db.CreativeCamera.OnInsert += HandleCreativeCameraInserted;
            SpacetimeManager.Conn.Db.CreativeCamera.OnUpdate += HandleCreativeCameraUpdated;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from SpacetimeDB connection events
        SpacetimeManager.OnConnected -= HandleConnected;

        if (SpacetimeManager.Conn != null)
        {
            SpacetimeManager.Conn.Db.CreativeCamera.OnInsert -= HandleCreativeCameraInserted;
            SpacetimeManager.Conn.Db.CreativeCamera.OnUpdate -= HandleCreativeCameraUpdated;
        }
    }

    private void HandleConnected()
    {
        // Subscribe to table events now that we're connected
        SpacetimeManager.Conn.Db.CreativeCamera.OnInsert += HandleCreativeCameraInserted;
        SpacetimeManager.Conn.Db.CreativeCamera.OnUpdate += HandleCreativeCameraUpdated;

        // Add subscription for online players
        SpacetimeManager.Instance.AddSubscription("select * from creative_camera");
    }

    private void HandleCreativeCameraInserted(EventContext context, CreativeCamera creativeCamera)
    {
        if (PlayerSync.playerObjects.TryGetValue(creativeCamera.Identity, out PlayerEntity playerEntity))
        {
            playerEntity.UpdateFromCreativeCameraData(creativeCamera);
        }
    }

    private void HandleCreativeCameraUpdated(EventContext context, CreativeCamera oldData, CreativeCamera newData)
    {
        if (PlayerSync.playerObjects.TryGetValue(newData.Identity, out PlayerEntity playerEntity))
        {
            playerEntity.UpdateFromCreativeCameraData(newData);
        }
    }
}
