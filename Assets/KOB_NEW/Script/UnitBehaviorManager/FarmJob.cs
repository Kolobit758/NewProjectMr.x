using UnityEngine;
using System.Collections.Generic;

public class FarmJob : JobBase
{
    private List<CropPlots> managedPlots = new List<CropPlots>();
    private Dictionary<UnitBase, CropPlots> unitAssignedPlots = new Dictionary<UnitBase, CropPlots>();

    public void Init(List<CropPlots> plots)
    {
        jobType = JobType.Farm;
        managedPlots = plots;
        JobManager.Instance.RegisterJob(this);
    }

    protected override void OnUnitAssigned(UnitBase unit)
    {
        AssignNextTaskToUnit(unit);
    }

    protected override void OnUnitUnassigned(UnitBase unit)
    {
        if (unitAssignedPlots.ContainsKey(unit))
        {
            if (unitAssignedPlots[unit] != null)
                unitAssignedPlots[unit].isBeingServiced = false;
            unitAssignedPlots.Remove(unit);
        }
    }

    public void AssignNextTaskToUnit(UnitBase unit)
    {
        if (!assignedUnits.Contains(unit)) return;

        if (unitAssignedPlots.ContainsKey(unit) && unitAssignedPlots[unit] != null)
        {
            unitAssignedPlots[unit].isBeingServiced = false;
            unitAssignedPlots[unit] = null;
        }

        // ค้นหาแปลงที่ต้องดูแล (ไม่ว่าจะเป็นแปลงที่ต้องใส่ปุ๋ย รดน้ำ หรือเก็บเกี่ยว)
        CropPlots nextPlot = null;
        foreach (var plot in managedPlots)
        {
            if (plot == null) continue;
            if (plot.currentStage == CropStage.Empty) continue; // แปลงว่างข้ามไปก่อน
            if (!plot.isBeingServiced)
            {
                nextPlot = plot;
                break;
            }
        }

        if (nextPlot != null)
        {
            nextPlot.isBeingServiced = true;
            unitAssignedPlots[unit] = nextPlot;
            unit.AssignFarmTask(nextPlot, this);
        }
        else
        {
            // ถ้าไม่มีงาน ให้เดิน Patrol รอบๆ โซนฟาร์ม
            unit.StartPatrol();
        }
    }

    public override void EvaluateJob() { }

    public override void CancelJob()
    {
        foreach (var unit in assignedUnits.ToArray())
        {
            UnassignUnit(unit);
            unit.ResetUnitState();
        }
        JobManager.Instance.UnregisterJob(this);
        Destroy(gameObject);
    }
}