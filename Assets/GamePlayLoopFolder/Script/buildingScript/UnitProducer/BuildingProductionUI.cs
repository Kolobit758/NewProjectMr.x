using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

[RequireComponent(typeof(UIAnimationController))] // 🟢 บังคับให้ต้องมีสคริปต์อนิเมชันร่วมด้วย
public class BuildingProductionUI : MonoBehaviour
{
    public static BuildingProductionUI Instance { get; private set; }

    [Header("UI Panel")]
    public GameObject productionPanelParent; // หน้าต่าง UI กลางที่จะให้เปิด/ปิด

    [Header("UI References")]
    [HideInInspector] public UnitProducerBuilding producerBuilding; 
    public Transform dataContainer;          
    public GameObject unitButtonPrefab;      

    private UIAnimationController animController; // 🟢 ตัวควบคุมแอนิเมชันและฝุ่น

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // ดึงคอมโพเนนต์แอนิเมชันที่อยู่บน Panel เดียวกันมาเก็บไว้
        if (productionPanelParent != null)
        {
            animController = productionPanelParent.GetComponent<UIAnimationController>();
        }

        // ปิด Panel ไว้ก่อนตอนเริ่มเกม
        if (productionPanelParent != null) productionPanelParent.SetActive(false);
    }

    // 🟢 ฟังก์ชัน Open ที่สั่งรันผ่าน UIAnimationController (มีสไลด์ + ฝุ่น)
    public void Open(UnitProducerBuilding targetBuilding)
    {
        if (targetBuilding == null) return;

        producerBuilding = targetBuilding;

        if (productionPanelParent != null)
        {
            if (animController != null)
            {
                Debug.Log("try to play animation");
                // สสั่งเปิดผ่านอนิเมชัน (มันจะเปิด GameObject + สไลด์ + เล่นฝุ่นให้เอง)
                animController.OpenUI();
            }
            else
            {
                productionPanelParent.SetActive(true);
            }
        }

        RefreshButtons();
    }

    // 🔴 ฟังก์ชัน Close ที่สั่งรันผ่าน UIAnimationController (สไลด์เก็บ + ฝุ่น แล้วปิดตัวเอง)
    public void Close()
    {
        if (productionPanelParent != null)
        {
            if (animController != null)
            {
                // สั่งปิดผ่านอนิเมชัน (มันจะสไลด์ออก + เล่นฝุ่น แล้ว Active(false) ให้เองอัตโนมัติ)
                animController.CloseUI(() => {
                    producerBuilding = null;
                });
            }
            else
            {
                productionPanelParent.SetActive(false);
                producerBuilding = null;
            }
        }
    }

    public void RefreshButtons()
    {
        foreach (Transform child in dataContainer)
        {
            Destroy(child.gameObject);
        }

        if (producerBuilding == null || unitButtonPrefab == null || dataContainer == null) return;

        for (int i = 0; i < producerBuilding.availableUnits.Count; i++)
        {
            int index = i; 
            UnitDataSO unitData = producerBuilding.availableUnits[i];
            if (unitData == null) continue;

            GameObject btnObj = Instantiate(unitButtonPrefab, dataContainer);
            
            TMP_Text nameText = btnObj.GetComponentInChildren<TMP_Text>();
            if (nameText != null)
            {
                nameText.text = $"{unitData.speciesName}\n💰 {unitData.coinCost} G";
            }

            Image iconImg = btnObj.transform.Find("Icon")?.GetComponent<Image>();
            if (iconImg != null && unitData.unitIcon != null)
            {
                iconImg.sprite = unitData.unitIcon;
            }

            Button btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() => producerBuilding.RequestProduceUnit(index));
            }
        }
    }
}