using UnityEngine;
using SpacetimeDB.Types;
using System.Collections.Generic;

public class BuildingManager : MonoBehaviour
{
    [SerializeField]
    private BuildingPieceDatabase buildingPieceDatabase;
    private BuildingSystem buildingSystem;
    private Dictionary<uint, GameObject> spawnedPieces = new Dictionary<uint, GameObject>();

    private void Awake()
    {
        buildingSystem = GetComponent<BuildingSystem>();
    }

    private void OnEnable()
    {
        // Subscribe to SpacetimeDB connection events
        ConnectionManager.OnConnected += HandleConnected;

        // Subscribe to building piece table events if already connected
        if (ConnectionManager.Conn != null)
        {
            ConnectionManager.Conn.Db.BuildingPiecePlaced.OnInsert += HandleBuildingPieceInserted;
            ConnectionManager.Conn.Db.BuildingPiecePlaced.OnDelete += HandleBuildingPieceDeleted;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from SpacetimeDB connection events
        ConnectionManager.OnConnected -= HandleConnected;

        if (ConnectionManager.Conn != null)
        {
            ConnectionManager.Conn.Db.BuildingPiecePlaced.OnInsert -= HandleBuildingPieceInserted;
            ConnectionManager.Conn.Db.BuildingPiecePlaced.OnDelete -= HandleBuildingPieceDeleted;
        }
    }

    private void HandleConnected()
    {
        // Subscribe to table events now that we're connected
        ConnectionManager.Conn.Db.BuildingPiecePlaced.OnInsert += HandleBuildingPieceInserted;
        ConnectionManager.Conn.Db.BuildingPiecePlaced.OnDelete += HandleBuildingPieceDeleted;

        // Add subscription for building pieces
        ConnectionManager.Instance.AddSubscription("select * from building_piece_variant");
        ConnectionManager.Instance.AddSubscription("select * from building_piece_placed");
    }

    public void BuildingPiecePlace(uint variantId, GameObject placedPiece)
    {
        // Convert Unity Vector3 to DbVector3
        var position = placedPiece.transform.position;
        var rotation = placedPiece.transform.eulerAngles;

        var dbPosition = new DbVector3 { X = position.x, Y = position.y, Z = position.z };
        var dbRotation = new DbVector3 { X = rotation.x, Y = rotation.y, Z = rotation.z };

        // Call the reducer to place the piece
        ConnectionManager.Conn.Reducers.BuildingPiecePlace(variantId, dbPosition, dbRotation);
    }

    public void BuildingPieceRemove(uint pieceId)
    {
        // Call the reducer to remove the piece
        ConnectionManager.Conn.Reducers.BuildingPieceRemove(pieceId);
    }

    private void HandleBuildingPieceInserted(EventContext context, DbBuildingPiecePlaced piece)
    {
        // Don't spawn if we already have this piece
        if (spawnedPieces.ContainsKey(piece.PieceId))
            return;

        // Get the prefab from the variant database
        GameObject prefab = buildingPieceDatabase.GetPrefabByVariantId(piece.VariantId).gameObject;

        if (prefab != null)
        {
            // Convert DbVector3 to Unity Vector3
            Vector3 position = new(piece.Position.X, piece.Position.Y, piece.Position.Z);
            Vector3 rotation = new(piece.Rotation.X, piece.Rotation.Y, piece.Rotation.Z);

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
        else
        {
            Debug.LogError($"Failed to find prefab for variant ID: {piece.VariantId}");
        }
    }

    private void HandleBuildingPieceDeleted(EventContext context, DbBuildingPiecePlaced piece)
    {
        if (spawnedPieces.TryGetValue(piece.PieceId, out GameObject spawnedPiece))
        {
            Destroy(spawnedPiece);
            spawnedPieces.Remove(piece.PieceId);
        }
    }
}