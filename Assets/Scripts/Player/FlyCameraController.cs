using UnityEngine;

public class FlyCameraController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float fastMoveSpeed = 20f;
    [SerializeField] private float mouseSensitivity = 2f;

    private float rotationX = 0f;
    private float rotationY = 0f;

    private void Start()
    {
        // Lock and hide the cursor at start
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleMouseLook();
        HandleMovement();
    }

    public void UpdateTransform(Vector3 position, Quaternion rotation)
    {
        transform.position = position;

        // Convert the incoming rotation to proper euler angles
        Vector3 euler = rotation.eulerAngles;
        rotationX = euler.x;
        // Normalize rotationX to be between -90 and 90
        if (rotationX > 180f) rotationX -= 360f;
        rotationX = Mathf.Clamp(rotationX, -90f, 90f);

        rotationY = euler.y;
        transform.rotation = Quaternion.Euler(rotationX, rotationY, 0);
    }

    private void HandleMouseLook()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;

        // Get mouse input without smoothing
        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

        // Calculate camera rotation
        rotationY += mouseX;
        rotationX -= mouseY; // Subtract to invert the rotation
        rotationX = Mathf.Clamp(rotationX, -90f, 90f); // Clamp vertical rotation

        // Apply rotation
        transform.rotation = Quaternion.Euler(rotationX, rotationY, 0);
    }

    private void HandleMovement()
    {
        float currentMoveSpeed = Input.GetKey(KeyCode.LeftShift) ? fastMoveSpeed : moveSpeed;

        // Get input for movement
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // Calculate movement direction
        Vector3 moveDirection = transform.right * horizontal +
                              transform.forward * vertical;

        // Apply movement
        transform.position += moveDirection * currentMoveSpeed * Time.deltaTime;
    }
}