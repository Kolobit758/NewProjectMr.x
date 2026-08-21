using UnityEngine;
using System;
using System.Collections.Generic;

public class BuildingUnlockManagers : MonoBehaviour
{
    public static BuildingUnlockManagers Instance { get; private set; }

    // 🟢 ใช้ตัว SO_Building asset เป็น key ตรงๆ เลย ไม่ต้องพึ่ง itemId string
    public HashSet<SO_Building> unlockedBuildings = new HashSet<SO_Building>();

    public event Action OnUnlockChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // 🟢 เรียกครั้งเดียวตอนเริ่มเกม โดยส่ง list ที่ BuildingUIManager มีอยู่แล้วเข้ามา
    public void InitializeStartingUnlocks(List<SO_Building> allBuildings)
    {
        unlockedBuildings.Clear();

        foreach (var b in allBuildings)
        {
            if (b == null) continue;
            if (b.startUnlocked || b.prerequisites == null || b.prerequisites.Length == 0)
            {
                unlockedBuildings.Add(b);
            }
        }
        OnUnlockChanged?.Invoke();
    }

    public bool IsUnlocked(SO_Building building)
    {
        if (building == null) return false;
        return unlockedBuildings.Contains(building);
    }

    public void NotifyBuildingPlaced(SO_Building placedBuilding, List<SO_Building> allBuildings)
    {
        if (placedBuilding == null) return;

        unlockedBuildings.Add(placedBuilding);

        foreach (var candidate in allBuildings)
        {
            if (candidate == null) continue;
            if (unlockedBuildings.Contains(candidate)) continue;

            if (ArePrerequisitesMet(candidate))
            {
                unlockedBuildings.Add(candidate);
                Debug.Log($"[BuildTree] 🔓 ปลดล็อค {candidate.itemName} แล้ว!");
            }
        }

        OnUnlockChanged?.Invoke();
    }

    private bool ArePrerequisitesMet(SO_Building building)
    {
        if (building.prerequisites == null || building.prerequisites.Length == 0) return true;

        foreach (var prereq in building.prerequisites)
        {
            if (prereq == null) continue;
            if (!unlockedBuildings.Contains(prereq)) return false; // 🟢 เทียบ object ตรงๆ ไม่ต้องเทียบ string
        }
        return true;
    }
}