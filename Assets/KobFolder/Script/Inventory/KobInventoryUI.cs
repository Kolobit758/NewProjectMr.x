using System.Collections.Generic;
using UnityEngine;

public class KobInventoryUI : MonoBehaviour
{
    [Header("Grid Layout Setting")]
    public Transform gridRoot;
    public GameObject slotPrefab;

    private List<ItemSlotUI> spawnedSlots = new List<ItemSlotUI>();

    private void Awake()
    {
        if (ResourceInventory.Instance != null)
        {
            ResourceInventory.Instance.OnInventoryChanged += RefreshGridDisplay;
        }
    }

    private void OnEnable()
    {
        RefreshGridDisplay();
    }

    private void OnDestroy()
    {
        if (ResourceInventory.Instance != null)
        {
            ResourceInventory.Instance.OnInventoryChanged -= RefreshGridDisplay;
        }
    }

    public void RefreshGridDisplay()
    {
        if (ResourceInventory.Instance == null) return;
        if (gridRoot == null || slotPrefab == null) return;

        // 🟢 สั่งเสกช่องกระเป๋าใหญ่ 24 ช่องตามขนาดฟิกคงเดิม
        if (spawnedSlots.Count == 0)
        {
            for (int i = 0; i < 24; i++)
            {
                GameObject slotGo = Instantiate(slotPrefab, gridRoot);
                ItemSlotUI slotScript = slotGo.GetComponent<ItemSlotUI>();

                // 💥 ไม้ตาย Minecraft: สั่งบวกเลข Offset กระโดดข้ามแถว Toolbar ไป 8 ช่อง!
                // ช่องแรกของกระเป๋าใหญ่บนหน้าจอ (i=0) จะผูกเข้ากับช่องแรมที่ 8 จริงๆ ในระบบ C# (0 + 8 = 8)
                // ช่องสุดท้ายบนหน้าจอ (i=23) จะผูกเข้ากับช่องแรมที่ 31 จริงๆ ในระบบ C# (23 + 8 = 31)
                slotScript.slotIndex = i + 8; 
                
                spawnedSlots.Add(slotScript);
            }
        }

        // ลูปพ่นสีรูปไอคอนไอเทมบนกระเป๋าใหญ่
        for (int i = 0; i < spawnedSlots.Count; i++)
        {
            int actualRamIndex = i + 8; // ดึงค่าจาก RAM ช่องที่ 8 เป็นต้นไปมาพ่นสี

            if (actualRamIndex < ResourceInventory.Instance.slots.Length)
            {
                InventorySlotData slotData = ResourceInventory.Instance.slots[actualRamIndex];
                spawnedSlots[i].UpdateSlotDisplay(slotData);
            }
        }
    }
}