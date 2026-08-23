using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.EventSystems;

/// <summary>
/// ติดสคริปต์นี้ไว้บน Prefab ของ "ช่องขายพืช 1 ช่อง" (ตัว Button + Text ชื่อ + Text ราคา)
/// หน้าที่ของมันคือโชว์ข้อมูลของตัวเอง (จาก data ที่ถูกส่งเข้ามา) และแจ้ง PlantShopUI ตอนถูกกด
/// ไม่ต้องผูก onClick ในตัว Prefab เอง - สคริปต์นี้ผูกให้เองตอน Setup()
/// </summary>
public class PlantShopSlotUI : MonoBehaviour , IPointerEnterHandler , IPointerExitHandler
{
    [Header("UI References (ลากมาจากตัว Prefab เอง)")]
    public Button selectButton;
    public TMP_Text nameText;
    public TMP_Text priceText;
    public Image iconImage; // ถ้า SO_ItemData ของคุณมีฟิลด์รูปไอคอนอยู่ ค่อยต่อสายเพิ่มเองใน Setup()

    private SO_PlantData plantData;
    private int price;
    private Action<SO_PlantData, int> onSelectCallback;

    /// <summary>
    /// เรียกจาก PlantShopUI ตอน spawn slot นี้ขึ้นมา เพื่อยัดข้อมูลพืชและ callback ตอนกด
    /// </summary>
    public void Setup(SO_PlantData plantDataIn, int priceIn, Action<SO_PlantData, int> onSelect)
    {
        plantData = plantDataIn;
        price = priceIn;
        onSelectCallback = onSelect;

        if (nameText != null && plantData != null)
            nameText.text = plantData.itemName;

        if (priceText != null)
            priceText.text = $"{price} G";

        // ตัวอย่างถ้า SO_ItemData มีฟิลด์ไอคอน ให้ปลดคอมเมนต์แล้วแก้ชื่อฟิลด์ตามจริง
        // if (iconImage != null && plantData.icon != null)
        //     iconImage.sprite = plantData.icon;

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(HandleClick);
        }
    }

    private void HandleClick()
    {
        onSelectCallback?.Invoke(plantData, price);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        PlantTooltipUI.Instance.ShowPlantTooltip(plantData,transform.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        PlantTooltipUI.Instance.HideTooltip();
    }
}