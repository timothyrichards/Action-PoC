using UnityEngine;
using System.Collections.Generic;
using SpacetimeDB.Types;
public class BuildingPiece : MonoBehaviour
{
    public uint PieceId { get; set; }
    public uint variantId;
    public DbBuildingPieceType pieceType;
    public bool requiresFoundation = true;
    public List<Transform> anchorPoints = new();

    private void OnDrawGizmos()
    {
        if (BuildingSystem.Instance == null) return;

        // Helper to visualize anchor points in editor
        var buildingSystem = BuildingSystem.Instance;
        for (int i = 0; i < anchorPoints.Count; i++)
        {
            var anchor = anchorPoints[i];
            if (anchor != null)
            {
                // Check if this is a preview piece in the BuildingSystem
                bool isPreview = buildingSystem != null && buildingSystem.IsPreviewPiece(gameObject);

                // Set color based on whether this is the active anchor in preview
                Gizmos.color = (isPreview && i == buildingSystem.GetCurrentAnchorIndex()) ? Color.green : Color.yellow;
                float radius = isPreview && i == buildingSystem.GetCurrentAnchorIndex() ? 0.2f : 0.1f;

                Gizmos.DrawWireSphere(anchor.position, radius);
                Gizmos.DrawRay(anchor.position, anchor.forward * 0.2f);
            }
        }
    }
}