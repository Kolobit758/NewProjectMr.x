using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// แปะสคริปต์นี้ไว้ในซีนตอนเริ่มเกม (แนบไว้กับ GameObject เดียวกับ ResourceInventory ก็ได้)
/// ใช้ตั้งค่าทองเริ่มต้น + ทรัพยากรเริ่มต้น (ไม้ น้ำ ฯลฯ) ที่จะยัดเข้ากระเป๋าตอนเริ่มเกม
/// </summary>
public class GameStartResourceSetup : MonoBehaviour
{
    [System.Serializable]
    public class StartingResourceEntry
    {
        public SO_ItemData itemData;
        public int amount;
    }

    [Header("ทองเริ่มต้น")]
    [Tooltip("ลาก SO_ItemData ของทองคำมาใส่ (ตัวเดียวกับที่ใช้ใน ResourceHudUI)")]
    public SO_ItemData goldItemData;
    public int startingGold = 100;

    [Header("ทรัพยากรเริ่มต้นอื่นๆ (ไม้ น้ำ ฯลฯ)")]
    public List<StartingResourceEntry> startingResources = new List<StartingResourceEntry>();

    void Start()
    {
        if (ResourceInventory.Instance == null)
        {
            Debug.LogWarning("[GameStartResourceSetup] ไม่พบ ResourceInventory.Instance ในซีน");
            return;
        }

        // ⚠️ ถ้าเปิดระบบเซฟ/โหลดไว้ ให้ข้ามการยัดทรัพยากรเริ่มต้น
        // เพราะข้อมูลจริงควรมาจาก SaveLoadManager แทน ไม่งั้นจะทับ/บวกซ้ำกับของที่โหลดมา
        if (ResourceInventory.Instance.enableInventorySaveLoad)
        {
            Debug.Log("[GameStartResourceSetup] เปิดระบบเซฟไว้ (enableInventorySaveLoad = true) จึงข้ามการตั้งค่าทรัพยากรเริ่มต้น");
            return;
        }

        if (goldItemData != null && startingGold > 0)
        {
            ResourceInventory.Instance.AddResource(goldItemData, startingGold, false);
        }

        foreach (var entry in startingResources)
        {
            if (entry != null && entry.itemData != null && entry.amount > 0)
            {
                ResourceInventory.Instance.AddResource(entry.itemData, entry.amount, false);
            }
        }
    }
}