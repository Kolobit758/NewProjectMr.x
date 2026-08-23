using UnityEngine;
using System;
using System.Collections.Generic;

public class BuildingUnlockManagers : MonoBehaviour
{
    public static BuildingUnlockManagers Instance { get; private set; }

    // 🟢 เก็บรายการตึกที่ปลดล็อคแล้วใน UI Menu
    public HashSet<SO_Building> unlockedBuildings = new HashSet<SO_Building>();
    
    // 🟢 เก็บรายการตึกที่สร้างเสร็จจริงแล้วบนแมพ (ไว้ใช้เช็ค prerequisite)
    public HashSet<SO_Building> placedBuildings = new HashSet<SO_Building>();

    private List<SO_Building> cachedAllBuildings = new List<SO_Building>();

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
        placedBuildings.Clear();
        cachedAllBuildings = allBuildings != null ? new List<SO_Building>(allBuildings) : new List<SO_Building>();

        foreach (var b in cachedAllBuildings)
        {
            if (b == null) continue;
            // 🟢 ปลดล็อคตอนเริ่มเกมเฉพาะตึกที่เป็น starter (startUnlocked == true) หรือไม่มี prerequisite
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

    public bool IsPlaced(SO_Building building)
    {
        if (building == null) return false;
        return placedBuildings.Contains(building);
    }

    // 🟢 เรียกเมื่อ "สร้างตึกจริงเสร็จสมบูรณ์" ( FinishBuilding ใน GhostBuilding )
    public void NotifyBuildingCompleted(SO_Building completedBuilding, List<SO_Building> allBuildings = null)
    {
        if (completedBuilding == null) return;

        if (allBuildings != null && allBuildings.Count > 0)
        {
            cachedAllBuildings = allBuildings;
        }

        // เพิ่มตึกนี้เข้าไปในรายการตึกที่สร้างเสร็จแล้วบนแมพ
        placedBuildings.Add(completedBuilding);
        unlockedBuildings.Add(completedBuilding); // ตึกที่สร้างเสร็จแล้วย่อมถือว่าปลดล็อคแล้วด้วย

        bool anyNewUnlocked = false;

        // วนลูปเช็คตึกทั้งหมดว่ามีตึกไหนปลดล็อคเพิ่มไหม
        foreach (var candidate in cachedAllBuildings)
        {
            if (candidate == null) continue;
            if (unlockedBuildings.Contains(candidate)) continue; // ปลดล็อคอยู่แล้ว ข้ามไป

            if (ArePrerequisitesMet(candidate))
            {
                unlockedBuildings.Add(candidate);
                anyNewUnlocked = true;
                Debug.Log($"[BuildTree] 🔓 ปลดล็อค {candidate.itemName} แล้ว!");
            }
        }

        if (anyNewUnlocked)
        {
            OnUnlockChanged?.Invoke();
        }
    }

    // เพื่อรองรับโค้ดเก่า หากมีที่ไหนเรียก NotifyBuildingPlaced
    public void NotifyBuildingPlaced(SO_Building placedBuilding, List<SO_Building> allBuildings = null)
    {
        NotifyBuildingCompleted(placedBuilding, allBuildings);
    }

    private bool ArePrerequisitesMet(SO_Building building)
    {
        if (building.prerequisites == null || building.prerequisites.Length == 0) return true;

        foreach (var prereq in building.prerequisites)
        {
            if (prereq == null) continue;
            // 🟢 เงื่อนไข: ต้องสร้างตึกที่เป็น prerequisite บนแมพเสร็จแล้วจริงๆ (อยู่ใน placedBuildings)
            if (!placedBuildings.Contains(prereq)) return false;
        }
        return true;
    }
}