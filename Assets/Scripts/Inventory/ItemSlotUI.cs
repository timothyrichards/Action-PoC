using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SpacetimeDB.Types;

public class ItemSlotUI : MonoBehaviour
{
    [SerializeField] private CollectiblesDatabase collectiblesDatabase;
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI quantityText;

    public void SetupSlot(ItemRef item)
    {
        // Handle empty slot
        if (item == null)
        {
            itemIcon.enabled = false;
            quantityText.text = string.Empty;

            return;
        }

        var itemPickup = collectiblesDatabase.GetCollectibleById(item.Id);
        itemIcon.enabled = true;
        itemIcon.sprite = itemPickup.itemIcon;
        quantityText.text = item.Quantity > 1 ? $"x{item.Quantity}" : string.Empty;
    }
}