using UnityEngine;
using System.Collections.Generic;
using System;

public class Inventory : MonoBehaviour
{
    private const string ItemTag = "item";

    [System.Serializable]
    public class ItemStack
    {
        public string itemId;
        public int quantity;
        public Sprite sprite;

        public ItemStack(string itemId, int quantity, Sprite sprite)
        {
            this.itemId = itemId;
            this.quantity = quantity;
            this.sprite = sprite;
        }
    }

    [SerializeField] private List<ItemStack> items = new List<ItemStack>();
    public event Action Changed;

    public IReadOnlyList<ItemStack> Items => items;

    public void ForceNotifyChanged()
    {
        Changed?.Invoke();
    }

    public void AddItem(string itemId, int quantity = 1, Sprite sprite = null)
    {
        if (string.IsNullOrWhiteSpace(itemId) || quantity <= 0)
        {
            return;
        }

        ItemStack existing = items.Find(item => item.itemId == itemId);
        if (existing != null)
        {
            existing.quantity += quantity;
            if (existing.sprite == null && sprite != null)
            {
                existing.sprite = sprite;
            }
            Changed?.Invoke();
            return;
        }

        items.Add(new ItemStack(itemId, quantity, sprite));
        Changed?.Invoke();
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
