using UnityEngine;
using System.Collections.Generic;

public class Inventory : MonoBehaviour
{
    private const string ItemTag = "item";

    [System.Serializable]
    public class ItemStack
    {
        public string itemId;
        public int quantity;

        public ItemStack(string itemId, int quantity)
        {
            this.itemId = itemId;
            this.quantity = quantity;
        }
    }

    [SerializeField] private List<ItemStack> items = new List<ItemStack>();

    public IReadOnlyList<ItemStack> Items => items;

    public void AddItem(string itemId, int quantity = 1)
    {
        if (string.IsNullOrWhiteSpace(itemId) || quantity <= 0)
        {
            return;
        }

        ItemStack existing = items.Find(item => item.itemId == itemId);
        if (existing != null)
        {
            existing.quantity += quantity;
            return;
        }

        items.Add(new ItemStack(itemId, quantity));
    }

    private void OnTriggerEnter(Collider other)
    {
        Item item = other.gameObject?.GetComponentInParent<Item>();
        if (item)
        {
            Debug.Log($"Picked up {item.Quantity} x {item.ItemId}");
            item.Pickup(this);
        }
    }
}
