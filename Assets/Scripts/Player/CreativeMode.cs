using SpacetimeDB.Types;
using UnityEngine;

public class CreativeMode : MonoBehaviour
{
    [Header("References")]
    public FlyCameraController flyingCamera;

    [Header("Interpolation Settings")]
    public Vector3 targetPosition;
    public Quaternion targetRotation;
    [SerializeField] private float positionSmoothTime = 0.15f;
    [SerializeField] private float rotationLerpSpeed = 8f;
    [SerializeField] private Vector3 currentVelocity;

    private void Start()
    {
        var components = flyingCamera.GetComponents<Behaviour>();
        foreach (var component in components)
        {
            component.enabled = false;
        }
    }

    private void Update()
    {
        var transform = flyingCamera.transform;
        var distance = Vector3.Distance(transform.position, targetPosition);

        transform.position = distance > 5f ? targetPosition : Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, positionSmoothTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationLerpSpeed * Time.deltaTime);
    }

    private void FixedUpdate()
    {
        var transform = flyingCamera.transform;
        ConnectionManager.Conn.Reducers.CreativeCameraMove(
            new DbVector3(transform.position.x, transform.position.y, transform.position.z),
            new DbVector3(transform.eulerAngles.x, transform.eulerAngles.y, transform.eulerAngles.z)
        );
    }

    private void OnEnable()
    {
        if (BuildingSystem.Instance.IsEnabled)
        {
            EnableCreativeMode();
        }

        BuildingSystem.Instance.OnBuildingModeChanged += OnBuildingModeChanged;
    }

    private void OnDisable()
    {
        if (PlayerEntity.LocalPlayer.CameraFreeForm)
        {
            DisableCreativeMode();
        }

        if (!BuildingSystem.Instance) return;

        BuildingSystem.Instance.OnBuildingModeChanged -= OnBuildingModeChanged;
    }

    private void OnBuildingModeChanged(bool isBuilding)
    {
        if (isBuilding && BuildingSystem.Instance.IsCreativeMode)
        {
            EnableCreativeMode();
        }
        else
        {
            DisableCreativeMode();
        }
    }

    private void EnableCreativeMode()
    {
        PlayerEntity.LocalPlayer.RequestInputDisabled(this);
        PlayerEntity.LocalPlayer.CameraFreeForm.gameObject.SetActive(false);

        flyingCamera.gameObject.SetActive(true);
        flyingCamera.UpdateTransform(PlayerEntity.LocalPlayer.CameraFreeForm.transform.position, PlayerEntity.LocalPlayer.CameraFreeForm.transform.rotation);

        if (!ConnectionManager.IsConnected()) return;

        ConnectionManager.Conn.Reducers.CreativeCameraSetEnabled(true);
    }

    private void DisableCreativeMode()
    {
        flyingCamera.gameObject.SetActive(false);

        PlayerEntity.LocalPlayer.RequestInputEnabled(this);
        PlayerEntity.LocalPlayer.CameraFreeForm.gameObject.SetActive(true);

        if (!ConnectionManager.IsConnected()) return;

        ConnectionManager.Conn.Reducers.CreativeCameraSetEnabled(false);
    }
}
