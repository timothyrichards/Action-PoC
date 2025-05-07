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
        if (buildingSystem.IsBuildingMode() && Input.GetMouseButtonDown(1))
        {
            ToggleBuildMenuPanel();
        }
    }

    private void SetupButtons()
    {
        var foundationPrefabs = buildingSystem.foundationPrefabs;
        for (int i = 0; i < foundationPrefabs.Length; i++)
        {
            var index = i;
            var button = Instantiate(buttonPrefab, foundationPiecesPanel.transform);
            button.name = foundationPrefabs[i].name;
            button.GetComponentInChildren<TextMeshProUGUI>().text = foundationPrefabs[i].name;
            button.GetComponent<Button>().onClick.AddListener(() => SwitchToFoundation(index));
        }

        var floorPrefabs = buildingSystem.floorPrefabs;
        for (int i = 0; i < floorPrefabs.Length; i++)
        {
            var index = i;
            var button = Instantiate(buttonPrefab, floorPiecesPanel.transform);
            button.name = floorPrefabs[i].name;
            button.GetComponentInChildren<TextMeshProUGUI>().text = floorPrefabs[i].name;
            button.GetComponent<Button>().onClick.AddListener(() => SwitchToFloor(index));
        }

        var wallPrefabs = buildingSystem.wallPrefabs;
        for (int i = 0; i < wallPrefabs.Length; i++)
        {
            var index = i;
            var button = Instantiate(buttonPrefab, wallPiecesPanel.transform);
            button.name = wallPrefabs[i].name;
            button.GetComponentInChildren<TextMeshProUGUI>().text = wallPrefabs[i].name;
            button.GetComponent<Button>().onClick.AddListener(() => SwitchToWall(index));
        }

        var stairPrefabs = buildingSystem.stairPrefabs;
        for (int i = 0; i < stairPrefabs.Length; i++)
        {
            var index = i;
            var button = Instantiate(buttonPrefab, stairsPiecesPanel.transform);
            button.name = stairPrefabs[i].name;
            button.GetComponentInChildren<TextMeshProUGUI>().text = stairPrefabs[i].name;
            button.GetComponent<Button>().onClick.AddListener(() => SwitchToStairs(index));
        }

        deleteButton?.onClick.AddListener(SwitchToDelete);
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

    public void SwitchToFoundation(int index)
    {
        SwitchToBuildMode(BuildMode.Foundation, index);
    }

    public void SwitchToFloor(int index)
    {
        SwitchToBuildMode(BuildMode.Floor, index);
    }

    public void SwitchToWall(int index)
    {
        SwitchToBuildMode(BuildMode.Wall, index);
    }

    public void SwitchToStairs(int index)
    {
        SwitchToBuildMode(BuildMode.Stairs, index);
    }

    public void SwitchToDelete()
    {
        SwitchToBuildMode(BuildMode.Delete);
    }

    public void SwitchToBuildMode(BuildMode mode, int index = 0)
    {
        buildingSystem.SetBuildMode(mode, index);
        CloseBuildingPanel();
    }
}