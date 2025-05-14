using UnityEngine;
using SpacetimeDB.Types;
using System.Collections.Generic;
using System.Linq;

public class BuildingSync : MonoBehaviour
{
    [SerializeField] private BuildingPieceDatabase buildingPieceDatabase;
    private BuildingSystem buildingSystem;
    private Dictionary<uint, GameObject> spawnedPieces = new Dictionary<uint, GameObject>();

    private void Awake()
    {
        buildingSystem = GetComponent<BuildingSystem>();
    }

    private void OnEnable()
    {
        // Subscribe to SpacetimeDB connection events
        SpacetimeManager.OnConnected += HandleConnected;

        // Subscribe to building piece table events if already connected
        if (SpacetimeManager.Conn != null)
        {
            SpacetimeManager.Conn.Db.BuildingPiecePlaced.OnInsert += HandleBuildingPieceInserted;
            SpacetimeManager.Conn.Db.BuildingPiecePlaced.OnDelete += HandleBuildingPieceDeleted;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from SpacetimeDB connection events
        SpacetimeManager.OnConnected -= HandleConnected;

        if (SpacetimeManager.Conn != null)
        {
            SpacetimeManager.Conn.Db.BuildingPiecePlaced.OnInsert -= HandleBuildingPieceInserted;
            SpacetimeManager.Conn.Db.BuildingPiecePlaced.OnDelete -= HandleBuildingPieceDeleted;
        }
    }

    private void HandleConnected()
    {
        // Subscribe to table events now that we're connected
        SpacetimeManager.Conn.Db.BuildingPiecePlaced.OnInsert += HandleBuildingPieceInserted;
        SpacetimeManager.Conn.Db.BuildingPiecePlaced.OnDelete += HandleBuildingPieceDeleted;

        // Add subscription for building pieces
        SpacetimeManager.Instance.AddSubscription("select * from building_piece_variant");
        SpacetimeManager.Instance.AddSubscription("select * from building_piece_placed");
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

    public static void BuildingPiecePlace(uint variantId, GameObject placedPiece)
    {
        // Convert Unity Vector3 to DbVector3
        var position = placedPiece.transform.position;
        var rotation = placedPiece.transform.eulerAngles;

        var dbPosition = new DbVector3 { X = position.x, Y = position.y, Z = position.z };
        var dbRotation = new DbVector3 { X = rotation.x, Y = rotation.y, Z = rotation.z };

        if (PlayerHasMaterials(variantId))
        {
            SpacetimeManager.Conn.Reducers.BuildingPiecePlace(variantId, dbPosition, dbRotation);
        }
        else
        {
            Debug.LogError($"Not enough materials to build this piece");
        }
    }

    private static bool PlayerHasMaterials(uint variantId)
    {
        var inventory = SpacetimeManager.Conn.Db.Inventory.Identity.Find(SpacetimeManager.LocalIdentity);
        var variant = SpacetimeManager.Conn.Db.BuildingPieceVariant.VariantId.Find(variantId);

        return variant.BuildCost.All(cost =>
            inventory.Items.Any(item => item.Id == cost.ItemId && item.Quantity >= cost.Quantity)
        );
    }
    public static void BuildingPieceRemove(uint pieceId)
    {
        // Call the reducer to remove the piece
        SpacetimeManager.Conn.Reducers.BuildingPieceRemove(pieceId);
    }
}