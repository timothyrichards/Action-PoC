using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuildingUI : MonoBehaviour
{
    [Header("Runtime")]
    public GameObject buttonPrefab;

    [Header("UI References")]
    public GameObject buildingPanel;
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
        for (int i = 0; i < buildingSystem.foundationPrefabs.Length; i++)
        {
            var index = i;
            var button = Instantiate(buttonPrefab, buildingPanel.transform);
            button.name = buildingSystem.foundationPrefabs[i].name;
            button.GetComponentInChildren<TextMeshProUGUI>().text = buildingSystem.foundationPrefabs[i].name;
            button.GetComponent<Button>().onClick.AddListener(() => SwitchToFoundation(index));
        }

        for (int i = 0; i < buildingSystem.floorPrefabs.Length; i++)
        {
            var index = i;
            var button = Instantiate(buttonPrefab, buildingPanel.transform);
            button.name = buildingSystem.floorPrefabs[i].name;
            button.GetComponentInChildren<TextMeshProUGUI>().text = buildingSystem.floorPrefabs[i].name;
            button.GetComponent<Button>().onClick.AddListener(() => SwitchToFloor(index));
        }

        for (int i = 0; i < buildingSystem.wallPrefabs.Length; i++)
        {
            var index = i;
            var button = Instantiate(buttonPrefab, buildingPanel.transform);
            button.name = buildingSystem.wallPrefabs[i].name;
            button.GetComponentInChildren<TextMeshProUGUI>().text = buildingSystem.wallPrefabs[i].name;
            button.GetComponent<Button>().onClick.AddListener(() => SwitchToWall(index));
        }

        for (int i = 0; i < buildingSystem.stairPrefabs.Length; i++)
        {
            var index = i;
            var button = Instantiate(buttonPrefab, buildingPanel.transform);
            button.name = buildingSystem.stairPrefabs[i].name;
            button.GetComponentInChildren<TextMeshProUGUI>().text = buildingSystem.stairPrefabs[i].name;
            button.GetComponent<Button>().onClick.AddListener(() => SwitchToStairs(index));
        }

        deleteButton?.onClick.AddListener(SwitchToDelete);
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

    public void SwitchToFoundation(int index)
    {
        SwitchToBuildMode(BuildingSystem.BuildMode.Foundation, index);
    }

    public void SwitchToFloor(int index)
    {
        SwitchToBuildMode(BuildingSystem.BuildMode.Floor, index);
    }

    public void SwitchToWall(int index)
    {
        SwitchToBuildMode(BuildingSystem.BuildMode.Wall, index);
    }

    public void SwitchToStairs(int index)
    {
        SwitchToBuildMode(BuildingSystem.BuildMode.Stairs, index);
    }

    public void SwitchToDelete()
    {
        SwitchToBuildMode(BuildingSystem.BuildMode.Delete);
    }

    public void SwitchToBuildMode(BuildingSystem.BuildMode mode, int index = 0)
    {
        buildingSystem.SwitchMode(mode, index);
        CloseBuildingPanel();
    }
}