using UnityEngine;

public class Item : MonoBehaviour
{
    private const string ItemTag = "item";

    [SerializeField] private string itemId = "item";
    [SerializeField] private int quantity = 1;

    public string ItemId => itemId;
    public int Quantity => quantity;

    public void Pickup(Inventory inventory)
    {
        inventory.AddItem(itemId, quantity);
        Destroy(gameObject);
    }
}
