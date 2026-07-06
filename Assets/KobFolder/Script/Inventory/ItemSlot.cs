using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ItemSlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    public Image itemIconImage;
    public TextMeshProUGUI amountText;
    public TextMeshProUGUI itemName;
    
    [Header("Runtime Info")]
    public int slotIndex; 
    private InventorySlotData myCurrentData;

    private static GameObject dragIconClone; 
    private CanvasGroup canvasGroup;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
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

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (myCurrentData == null || myCurrentData.IsEmpty) return;

        canvasGroup.alpha = 0.5f;
        canvasGroup.blocksRaycasts = false; // เปิดช่องให้เมาส์มองทะลุเห็นช่องเป้าหมายข้างหลัง

        // 🏗️ เสกไอคอนร่างโคลน บังคับให้อยู่ใน Root Canvas ชั้นนอกสุดเพื่อบินข้ามได้ทุกหน้าต่างเกม
        dragIconClone = new GameObject("DragIconClone");
        Canvas rootCanvas = GetComponentInParent<Canvas>().rootCanvas;
        dragIconClone.transform.SetParent(rootCanvas.transform, false);
        dragIconClone.transform.SetAsLastSibling(); 

        Image cloneImage = dragIconClone.AddComponent<Image>();
        cloneImage.sprite = itemIconImage.sprite;
        cloneImage.raycastTarget = false; // 🚨 ป้องกันไอคอนร่างปลอมบดบังรังสีเมาส์ตัวเอง

        RectTransform cloneRect = dragIconClone.GetComponent<RectTransform>();
        RectTransform myRect = itemIconImage.GetComponent<RectTransform>();
        cloneRect.sizeDelta = myRect.sizeDelta;

        // ซ่อนกราฟิกที่ช่องเดิมชั่วคราว (ทำหน้าที่เป็นตู้จำลอง Mock Slot คาไว้ในตาราง Layout)
        itemIconImage.enabled = false;
        amountText.text = "";
        itemName.text = "";
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragIconClone != null)
        {
            dragIconClone.transform.position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;

        if (dragIconClone != null)
        {
            Destroy(dragIconClone);
            dragIconClone = null;
        }

        // 🟢 ตะโกนสั่งกระเป๋าใหญ่ และ Toolbar ให้ดึงค่าจริงจาก RAM ออกมาพ่นสีวาดภาพคืนรูปทรงเดิมพร้อมกันทันที
        KobInventoryUI mainUI = FindAnyObjectByType<KobInventoryUI>();
        if (mainUI != null) mainUI.RefreshGridDisplay();

        KobToolbarUI toolbarUI = FindAnyObjectByType<KobToolbarUI>();
        if (toolbarUI != null) toolbarUI.RefreshToolbarDisplay();
    }

    public void OnDrop(PointerEventData eventData)
    {
        ItemSlotUI droppedSlot = eventData.pointerDrag?.GetComponent<ItemSlotUI>();

        if (droppedSlot != null)
        {
            // ยิงคำสั่งส่งสัญญาณสลับพิกัดข้ามมิติไปยังหน่วยความจำแรมหลัก
            ResourceInventory.Instance.SwapItems(droppedSlot.slotIndex, this.slotIndex);
        }
    }
}