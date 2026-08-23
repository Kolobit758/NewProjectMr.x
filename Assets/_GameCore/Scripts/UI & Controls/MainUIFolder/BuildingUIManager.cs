using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class BuildingUIManager : MonoBehaviour
{
    public List<SO_Building> buildingData = new List<SO_Building>();
    public GameObject btnPrefab;
    public Transform menuContainer;

    [Header("Tooltip References")]
    public GameObject tooltipPanel;    // หน้าต่าง Tooltip เล็กๆ ที่ซ่อนไว้
    public Transform tooltipContentContainer; // Layout สำหรับวางรายการไอเทมใน Tooltip
    public GameObject tooltipItemRowPrefab;   // Prefab แถวแสดง (รูปไอคอน + จำนวน)

    void Start()
    {
        // if (tooltipPanel != null) tooltipPanel.SetActive(false);

        // 🟢 ดึง list ตึกทั้งหมดที่ลากใส่ไว้ใน Inspector (buildingData) ไปให้ UnlockManager ใช้เลย ไม่ต้องพิมพ์อะไรเพิ่ม
        if (BuildingUnlockManagers.Instance != null)
            BuildingUnlockManagers.Instance.InitializeStartingUnlocks(buildingData);

        CreateMenu();
    }

    void OnEnable()
    {
        GridPlacementManager.OnBuildingPlaced += HandleBuildingPlaced;
        if (BuildingUnlockManagers.Instance != null)
            BuildingUnlockManagers.Instance.OnUnlockChanged += CreateMenu;
    }

    void OnDisable()
    {
        GridPlacementManager.OnBuildingPlaced -= HandleBuildingPlaced;
        if (BuildingUnlockManagers.Instance != null)
            BuildingUnlockManagers.Instance.OnUnlockChanged -= CreateMenu;
    }

    private void HandleBuildingPlaced(SO_Building placed)
    {
        // 🟢 ไม่ต้องสั่งปลดล็อคตอนวางพิมพ์เขียว (Ghost) แล้ว
        // ระบบจะสั่งปลดล็อคผ่าน GhostBuilding.FinishBuilding() เมื่อสร้างตึกจริงเสร็จสมบูรณ์แทน
    }

    public void CreateMenu()
    {
        foreach (Transform child in menuContainer) Destroy(child.gameObject);

        foreach (SO_Building building in buildingData)   // 🟢 loop ทุกตึกใน list ไม่ข้ามตัวที่ล็อค
        {
            if (building == null) continue;

            bool unlocked = BuildingUnlockManagers.Instance == null || BuildingUnlockManagers.Instance.IsUnlocked(building);

            GameObject btnObj = Instantiate(btnPrefab, menuContainer);   // 🟢 สร้างปุ่มเสมอ ไม่ว่าจะล็อคหรือไม่
            BuildingButton btnScript = btnObj.GetComponent<BuildingButton>();
            if (btnScript != null) btnScript.InitButton(building, this, unlocked);
        }
    }

    public void SelectBuildingToPlace(SO_Building selectedBuilding)
    {
        if (BuildingUnlockManagers.Instance != null && !BuildingUnlockManagers.Instance.IsUnlocked(selectedBuilding))
        {
            Debug.LogWarning($"[Build Tree] 🔒 {selectedBuilding.itemName} ยังไม่ปลดล็อค!");
            return;
        }

        bool success = selectedBuilding.UseItem(null);
        if (success) Debug.Log("เปิดโหมดวางสำเร็จ!");
    }

    // ShowTooltip / HideTooltip เหมือนเดิม ไม่ต้องแก้

    public void ShowTooltip(SO_Building building, Vector3 buttonPosition)
    {
        if (tooltipPanel == null) return;

        ResourceCost[] buildingItem = building.requiredResources;

        // เปิด Panel ก่อนเพื่อให้ Layout ทำงาน
        tooltipPanel.SetActive(true);

        // เคลียร์รายการเก่าใน Tooltip ก่อน
        foreach (Transform child in tooltipContentContainer)
        {
            Destroy(child.gameObject);
        }

        // วนลูปสร้างแถวแสดงทรัพยากร
        foreach (ResourceCost cost in buildingItem)
        {
            if (cost.item == null) continue;

            GameObject rowObj = Instantiate(tooltipItemRowPrefab, tooltipContentContainer);

            Image iconImg = rowObj.transform.Find("Icon")?.GetComponent<Image>();
            TextMeshProUGUI amountTxt = rowObj.transform.Find("AmountText")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI ItemName = rowObj.transform.Find("ItemName")?.GetComponent<TextMeshProUGUI>();;

            if (iconImg != null && cost.item.itemIcon != null)
                iconImg.sprite = cost.item.itemIcon;

            if (amountTxt != null)
            {
                amountTxt.text = cost.amount.ToString();
                ItemName.text = cost.item.itemName.ToString();

                if (ResourceInventory.Instance != null)
                {
                    bool hasEnough = ResourceInventory.Instance.HasResource(cost.item.itemName, cost.amount);
                    amountTxt.color = hasEnough ? Color.white : Color.red;
                }
            }
        }

        // 🟢 ปรับตำแหน่งให้อยู่ "ด้านล่าง" ของปุ่ม (ลดค่า Y ลง เช่น -60 หรือ -80 ตามขนาดปุ่ม)
        tooltipPanel.transform.position = buttonPosition + new Vector3(0, +500f, 0);
    }

    // 🔴 ฟังก์ชันซ่อน Tooltip เมื่อเมาส์ออก
    public void HideTooltip()
    {
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
    }
}