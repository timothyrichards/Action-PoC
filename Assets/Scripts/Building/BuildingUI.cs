using System;
using SpacetimeDB.Types;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static BuildingSystem;

public class BuildingUI : MonoBehaviour
{
    [Header("Runtime")]
    public GameObject buttonPrefab;
    public TextMeshProUGUI currentAnchorText;

    [Header("UI References")]
    public GameObject buildingUI;
    public GameObject buildMenu;
    public GameObject foundationPiecesPanel;
    public GameObject floorPiecesPanel;
    public GameObject wallPiecesPanel;
    public GameObject stairsPiecesPanel;
    public Button deleteButton;

    private BuildingSystem buildingSystem;
    private bool isPanelVisible = false;

    // Add public getter for panel state
    public bool IsPanelOpen() => isPanelVisible;

    void Awake()
    {
        buildingSystem = FindAnyObjectByType<BuildingSystem>();
    }

    void Start()
    {
        // Hide UI initially
        buildingUI.SetActive(false);
        buildMenu.SetActive(false);

        // Setup button listeners
        SetupButtons();
    }

    void OnEnable()
    {
        buildingSystem.OnBuildingModeChanged += HandleBuildModeChanged;
        buildingSystem.OnAnchorChanged += HandleAnchorChanged;
    }

    void OnDisable()
    {
        buildingSystem.OnBuildingModeChanged -= HandleBuildModeChanged;
        buildingSystem.OnAnchorChanged -= HandleAnchorChanged;
    }

    private void HandleBuildModeChanged(bool inBuildMode)
    {
        buildingUI.SetActive(inBuildMode);
    }

    private void HandleAnchorChanged(AnchorMode anchorMode, string anchorName)
    {
        currentAnchorText.text = "Current Anchor: " + anchorName;
    }

    void Update()
    {
        // Only allow toggling piecesPanel if in build mode
        if (buildingSystem.IsEnabled && Input.GetMouseButtonDown(1))
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

            var pieces = buildingSystem.database.GetPrefabsByType(type);
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

    public void SwitchToBuildMode(BuildMode mode, BuildingPiece piece)
    {
        if (piece)
        {
            buildingSystem.SetBuildMode(mode, piece.pieceType, piece.variantId);
        }
        else
        {
            buildingSystem.SetBuildMode(mode);
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
        Cursor.lockState = isPanelVisible ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isPanelVisible;
    }
}