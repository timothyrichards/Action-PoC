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
        if (!playerEntity.IsLocalPlayer()) enabled = false;

        flyingCamera.transform.parent = null;
    }

    void OnEnable()
    {
        BuildingSystem.Instance.OnBuildingModeChanged += OnBuildingModeChanged;
    }

    void OnDisable()
    {
        if (!BuildingSystem.Instance) return;

        BuildingSystem.Instance.OnBuildingModeChanged -= OnBuildingModeChanged;
    }

    private void OnBuildingModeChanged(bool isBuilding)
    {
        if (isBuilding)
        {
            playerEntity.ToggleInput();
            playerEntity.CameraFreeForm.gameObject.SetActive(false);

            var flyingCameraController = flyingCamera.GetComponent<FlyCameraController>();
            flyingCamera.SetActive(true);
            flyingCameraController.UpdateTransform(playerEntity.CameraFreeForm.transform.position, playerEntity.CameraFreeForm.transform.rotation);
        }
        else
        {
            flyingCamera.SetActive(false);

            playerEntity.ToggleInput();
            playerEntity.CameraFreeForm.gameObject.SetActive(true);
        }
    }
}
