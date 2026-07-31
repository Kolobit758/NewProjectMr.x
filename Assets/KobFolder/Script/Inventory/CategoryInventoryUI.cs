using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class CategoryInventoryUI : MonoBehaviour
{
    [Header("UI References")]
    public Transform container;          // Parent (เช่น Layout Group ใน Panel ของพืช/ปุ๋ย/ผลผลิต)
    public GameObject slotPrefab;        // Prefab ช่องแสดงผล (ใช้ ItemSlotUI หรือ Prefab ปุ่มย่อยของคุณ)

    [Header("Filter Settings")]
    public ItemType targetItemCategory;  // เลือกประเภทที่จะให้แสดงในหน้านี้ (เช่น Seed, Fertilizer, Plant_Product)
    [Header("Tooltip References")]
    public GameObject tooltipPanel;    // หน้าต่าง Tooltip เล็กๆ ที่ซ่อนไว้
    public TMP_Text tooltipLebel;
    public TMP_Text tooltipDetail;

    private List<ItemSlotUI> spawnedSlots = new List<ItemSlotUI>();

    private void OnEnable()
    {
        RefreshCategoryDisplay();

        // ผูกอีเวนต์คลังเปลี่ยนแปลง เพื่อให้อัปเดตตัวเลขแบบ Realtime
        if (ResourceInventory.Instance != null)
        {
            ResourceInventory.Instance.OnInventoryChanged += RefreshCategoryDisplay;
        }
    }

    private void OnDisable()
    {
        if (ResourceInventory.Instance != null)
        {
            ResourceInventory.Instance.OnInventoryChanged -= RefreshCategoryDisplay;
        }
    }

    public void RefreshCategoryDisplay()
    {
        if (ResourceInventory.Instance == null || container == null || slotPrefab == null) return;

        // 1. กรองเฉพาะไอเทมในกระเป๋าหลัก (ResourceInventory) ที่ตรงกับประเภท (targetItemCategory)
        List<InventorySlotData> filteredItems = new List<InventorySlotData>();
        foreach (var slot in ResourceInventory.Instance.slots)
        {
            if (!slot.IsEmpty && slot.itemData != null && slot.itemData.itemType == targetItemCategory)
            {
                filteredItems.Add(slot);
            }
        }

        // 2. จัดการสร้างหรือทำลาย Slot UI ใน Container ให้ตรงกับจำนวนไอเทมที่มี
        while (spawnedSlots.Count < filteredItems.Count)
        {
            GameObject slotGo = Instantiate(slotPrefab, container);
            ItemSlotUI slotScript = slotGo.GetComponent<ItemSlotUI>();
            if (slotScript != null) spawnedSlots.Add(slotScript);
        }

        for (int i = 0; i < spawnedSlots.Count; i++)
        {
            if (i < filteredItems.Count)
            {
                spawnedSlots[i].gameObject.SetActive(true);

                // 🟢 เพิ่มบรรทัดนี้: ส่งเรฟเฟอเรนซ์ของตัว CategoryInventoryUI นี้ให้ Slot รู้จัก
                spawnedSlots[i].SetInventoryUI(this);

                // ส่งข้อมูลไอเทมและจำนวนที่มีอยู่จริงไปวาดภาพบน UI
                spawnedSlots[i].UpdateSlotDisplay(filteredItems[i]);
            }
            else
            {
                // ถ้ามีช่องเหลือ ให้ซ่อนไว้
                spawnedSlots[i].gameObject.SetActive(false);
            }
        }
    }
    public void ShowTooltip(SO_ItemData itemData, Vector3 buttonPosition)
    {
        if (tooltipPanel == null) return;

        // เปิด Panel ก่อนเพื่อให้ Layout ทำงาน
        tooltipPanel.SetActive(true);

        // เคลียร์รายการเก่าใน Tooltip ก่อน
        tooltipLebel.text = itemData.itemName;
        tooltipDetail.text = itemData.detail;

        // 🟢 ปรับตำแหน่งให้อยู่ "ด้านล่าง" ของปุ่ม (ลดค่า Y ลง เช่น -60 หรือ -80 ตามขนาดปุ่ม)
        tooltipPanel.transform.position = buttonPosition + new Vector3(0, -500f, 0);
    }
    
    public void HideTooltip()
    {
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
    }
}