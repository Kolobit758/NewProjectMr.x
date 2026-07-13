using UnityEngine;

public enum ItemType { Material, Fertilizer, Building, Weapon, Armor, Plant_Product, Seed }

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item Data")]
public class SO_ItemData : ScriptableObject
{
    public string itemId; // ใช้แทน resourceId ตัวเดิม
    public string itemName;
    public ItemType itemType;
    public Sprite itemIcon;
    [TextArea(3, 5)] public string detail;
    public GameObject dropItemPrefab; // Prefab 3D ในฉาก หรือโมเดลอาวุธตอนถือ


    public virtual bool UseItem(GameObject user)
    {
        if (itemType == ItemType.Weapon || itemType == ItemType.Armor)
        {
            Debug.Log($"[Item] สวมใส่ {itemName} แล้ว! (เพิ่มสเตตัสให้ตัวละคร)");
            // ลอจิกสวมใส่เกราะ/อาวุธของคุณ...
            return true;
        }
        if (itemType == ItemType.Plant_Product)
        {
            Debug.Log($"[Item] สวมใส่ {itemName} แล้ว!");
        }
        if (itemType == ItemType.Seed)
        {
            Debug.Log($"[Item] สวมใส่ {itemName} แล้ว!");
        }

        Debug.LogWarning($"[Item] {itemName} เป็นวัตถุดิบ ไม่สามารถกดใช้ตรงๆ ได้");
        return false;
    }
}