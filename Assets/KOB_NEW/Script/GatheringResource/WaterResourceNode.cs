using UnityEngine;

public class WaterResourceNode : ResourceNodeBase
{
    // แม่น้ำเก็บได้เรื่อยๆ ไม่ต้องปิดตัว
    void Start()
    {
        canGathering = true;
    }

    protected override void StartLogistic()
    {
        Transform nearestVault = FindNearestVault();
        if (nearestVault == null) return;

        // สำหรับน้ำ: ไม่ต้องสร้างซุงลาก ให้ยูนิตเดินตัวเปล่าหรือถือถังน้ำไปส่งที่ Vault เลย
        foreach (var unit in units)
        {
            if (unit == null) continue;
            unit.gatBase = this;
            unit.carriedItem = resourceToProduce;
            unit.carriedAmount = amountResource;
            
            // สั่งให้เดินไปส่งที่ Vault โดยตรง (อาจจะใช้ฟังก์ชันการเดินปกติ หรือประยุกต์ StartDragging แบบไม่ใช้ Visual Target)
            unit.StartDragging(null, nearestVault, 10f); 
        }
    }

    public override void OnUnitSentResourced()
    {
        // เมื่อส่งน้ำถึง Vault เรียบร้อย ปลดปล่อยยูนิตและเปิดรับงานใหม่ทันทีโดยไม่ต้องทำลาย Object แม่น้ำ
        foreach (UnitBase unit in units)
        {
            if (unit != null)
            {
                unit.ResetUnitState();
            }
        }

        units.Clear();
        isLogisticStarted = false;
    }
}