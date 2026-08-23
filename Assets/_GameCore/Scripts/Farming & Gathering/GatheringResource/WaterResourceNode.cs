using UnityEngine;

public class WaterResourceNode : ResourceNodeBase
{
    // แม่น้ำเก็บได้เรื่อยๆ ไม่ต้องปิดตัว
    void Start()
    {
        canGathering = true;
    }

    protected override void StartLogisticForUnit(UnitBase unit)
    {
        if (unit == null) return;
        Transform nearestVault = FindNearestVault();
        if (nearestVault == null) return;

        unit.gatBase = this;
        unit.carriedItem = resourceToProduce;
        unit.carriedAmount = amountResource;

        // สั่งให้ยูนิตตักน้ำและเดินไปส่งที่ Vault โดยตรง
        unit.StartDragging(null, nearestVault, 10f);
    }
}