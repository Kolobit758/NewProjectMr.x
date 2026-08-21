using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ResourceNodeBase : MonoBehaviour, ITaskable
{
    public List<UnitBase> units = new List<UnitBase>();
    public int minPowerToGathering = 0;
    public SO_ItemData resourceToProduce;
    public int amountResource;
    public bool canGathering = true;
    
    protected bool isLogisticStarted = false;


    public bool isGruopTask = false;

    public virtual void OnUnitInteract(UnitBase unit)
    {
        // 🔒 เพิ่ม unit เข้า list แบบ safe (ไม่ซ้ำ, ไม่ null)
        if (!units.Contains(unit))
        {
            units.Add(unit);
        }
        units.RemoveAll(u => u == null);

        // 🔒 ถ้าเป็น GroupTask และ logistic เริ่มแล้ว ไม่ต้อง StartLogistic ซ้ำ
        // แต่ต้อง Add unit เข้า list ก่อนแล้วค่อย return
        if (isGruopTask && isLogisticStarted) return;

        if (CalculateGatheringPower() >= minPowerToGathering)
        {
            isLogisticStarted = true;
            StartLogistic();
        }
    }

    public virtual void OnUnitExit(UnitBase unit) { }

    // ฟังก์ชันนี้ให้ Subclass ไปOverride เขียนพฤติกรรมเฉพาะตัว (เช่น ต้นไม้ลากซุง, น้ำเสกของ)
    protected abstract void StartLogistic();

    public abstract void OnUnitSentResourced();

    protected Transform FindNearestVault()
    {
        GameObject[] vaults = GameObject.FindGameObjectsWithTag("Vault");
        Transform nearest = null;
        float minDistance = Mathf.Infinity;

        foreach (GameObject v in vaults)
        {
            float dist = Vector3.Distance(transform.position, v.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                nearest = v.transform;
            }
        }
        return nearest;
    }

    protected int CalculateGatheringPower()
    {
        int gatPower = 0;
        foreach (UnitBase unit in units)
        {
            if (unit != null) gatPower += unit.gatheringPower;
        }
        return gatPower;
    }

    public Vector3 GetInteractionPoint() => transform.position;
    public Vector3 GetGatheringAreaCenter() => transform.position;
}