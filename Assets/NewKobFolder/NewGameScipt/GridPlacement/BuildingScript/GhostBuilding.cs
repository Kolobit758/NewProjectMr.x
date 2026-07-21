using UnityEngine;
using System.Collections.Generic;

public class GhostBuilding : MonoBehaviour, ITaskable
{
    public SO_Building buildingData;
    public float totalWorkRequired = 100f; // ต้องใช้พลังงานเท่าไหร่ถึงสร้างเสร็จ
    public float currentWorkDone = 0f;

    // เก็บรายการยูนิตที่กำลังทำงานอยู่
    private HashSet<UnitBase> activeUnits = new HashSet<UnitBase>();

    void Start()
    {
        MeshRenderer[] renders = GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer render in renders)
        {
            render.material.color = new Color(0, 0.5f, 1f, 0.5f);
        }

    }
    void Update()
    {
        // คำนวณความเร็วรวมจากยูนิตที่กำลังทำงานอยู่
        float speedPerSecond = 0f;
        foreach (var unit in activeUnits)
        {
            // สมมติมึงมีค่า buildSpeed ใน UnitBase หรือ StatsManager
            speedPerSecond += unit.buildSpeed;
        }

        if (speedPerSecond > 0)
        {
            currentWorkDone += speedPerSecond * Time.deltaTime;
            Debug.Log($"Progress: {currentWorkDone}/{totalWorkRequired}");

            if (currentWorkDone >= totalWorkRequired)
            {
                FinishBuilding();
            }
        }
    }

    public void OnUnitInteract(UnitBase unit)
    {
        if (!activeUnits.Contains(unit))
        {
            activeUnits.Add(unit);
            Debug.Log($"{unit.name} เริ่มช่วยสร้าง!");
        }
    }

    // 🟢 เพิ่มฟังก์ชันนี้เพื่อจัดการตอนยูนิตเดินออกไป
    public void OnUnitExit(UnitBase unit)
    {
        if (activeUnits.Contains(unit))
        {
            activeUnits.Remove(unit);
            Debug.Log($"{unit.name} เลิกช่วยสร้างแล้ว!");
        }
    }

    private void FinishBuilding()
    {
        foreach (ResourceCost cost in buildingData.requiredResources)
        {
            ResourceInventory.Instance.ConsumeResource(cost.item, cost.amount);
        }
        // 🟢 ต้อง snapshot ก่อนวน เพราะ ResetUnitState() -> AbandonCurrentOrder()
        // จะย้อนมาเรียก OnUnitExit(unit) ซึ่งไป activeUnits.Remove(unit)
        // ถ้าวนบน activeUnits ตรงๆ จะโดน "Collection was modified" ทันที
        foreach (UnitBase unitBase in new List<UnitBase>(activeUnits))
        {
            unitBase.ResetUnitState();
        }
        activeUnits.Clear();

        Debug.Log("วางตึกสำเร็จ หักทรัพยากรเรียบร้อย!");

        Instantiate(buildingData.Realprefab, transform.position, transform.rotation);
        Destroy(gameObject);
    }

    public Vector3 GetInteractionPoint() => transform.position;
}