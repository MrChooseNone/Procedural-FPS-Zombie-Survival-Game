using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public GameObject prefab; // Reference to the actual 3D object (if needed)
    public int quantity;
    public int quantityPerItem;
    public bool isStackable;
}
