using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class BuildingPieceDatabaseProcessor : AssetModificationProcessor
{
    private static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions options)
    {
        // Check if this is a prefab
        if (!assetPath.EndsWith(".prefab")) return AssetDeleteResult.DidNotDelete;

        // Load the asset to check if it's a building piece
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab == null) return AssetDeleteResult.DidNotDelete;

        BuildingPiece buildingPiece = prefab.GetComponent<BuildingPiece>();
        if (buildingPiece == null) return AssetDeleteResult.DidNotDelete;

        // Find the building piece database
        string[] guids = AssetDatabase.FindAssets("t:BuildingPieceDatabase");
        if (guids.Length == 0)
        {
            Debug.LogWarning("Building Piece Database not found when trying to remove deleted prefab reference!");
            return AssetDeleteResult.DidNotDelete;
        }

        string databasePath = AssetDatabase.GUIDToAssetPath(guids[0]);
        BuildingPieceDatabase database = AssetDatabase.LoadAssetAtPath<BuildingPieceDatabase>(databasePath);

        if (database == null)
        {
            Debug.LogWarning("Could not load Building Piece Database when trying to remove deleted prefab reference!");
            return AssetDeleteResult.DidNotDelete;
        }

        // Remove the prefab and any null entries
        database.RemovePrefabVariant(buildingPiece);
        database.RemoveNullEntries();

        // Save the changes
        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();

        Debug.Log($"Removed deleted building piece '{assetPath}' from database.");

        // Allow the deletion to proceed
        return AssetDeleteResult.DidNotDelete;
    }
}