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
            Debug.Log($"[Item] สวมใส่ {itemName} แล้ว!");
            return true;
        }

        // 🟢 1. ถ้าเป็นเมล็ดพืช (Seed) ➡️ ส่งไปให้ FarmingManager จำค่าไว้เตรียมปลูก
        if (itemType == ItemType.Seed)
        {
            if (this is SO_PlantData plantData && FarmingManager.Instance != null)
            {
                FarmingManager.Instance.currentSelectedSeed = plantData;
                Debug.Log($"🌱 [Item]: เลือกเมล็ด {itemName} เตรียมปลูกแล้ว คลิกซ้ายที่แปลงผักได้เลย!");
                return true;
            }
        }

        // 🟢 2. ถ้าเป็นผลผลิต (Plant_Product) ➡️ ส่งไปให้ PlayerThrowManager เปิดโหมดขว้างล่อสัตว์
        if (itemType == ItemType.Plant_Product)
        {
            if (PlayerThrowManager.Instance != null)
            {
                PlayerThrowManager.Instance.StartThrowMode(this);
                Debug.Log($"🍎 [Item]: เลือกผลผลิต {itemName} เตรียมขว้างล่อสัตว์แล้ว คลิกซ้ายเลือกจุดตกบนพื้น!");
                return true;
            }
        }



        Debug.LogWarning($"[Item] {itemName} ไม่สามารถกดใช้งานตรงๆ ได้");
        return false;
    }
}