using System.Collections.Generic;
using UnityEngine;

public class BuildingUnlockManager : MonoBehaviour
{
    public static BuildingUnlockManager Instance { get; private set; }

    [Header("Active Building Counters")]
    private int autoGatherBuildingCount = 0;

    public bool isAutoGatherUnlocked => autoGatherBuildingCount > 0;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    // เรียกตอนตึกถูกสร้างเสร็จ
    public void RegisterBuilding(ResearchBuilding building)
    {
        if (building.unlocksAutoGather)
        {
            autoGatherBuildingCount++;
            if (autoGatherBuildingCount == 1) // เพิ่งมีตึกแรกที่เปิดใช้
            {
                SetAutoGatherState(true);
            }
        }
    }

    // เรียกตอนตึกถูกทำลาย (OnDestroy)
    public void UnregisterBuilding(ResearchBuilding building)
    {
        if (building.unlocksAutoGather)
        {
            autoGatherBuildingCount = Mathf.Max(0, autoGatherBuildingCount - 1);
            if (autoGatherBuildingCount == 0) // ตึกหมดเกลี้ยงแล้ว!
            {
                SetAutoGatherState(false);
            }
        }
    }

    private void SetAutoGatherState(bool active)
    {
        foreach (var unit in RTS_movement.instance.allUnits)
        {
            if (unit != null) unit.SetAutoGatherUpgrade(active);
        }
        Debug.Log(active ? "🔓 ปลดล็อกสกิล: เก็บทรัพยากรซ้ำอัตโนมัติ (Auto-Gather)" : "🔒 ปิดใช้งานสกิล: เก็บทรัพยากรซ้ำอัตโนมัติ (ตึกถูกทำลายหมด)");
    }

    // ฟังก์ชันให้ยูนิตเรียกตอนเกิดใหม่ (Spawn)
    public void ApplyUnlocksToNewUnit(UnitBase newUnit)
    {
        if (newUnit == null) return;
        newUnit.SetAutoGatherUpgrade(isAutoGatherUnlocked);
    }
}