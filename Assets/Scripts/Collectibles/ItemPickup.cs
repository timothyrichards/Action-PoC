using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
public class ItemPickup : MonoBehaviour
{
    public uint itemId;
    public Sprite itemIcon;
    public string itemName;
    public string itemDescription;
    public float itemWeight = 1f;
    public uint quantity = 1;

    private PlayerInputActions inputActions;

    private void Awake()
    {
        inputActions = new PlayerInputActions();
    }

    private void Start()
    {
        // Make sure the collider is trigger
        var collider = GetComponent<Collider>();
        collider.isTrigger = true;
    }

    private void OnEnable()
    {
        inputActions.Player.PickUp.performed += OnPickUp;
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Player.PickUp.performed -= OnPickUp;
        inputActions.Disable();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerEntity>() != null)
        {
            ShowPickupPrompt();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerEntity>() != null)
        {
            HidePickupPrompt();
        }
    }

    private void OnPickUp(InputAction.CallbackContext context)
    {
        if (context.ReadValue<float>() > 0.5f)
        {
            Debug.Log($"Picking up {itemName}");
            PickupItem();
        }
    }

    private void PickupItem()
    {
        // Send to server
        SpacetimeManager.Conn.Reducers.InventoryAddItem(itemId, quantity);

        // Destroy the pickup object
        // Destroy(gameObject);
    }

    private void ShowPickupPrompt()
    {
        InventoryUI.Instance.ShowPickupPrompt(itemName);
    }

    private void HidePickupPrompt()
    {
        InventoryUI.Instance.HidePickupPrompt();
    }
}
