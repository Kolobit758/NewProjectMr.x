using System.Collections.Generic;
using UnityEngine;

public class KobToolbarUI : MonoBehaviour
{
    [Header("Toolbar Settings")]
    public Transform toolbarRoot;
    public GameObject slotPrefab;
    public int toolbarSize = 8;

    [Header("Selection Indicator")]
    public RectTransform selectionHighlight;

    private List<ItemSlotUI> toolbarSlots = new List<ItemSlotUI>();
    private int currentSelectedIndex = 0;

    private void Awake()
    {
        if (ResourceInventory.Instance != null)
        {
            ResourceInventory.Instance.OnInventoryChanged += RefreshToolbarDisplay;
        }
    }

    private void Start()
    {
        BuildToolbarSlots();
        SelectSlot(0);
    }

    private void Update()
    {
        HandleKeyboardInput();
    }

    // 🏗️ ตรงฟังก์ชันเสกช่องสี่เหลี่ยมของ Toolbar ปรับให้ชี้พิกัด 0 ถึง 7 ตามเดิม
    private void BuildToolbarSlots()
    {
        if (toolbarSlots.Count > 0) return;

        for (int i = 0; i < toolbarSize; i++)
        {
            GameObject slotGo = Instantiate(slotPrefab, toolbarRoot);
            ItemSlotUI slotScript = slotGo.GetComponent<ItemSlotUI>();

            // 🟢 มอบสิทธิ์ขาด: ช่อง Toolbar ที่ i คุมพื้นที่อาเรย์ใน RAM ช่องที่ i ตรงๆ (0-7)
            slotScript.slotIndex = i;
            toolbarSlots.Add(slotScript);
        }
        RefreshToolbarDisplay();
    }

    public void RefreshToolbarDisplay()
    {
        if (ResourceInventory.Instance == null) return;
        if (toolbarSlots.Count == 0) BuildToolbarSlots();

        for (int i = 0; i < toolbarSlots.Count; i++)
        {
            if (i < ResourceInventory.Instance.slots.Length)
            {
                InventorySlotData slotData = ResourceInventory.Instance.slots[i];
                toolbarSlots[i].UpdateSlotDisplay(slotData);
            }
        }
    }

    private void HandleKeyboardInput()
    {
        for (int i = 0; i < toolbarSize; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                SelectSlot(i);
                break;
            }
        }

        float scroll = Input.GetAxisRaw("Mouse ScrollWheel");
        if (scroll > 0f)
        {
            int nextSlot = (currentSelectedIndex - 1 + toolbarSize) % toolbarSize;
            SelectSlot(nextSlot);
        }
        else if (scroll < 0f)
        {
            int nextSlot = (currentSelectedIndex + 1) % toolbarSize;
            SelectSlot(nextSlot);
        }
    }

    public void SelectSlot(int index)
    {
        if (index < 0 || index >= toolbarSize) return;
        currentSelectedIndex = index;

        if (selectionHighlight != null && toolbarSlots.Count > index && toolbarSlots[index] != null)
        {
            // 🟢 1. บังคับให้ Unity คำนวณตำแหน่ง Grid Layout ของ Slot ให้เสร็จในเฟรมนี้ทันที ป้องกันพิกัดเอ๋อตอนเริ่มเกม
            Canvas.ForceUpdateCanvases();

            // 🟢 2. ดึง RectTransform ของช่อง Slot ที่เรากำลังเลือก
            RectTransform slotRect = toolbarSlots[index].GetComponent<RectTransform>();

            if (slotRect != null)
            {
                // 🟢 3. ย้ายกรอบสีแดงไปเกาะที่ตำแหน่งของช่อง Slot นั้นตรงๆ แบบอ้างอิง Parent เดียวกัน
                selectionHighlight.transform.position = slotRect.transform.position;

                // ถ้าย้ายแล้วยังไม่ตรง/หลุดจอ ให้สลับไปใช้บรรทัดข้างล่างนี้แทนครับ:
                // selectionHighlight.anchoredPosition = slotRect.anchoredPosition;
            }
        }
    }
    // 🟢 แปะฟังก์ชันนี้เพิ่มเข้าไปใน KobToolbarUI.cs เพื่อให้สคริปต์เมาส์ซ้ายวิ่งมาดึงค่าช่องปัจจุบันไปใช้
    public int GetCurrentSelectedIndex()
    {
        return currentSelectedIndex;
    }
    private void TriggerItemUsageOfSelectedSlot()
    {
        if (ResourceInventory.Instance == null) return;

        InventorySlotData selectedSlot = ResourceInventory.Instance.slots[currentSelectedIndex];

        if (selectedSlot != null && !selectedSlot.IsEmpty && selectedSlot.itemData != null)
        {
            // ดึงสคริปต์ไอเทม/อาวุธคอมโบขึ้นมาสวมใส่เข้าตัวผู้เล่นทันที
            selectedSlot.itemData.UseItem(GameObject.FindGameObjectWithTag("Player"));
        }
        else
        {
            // มือเปล่ากรณีช่องนั้นไม่มีของ
            // PlayerCombat combat = FindAnyObjectByType<PlayerCombat>();
            // if (combat != null) combat.UnEquipCurrentWeapon();
        }
    }

    private void OnDestroy()
    {
        if (ResourceInventory.Instance != null)
        {
            ResourceInventory.Instance.OnInventoryChanged -= RefreshToolbarDisplay;
        }
    }
}