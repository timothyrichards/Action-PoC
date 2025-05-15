using UnityEngine;

public class WindmillSpin : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 10f; // Degrees per second, adjustable in Inspector
    [SerializeField] private bool reverseDirection = false; // Toggle to reverse rotation direction

    void Start()
    {
        // Set initial rotation to match the provided values
        transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
    }

    void Update()
    {
        // Rotate around the local Y-axis (green axis) in local space
        float adjustedSpeed = reverseDirection ? -rotationSpeed : rotationSpeed;
        transform.Rotate(0, adjustedSpeed * Time.deltaTime, 0, Space.Self);
    }
}