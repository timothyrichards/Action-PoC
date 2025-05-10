using SpacetimeDB.Types;
using UnityEngine;

public class CreativeMode : MonoBehaviour
{
    [Header("References")]
    public PlayerEntity playerEntity;
    public FlyCameraController flyingCamera;

    [Header("Interpolation Settings")]
    private Vector3 currentVelocity;
    public Vector3 targetPosition;
    public Quaternion targetRotation;
    public float positionSmoothTime = 0.15f;
    public float rotationLerpSpeed = 8f;

    private void Awake()
    {
        playerEntity = GetComponent<PlayerEntity>();
    }

    void Start()
    {
        if (!playerEntity.IsLocalPlayer())
        {
            var components = flyingCamera.GetComponents<Behaviour>();
            foreach (var component in components)
            {
                component.enabled = false;
            }
        }
    }

    void Update()
    {
        if (playerEntity.IsLocalPlayer()) return;

        var transform = flyingCamera.transform;
        var distance = Vector3.Distance(transform.position, targetPosition);

        transform.position = distance > 5f ? targetPosition : Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, positionSmoothTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationLerpSpeed * Time.deltaTime);
    }

    void FixedUpdate()
    {
        if (!playerEntity.IsLocalPlayer() || !flyingCamera.gameObject.activeSelf) return;

        var transform = flyingCamera.transform;
        ConnectionManager.Conn.Reducers.CreativeCameraMove(
            new DbVector3(transform.position.x, transform.position.y, transform.position.z),
            new DbVector3(transform.eulerAngles.x, transform.eulerAngles.y, transform.eulerAngles.z)
        );
    }

    void OnEnable()
    {
        if (BuildingSystem.Instance.IsEnabled)
        {
            EnableCreativeMode();
        }

        BuildingSystem.Instance.OnBuildingModeChanged += OnBuildingModeChanged;
    }

    void OnDisable()
    {
        if (playerEntity.CameraFreeForm)
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
        if (!playerEntity.IsLocalPlayer()) return;

        playerEntity.RequestInputDisabled(this);
        playerEntity.CameraFreeForm.gameObject.SetActive(false);

        flyingCamera.gameObject.SetActive(true);
        flyingCamera.UpdateTransform(playerEntity.CameraFreeForm.transform.position, playerEntity.CameraFreeForm.transform.rotation);

        if (!ConnectionManager.IsConnected()) return;

        ConnectionManager.Conn.Reducers.CreativeCameraSetEnabled(true);
    }

    private void DisableCreativeMode()
    {
        if (!playerEntity.IsLocalPlayer()) return;

        flyingCamera.gameObject.SetActive(false);

        playerEntity.RequestInputEnabled(this);
        playerEntity.CameraFreeForm.gameObject.SetActive(true);

        if (!ConnectionManager.IsConnected()) return;

        ConnectionManager.Conn.Reducers.CreativeCameraSetEnabled(false);
    }
}
