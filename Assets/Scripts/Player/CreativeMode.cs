using SpacetimeDB.Types;
using UnityEngine;

public class CreativeMode : MonoBehaviour
{
    [Header("References")]
    public PlayerEntity playerEntity;
    public FlyCameraController flyingCamera;

    [Header("Interpolation Settings")]
    public float positionLerpSpeed = 15f;
    public float rotationLerpSpeed = 15f;

    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private Vector3 currentVelocity;

    private void Awake()
    {
        playerEntity = GetComponent<PlayerEntity>();
    }

    void Start()
    {
        bool isLocalPlayer = playerEntity.IsLocalPlayer();
        if (!isLocalPlayer)
        {
            flyingCamera.GetComponent<Camera>().enabled = false;
            flyingCamera.GetComponent<AudioListener>().enabled = false;

            // Disable the FlyCameraController and any other components on the flying camera
            var components = flyingCamera.GetComponents<MonoBehaviour>();
            foreach (var component in components)
            {
                component.enabled = false;
            }
        }
    }

    void Update()
    {
        if (playerEntity.IsLocalPlayer())
        {
            var transform = flyingCamera.transform;
            ConnectionManager.Conn.Reducers.MoveCreativeCamera(
                new DbVector3(transform.position.x, transform.position.y, transform.position.z),
                new DbVector3(transform.eulerAngles.x, transform.eulerAngles.y, transform.eulerAngles.z)
            );
            return;
        }

        flyingCamera.transform.position = Vector3.SmoothDamp(flyingCamera.transform.position, targetPosition, ref currentVelocity, positionLerpSpeed * Time.deltaTime);
        flyingCamera.transform.rotation = Quaternion.Slerp(flyingCamera.transform.rotation, targetRotation, rotationLerpSpeed * Time.deltaTime);
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

    public void UpdateFromCreativeCamera(CreativeCamera creativeCamera, bool instantUpdate = false)
    {
        if (playerEntity.IsLocalPlayer()) return;

        targetPosition = new Vector3(creativeCamera.Position.X, creativeCamera.Position.Y, creativeCamera.Position.Z);
        targetRotation = Quaternion.Euler(creativeCamera.Rotation.X, creativeCamera.Rotation.Y, creativeCamera.Rotation.Z);

        flyingCamera.gameObject.SetActive(creativeCamera.Enabled);

        if (instantUpdate)
        {
            flyingCamera.UpdateTransform(targetPosition, targetRotation);
        }
    }

    private void OnBuildingModeChanged(bool isBuilding)
    {
        if (playerEntity.IsLocalPlayer())
        {
            if (isBuilding)
            {
                playerEntity.ToggleInput();
                playerEntity.CameraFreeForm.gameObject.SetActive(false);

                flyingCamera.gameObject.SetActive(true);
                flyingCamera.UpdateTransform(playerEntity.CameraFreeForm.transform.position, playerEntity.CameraFreeForm.transform.rotation);
            }
            else
            {
                flyingCamera.gameObject.SetActive(false);

                playerEntity.ToggleInput();
                playerEntity.CameraFreeForm.gameObject.SetActive(true);
            }

            ConnectionManager.Conn.Reducers.SetCreativeCameraEnabled(isBuilding);
            return;
        }
    }
}
