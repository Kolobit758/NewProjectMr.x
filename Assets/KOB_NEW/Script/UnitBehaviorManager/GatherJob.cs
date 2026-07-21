using UnityEngine;

public class GatherJob : JobBase
{
    public GatheringBase resourceNode;
    public bool repeat = true;
    private Transform targetVault;

    public void Init(GatheringBase node, Transform vault)
    {
        jobType = JobType.Gather;
        resourceNode = node;
        targetVault = vault;
        JobManager.Instance.RegisterJob(this);
    }

    protected override void OnUnitAssigned(UnitBase unit)
    {
        if (resourceNode != null)
        {
            unit.AssignGatherTask(resourceNode, targetVault, this);
        }
    }

    protected override void OnUnitUnassigned(UnitBase unit)
    {
        // จัดการเมื่อยูนิตออกจากงานนี้
    }

    public override void EvaluateJob()
    {
        if (resourceNode == null || resourceNode.IsDepleted())
        {
            CancelJob();
        }
    }

    public override void CancelJob()
    {
        foreach (var unit in assignedUnits.ToArray())
        {
            unit.ResetUnitState();
        }
        JobManager.Instance.UnregisterJob(this);
        Destroy(gameObject);
    }
}