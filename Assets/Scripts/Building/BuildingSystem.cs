using UnityEngine;
using System.Collections.Generic;
using System;
using SpacetimeDB.Types;

public class BuildingSystem : MonoBehaviour
{
    private static BuildingSystem _instance;
    public static BuildingSystem Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<BuildingSystem>();
            }

            return _instance;
        }
    }

    [Header("Runtime")]
    public bool IsEnabled { get; private set; } = false;
    public bool IsCreativeMode { get; private set; } = false;

    [Header("Building Settings")]
    public float rotationStep = 15f;
    public float gridSize = 1.0f;
    public float anchorDetectionRadius = 2f;

    [Header("Layers")]
    public LayerMask placementLayerMask;
    public LayerMask buildingLayerMask;

    [Header("Materials")]
    public Material validMaterial;
    public Material invalidMaterial;

    [Header("Prefabs")]
    public BuildingPieceDatabase database;

    public enum BuildMode { None, Foundation, Floor, Wall, Stairs, Delete }
    public enum AnchorMode { Auto, Manual }
    private BuildingUI buildingUI;
    private BuildingManager buildingSync;
    private BuildMode currentMode = BuildMode.None;
    private GameObject previewInstance;
    private BuildingPiece currentPrefab;
    private GameObject highlightedObject;
    private bool isValidPlacement = false;
    private float rotationY = 0f;
    private Dictionary<GameObject, Material[]> originalMaterials = new();
    private int currentAnchorIndex = 0;
    private List<Transform> currentAnchors = new();
    private Vector3 lastPreviewPosition;
    private bool manualAnchorOverride = false;
    private Transform lastTargetAnchor = null;
    private Vector3 lastHitNormal = Vector3.up;
    private AnchorMode anchorMode = AnchorMode.Auto;
    public event Action<bool> OnBuildingModeChanged;
    public event Action<AnchorMode, string> OnAnchorChanged;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    void Start()
    {
        buildingUI = FindAnyObjectByType<BuildingUI>();
        buildingSync = GetComponent<BuildingManager>();
        SetBuildMode(currentMode);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            ToggleBuildingMode();
            return;
        }

        if (!IsEnabled) return;

        // Only handle building input if the panel is closed and cursor is locked
        if (!buildingUI.IsPanelOpen() && Cursor.lockState == CursorLockMode.Locked)
        {
            HandleInput();
            HandleRotationInput();
            if (currentMode == BuildMode.Delete)
            {
                UpdateDeleteMode();
            }
            else
            {
                UpdatePreview();
                if (Input.GetMouseButtonDown(0) && isValidPlacement && currentMode != BuildMode.None)
                {
                    PlacePiece(currentPrefab.variantId);
                }
            }
        }
        else
        {
            // Still update preview position but don't allow placement
            if (currentMode != BuildMode.Delete && currentMode != BuildMode.None)
            {
                UpdatePreview();
            }
        }
    }

    private void HandleInput()
    {
        if (previewInstance != null && currentAnchors.Count > 0)
        {
            if (Input.GetKeyDown(KeyCode.H))
            {
                IsCreativeMode = !IsCreativeMode;
                OnBuildingModeChanged?.Invoke(IsEnabled);
            }

            bool changed = false;
            if (Input.GetKeyDown(KeyCode.Y))
            {
                if (anchorMode == AnchorMode.Auto)
                {
                    anchorMode = AnchorMode.Manual;
                }
                else
                {
                    anchorMode = AnchorMode.Auto;
                    lastHitNormal = Vector3.zero;
                    UpdatePreview();
                }

                manualAnchorOverride = anchorMode == AnchorMode.Manual;
                changed = true;
            }
            else if (Input.GetKeyDown(KeyCode.Q))
            {
                anchorMode = AnchorMode.Manual;
                currentAnchorIndex = (currentAnchorIndex + 1) % currentAnchors.Count;
                changed = true;
            }
            else if (Input.GetKeyDown(KeyCode.E))
            {
                anchorMode = AnchorMode.Manual;
                currentAnchorIndex = (currentAnchorIndex - 1 + currentAnchors.Count) % currentAnchors.Count;
                changed = true;
            }

            if (changed)
            {
                FireAnchorChangedEvent();
            }
        }
    }

    private void HandleRotationInput()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            rotationY += Mathf.Sign(scroll) * rotationStep;
            if (rotationY >= 360f) rotationY -= 360f;
            if (rotationY < 0f) rotationY += 360f;
        }
    }

    public void SetBuildMode(BuildMode mode, DbBuildingPieceType type = DbBuildingPieceType.Foundation, uint variant = 0)
    {
        currentMode = mode;
        currentPrefab = database.GetPrefabByTypeAndVariant(type, variant);

        if (previewInstance != null)
        {
            Destroy(previewInstance);
        }

        if (currentPrefab != null && mode != BuildMode.None && mode != BuildMode.Delete)
        {
            previewInstance = Instantiate(currentPrefab.gameObject);
            foreach (Collider col in previewInstance.GetComponentsInChildren<Collider>())
            {
                col.enabled = false;
            }

            SetPreviewMaterial(invalidMaterial);
            SetupAnchors();
        }

        if (highlightedObject != null)
        {
            RestoreHighlightedMaterial();
        }

        currentAnchorIndex = 0;
    }

    private void SetupAnchors()
    {
        currentAnchors.Clear();
        var helper = previewInstance.GetComponentInParent<BuildingPiece>();
        if (helper != null && helper.anchorPoints != null && helper.anchorPoints.Count > 0)
        {
            foreach (var go in helper.anchorPoints)
            {
                if (go != null) currentAnchors.Add(go.transform);
            }
        }
    }

    private Vector3 GetSnappedPosition(Vector3 hitPoint)
    {
        if (currentAnchors.Count > 0)
        {
            Transform anchor = currentAnchors[currentAnchorIndex];
            Vector3 anchorWorldPos = anchor.position;
            Vector3 snappedAnchorPos = anchorWorldPos;
            snappedAnchorPos.x = Mathf.Round(hitPoint.x / gridSize) * gridSize;
            snappedAnchorPos.z = Mathf.Round(hitPoint.z / gridSize) * gridSize;
            snappedAnchorPos.y = hitPoint.y;
            Vector3 delta = snappedAnchorPos - anchorWorldPos;
            return previewInstance.transform.position + delta;
        }
        else
        {
            Vector3 pos = hitPoint;
            pos.x = Mathf.Round(pos.x / gridSize) * gridSize;
            pos.z = Mathf.Round(pos.z / gridSize) * gridSize;
            return pos;
        }
    }

    private void UpdatePreview()
    {
        if (previewInstance == null || currentMode == BuildMode.None) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Vector3 position = Vector3.zero;
        Vector3 originalRotation = currentPrefab.transform.rotation.eulerAngles;
        Quaternion rotation = Quaternion.Euler(originalRotation.x, rotationY, originalRotation.z);
        bool snappedToAnchor = false;
        BuildingPiece hitPiece = null;
        Vector3 hitPoint = Vector3.zero;
        Vector3 hitNormal = Vector3.up;
        Transform closestAnchor = null;

        if (Physics.Raycast(ray, out RaycastHit hitBuilding, 100f, buildingLayerMask))
        {
            hitPiece = hitBuilding.collider.GetComponentInParent<BuildingPiece>();
            if (hitPiece != null && previewInstance != null && currentAnchors.Count > 0)
            {
                closestAnchor = FindClosestAnchor(hitPiece, hitBuilding.point);
                if (closestAnchor != null)
                {
                    Transform previewAnchor = currentAnchors[currentAnchorIndex];
                    Vector3 anchorOffset = previewAnchor.position - previewInstance.transform.position;
                    position = closestAnchor.position - anchorOffset;
                    snappedToAnchor = true;
                    hitPoint = hitBuilding.point;
                    hitNormal = hitBuilding.normal;
                }
                else
                {
                    // If no anchor found but we hit a building, use the current preview anchor
                    Transform previewAnchor = currentAnchors[currentAnchorIndex];
                    Vector3 anchorOffset = previewAnchor.position - previewInstance.transform.position;
                    position = hitBuilding.point - anchorOffset;
                    hitPoint = hitBuilding.point;
                    hitNormal = hitBuilding.normal;
                }
            }
        }
        else if (Physics.Raycast(ray, out RaycastHit hitTerrain, 100f, placementLayerMask))
        {
            position = GetSnappedPosition(hitTerrain.point);
        }

        // Reset manualAnchorOverride if looking at a new anchor
        if (closestAnchor != null && closestAnchor != lastTargetAnchor)
        {
            if (anchorMode == AnchorMode.Auto) // Only reset in auto mode
            {
                manualAnchorOverride = false;
                lastTargetAnchor = closestAnchor;
                FireAnchorChangedEvent();
            }
        }
        else if (closestAnchor == null)
        {
            lastTargetAnchor = null;
        }

        // Check if preview has moved to a new position or face normal changed
        bool faceChanged = snappedToAnchor && (hitNormal != lastHitNormal);
        if (faceChanged)
        {
            lastHitNormal = hitNormal;
        }
        if (Vector3.Distance(position, lastPreviewPosition) > 0.01f || faceChanged)
        {
            lastPreviewPosition = position;
            if (anchorMode == AnchorMode.Auto && !manualAnchorOverride && hitPiece != null)
            {
                AutoSelectBestAnchor(hitPiece, hitPoint, hitNormal);
            }
        }

        previewInstance.transform.position = position;
        previewInstance.transform.rotation = rotation;
        isValidPlacement = CheckPlacementValidity();
        SetPreviewMaterial(isValidPlacement ? validMaterial : invalidMaterial);
    }

    private bool CheckPlacementValidity()
    {
        if (previewInstance == null) return false;

        BuildingPiece previewPiece = previewInstance.GetComponentInParent<BuildingPiece>();
        if (previewPiece == null) return false;

        // Count how many anchors are connected
        var connectedAnchors = 0;
        var previewAnchorCount = 0;
        var connectedPieces = new List<BuildingPiece>();
        foreach (Transform anchor in previewPiece.anchorPoints)
        {
            if (anchor == null) continue;
            previewAnchorCount++;

            var hitColliders = Physics.OverlapSphere(anchor.position, 0.2f, buildingLayerMask);
            foreach (Collider col in hitColliders)
            {
                if (col.gameObject == previewInstance.gameObject) continue;

                var piece = col.GetComponentInParent<BuildingPiece>();
                if (piece != null)
                {
                    if (!connectedPieces.Contains(piece))
                        connectedPieces.Add(piece);

                    connectedAnchors++;
                }
            }
        }

        // Check if any connected pieces are fully overlapping by anchors
        foreach (var piece in connectedPieces)
        {
            bool allAnchorsConnected = true;
            foreach (var previewAnchor in previewPiece.anchorPoints)
            {
                bool anchorConnected = false;
                foreach (var otherAnchor in piece.anchorPoints)
                {
                    if (previewAnchor != null && otherAnchor != null && Vector3.Distance(previewAnchor.position, otherAnchor.position) < 0.2f)
                    {
                        anchorConnected = true;
                        break;
                    }
                }
                if (!anchorConnected)
                {
                    allAnchorsConnected = false;
                    break;
                }
            }
            if (allAnchorsConnected)
            {
                return false;
            }
        }

        if (!previewPiece.requiresFoundation) return true;

        // At least one anchor must connect to something
        return connectedAnchors > 0;
    }

    private void SetPreviewMaterial(Material mat)
    {
        if (mat == null) return;
        MeshRenderer[] renderers = previewInstance.GetComponentsInChildren<MeshRenderer>();
        foreach (var r in renderers)
        {
            r.sharedMaterial = mat;
        }
    }

    private void PlacePiece(uint variantId)
    {
        // Sync the placed piece over the network
        if (currentPrefab.TryGetComponent(out BuildingPiece piece))
        {
            buildingSync.BuildingPiecePlace(variantId, previewInstance);
        }
    }

    public GameObject PlacePieceAtPosition(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        GameObject placed = Instantiate(prefab, position, rotation);
        placed.transform.SetParent(transform);
        int buildingLayer = LayerMask.NameToLayer("Building");
        SetLayerRecursively(placed, buildingLayer);
        return placed;
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    private void UpdateDeleteMode()
    {
        if (highlightedObject != null)
        {
            RestoreHighlightedMaterial();
            highlightedObject = null;
        }
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, buildingLayerMask))
        {
            GameObject target = hit.collider.gameObject;
            highlightedObject = target;
            StoreOriginalMaterials(target);
            SetObjectMaterial(target, invalidMaterial);
            if (Input.GetMouseButtonDown(0))
            {
                // Try to remove the piece through BuildingSync
                BuildingPiece piece = target.GetComponentInParent<BuildingPiece>();
                if (piece != null && buildingSync != null)
                {
                    buildingSync.BuildingPieceRemove(piece.PieceId);
                }

                highlightedObject = null;
                RestoreHighlightedMaterial();
            }
        }
    }

    private void StoreOriginalMaterials(GameObject obj)
    {
        if (!originalMaterials.ContainsKey(obj))
        {
            MeshRenderer[] renderers = obj.GetComponentsInChildren<MeshRenderer>();
            Material[] mats = new Material[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                mats[i] = renderers[i].sharedMaterial;
            }
            originalMaterials[obj] = mats;
        }
    }

    private void SetObjectMaterial(GameObject obj, Material mat)
    {
        MeshRenderer[] renderers = obj.GetComponentsInChildren<MeshRenderer>();
        foreach (var r in renderers)
        {
            r.sharedMaterial = mat;
        }
    }

    private void RestoreHighlightedMaterial()
    {
        if (highlightedObject == null) return;
        if (originalMaterials.TryGetValue(highlightedObject, out Material[] mats))
        {
            MeshRenderer[] renderers = highlightedObject.GetComponentsInChildren<MeshRenderer>();
            for (int i = 0; i < renderers.Length && i < mats.Length; i++)
            {
                renderers[i].sharedMaterial = mats[i];
            }
            originalMaterials.Remove(highlightedObject);
        }
    }

    public Transform FindClosestAnchor(BuildingPiece piece, Vector3 point)
    {
        if (piece == null || piece.anchorPoints == null || piece.anchorPoints.Count == 0)
            return null;

        Transform closest = null;
        float closestDistance = float.MaxValue;

        foreach (Transform anchor in piece.anchorPoints)
        {
            if (anchor == null)
                continue;

            float distance = Vector3.Distance(point, anchor.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = anchor;
            }
        }

        return closest;
    }

    private void AutoSelectBestAnchor(BuildingPiece targetPiece, Vector3 hitPoint, Vector3 faceNormal)
    {
        if (previewInstance == null || currentAnchors.Count == 0 || targetPiece == null)
            return;

        // Find the anchor on the preview furthest in the -faceNormal direction from the preview's center
        Vector3 previewCenter = previewInstance.transform.position;
        Vector3 oppositeDir = -faceNormal.normalized;

        float maxDot = float.NegativeInfinity;
        int bestAnchorIndex = 0;

        for (int i = 0; i < currentAnchors.Count; i++)
        {
            Transform anchor = currentAnchors[i];
            Vector3 anchorDir = (anchor.position - previewCenter).normalized;
            float dot = Vector3.Dot(anchorDir, oppositeDir);
            if (dot > maxDot)
            {
                maxDot = dot;
                bestAnchorIndex = i;
            }
        }

        // Snap this anchor to the closest anchor on the target piece
        currentAnchorIndex = bestAnchorIndex;
        Transform bestPreviewAnchor = currentAnchors[bestAnchorIndex];
        Transform closestTargetAnchor = FindClosestAnchor(targetPiece, hitPoint);
        if (closestTargetAnchor != null)
        {
            Vector3 anchorOffset = bestPreviewAnchor.position - previewInstance.transform.position;
            previewInstance.transform.position = closestTargetAnchor.position - anchorOffset;
        }
    }

    private void ToggleBuildingMode()
    {
        IsEnabled = !IsEnabled;

        if (!IsEnabled)
        {
            // Clean up when disabling
            if (previewInstance != null)
            {
                Destroy(previewInstance);
                previewInstance = null;
            }
            if (highlightedObject != null)
            {
                RestoreHighlightedMaterial();
                highlightedObject = null;
            }
            currentMode = BuildMode.None;

            // Close the building UI panel
            buildingUI?.CloseBuildingPanel();
        }
        else
        {
            // Set Foundation as default mode when enabling
            currentMode = BuildMode.Foundation;
            SetBuildMode(currentMode);
        }

        OnBuildingModeChanged?.Invoke(IsEnabled);
    }

    // Method to check if a GameObject is the current preview piece
    public bool IsPreviewPiece(GameObject obj)
    {
        return previewInstance == obj;
    }

    // Method to get the current anchor index
    public int GetCurrentAnchorIndex()
    {
        return anchorMode == AnchorMode.Auto ? -1 : currentAnchorIndex;
    }

    // Method to get the current anchor mode
    public AnchorMode GetCurrentAnchorMode()
    {
        return anchorMode;
    }

    // Fire anchor changed event
    private void FireAnchorChangedEvent()
    {
        string anchorName = anchorMode == AnchorMode.Auto ? "Auto" : (currentAnchors.Count > currentAnchorIndex ? currentAnchors[currentAnchorIndex].name : "");
        OnAnchorChanged?.Invoke(anchorMode, anchorName);
    }
}