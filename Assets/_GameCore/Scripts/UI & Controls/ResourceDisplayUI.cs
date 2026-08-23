using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// ใช้แทน ResourceDisplayUI เดิม
/// ต่างจากเดิมตรงที่: แทนที่จะไล่ดูของใน inventory ว่ามีอะไร "ไม่ว่าง" บ้างแล้วค่อยโชว์
/// สคริปต์นี้จะโชว์ตามรายการที่คุณตั้งไว้ใน Inspector (resourcesToShow) เสมอ
/// แม้ของชิ้นนั้นจะมี 0 ชิ้นในกระเป๋าก็ยังขึ้นให้เห็น (เหมาะกับไม้/น้ำ ที่อยากโชว์ตั้งแต่ต้นเกม)
/// และเปลี่ยนจากเช็คทุกเฟรม (Update) เป็นอัปเดตเฉพาะตอน inventory เปลี่ยนแปลงจริง (event-driven)
/// </summary>
public class ResourceDisplayUI : MonoBehaviour
{
    [System.Serializable]
    public class ResourceDisplayEntry
    {
        public SO_ItemData itemData;

        [Tooltip("ชื่อที่จะโชว์บน UI ถ้าปล่อยว่างจะใช้ itemName ของไอเทมแทน")]
        public string displayNameOverride;
    }

    [Header("UI References")]
    public TMP_Text goldDisplayText;
    public TMP_Text resourceDisplayText;

    [Header("Item Settings")]
    [Tooltip("SO_ItemData ของทองคำ")]
    public SO_ItemData goldItemData;

    [Header("ตั้งค่า Resource ที่อยากให้โชว์ตลอด (แม้จะมี 0 ชิ้น)")]
    [Tooltip("ลากไม้ น้ำ ฯลฯ มาใส่ตรงนี้ อยากโชว์อะไรก็เพิ่มเข้าลิสต์นี้")]
    public List<ResourceDisplayEntry> resourcesToShow = new List<ResourceDisplayEntry>();

    void OnEnable()
    {
        if (ResourceInventory.Instance != null)
            ResourceInventory.Instance.OnInventoryChanged += RefreshDisplay;

        RefreshDisplay();
    }

    void OnDisable()
    {
        if (ResourceInventory.Instance != null)
            ResourceInventory.Instance.OnInventoryChanged -= RefreshDisplay;
    }

    void RefreshDisplay()
    {
        UpdateGoldDisplay();
        UpdateResourceDisplay();
    }

    void UpdateGoldDisplay()
    {
        if (goldDisplayText == null || goldItemData == null || ResourceInventory.Instance == null) return;

        int currentGold = ResourceInventory.Instance.GetResourceAmount(goldItemData.itemName);
        goldDisplayText.text = $"💰 Gold: {currentGold:N0} G";
    }

    void UpdateResourceDisplay()
    {
        if (resourceDisplayText == null || ResourceInventory.Instance == null) return;

        string displayInfo = "<b>Materials:</b>\n";

        foreach (var entry in resourcesToShow)
        {
            if (entry == null || entry.itemData == null) continue;

            // ข้ามช่องทองคำถ้าดันลากมาใส่ในลิสต์นี้ด้วย (กันโชว์ซ้ำ)
            if (goldItemData != null && entry.itemData == goldItemData) continue;

            int amount = ResourceInventory.Instance.GetResourceAmount(entry.itemData.itemName);
            string label = string.IsNullOrEmpty(entry.displayNameOverride) ? entry.itemData.itemName : entry.displayNameOverride;

            displayInfo += $"- {label}: {amount}\n";
        }

        resourceDisplayText.text = displayInfo;
    }
}