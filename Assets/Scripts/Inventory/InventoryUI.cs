using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using UnityEngine.InputSystem;
using SpacetimeDB.Types;
using SpacetimeDB;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; }

    [Header("References")]
    public GameObject inventoryPanel;
    public Transform itemContainer;
    public Transform pickupPromptContainer;
    public TextMeshProUGUI pickupKeybindText;
    public TextMeshProUGUI pickupPromptText;
    public GameObject itemPrefab;
    public KeyCode toggleKey = KeyCode.Tab;
    public InputActionReference inputActionReference;

    private List<GameObject> itemSlots = new();

    private void Awake()
    {
        Instance = this;

        // Hide inventory at start
        inventoryPanel.SetActive(false);
        pickupPromptContainer.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        InventorySync.OnInventoryChanged += UpdateInventoryUI;
    }

    private void OnDisable()
    {
        InventorySync.OnInventoryChanged -= UpdateInventoryUI;
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
                CursorManager.Instance.RequestCursorUnlock(this);
                UpdateInventoryUI(PlayerEntity.LocalPlayer.ownerIdentity, PlayerEntity.LocalPlayer.inventory);
            }
            else
            {
                CursorManager.Instance.RequestCursorLock(this);
            }
        }
    }

    private void UpdateInventoryUI(Identity identity, List<ItemRef> items)
    {
        // Clear existing slots
        foreach (var slot in itemSlots)
        {
            Destroy(slot);
        }
        itemSlots.Clear();

        // Create slots for the entire inventory size
        var inventory = InventorySync.GetInventory(PlayerEntity.LocalPlayer);
        for (int i = 0; i < inventory.Size; i++)
        {
            GameObject slot = Instantiate(itemPrefab, itemContainer);
            itemSlots.Add(slot);

            // Find matching item for this slot index
            ItemRef matchingItem = i < items.Count ? items[i] : null;

            // Setup slot UI
            var slotUI = slot.GetComponent<ItemSlotUI>();
            if (slotUI != null)
            {
                slotUI.SetupSlot(matchingItem);
            }

            // Add click handler if there's an item
            if (matchingItem != null)
            {
                var button = slot.GetComponent<Button>();
                button?.onClick.AddListener(() => SelectItem(matchingItem));
            }
        }
    }

    public void ShowPickupPrompt(string itemName)
    {
        pickupPromptContainer.gameObject.SetActive(true);
        pickupKeybindText.text = inputActionReference.action.GetBindingDisplayString(0);
        pickupPromptText.text = itemName;
    }

    public void HidePickupPrompt()
    {
        pickupPromptContainer.gameObject.SetActive(false);
    }

    private void SelectItem(ItemRef item)
    {
        // TODO: Implement item selection UI
    }
}