using UnityEngine;
using System.Collections.Generic;
using SpacetimeDB.Types;

[CreateAssetMenu(fileName = "BuildingPieceDatabase", menuName = "Building/Building Piece Database")]
public class BuildingPieceDatabase : ScriptableObject
{
    [SerializeField]
    private List<BuildingPiece> buildingPiecePrefabs = new();

    // Dictionaries for quick lookup, populated on validation
    private Dictionary<uint, BuildingPiece> variantMap = new();
    private Dictionary<DbBuildingPieceType, List<BuildingPiece>> pieceTypeMap = new();

    private void OnValidate()
    {
        RebuildLookupMaps();
    }

    private void RebuildLookupMaps()
    {
        variantMap.Clear();
        pieceTypeMap.Clear();

        foreach (var variant in buildingPiecePrefabs)
        {
            if (variant == null) continue;

            // Add to variant map
            variantMap[variant.variantId] = variant;

            // Add to piece type map
            var pieceType = variant.pieceType;
            if (!pieceTypeMap.ContainsKey(pieceType))
            {
                pieceTypeMap[pieceType] = new List<BuildingPiece>();
            }
            pieceTypeMap[pieceType].Add(variant);
        }
    }

    public void AddPrefabVariant(BuildingPiece prefab, uint variantId)
    {
        // Check if variant ID already exists
        if (variantMap.ContainsKey(variantId))
        {
            Debug.LogError($"Variant ID {variantId} already exists in the database!");
            return;
        }

        buildingPiecePrefabs.Add(prefab);
        RebuildLookupMaps();
    }

    public void RemovePrefabVariant(BuildingPiece prefab)
    {
        buildingPiecePrefabs.Remove(prefab);
        RebuildLookupMaps();
    }

    public void RemoveNullEntries()
    {
        buildingPiecePrefabs.RemoveAll(piece => piece == null);
        RebuildLookupMaps();
    }

    public List<BuildingPiece> GetPrefabs()
    {
        return buildingPiecePrefabs;
    }

    public List<BuildingPiece> GetPrefabsByType(DbBuildingPieceType pieceType)
    {
        if (pieceTypeMap.Count == 0)
        {
            RebuildLookupMaps();
        }

        return pieceTypeMap.TryGetValue(pieceType, out var variants)
            ? variants
            : new List<BuildingPiece>();
    }

    public BuildingPiece GetPrefabByTypeAndVariant(DbBuildingPieceType pieceType, uint variantId)
    {
        var variants = GetPrefabsByType(pieceType);
        var variant = variants.Find(v => v.variantId == variantId);

        return variant;
    }

    public BuildingPiece GetPrefabByVariantId(uint variantId)
    {
        if (variantMap.Count == 0)
        {
            RebuildLookupMaps();
        }

        return variantMap.TryGetValue(variantId, out var variant) ? variant : null;
    }

    public bool HasVariantId(uint variantId)
    {
        if (variantMap.Count == 0)
        {
            RebuildLookupMaps();
        }

        return variantMap.ContainsKey(variantId);
    }
}