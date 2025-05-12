using System;
using SpacetimeDB.Types;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static BuildingSystem;

public class BuildingUI : MonoBehaviour
{
    [Header("Runtime")]
    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private TextMeshProUGUI currentAnchorText;
    [SerializeField] private BuildingPieceDatabase database;

    [Header("UI References")]
    [SerializeField] private GameObject buildingUI;
    [SerializeField] private GameObject buildMenu;
    [SerializeField] private GameObject foundationPiecesPanel;
    [SerializeField] private GameObject floorPiecesPanel;
    [SerializeField] private GameObject wallPiecesPanel;
    [SerializeField] private GameObject stairsPiecesPanel;
    [SerializeField] private Button deleteButton;

    private bool isPanelVisible = false;
    public bool IsPanelOpen() => isPanelVisible;

    private void Start()
    {
        // Hide UI initially
        buildingUI.SetActive(false);
        buildMenu.SetActive(false);

        // Setup button listeners
        SetupButtons();
    }

    private void OnEnable()
    {
        BuildingSystem.Instance.OnBuildingModeChanged += HandleBuildModeChanged;
        BuildingSystem.Instance.OnAnchorChanged += HandleAnchorChanged;
    }

    private void OnDisable()
    {
        BuildingSystem.Instance.OnBuildingModeChanged -= HandleBuildModeChanged;
        BuildingSystem.Instance.OnAnchorChanged -= HandleAnchorChanged;
    }

    private void HandleBuildModeChanged(bool inBuildMode)
    {
        buildingUI.SetActive(inBuildMode);
    }

    private void HandleAnchorChanged(AnchorMode anchorMode, string anchorName)
    {
        currentAnchorText.text = "Current Anchor: " + anchorName;
    }

    private void Update()
    {
        // Only allow toggling piecesPanel if in build mode
        if (BuildingSystem.Instance.IsEnabled && Input.GetMouseButtonDown(1))
        {
            ToggleBuildMenuPanel();
        }
    }

    private void SetupButtons()
    {
        foreach (DbBuildingPieceType type in Enum.GetValues(typeof(DbBuildingPieceType)))
        {
            var panel = type switch
            {
                DbBuildingPieceType.Foundation => foundationPiecesPanel,
                DbBuildingPieceType.Floor => floorPiecesPanel,
                DbBuildingPieceType.Wall => wallPiecesPanel,
                DbBuildingPieceType.Stair => stairsPiecesPanel,
                _ => throw new NotImplementedException(),
            };

            var pieces = database.GetPrefabsByType(type);
            foreach (var piece in pieces)
            {
                var index = pieces.IndexOf(piece);
                var button = Instantiate(buttonPrefab, panel.transform);
                button.name = piece.name;
                button.GetComponentInChildren<TextMeshProUGUI>().text = piece.name;
                button.GetComponent<Button>().onClick.AddListener(() => SwitchToBuildMode(BuildMode.Foundation, piece));
            }
        }

        deleteButton?.onClick.AddListener(() => SwitchToBuildMode(BuildMode.Delete, null));
    }

    private void SwitchToBuildMode(BuildMode mode, BuildingPiece piece)
    {
        if (piece)
        {
            BuildingSystem.Instance.SetBuildMode(mode, piece.pieceType, piece.variantId);
        }
        else
        {
            BuildingSystem.Instance.SetBuildMode(mode);
        }

        CloseBuildingPanel();
    }

    private void ToggleBuildMenuPanel(bool overrideState = false)
    {
        isPanelVisible = !isPanelVisible;
        UpdateBuildMenuPanelState();
    }

    private void OpenBuildingPanel()
    {
        isPanelVisible = true;
        UpdateBuildMenuPanelState();
    }

    public void CloseBuildingPanel()
    {
        isPanelVisible = false;
        UpdateBuildMenuPanelState();
    }

    private void UpdateBuildMenuPanelState()
    {
        buildMenu.SetActive(isPanelVisible);

        // Set cursor state based on panel visibility
        if (isPanelVisible)
        {
            CursorManager.Instance.RequestCursorUnlock(this);
        }
        else
        {
            CursorManager.Instance.RequestCursorLock(this);
        }
    }
}
