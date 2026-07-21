using UnityEngine;
using System.Collections.Generic;

public class GhostBuilding : MonoBehaviour, ITaskable
{
    public SO_Building buildingData;
    public float totalWorkRequired = 100f;
    public float currentWorkDone = 0f;

    private HashSet<UnitBase> activeUnits = new HashSet<UnitBase>();

    void Start()
    {
        MeshRenderer[] renders = GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer render in renders)
        {
            render.material.color = new Color(0, 0.5f, 1f, 0.5f);
        }

        // 🟢 ตรวจสอบว่ามี Collider ที่เป็น Trigger หรือยัง ถ้ายังให้สร้างขึ้นมาครอบ
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(3f, 2f, 3f); // ปรับขนาดตามความเหมาะสมของตึก
        }
        else
        {
            col.isTrigger = true;
        }
    }

    void Update()
    {
        float speedPerSecond = 0f;
        foreach (var unit in activeUnits)
        {
            speedPerSecond += unit.buildSpeed;
        }

        if (speedPerSecond > 0 && activeUnits.Count > 0)
        {
            currentWorkDone += speedPerSecond * Time.deltaTime;
            Debug.Log($"🏗️ Progress: {currentWorkDone:F1}/{totalWorkRequired}");

            if (currentWorkDone >= totalWorkRequired)
            {
                FinishBuilding();
            }
        }
    }

    // 🟢 ให้ Unit เดินชน Trigger แล้วเริ่มสร้างทันที
    private void OnTriggerEnter(Collider other)
    {
        UnitBase unit = other.GetComponentInParent<UnitBase>();
        if (unit != null)
        {
            unit.agent.isStopped = true; // หยุดเดินเมื่อถึงตึก
            OnUnitInteract(unit);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        UnitBase unit = other.GetComponentInParent<UnitBase>();
        if (unit != null)
        {
            OnUnitExit(unit);
        }
    }

    public void OnUnitInteract(UnitBase unit)
    {
        if (!activeUnits.Contains(unit))
        {
            activeUnits.Add(unit);
            Debug.Log($"👷 {unit.name} เริ่มช่วยสร้างตึก!");
        }
    }

    public void OnUnitExit(UnitBase unit)
    {
        if (activeUnits.Contains(unit))
        {
            activeUnits.Remove(unit);
            Debug.Log($"🚶 {unit.name} เลิกช่วยสร้างตึก!");
        }
    }

    private void FinishBuilding()
    {
        if (buildingData != null && buildingData.Realprefab != null)
        {
            Instantiate(buildingData.Realprefab, transform.position, transform.rotation);
        }

        foreach (UnitBase unitBase in new List<UnitBase>(activeUnits))
        {
            unitBase.ResetUnitState();
        }
        activeUnits.Clear();

        Debug.Log("🎉 สร้างตึกสำเร็จ!");
        Destroy(gameObject);
    }

    public Vector3 GetInteractionPoint() => transform.position;
}