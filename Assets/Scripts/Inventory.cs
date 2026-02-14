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

    public bool HasItem(string itemId, int quantity = 1)
    {
        if (string.IsNullOrWhiteSpace(itemId) || quantity <= 0)
        {
            return false;
        }

        var existing = items.Find(item => item.itemId == itemId);
        if (existing == null)
        {
            return false;
        }

        if (existing.quantity == int.MaxValue)
        {
            return true;
        }

        return existing.quantity >= quantity;
    }

    public bool TryConsumeItem(string itemId, int quantity = 1)
    {
        if (string.IsNullOrWhiteSpace(itemId) || quantity <= 0)
        {
            return false;
        }

        var existing = items.Find(item => item.itemId == itemId);
        if (existing == null)
        {
            return false;
        }

        if (existing.quantity == int.MaxValue)
        {
            return true;
        }

        if (existing.quantity < quantity)
        {
            return false;
        }

        existing.quantity -= quantity;
        if (existing.quantity <= 0)
        {
            items.Remove(existing);
        }

        Changed?.Invoke();
        return true;
    }

    private void OnTriggerEnter(Collider other)
    {
        Item item = other.gameObject?.GetComponentInParent<Item>();
        if (item)
        {
            Debug.Log($"Picked up {item.Quantity} x {item.ItemId}");
            item.Pickup(this);
            TriggerStoryForItem(item.ItemId);
        }
    }

    private void TriggerStoryForItem(string itemId)
    {
        if (StoryAudioManager.Instance == null) return;

        string id = itemId.ToLowerInvariant();
        if (id.Contains("note") || id.Contains("audio"))
            StoryAudioManager.Instance.TriggerStory(StoryTriggerType.PickupNote);
        else if (id.Contains("energy") || id.Contains("cristal") || id.Contains("noyau"))
            StoryAudioManager.Instance.TriggerStory(StoryTriggerType.PickupEnergy);
        else if (id.Contains("gas") || id.Contains("gaz") || id.Contains("combustible"))
            StoryAudioManager.Instance.TriggerStory(StoryTriggerType.PickupGas);
        else if (id.Contains("blueprint") || id.Contains("plan") || id.Contains("schema"))
            StoryAudioManager.Instance.TriggerStory(StoryTriggerType.PickupBlueprint);
    }
}
