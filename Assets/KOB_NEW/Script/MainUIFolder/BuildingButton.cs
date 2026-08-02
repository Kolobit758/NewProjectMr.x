using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // 🟢 1. เรียกใช้น้ำพุ EventSystems เพื่อดักจับเมาส์ชี้
using TMPro;

public class BuildingButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI Elements in Button")]
    public Image buildingIconImage;       // รูปภาพตึก/สิ่งก่อสร้าง
    public TextMeshProUGUI buildingNameText; // ชื่อสิ่งก่อสร้าง (ตามที่วาดไอเดียไว้)

    private SO_Building buildingData;
    private BuildingUIManager uiManager;

    public void InitButton(SO_Building data, BuildingUIManager manager)
    {
        buildingData = data;
        uiManager = manager;

        // 1. เซ็ตภาพไอคอนตึก
        if (buildingIconImage != null && buildingData.itemIcon != null)
        {
            buildingIconImage.sprite = buildingData.itemIcon;
        }

        // 2. เซ็ตชื่อตึก
        if (buildingNameText != null)
        {
            buildingNameText.text = buildingData.itemName;
        }

        // 3. ผูกปุ่มกดเลือกสร้าง
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnClickBuildingButton);
        }
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