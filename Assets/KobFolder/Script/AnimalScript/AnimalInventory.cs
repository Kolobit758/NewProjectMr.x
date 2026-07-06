using System;
using System.Collections.Generic;
using UnityEngine;

public class AnimalInventory : MonoBehaviour
{
    public static AnimalInventory Instance { get; private set; }

    [Header("คลังเก็บยูนิตสำรองทั้งหมดที่ผู้เล่นมี")]
    public List<UnitInstance> inventoryUnits = new List<UnitInstance>();

    public event Action OnAnimalInventoryChanged;

    [Header("🛡️ Animal Database Setup (หัวใจหลักกู้คืนไฟล์เซฟขากลับ)")]
    // 💡 ทริคสำคัญ: ให้เพื่อนลากไฟล์ ScriptableObject (UnitDataSO) ของสัตว์เลี้ยงทุกตัวในเกม (เช่น SO_Wolf, SO_Ferret) 
    // มาใส่ไว้ในช่อง List Database นี้ในหน้าต่าง Inspector ให้ครบถ้วนนะจ๊ะ!
    public List<UnitDataSO> animalDatabase = new List<UnitDataSO>();

    public List<UnitInstance> tamedUnits
    {
        get { return inventoryUnits; }
        set { inventoryUnits = value; }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (SaveLoadManager.Instance != null)
        {
            SaveLoadManager.Instance.TryResolvePendingSaveData();
        }
    }

    public void AddUnitToInventory(UnitInstance newUnit)
    {
        if (newUnit == null || string.IsNullOrEmpty(newUnit.uniqueId)) return;
        
        bool isDuplicate = inventoryUnits.Exists(u => u != null && u.uniqueId.Trim() == newUnit.uniqueId.Trim());
        if (isDuplicate) return;

        inventoryUnits.Add(newUnit);
        OnAnimalInventoryChanged?.Invoke();
    }

    public bool RemoveUnitFromInventory(string uniqueId)
    {
        if (string.IsNullOrEmpty(uniqueId)) return false;
        string targetId = uniqueId.Trim();

        int removedCount = inventoryUnits.RemoveAll(u => 
            u == null || 
            string.IsNullOrEmpty(u.uniqueId) || 
            u.uniqueId.Trim().Equals(targetId, StringComparison.OrdinalIgnoreCase)
        );
        
        if (removedCount > 0)
        {
            OnAnimalInventoryChanged?.Invoke();
            return true;
        }
        return false;
    }

    #region 🟢 NEW: ระบบสะพานเชื่อมฟังก์ชันเซฟโหลดผ่านรหัสสตริง (String Binding Bridge)

    // 📥 1. แปลงข้อมูลส่งออก: ส่งลิสต์ไอดีต้นแบบ (Species ID หรือ Asset Name) ไปให้ระบบเซฟหลักจดจำ
    public List<string> GetSaveTemplateIds()
    {
        List<string> templateIds = new List<string>();
        foreach (var unit in inventoryUnits)
        {
            // ใช้ชื่อไฟล์ SO หรือ String ID ประจำสายพันธุ์ดักไว้
            templateIds.Add(unit == null || unit.template == null ? "" : unit.template.name);
        }
        return templateIds;
    }

    // 📥 2. แปลงข้อมูลส่งออก: ส่งรายชื่อ ID เฉพาะตัว (UniqueId) ของสัตว์แต่ละตัวไปบันทึก
    public List<string> GetSaveUniqueIds()
    {
        List<string> uIds = new List<string>();
        foreach (var unit in inventoryUnits) uIds.Add(unit == null ? "" : unit.uniqueId);
        return uIds;
    }

    // 📥 3. แปลงข้อมูลส่งออก: ส่งรายชื่อเล่น (CustomName) ไปบันทึก
    public List<string> GetSaveCustomNames()
    {
        List<string> names = new List<string>();
        foreach (var unit in inventoryUnits) names.Add(unit == null ? "" : unit.customName);
        return names;
    }

    // 📥 4. แปลงข้อมูลส่งออก: ส่งพลังชีวิตล่าสุดไปบันทึกป้องกันเลือดเด้งเต็ม
    public List<int> GetSaveCurrentHPs()
    {
        List<int> hps = new List<int>();
        foreach (var unit in inventoryUnits) hps.Add(unit == null ? 0 : unit.currentHP);
        return hps;
    }

    // 🔄 5. ฟังก์ชันรับขากลับมหาประลัย: งัดอ่านฐานข้อมูลกู้ชีพสัตว์เลี้ยงข้ามมิติโปรแกรมปิดเปิดใหม่
    public void LoadSavedAnimalSlots(List<string> templateNames, List<string> uniqueIds, List<string> customNames, List<int> currentHPs)
    {
        if (templateNames == null || uniqueIds == null) return;

        inventoryUnits.Clear(); // เคลียร์ตู้คลังเดิมชั่วคราวเพื่อเทสีข้อมูลเซฟใหม่

        for (int i = 0; i < templateNames.Count; i++)
        {
            if (string.IsNullOrEmpty(templateNames[i]) || i >= uniqueIds.Count) continue;

            // 🔍 ลุยค้นหาวัตถุ ScriptableObject ตัวแม่จากตู้ Database ของเรา!
            UnitDataSO foundTemplate = FindTemplateFromDatabase(templateNames[i]);

            if (foundTemplate != null)
            {
                // 🏗️ กู้คืนอินสแตนซ์ข้อมูลแรมตัวใหม่ ยัด SO ตัวแม่สวมหัวเข้าไปดับบั๊ก CS7036 ทันที!
                UnitInstance restoredUnit = new UnitInstance(foundTemplate);
                
                // คืนชีพพาสปอร์ตประจำตัวดั้งเดิม
                restoredUnit.uniqueId = uniqueIds[i];
                restoredUnit.customName = i < customNames.Count ? customNames[i] : foundTemplate.speciesName;
                restoredUnit.currentHP = i < currentHPs.Count ? currentHPs[i] : foundTemplate.baseMaxHP;
                restoredUnit.maxHP = foundTemplate.baseMaxHP;
                restoredUnit.moveSpeed = foundTemplate.baseMoveSpeed;
                restoredUnit.attackDamage = foundTemplate.baseAttackDamage;
                restoredUnit.attackRange = 1.5f; 
                restoredUnit.guardRadius = 5f;

                inventoryUnits.Add(restoredUnit);
            }
        }

        OnAnimalInventoryChanged?.Invoke(); // ปลุกระบบ UI กองทัพและหน้าต่างคลังสัตว์เลี้ยงให้ตื่นมารับทราบ
    }

    // 🔍 6. ฟังก์ชันตามล่าหาแก่นแท้ SO สัตว์จากชื่อสตริง
    private UnitDataSO FindTemplateFromDatabase(string templateName)
    {
        if (string.IsNullOrEmpty(templateName)) return null;

        // ขั้นที่ 1: ค้นหาในตู้ลิสต์ดักจับ Inspector ที่เราลากวางไว้ชัวร์สุด 100%
        if (animalDatabase != null)
        {
            foreach (var dbAsset in animalDatabase)
            {
                if (dbAsset != null && dbAsset.name.Trim().Equals(templateName.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return dbAsset;
                }
            }
        }

        // ขั้นที่ 2: ระบบ Fallback โหลดอัตโนมัติจากตู้ Resources ในโปรเจกต์กรณีลืมลากสายไฟ
        UnitDataSO autoLoad = Resources.Load<UnitDataSO>($"Animals/{templateName}");
        if (autoLoad != null) return autoLoad;

        // ขั้นที่ 3: กวาดล้างหาขอบเขต Resources ทั้งระบบฉุกเฉิน
        UnitDataSO[] allLoaded = Resources.LoadAll<UnitDataSO>("");
        foreach (var asset in allLoaded)
        {
            if (asset != null && asset.name.Trim().Equals(templateName.Trim(), StringComparison.OrdinalIgnoreCase)) return asset;
        }

        Debug.LogError($"[AnimalInventory] ❌ ทัพล่มขากลับ! ไม่เจอไฟล์ ScriptableObject สัตว์เลี้ยงที่ชื่อ '{templateName}' ในระบบคลังสำรอง Database เลยเพื่อน!");
        return null;
    }
    #endregion
}