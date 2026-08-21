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
        // 🟢 [FIXED] ดึงสิทธิ์ให้มาซับสคริบอีเวนต์ใน Awake เพื่อให้ทันจังหวะระบบโหลดเกมตัวอื่นทำงาน
        if (ResourceInventory.Instance != null)
        {
            ResourceInventory.Instance.OnInventoryChanged += RefreshGridDisplay;
        }
    }

    private void OnEnable()
    {
        RefreshGridDisplay();
    }

    void Start()
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

        // 🟢 [FIXED] ถ้าสล็อตยังไม่เคยเกิด ให้เสกสร้างขึ้นมา
        if (spawnedSlots.Count == 0)
        {
            for (int i = 0; i < 24; i++)
            {
                GameObject slotGo = Instantiate(slotPrefab, gridRoot);
                ItemSlotUI slotScript = slotGo.GetComponent<ItemSlotUI>();

                slotScript.slotIndex = i + 8; 
                spawnedSlots.Add(slotScript);
            }
        }

        // 🟢 ลูปอัปเดตพ่นสีทับข้อมูล Realtime
        for (int i = 0; i < spawnedSlots.Count; i++)
        {
            int actualRamIndex = i + 8;

            if (actualRamIndex < ResourceInventory.Instance.slots.Length)
            {
                InventorySlotData slotData = ResourceInventory.Instance.slots[actualRamIndex];
                
                // 🟢 [FIXED] ส่งข้อมูลสเตตัสล่าสุดให้สล็อตวาดภาพเสมอ
                if (slotData != null)
                {
                    spawnedSlots[i].UpdateSlotDisplay(slotData);
                }
            }
        }
    }
}