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
        // 🟢 [FIXED] ย้ายมารองรับฟังก์ชันอัปเดตแผงพลังตั้งแต่จังหวะเปิดเกมสากล
        if (ResourceInventory.Instance != null)
        {
            ResourceInventory.Instance.OnInventoryChanged += RefreshToolbarDisplay;
        }
    }

    private void OnEnable()
    {
        RefreshToolbarDisplay();
    }

    private void Start()
    {
        BuildToolbarSlots();
        SelectSlot(0);
        RefreshToolbarDisplay();
    }

    private void Update()
    {
        HandleKeyboardInput();
        if (Input.GetMouseButtonDown(0))
        {
            TriggerItemUsageOfSelectedSlot();
        }
    }

    private void BuildToolbarSlots()
    {
        if (toolbarSlots.Count > 0) return;

        for (int i = 0; i < toolbarSize; i++)
        {
            GameObject slotGo = Instantiate(slotPrefab, toolbarRoot);
            ItemSlotUI slotScript = slotGo.GetComponent<ItemSlotUI>();

            slotScript.slotIndex = i;
            toolbarSlots.Add(slotScript);
        }
    }

    public void RefreshToolbarDisplay()
    {
        if (ResourceInventory.Instance == null) return;

        // 🟢 [FIXED] เช็คและคุมการสร้างสล็อตอย่างปลอดภัยก่อนลงสี
        if (toolbarSlots.Count == 0) BuildToolbarSlots();

        for (int i = 0; i < toolbarSlots.Count; i++)
        {
            if (i < ResourceInventory.Instance.slots.Length)
            {
                InventorySlotData slotData = ResourceInventory.Instance.slots[i];
                if (slotData != null)
                {
                    toolbarSlots[i].UpdateSlotDisplay(slotData);
                }
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
            Canvas.ForceUpdateCanvases();
            RectTransform slotRect = toolbarSlots[index].GetComponent<RectTransform>();

            if (slotRect != null)
            {
                selectionHighlight.transform.position = slotRect.transform.position;
            }
        }
    }

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
            // สั่งใช้งานไอเทม (เช่น เรียกพิมพ์เขียว Grid Placement)
            bool useSuccess = selectedSlot.itemData.UseItem(GameObject.FindGameObjectWithTag("Player"));

            // 🟢 [FIXED ตรงนี้]: ถ้าใช้ไอเทมสำเร็จ หรือมีการหักของออกไปแล้ว
            // ให้สั่งกระเป๋าใหญ่ตะโกนบอกให้ UI ทุกตัว (ทั้ง Toolbar และ InventoryUI) วาดรูปของใหม่ทันที!
            if (useSuccess)
            {
                ResourceInventory.Instance.NotifyChanged();
            }
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