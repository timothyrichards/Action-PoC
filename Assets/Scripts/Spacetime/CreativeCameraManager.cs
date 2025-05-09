using UnityEngine;
using SpacetimeDB.Types;

public class CreativeCameraManager : MonoBehaviour
{
    private void OnEnable()
    {
        // Subscribe to SpacetimeDB connection events
        ConnectionManager.OnConnected += HandleConnected;

        // Subscribe to player table events
        if (ConnectionManager.Conn != null)
        {
            ConnectionManager.Conn.Db.CreativeCamera.OnInsert += HandleCreativeCameraInserted;
            ConnectionManager.Conn.Db.CreativeCamera.OnUpdate += HandleCreativeCameraUpdated;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from SpacetimeDB connection events
        ConnectionManager.OnConnected -= HandleConnected;

        if (ConnectionManager.Conn != null)
        {
            ConnectionManager.Conn.Db.CreativeCamera.OnInsert -= HandleCreativeCameraInserted;
            ConnectionManager.Conn.Db.CreativeCamera.OnUpdate -= HandleCreativeCameraUpdated;
        }
    }

    private void HandleConnected()
    {
        // Subscribe to table events now that we're connected
        ConnectionManager.Conn.Db.CreativeCamera.OnInsert += HandleCreativeCameraInserted;
        ConnectionManager.Conn.Db.CreativeCamera.OnUpdate += HandleCreativeCameraUpdated;

        // Add subscription for online players
        ConnectionManager.Instance.AddSubscription("select * from creative_camera");
    }

    private void HandleCreativeCameraInserted(EventContext context, CreativeCamera creativeCamera)
    {
        if (PlayerManager.Instance.playerObjects.TryGetValue(creativeCamera.Identity, out PlayerEntity playerEntity))
        {
            UpdateFromCreativeCamera(playerEntity, creativeCamera, true);
        }
    }

    private void HandleCreativeCameraUpdated(EventContext context, CreativeCamera oldData, CreativeCamera newData)
    {
        if (PlayerManager.Instance.playerObjects.TryGetValue(newData.Identity, out PlayerEntity playerEntity))
        {
            UpdateFromCreativeCamera(playerEntity, newData, oldData.Enabled != newData.Enabled);
        }
    }

    public void UpdateFromCreativeCamera(PlayerEntity playerEntity, CreativeCamera creativeCamera, bool instantUpdate = false)
    {
        if (playerEntity.IsLocalPlayer()) return;

        var creativeMode = playerEntity.creativeMode;
        var flyingCamera = creativeMode.flyingCamera;

        creativeMode.targetPosition = new Vector3(creativeCamera.Position.X, creativeCamera.Position.Y, creativeCamera.Position.Z);
        creativeMode.targetRotation = Quaternion.Euler(creativeCamera.Rotation.X, creativeCamera.Rotation.Y, creativeCamera.Rotation.Z);
        flyingCamera.gameObject.SetActive(creativeCamera.Enabled);
    }
}
