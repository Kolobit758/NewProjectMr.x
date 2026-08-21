using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // 🟢 1. เรียกใช้น้ำพุ EventSystems เพื่อดักจับเมาส์ชี้
using TMPro;

public class BuildingButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI Elements in Button")]
    public Image buildingIconImage;
    public TextMeshProUGUI buildingNameText;
    public GameObject lockIcon; // 🟢 ไอคอนกุญแจ ลากใส่ใน Inspector (จะซ่อน/โชว์อัตโนมัติ)

    private SO_Building buildingData;
    private BuildingUIManager uiManager;

    public void InitButton(SO_Building data, BuildingUIManager manager, bool isUnlocked)
    {
        buildingData = data;
        uiManager = manager;

        if (buildingIconImage != null && buildingData.itemIcon != null)
            buildingIconImage.sprite = buildingData.itemIcon;

        if (buildingNameText != null)
            buildingNameText.text = buildingData.itemName; // ชื่อโชว์ปกติ (ถ้าอยากซ่อนชื่อเป็น "???" ตอนล็อค บอกได้ ปรับเพิ่มให้)

        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.interactable = isUnlocked;              // 🔒 กดไม่ได้ถ้ายังไม่ปลดล็อค
            btn.onClick.AddListener(OnClickBuildingButton);
        }

        if (buildingIconImage != null)
            buildingIconImage.color = isUnlocked ? Color.white : new Color(0.4f, 0.4f, 0.4f, 1f); // 🟢 หรี่ไอคอนเป็นสีเทาถ้าล็อค

        if (buildingNameText != null)
            buildingNameText.color = isUnlocked ? Color.black : new Color(0.5f, 0.5f, 0.5f, 1f); // 🟢 ทำชื่อเป็นสีเทาด้วย จะได้ดูล็อคชัดๆ
    }

    private void OnClickBuildingButton()
    {
        if (uiManager != null && buildingData != null)
        {
            uiManager.SelectBuildingToPlace(buildingData);
        }
    }

    // 🟢 2. เมื่อเมาส์ "ชี้" เข้ามาที่ปุ่มนี้
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (buildingData != null && uiManager != null)
        {
            // สั่งให้ UI Manager แสดง Tooltip ขึ้นมา พร้อมส่งข้อมูล requiredResources ไปโชว์
            uiManager.ShowTooltip(buildingData.requiredResources, transform.position);
        }
    }

    // 🔴 3. เมื่อเมาส์ "ออก" จากปุ่มนี้
    public void OnPointerExit(PointerEventData eventData)
    {
        if (uiManager != null)
        {
            uiManager.HideTooltip();
        }
    }
}