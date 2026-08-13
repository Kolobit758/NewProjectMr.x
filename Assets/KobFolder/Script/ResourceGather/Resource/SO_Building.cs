using UnityEngine;
using System; // 🟢 จำเป็นต้องใช้สำหรับ [Serializable]

// 🟢 สร้าง Struct สำหรับเก็บข้อมูลว่า "ต้องใช้ของอะไร" และ "จำนวนเท่าไหร่"
[Serializable]
public struct ResourceCost
{
    public SO_ItemData item;
    public int amount;
}

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item Building")]
public class SO_Building : SO_ItemData
{
    [Header("Building System Connection")]
    public GameObject buildingGhostPrefab; // โมเดลโปร่งแสงสีเขียวตอนกำลังเล็งวาง
    public GameObject Realprefab;

    [Header("Building Requirements (ทรัพยากรที่ต้องใช้)")]
    public ResourceCost[] requiredResources; // 🟢 ใส่ใน Inspector ได้เลยว่าใช้ หิน 10, ไม้ 20
    [Header("Build Tree (Unlock System)")]
    public SO_Building[] prerequisites;   // 🟢 ต้องสร้างตึกเหล่านี้ก่อน ถึงจะปลดล็อคตึกนี้
    public bool startUnlocked = false;    // 🟢 ตึกนี้ปลดล็อคตั้งแต่ต้นเกมเลยไหม (ไม่ต้องมี prerequisite ก็ได้)

    public bool IsUnlocked()
    {
        if (BuildingUnlockManagers.Instance == null) return true; // กันพังกรณีลืมใส่ manager
        return BuildingUnlockManagers.Instance.IsUnlocked(this);
    }

    public override bool UseItem(GameObject user)
    {
        if (!IsUnlocked())
        {
            Debug.LogWarning($"[Build Tree] 🔒 {itemName} ยังไม่ปลดล็อค!");
            return false;
        }

        if (!CanAffordBuilding())
        {
            Debug.LogWarning($"[Grid System] ❌ ทรัพยากรไม่พอสำหรับสร้าง {itemName}!");
            return false;
        }

        if (GridPlacementManager.Instance != null && buildingGhostPrefab != null)
        {
            GridPlacementManager.Instance.StartPlacementMode(buildingGhostPrefab, this);
            Debug.Log($"[Grid System] 🏗️ เปิดพิมพ์เขียว {itemName} เล็งตำแหน่งสร้างบน Grid แล้ว!");
            return true;
        }

        return false;
    }

    // 🟢 ฟังก์ชันสำหรับเช็คว่าของในคลัง (ResourceInventory) มีพอกับที่ต้องใช้หรือไม่
    public bool CanAffordBuilding()
    {
        if (ResourceInventory.Instance == null) return false;

        foreach (ResourceCost cost in requiredResources)
        {


            if (ResourceInventory.Instance.HasResource(cost.item.itemName, cost.amount) == false) return false; // ของขาด!
        }
        return true; // ของครบ!
    }
}