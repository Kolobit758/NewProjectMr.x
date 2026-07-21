using UnityEngine;
using System.Collections.Generic;

public class JobManager : MonoBehaviour
{
    public static JobManager Instance { get; private set; }
    
    private List<JobBase> activeJobs = new List<JobBase>();

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    public void RegisterJob(JobBase job)
    {
        if (!activeJobs.Contains(job)) activeJobs.Add(job);
    }

    public void UnregisterJob(JobBase job)
    {
        if (activeJobs.Contains(job)) activeJobs.Remove(job);
    }

    /// <summary>ยกเลิก Job ทั้งหมดของยูนิต เมื่อยูนิตได้รับคำสั่งใหม่ (Move, Attack, ฯลฯ)</summary>
    public void CancelAllJobsForUnit(UnitBase unit)
    {
        foreach (var job in activeJobs)
        {
            if (job.assignedUnits.Contains(unit))
            {
                job.UnassignUnit(unit);
            }
        }
        unit.ResetUnitState();
    }
}