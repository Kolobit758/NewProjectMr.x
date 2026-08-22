using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class UITeamManager : MonoBehaviour
{
    [Header("Inventory Column (ฝั่งกระเป๋าคลังสัตว์)")]
    public GameObject AnimalCardPrefab;
    public Transform inventoryContentParent;

    [Header("Formation Grid Column (ฝั่งตารางจัดทีมจำลอง)")]
    public GameObject GridSlotPrefab;
    public Transform gridLayoutParent;

    [Header("Confirm Button")]
    public Button confirmButton;

    private UnitInstance selectedUnitForFormation;
    private List<GameObject> spawnedButtons = new List<GameObject>();
    private List<UIFormationSlot> spawnedSlots = new List<UIFormationSlot>();
    [Header("Open Close : Panel")]
    public GameObject TeamPanel;
    public GameObject GridPanel;
    private bool isTeamPanelOpened;

    void OnEnable()
    {
        if (AnimalInventory.Instance != null)
        {
            AnimalInventory.Instance.OnAnimalInventoryChanged += RefreshAnimalInventoryUI;
        }
    }

    void OnDisable()
    {
        if (AnimalInventory.Instance != null)
        {
            AnimalInventory.Instance.OnAnimalInventoryChanged -= RefreshAnimalInventoryUI;
        }
    }

    void Start()
    {
        RefreshAnimalInventoryUI();
        GenerateFormationGridUI();

        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmTeamPlacement);
        }

        GridPanel.SetActive(false);
        TeamPanel.SetActive(false);
        isTeamPanelOpened = false;
    }

    [ContextMenu("Refresh Inventory")]
    public void RefreshAnimalInventoryUI()
    {
        foreach (var btn in spawnedButtons) { if (btn != null) Destroy(btn); }
        spawnedButtons.Clear();

        if (AnimalInventory.Instance == null) return;

        foreach (var unit in AnimalInventory.Instance.inventoryUnits)
        {
            if (unit == null) continue;
            GameObject newCard = Instantiate(AnimalCardPrefab, inventoryContentParent);
            spawnedButtons.Add(newCard);

            AnimalButtonUI buttonScript = newCard.GetComponent<AnimalButtonUI>();
            if (buttonScript != null)
            {
                buttonScript.SetupButton(unit, this);
            }
        }
    }

    public void GenerateFormationGridUI()
    {
        // 1. ล้างปุ่ม UI เก่าในฉากทิ้งก่อน
        foreach (var slot in spawnedSlots) { if (slot != null) Destroy(slot.gameObject); }
        spawnedSlots.Clear();

        if (TeamFormationManager.Instance == null || TeamFormationManager.Instance.formationData == null)
        {
            Debug.LogError("[UI Team] ไม่พบข้อมูล Format Data ใน TeamFormationManager!");
            return;
        }

        var data = TeamFormationManager.Instance.formationData;

        // ตั้งค่า Grid Layout Group ให้ล็อก Column ตามขนาดทัพ
        GridLayoutGroup gridLayout = gridLayoutParent.GetComponent<GridLayoutGroup>();
        if (gridLayout != null)
        {
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = data.gridWidth;
        }

        // 2. เสกปุ่มออกมากองไว้ในตู้ให้ครบตามจำนวนช่องทั้งหมดก่อน (เช่น 5x5 = 25 ปุ่ม)
        int totalSlotsNeeded = data.gridHeight * data.gridWidth;
        List<UIFormationSlot> tempSlotsList = new List<UIFormationSlot>();

        for (int i = 0; i < totalSlotsNeeded; i++)
        {
            GameObject slotObj = Instantiate(GridSlotPrefab, gridLayoutParent);
            UIFormationSlot slotScript = slotObj.GetComponent<UIFormationSlot>();
            if (slotScript != null)
            {
                tempSlotsList.Add(slotScript);
            }
        }

        // 3. 🔥 จุดแก้ไขสำคัญ: เมื่อปุ่มเรียงกันนิ่งในตู้แล้ว ค่อยวนลูปแจกพิกัด Row/Col ที่ถูกต้องให้ทีละปุ่ม!
        int currentButtonIndex = 0;
        for (int y = 0; y < data.gridHeight; y++)
        {
            for (int x = 0; x < data.gridWidth; x++)
            {
                if (currentButtonIndex < tempSlotsList.Count)
                {
                    // แจกพิกัดแบบเรียงลำดับตามสายตาที่มองเห็นชัวร์ ๆ ห้ามเยื้อง
                    tempSlotsList[currentButtonIndex].SetupSlot(y, x, this);
                    spawnedSlots.Add(tempSlotsList[currentButtonIndex]);
                    currentButtonIndex++;
                }
            }
        }

        // 4. สั่งรีเฟรชหน้าจอโชว์สีและข้อความให้ตรงตามพิกัดจริง
        UpdateGridDisplay();
    }

    public void UpdateGridDisplay()
    {
        ArmyController army = FindAnyObjectByType<ArmyController>();

        foreach (var slot in spawnedSlots)
        {
            // 🟢 เปลี่ยนมาวิ่งหา TeamFormationManager
            var tamedAssign = TeamFormationManager.Instance.activeFormation.Find(a => a.row == slot.Row && a.col == slot.Col);

            bool isDefaultUnit = false;
            string defaultUnitName = "";
            if (army != null)
            {
                var defaultAssign = army.slotAssignments.Find(s => s.row == slot.Row && s.col == slot.Col && s.unitData != null);
                if (defaultAssign != null)
                {
                    isDefaultUnit = true;
                    defaultUnitName = defaultAssign.unitData.speciesName;
                }
            }

            slot.RefreshSlotState(tamedAssign?.unit, isDefaultUnit, defaultUnitName);
        }
    }

    public void OnAnimalCardClicked(UnitInstance selectedUnit)
    {
        GridPanel.SetActive(true);
        selectedUnitForFormation = selectedUnit;
        Debug.Log($"[UI Team] เลือกรอจัดทัพ: {selectedUnit.customName}");
    }

    public void OnGridSlotClicked(int row, int col)
    {
        if (selectedUnitForFormation != null)
        {
            // 🟢 ส่งข้อมูลเข้า TeamFormationManager
            bool success = TeamFormationManager.Instance.AssignUnitToSlotById(selectedUnitForFormation.uniqueId, row, col);
            if (success)
            {
                selectedUnitForFormation = null;
                UpdateGridDisplay();
            }
        }
        else
        {
            // ถ้ากดช่องเดิมเพื่อถอดออก
            var tamedAssign = TeamFormationManager.Instance.activeFormation.Find(a => a.row == row && a.col == col);
            if (tamedAssign != null)
            {
                TeamFormationManager.Instance.RemoveUnitFromFormationById(tamedAssign.unitUniqueId);
                UpdateGridDisplay();
            }
        }
        GridPanel.SetActive(false);
    }

    // 📄 ตัวอย่างการปรับแก้ภายในสคริปต์ UITeamManager.cs ของเพื่อนจ้า
    public void OnConfirmTeamPlacement()
    {
        // ... (โค้ดลอจิกการบันทึกข้อมูลจัดทัพเดิมของเพื่อน ปล่อยไว้เหมือนเดิม) ...

        Debug.Log("[UI Team] ⚔ ยืนยันการจัดทัพลงสู่สนามรบจริงสำเร็จ!");

        // 🟢 NEW: เสียบสายไฟปลุกระบบเสกกองทัพ 3D ให้ตื่นขึ้นมาสร้างตัวละครลงสนามทันที!
        if (ArmyController.Instance != null)
        {
            Debug.Log("[UI Team] 📡 ส่งสัญญาณสั่งให้ ArmyController สปอว์นกองทัพใหม่แล้วจ้า!");

            // สั่งเคลียร์ตัวเก่าบนสนามรบ และรันลูป InitializeArmy รอบใหม่ทันที คลื่นสัตว์เลี้ยงจะงอกออกมาคาตาเลยครับ
            ArmyController.Instance.RespawnArmyBasedOnManager();
        }
        else
        {
            Debug.LogWarning("[UI Team] ❌ หาวัตถุ ArmyController.Instance ไม่เจอในฉากก!");
        }
    }

    public void OpenCloseTeamPanel()
    {
        if (isTeamPanelOpened)
        {
            isTeamPanelOpened = false;
            TeamPanel.SetActive(false);
        }
        else
        {
            isTeamPanelOpened = true;
            TeamPanel.SetActive(true);
            RefreshAnimalInventoryUI();
        }
    }

}