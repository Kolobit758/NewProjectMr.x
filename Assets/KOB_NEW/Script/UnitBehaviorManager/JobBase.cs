using UnityEngine;
using System.Collections.Generic;

public enum JobType { Gather, Farm, Build, Combat, Move }

public abstract class TaskBase
{
    public enum TaskState { Pending, Running, Completed, Failed }
    public TaskState state = TaskState.Pending;

    public abstract void OnStart(UnitBase unit);
    public abstract bool OnUpdate(UnitBase unit);
    public abstract void OnAbort(UnitBase unit);
}

public abstract class JobBase : MonoBehaviour
{
    public JobType jobType;
    public List<UnitBase> assignedUnits = new List<UnitBase>();
    public bool isCompleted = false;

    public virtual void AssignUnit(UnitBase unit)
    {
        if (!assignedUnits.Contains(unit))
        {
            assignedUnits.Add(unit);
            OnUnitAssigned(unit);
        }
    }

    public virtual void UnassignUnit(UnitBase unit)
    {
        if (assignedUnits.Contains(unit))
        {
            assignedUnits.Remove(unit);
            OnUnitUnassigned(unit);
        }
    }

    protected abstract void OnUnitAssigned(UnitBase unit);
    protected abstract void OnUnitUnassigned(UnitBase unit);
    public abstract void EvaluateJob();
    public abstract void CancelJob();
}