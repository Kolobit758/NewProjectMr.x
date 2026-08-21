using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class TeamFormationManager : MonoBehaviour
{
    public static TeamFormationManager Instance { get; private set; }

    [Header("Formation Template Data")]
    public CustomFormationData formationData; // ไฟล์ ScriptableObject ของขบวนทัพ

    [System.Serializable]
    public class ActiveSlotAssignment
    {
        public int row;
        public int col;
        public string unitUniqueId;
        [System.NonSerialized] public UnitInstance unit;
    }

    [Header("Active Formation Assignments (สัตว์ที่ถูกจัดลงทีมจริง)")]
    public List<ActiveSlotAssignment> activeFormation = new List<ActiveSlotAssignment>();

    private string SavePath => Path.Combine(Application.persistentDataPath, "team_formation_save.json");

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        ReconstructFormationReferences();
    }

    /// <summary>
    /// สั่งเอาสัตว์จากคลังยัดลงพิกัดช่องขบวนทัพ
    /// </summary>
    public bool AssignUnitToSlotById(string uniqueId, int targetRow, int targetCol)
    {
        if (formationData == null) return false;

        // เช็คว่าช่องนี้เปิดใช้งานในขบวนทัพไหม
        if (!IsValidFormationSlot(targetRow, targetCol)) return false;

        // ค้นหาสัตว์จากกระเป๋า AnimalInventory ของเพื่อน
        UnitInstance unitToAssign = AnimalInventory.Instance.inventoryUnits.Find(u => u.uniqueId == uniqueId);
        if (unitToAssign == null) return false;

        // ถ้าสัตว์ตัวนี้เคยจัดลงช่องอื่นอยู่แล้ว ให้ถอดออกจากช่องเก่าก่อน
        RemoveUnitFromFormationById(uniqueId);
        
        // เคลียร์ยูนิตเก่าที่เคยอยู่ช่องเป้าหมายนี้ออก (ถ้ามี)
        activeFormation.RemoveAll(a => a.row == targetRow && a.col == targetCol);

        // บันทึกตำแหน่งใหม่
        ActiveSlotAssignment newAssignment = new ActiveSlotAssignment
        {
            row = targetRow,
            col = targetCol,
            unitUniqueId = uniqueId,
            unit = unitToAssign
        };
        activeFormation.Add(newAssignment);

        // อัปเดตสถานะในตัว Instance
        unitToAssign.isInFormation = true;
        unitToAssign.formationSlotKey = $"{targetRow}:{targetCol}";

        SaveFormationData();
        return true;
    }

    /// <summary>
    /// ถอดสัตว์ตัวนี้ออกจากขบวนทัพ
    /// </summary>
    public void RemoveUnitFromFormationById(string uniqueId)
    {
        int removedCount = activeFormation.RemoveAll(a => a.unitUniqueId == uniqueId);
        if (removedCount > 0)
        {
            UnitInstance unitToUpdate = AnimalInventory.Instance.inventoryUnits.Find(u => u != null && u.uniqueId == uniqueId);
            if (unitToUpdate != null)
            {
                unitToUpdate.isInFormation = false;
                unitToUpdate.formationSlotKey = string.Empty;
            }
            SaveFormationData();
        }
    }

    /// <summary>
    /// ตรวจสอบว่าช่องพิกัด Grid นี้เปิดใช้งานใน Formation หรือไม่
    /// </summary>
    public bool IsValidFormationSlot(int row, int col)
    {
        if (formationData == null) return false;
        if (row < 0 || col < 0 || row >= formationData.gridHeight || col >= formationData.gridWidth) return false;
        if (row >= formationData.rows.Length) return false;
        if (col >= formationData.rows[row].cols.Length) return false;
        return formationData.rows[row].cols[col] == true;
    }

    // ================= [ ระบบ SAVE / LOAD การจัดทัพ ] =================
    [System.Serializable]
    private class SaveFormationWrapper
    {
        public List<ActiveSlotAssignment> savedActiveFormation;
    }

    public void SaveFormationData()
    {
        try
        {
            SaveFormationWrapper wrapper = new SaveFormationWrapper { savedActiveFormation = this.activeFormation };
            File.WriteAllText(SavePath, JsonUtility.ToJson(wrapper, true));
        }
        catch (System.Exception e) { Debug.LogError($"[Save Formation Error] {e.Message}"); }
    }

    public void LoadFormationData()
    {
        if (!File.Exists(SavePath)) return;
        try
        {
            SaveFormationWrapper wrapper = JsonUtility.FromJson<SaveFormationWrapper>(File.ReadAllText(SavePath));
            if (wrapper != null)
            {
                this.activeFormation = wrapper.savedActiveFormation;
                ReconstructFormationReferences();
            }
        }
        catch (System.Exception e) { Debug.LogError($"[Load Formation Error] {e.Message}"); }
    }

    private void ReconstructFormationReferences()
    {
        if (AnimalInventory.Instance == null) return;
        foreach (var assignment in activeFormation)
        {
            if (!string.IsNullOrEmpty(assignment.unitUniqueId))
                assignment.unit = AnimalInventory.Instance.inventoryUnits.Find(u => u.uniqueId == assignment.unitUniqueId);
        }
    }
}