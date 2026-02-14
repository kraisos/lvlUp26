using UnityEngine;

public class Item : MonoBehaviour
{
    private const string ItemTag = "item";

    [SerializeField] private string itemId = "item";
    [SerializeField] private int quantity = 1;
    [SerializeField] private Sprite iconSprite;

    public string ItemId => itemId;
    public int Quantity => quantity;
    public Sprite IconSprite => iconSprite;

    public void Configure(string newItemId, int newQuantity, Sprite newIconSprite)
    {
        if (!string.IsNullOrWhiteSpace(newItemId))
        {
            itemId = newItemId;
        }

        quantity = Mathf.Max(1, newQuantity);
        iconSprite = newIconSprite;
    }

    public void Pickup(Inventory inventory)
    {
        inventory.AddItem(itemId, quantity, iconSprite);
        Destroy(gameObject);
    }
}
