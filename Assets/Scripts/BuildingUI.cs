using UnityEngine;
using UnityEngine.UI;

public class BuildingUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject buildingPanel;
    public Button foundationButton;
    public Button floorButton;
    public Button wallButton;
    public Button stairsButton;
    public Button deleteButton;

    private BuildingSystem buildingSystem;
    private bool isPanelVisible = false;

    // Add public getter for panel state
    public bool IsPanelOpen() => isPanelVisible;

    void Start()
    {
        // Get reference to the BuildingSystem
        buildingSystem = FindAnyObjectByType<BuildingSystem>();

        // Hide panel initially
        if (buildingPanel != null)
            buildingPanel.SetActive(false);

        // Setup button listeners
        SetupButtons();
    }

    void Update()
    {
        // Toggle panel on right click
        if (Input.GetMouseButtonDown(1))
        {
            if (!buildingSystem.IsBuildingMode())
            {
                // If we're not in building mode, don't show the panel
                return;
            }

            ToggleBuildingPanel();
        }
    }

    private void SetupButtons()
    {
        if (foundationButton != null)
            foundationButton.onClick.AddListener(SwitchToFoundation);

        if (floorButton != null)
            floorButton.onClick.AddListener(SwitchToFloor);

        if (wallButton != null)
            wallButton.onClick.AddListener(SwitchToWall);

        if (stairsButton != null)
            stairsButton.onClick.AddListener(SwitchToStairs);

        if (deleteButton != null)
            deleteButton.onClick.AddListener(SwitchToDelete);
    }

    private void ToggleBuildingPanel(bool overrideState = false)
    {
        isPanelVisible = !isPanelVisible;
        UpdateBuildingPanelState();
    }

    private void OpenBuildingPanel()
    {
        isPanelVisible = true;
        UpdateBuildingPanelState();
    }

    public void CloseBuildingPanel()
    {
        isPanelVisible = false;
        UpdateBuildingPanelState();
    }

    private void UpdateBuildingPanelState()
    {
        buildingPanel.SetActive(isPanelVisible);

        // Set cursor state based on panel visibility
        Cursor.lockState = isPanelVisible ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isPanelVisible;
    }

    public void SwitchToFoundation()
    {
        SwitchToBuildMode(BuildingSystem.BuildMode.Foundation);
    }

    public void SwitchToFloor()
    {
        SwitchToBuildMode(BuildingSystem.BuildMode.Floor);
    }

    public void SwitchToWall()
    {
        SwitchToBuildMode(BuildingSystem.BuildMode.Wall);
    }

    public void SwitchToStairs()
    {
        SwitchToBuildMode(BuildingSystem.BuildMode.Stairs);
    }

    public void SwitchToDelete()
    {
        SwitchToBuildMode(BuildingSystem.BuildMode.Delete);
    }

    public void SwitchToBuildMode(BuildingSystem.BuildMode mode)
    {
        buildingSystem.SwitchMode(mode);
        CloseBuildingPanel();
    }
}