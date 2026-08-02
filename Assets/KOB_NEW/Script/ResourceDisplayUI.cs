using UnityEngine;
using TMPro;

public class ResourceDisplayUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text goldDisplayText;     // Text สำหรับโชว์จำนวนเงิน/ทอง
    public TMP_Text resourceDisplayText; // Text สำหรับโชว์รายการวัตถุดิบ (Material)

    [Header("Item Settings")]
    public SO_ItemData goldItemData;     // 💰 ลาก SO_ItemData ของทองคำมาใส่

    void Update()
    {
        UpdateGoldDisplay();
        UpdateResourceDisplay();
    }

    // 🟢 อัปเดตแสดงจำนวนเงิน
    void UpdateGoldDisplay()
    {
        if (goldDisplayText == null || goldItemData == null || ResourceInventory.Instance == null) return;

        int currentGold = ResourceInventory.Instance.GetResourceAmount(goldItemData.itemName);
        goldDisplayText.text = $"💰 Gold: {currentGold:N0} G";
    }

    // 🟢 อัปเดตแสดงรายการเฉพาะประเภท Material เท่านั้น
    void UpdateResourceDisplay()
    {
        if (resourceDisplayText == null || ResourceInventory.Instance == null) return;

        string displayInfo = "<b>Materials:</b>\n";
        
        foreach (var slot in ResourceInventory.Instance.slots)
        {
            if (slot != null && !slot.IsEmpty && slot.itemData != null)
            {
                // 1. ข้ามช่องทองคำ
                if (goldItemData != null && slot.itemData == goldItemData) continue;

                // 2. 🔍 กรองเฉพาะประเภท Material 
                // (⚠️ หมายเหตุ: ถ้าใน SO_ItemData ของคุณตัวแปรชื่ออื่น เช่น category หรือ itemCategory ให้เปลี่ยน slot.itemData.itemType เป็นตัวแปรนั้นได้เลยครับ)
                // ตัวอย่างเงื่อนไข:
                if (slot.itemData.itemType != ItemType.Material) continue;

                // นำชื่อไอเทมและจำนวนมาแสดงผล
                displayInfo += $"- {slot.itemData.itemName}: {slot.amount}\n";
            }
        }

        resourceDisplayText.text = displayInfo;
    }
}