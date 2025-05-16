using UnityEngine;

public class ShipFloat : MonoBehaviour
{
    // Adjustable parameters in the Unity Inspector
    [SerializeField] private float yAmplitude = 0.2f; // Height of vertical bobbing
    [SerializeField] private float zAmplitude = 0.1f; // Distance of forward/back sway
    [SerializeField] private float ySpeed = 1f;      // Speed of vertical bobbing
    [SerializeField] private float zSpeed = 0.5f;    // Speed of forward/back sway

    private Vector3 initialPosition;

    void Start()
    {
        // Store the initial position of the ship
        initialPosition = transform.position;
    }

    void Update()
    {
        // Calculate new Y and Z offsets using sine waves
        float yOffset = Mathf.Sin(Time.time * ySpeed) * yAmplitude;
        float zOffset = Mathf.Sin(Time.time * zSpeed) * zAmplitude;

        // Apply the new position, keeping X unchanged
        transform.position = initialPosition + new Vector3(0f, yOffset, zOffset);
    }
}