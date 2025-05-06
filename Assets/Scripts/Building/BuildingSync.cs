using UnityEngine;
using SpacetimeDB.Types;
using System.Collections.Generic;

public class BuildingSync : MonoBehaviour
{
    private BuildingSystem buildingSystem;
    private Dictionary<uint, GameObject> spawnedPieces = new Dictionary<uint, GameObject>();

    private void OnEnable()
    {
        // Subscribe to SpacetimeDB connection events
        ConnectionManager.OnConnected += HandleConnected;
        ConnectionManager.OnSubscriptionApplied += HandleSubscriptionApplied;

        // Subscribe to building piece table events if already connected
        if (ConnectionManager.Conn != null)
        {
            ConnectionManager.Conn.Db.BuildingPiece.OnInsert += HandleBuildingPieceInserted;
            ConnectionManager.Conn.Db.BuildingPiece.OnDelete += HandleBuildingPieceDeleted;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from SpacetimeDB connection events
        ConnectionManager.OnConnected -= HandleConnected;
        ConnectionManager.OnSubscriptionApplied -= HandleSubscriptionApplied;

        if (ConnectionManager.Conn != null)
        {
            ConnectionManager.Conn.Db.BuildingPiece.OnInsert -= HandleBuildingPieceInserted;
            ConnectionManager.Conn.Db.BuildingPiece.OnDelete -= HandleBuildingPieceDeleted;
        }
    }

    private void Start()
    {
        buildingSystem = GetComponent<BuildingSystem>();
    }

    private void HandleConnected()
    {
        // Subscribe to table events now that we're connected
        ConnectionManager.Conn.Db.BuildingPiece.OnInsert += HandleBuildingPieceInserted;
        ConnectionManager.Conn.Db.BuildingPiece.OnDelete += HandleBuildingPieceDeleted;
    }

    private void HandleSubscriptionApplied()
    {
        Debug.Log("Subscription applied, waiting for building piece data...");
    }

    public void PlaceBuildingPiece(GameObject placedPiece, DbBuildingPieceType pieceType)
    {
        // Convert Unity Vector3 to DbVector3
        var position = placedPiece.transform.position;
        var rotation = placedPiece.transform.eulerAngles;

        var dbPosition = new DbVector3 { X = position.x, Y = position.y, Z = position.z };
        var dbRotation = new DbVector3 { X = rotation.x, Y = rotation.y, Z = rotation.z };

        // Convert Unity piece type to SpacetimeDB piece type
        var dbPieceType = pieceType;

        // Call the reducer to place the piece
        ConnectionManager.Conn.Reducers.PlaceBuildingPiece(dbPieceType, dbPosition, dbRotation);
    }

    public void RemoveBuildingPiece(uint pieceId)
    {
        // Call the reducer to remove the piece
        ConnectionManager.Conn.Reducers.RemoveBuildingPiece(pieceId);
    }

    private void HandleBuildingPieceInserted(EventContext context, DbBuildingPiece piece)
    {
        // Don't spawn if we already have this piece
        if (spawnedPieces.ContainsKey(piece.PieceId))
            return;

        // Convert SpacetimeDB piece type to Unity piece type
        var pieceType = piece.PieceType;

        // Get the appropriate prefab based on piece type
        GameObject prefab = pieceType switch
        {
            DbBuildingPieceType.Foundation => buildingSystem.foundationPrefabs[0],
            DbBuildingPieceType.Wall => buildingSystem.wallPrefabs[0],
            DbBuildingPieceType.Floor => buildingSystem.floorPrefabs[0],
            DbBuildingPieceType.Stair => buildingSystem.stairPrefabs[0],
            _ => null
        };

        if (prefab != null)
        {
            // Convert DbVector3 to Unity Vector3
            Vector3 position = new Vector3(piece.Position.X, piece.Position.Y, piece.Position.Z);
            Vector3 rotation = new Vector3(piece.Rotation.X, piece.Rotation.Y, piece.Rotation.Z);

            // Use BuildingSystem to place the piece
            GameObject spawnedPiece = buildingSystem.PlacePieceAtPosition(prefab, position, Quaternion.Euler(rotation));

            // Store the piece ID in the BuildingPiece component
            var buildingPieceComponent = spawnedPiece.GetComponent<BuildingPiece>();
            if (buildingPieceComponent != null)
            {
                buildingPieceComponent.PieceId = piece.PieceId;
            }

            // Store the piece in our dictionary
            spawnedPieces[piece.PieceId] = spawnedPiece;
        }
    }

    private void HandleBuildingPieceDeleted(EventContext context, DbBuildingPiece piece)
    {
        if (spawnedPieces.TryGetValue(piece.PieceId, out GameObject spawnedPiece))
        {
            Destroy(spawnedPiece);
            spawnedPieces.Remove(piece.PieceId);
        }
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
}