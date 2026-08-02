using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuildingProductionUI : MonoBehaviour
{
    // 🟢 ทำเป็น Instance เพื่อให้เรียกใช้ง่ายจากที่ไหนก็ได้
    public static BuildingProductionUI Instance { get; private set; }

    [Header("UI Panel")]
    public GameObject productionPanelParent; // หน้าต่าง UI กลางที่จะให้เปิด/ปิด

    [Header("UI References")]
    [HideInInspector] public UnitProducerBuilding producerBuilding; 
    public Transform dataContainer;          
    public GameObject unitButtonPrefab;      

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // ปิด Panel ไว้ก่อนตอนเริ่มเกม
        if (productionPanelParent != null) productionPanelParent.SetActive(false);
    }

    // 🟢 ฟังชั่น Open ง่ายๆ ที่รับค่าตึกเข้ามาแล้วเปิด UI ทันที
    public void Open(UnitProducerBuilding targetBuilding)
    {
        if (targetBuilding == null) return;

        producerBuilding = targetBuilding;

        if (productionPanelParent != null)
        {
            productionPanelParent.SetActive(true);
        }

        RefreshButtons();
    }

    public void Close()
    {
        if (productionPanelParent != null) productionPanelParent.SetActive(false);
        producerBuilding = null;
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