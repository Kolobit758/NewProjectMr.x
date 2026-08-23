using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ResourceNodeBase : MonoBehaviour, ITaskable
{
    public List<UnitBase> units = new List<UnitBase>();
    public int minPowerToGathering = 0;
    public SO_ItemData resourceToProduce;
    public int amountResource = 10;
    public bool canGathering = true;
    
    protected bool isLogisticStarted = false;
    public bool isGruopTask = false;

    public virtual void OnUnitInteract(UnitBase unit)
    {
        if (unit == null) return;

        // 🔒 หากทรัพยากรตรงนี้อยู่ในสถานะถูกตัดไปแล้ว/หมดอยู่ (canGathering == false)
        // ให้ยูนิตค้นหาแหล่งทรัพยากรชนิดเดียวกันในบริเวณใกล้เคียงแทนทันที
        if (!canGathering)
        {
            if (unit.TryFindNearbyResourceOfSameType(resourceToProduce, transform.position, out ResourceNodeBase nearbyNode))
            {
                unit.CommandGather(nearbyNode);
            }
            else
            {
                unit.ResetUnitState();
            }
            return;
        }

        // 🟢 ให้ยูนิตแต่ละตัวเริ่มกระบวนการเก็บทรัพยากรรายบุคคลทันที
        StartLogisticForUnit(unit);
    }

    public virtual void OnUnitExit(UnitBase unit) { }

    // ฟังก์ชันนี้ให้ Subclass ไป Override เขียนพฤติกรรมเฉพาะตัวของแต่ละยูนิต
    protected abstract void StartLogisticForUnit(UnitBase unit);

    protected virtual void StartLogistic() { }

    public virtual void OnUnitSentResourced() { }

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