using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SpacetimeDB.Types;

public class ItemSlotUI : MonoBehaviour
{
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI quantityText;

    public void SetupSlot(ItemRef item)
    {
        // TODO: Set up item icon when you have an icon system
        // if (itemIcon != null)
        // {
        //     itemIcon.sprite = // Get sprite from your item database or asset system
        // }

        if (quantityText != null)
        {
            quantityText.text = item.Quantity > 1 ? $"x{item.Quantity}" : string.Empty;
        }
    }
}