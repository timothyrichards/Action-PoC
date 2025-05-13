using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using UnityEngine.InputSystem;
using SpacetimeDB.Types;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; }
    [Header("References")]
    public GameObject inventoryPanel;
    public Transform itemContainer;
    public Transform pickupPromptContainer;
    public TextMeshProUGUI pickupPromptText;
    public GameObject itemPrefab;
    public KeyCode toggleKey = KeyCode.Tab;
    public InputActionReference inputActionReference;

    private List<GameObject> itemSlots = new();
    private InventoryManager inventoryManager;

    private void Awake()
    {
        Instance = this;

        // Hide inventory at start
        inventoryPanel.SetActive(false);
        pickupPromptContainer.gameObject.SetActive(false);
    }

    private void Start()
    {
        inventoryManager = InventoryManager.Instance;
        if (inventoryManager != null)
        {
            inventoryManager.OnInventoryChanged += UpdateInventoryUI;
        }

        // Hide inventory at start
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleInventory();
        }
    }

    public void ToggleInventory()
    {
        if (inventoryPanel != null)
        {
            bool newState = !inventoryPanel.activeSelf;
            inventoryPanel.SetActive(newState);

            if (newState)
            {
                UpdateInventoryUI(PlayerEntity.LocalPlayer.inventory);
            }
        }
    }

    private void UpdateInventoryUI(List<ItemRef> items)
    {
        // Clear existing slots
        foreach (var slot in itemSlots)
        {
            Destroy(slot);
        }
        itemSlots.Clear();

        // Create new slots for each item
        foreach (var item in items)
        {
            GameObject slot = Instantiate(itemPrefab, itemContainer);
            itemSlots.Add(slot);

            // Setup slot UI
            var slotUI = slot.GetComponent<ItemSlotUI>();
            slotUI?.SetupSlot(item);

            // Add click handler
            var button = slot.GetComponent<Button>();
            button?.onClick.AddListener(() => SelectItem(item));
        }
    }

    public void ShowPickupPrompt(string itemName)
    {
        pickupPromptContainer.gameObject.SetActive(true);
        pickupPromptText.text = $"Press {inputActionReference.action.GetBindingDisplayString(0)} to pick up {itemName}";
    }

    public void HidePickupPrompt()
    {
        pickupPromptContainer.gameObject.SetActive(false);
    }

    private void SelectItem(ItemRef item)
    {
        // TODO: Implement item selection UI
    }

    private void OnDestroy()
    {
        if (inventoryManager != null)
        {
            inventoryManager.OnInventoryChanged -= UpdateInventoryUI;
        }
    }
}