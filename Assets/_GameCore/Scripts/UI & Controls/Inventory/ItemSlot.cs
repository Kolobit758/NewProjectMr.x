using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ItemSlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public Image itemIconImage;
    public TextMeshProUGUI amountText;
    public TextMeshProUGUI itemName;

    [Header("Runtime Info")]
    public int slotIndex;
    private InventorySlotData myCurrentData;

    // 🟢 เปลี่ยนมาเก็บเรฟเฟอเรนซ์ของหน้าต่างตัวเองโดยตรง (รองรับหลายตัว)
    private CategoryInventoryUI categoryUIManager;

    private static GameObject dragIconClone;
    private CanvasGroup canvasGroup;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    // 🟢 สร้างฟังก์ชันนี้เพื่อให้ CategoryInventoryUI วิ่งเข้ามาผูกค่าตอนสร้าง Slot
    public void SetInventoryUI(CategoryInventoryUI ui)
    {
        categoryUIManager = ui;
    }

    public void UpdateSlotDisplay(InventorySlotData data)
    {
        myCurrentData = data;

        if (data == null || data.IsEmpty)
        {
            itemIconImage.enabled = false;
            amountText.text = "";
            itemName.text = "";
        }
        else
        {
            itemIconImage.enabled = true;
            itemIconImage.sprite = data.itemData.itemIcon;
            amountText.text = data.amount > 1 ? data.amount.ToString() : "";
            itemName.text = data.itemData.itemName.ToString();
        }
    }

    // public void OnBeginDrag(PointerEventData eventData)
    // {
    //     if (myCurrentData == null || myCurrentData.IsEmpty) return;

    //     // 🟢 ไอเทมประเภทตึก ไม่ให้ลากสลับช่อง ให้ใช้วิธีคลิกอย่างเดียว
    //     if (myCurrentData.itemData is SO_Building)
    //     {
    //         eventData.pointerDrag = null; // บอก EventSystem ว่าอย่าเริ่ม Drag กับตัวนี้
    //         return;
    //     }

    //     canvasGroup.alpha = 0.5f;
    //     canvasGroup.blocksRaycasts = false;

    //     dragIconClone = new GameObject("DragIconClone");
    //     Canvas rootCanvas = GetComponentInParent<Canvas>().rootCanvas;
    //     dragIconClone.transform.SetParent(rootCanvas.transform, false);
    //     dragIconClone.transform.SetAsLastSibling();

    //     Image cloneImage = dragIconClone.AddComponent<Image>();
    //     cloneImage.sprite = itemIconImage.sprite;
    //     cloneImage.raycastTarget = false;

    //     RectTransform cloneRect = dragIconClone.GetComponent<RectTransform>();
    //     RectTransform myRect = itemIconImage.GetComponent<RectTransform>();
    //     cloneRect.sizeDelta = myRect.sizeDelta;

    //     itemIconImage.enabled = false;
    //     amountText.text = "";
    //     itemName.text = "";
    // }

    // public void OnDrag(PointerEventData eventData)
    // {
    //     if (dragIconClone != null)
    //     {
    //         dragIconClone.transform.position = eventData.position;
    //     }
    // }

    // public void OnEndDrag(PointerEventData eventData)
    // {
    //     canvasGroup.blocksRaycasts = true;

    //     if (dragIconClone != null)
    //     {
    //         Destroy(dragIconClone);
    //         dragIconClone = null;
    //     }

    //     KobInventoryUI mainUI = FindAnyObjectByType<KobInventoryUI>();
    //     if (mainUI != null) mainUI.RefreshGridDisplay();

    //     KobToolbarUI toolbarUI = FindAnyObjectByType<KobToolbarUI>();
    //     if (toolbarUI != null) toolbarUI.RefreshToolbarDisplay();
    // }

    // public void OnDrop(PointerEventData eventData)
    // {
    //     ItemSlotUI droppedSlot = eventData.pointerDrag?.GetComponent<ItemSlotUI>();

    //     if (droppedSlot != null)
    //     {
    //         ResourceInventory.Instance.SwapItems(droppedSlot.slotIndex, this.slotIndex);
    //     }
    // }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (myCurrentData == null || myCurrentData.IsEmpty || myCurrentData.itemData == null) return;

        bool success = myCurrentData.itemData.UseItem(GameObject.FindGameObjectWithTag("Player"));

        if (success)
        {
            Debug.Log($"[UI Click] กดใช้งาน {myCurrentData.itemData.itemName} จากหน้าต่าง UI สำเร็จ!");
        }
    }

    // 🟢 แก้ไขฟังก์ชัน OnPointerEnter ให้ถูกต้องตามตัวแปรจริงในคลาส
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (myCurrentData == null || myCurrentData.IsEmpty || myCurrentData.itemData == null) return;

        // ถ้าไอเทมชิ้นนี้เป็นเมล็ดพืช (SO_PlantData) ให้เรียกใช้ PlantTooltipUI แบบพิเศษ
        if (myCurrentData.itemData is SO_PlantData plantData)
        {
            if (PlantTooltipUI.Instance != null)
            {
                PlantTooltipUI.Instance.ShowPlantTooltip(plantData, transform.position);
            }
        }
        else
        {
            // ถ้าเป็นไอเทมธรรมดา ให้ใช้ Tooltip ปกติของ CategoryInventoryUI
            if (categoryUIManager != null)
            {
                categoryUIManager.ShowTooltip(myCurrentData.itemData, transform.position);
            }
        }
    }

    // 🟢 แก้ไขฟังก์ชัน OnPointerExit ให้ถูกต้อง
    public void OnPointerExit(PointerEventData eventData)
    {
        if (PlantTooltipUI.Instance != null) PlantTooltipUI.Instance.HideTooltip();
        if (categoryUIManager != null) categoryUIManager.HideTooltip();
    }
}