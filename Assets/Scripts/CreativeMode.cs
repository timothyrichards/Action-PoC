using UnityEngine;

public class CreativeMode : MonoBehaviour
{
    public GameObject flyingCamera;

    private PlayerEntity playerEntity;

    private void Awake()
    {
        playerEntity = GetComponent<PlayerEntity>();
    }

    void Start()
    {
        flyingCamera.transform.parent = null;
    }

    void OnEnable()
    {
        BuildingSystem.Instance.OnBuildingModeChanged += OnBuildingModeChanged;
    }

    void OnDisable()
    {
        BuildingSystem.Instance.OnBuildingModeChanged -= OnBuildingModeChanged;
    }

    private void OnBuildingModeChanged(bool isBuilding)
    {
        if (isBuilding)
        {
            playerEntity.ToggleInput();
            playerEntity.CameraFreeForm.gameObject.SetActive(false);

            flyingCamera.SetActive(true);
            flyingCamera.transform.position = playerEntity.CameraFreeForm.transform.position;
            flyingCamera.transform.rotation = playerEntity.CameraFreeForm.transform.rotation;
        }
        else
        {
            flyingCamera.SetActive(false);

            playerEntity.ToggleInput();
            playerEntity.CameraFreeForm.gameObject.SetActive(true);
        }
    }
}
