using UnityEngine;
using System.Collections.Generic;

public class BuildingSystem : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject[] foundationPrefabs;
    public GameObject[] floorPrefabs;
    public GameObject[] wallPrefabs;
    public GameObject[] stairPrefabs;

    [Header("Materials")]
    public Material validMaterial;
    public Material invalidMaterial;

    [Header("Layers")]
    public LayerMask placementLayerMask;
    public LayerMask buildingLayerMask;

    [Header("Grid")]
    public float gridSize = 1.0f;
    public float anchorDetectionRadius = 2f;
    public enum BuildMode { None, Foundation, Floor, Wall, Stairs, Delete }

    private GameObject previewInstance;
    private GameObject currentPrefab;
    private bool isValidPlacement = false;
    private BuildMode currentMode = BuildMode.None;
    private Camera cam;
    private float rotationY = 0f;
    public float rotationStep = 15f;
    private float originalZRotation = 0f;
    private GameObject highlightedObject;
    private Dictionary<GameObject, Material[]> originalMaterials = new Dictionary<GameObject, Material[]>();
    private int currentAnchorIndex = 0;
    private List<Transform> currentAnchors = new List<Transform>();
    private bool isEnabled = false;
    private Vector3 lastPreviewPosition;
    private bool manualAnchorOverride = false;
    private BuildingUI buildingUI;

    void Start()
    {
        cam = Camera.main;
        buildingUI = FindAnyObjectByType<BuildingUI>();
        SetBuildMode(currentMode);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            ToggleBuildingMode();
            return;
        }

        if (!isEnabled) return;

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
                    PlacePiece();
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
        if (Input.GetKeyDown(KeyCode.Q) && previewInstance != null && currentAnchors.Count > 0)
        {
            manualAnchorOverride = true;
            currentAnchorIndex = (currentAnchorIndex + 1) % currentAnchors.Count;
        }
        else if (Input.GetKeyDown(KeyCode.E) && previewInstance != null && currentAnchors.Count > 0)
        {
            manualAnchorOverride = true;
            currentAnchorIndex = (currentAnchorIndex - 1 + currentAnchors.Count) % currentAnchors.Count;
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

    public void SwitchMode(BuildMode mode)
    {
        if (currentMode == mode) return;
        currentMode = mode;
        SetBuildMode(mode);
    }

    private void SetBuildMode(BuildMode mode)
    {
        currentPrefab = mode switch
        {
            BuildMode.Foundation => foundationPrefabs[0],
            BuildMode.Floor => floorPrefabs[0],
            BuildMode.Wall => wallPrefabs[0],
            BuildMode.Stairs => stairPrefabs[0],
            _ => null
        };

        if (previewInstance != null)
        {
            Destroy(previewInstance);
        }
        if (currentPrefab != null && mode != BuildMode.Delete)
        {
            previewInstance = Instantiate(currentPrefab);
            // Disable all colliders on the preview
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
        var helper = previewInstance.GetComponent<BuildingPiece>();
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

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Vector3 position = Vector3.zero;
        Vector3 originalRotation = currentPrefab.transform.rotation.eulerAngles;
        Quaternion rotation = Quaternion.Euler(originalRotation.x, rotationY, originalRotation.z);
        bool snappedToAnchor = false;

        if (Physics.Raycast(ray, out RaycastHit hitBuilding, 100f, buildingLayerMask))
        {
            BuildingPiece hitPiece = hitBuilding.collider.GetComponent<BuildingPiece>();
            if (hitPiece != null && previewInstance != null && currentAnchors.Count > 0)
            {
                Transform closestAnchor = FindClosestAnchor(hitPiece, hitBuilding.point);
                if (closestAnchor != null)
                {
                    Transform previewAnchor = currentAnchors[currentAnchorIndex];
                    Vector3 anchorOffset = previewAnchor.position - previewInstance.transform.position;
                    position = closestAnchor.position - anchorOffset;
                    snappedToAnchor = true;
                }
            }
        }

        if (!snappedToAnchor && Physics.Raycast(ray, out RaycastHit hitTerrain, 100f, placementLayerMask))
        {
            position = GetSnappedPosition(hitTerrain.point);
        }

        // Check if preview has moved to a new position
        if (Vector3.Distance(position, lastPreviewPosition) > 0.01f)
        {
            lastPreviewPosition = position;
            if (!manualAnchorOverride)
            {
                AutoSelectBestAnchor();
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

        BuildingPiece piece = previewInstance.GetComponent<BuildingPiece>();
        if (piece == null) return false;

        if (!piece.requiresFoundation) return true;

        // Count how many anchors are connected
        int connectedAnchors = 0;
        int totalAnchors = 0;
        foreach (Transform anchor in piece.anchorPoints)
        {
            if (anchor == null) continue;
            totalAnchors++;

            Collider[] hitColliders = Physics.OverlapSphere(anchor.position, 0.2f, buildingLayerMask);
            foreach (Collider col in hitColliders)
            {
                if (col.gameObject != previewInstance.gameObject && col.GetComponent<BuildingPiece>() != null)
                {
                    connectedAnchors++;
                    break;
                }
            }
        }

        // Invalid if all anchors are connected (complete overlap)
        // if (totalAnchors > 0 && connectedAnchors == totalAnchors)
        // {
        //     return false;
        // }

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

    private void PlacePiece()
    {
        GameObject placed = Instantiate(currentPrefab, previewInstance.transform.position, previewInstance.transform.rotation);
        placed.transform.SetParent(transform);
        int buildingLayer = LayerMask.NameToLayer("Building");
        SetLayerRecursively(placed, buildingLayer);
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
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, buildingLayerMask))
        {
            GameObject target = hit.collider.gameObject;
            highlightedObject = target;
            StoreOriginalMaterials(target);
            SetObjectMaterial(target, invalidMaterial);
            if (Input.GetMouseButtonDown(0))
            {
                originalMaterials.Remove(target);
                Destroy(target);
                highlightedObject = null;
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

    private void AutoSelectBestAnchor()
    {

    }

    private void ToggleBuildingMode()
    {
        isEnabled = !isEnabled;

        if (!isEnabled)
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
            if (buildingUI != null)
            {
                buildingUI.CloseBuildingPanel();
            }
        }
        else
        {
            // Set Foundation as default mode when enabling
            currentMode = BuildMode.Foundation;
            SetBuildMode(currentMode);
        }
    }

    public bool IsBuildingMode()
    {
        return isEnabled;
    }

    // Method to check if a GameObject is the current preview piece
    public bool IsPreviewPiece(GameObject obj)
    {
        return previewInstance == obj;
    }

    // Method to get the current anchor index
    public int GetCurrentAnchorIndex()
    {
        return currentAnchorIndex;
    }
}