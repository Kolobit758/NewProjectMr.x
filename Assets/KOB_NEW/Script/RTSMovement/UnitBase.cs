using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public enum UnitBehavior
{
    Idle,
    Moving,
    Interacting,
    CarryingToVault,
    FetchingItem,
    Attacking,
    Patrolling
}

public class UnitBase : MonoBehaviour
{
    public GameObject selectionCircle;
    public NavMeshAgent agent;
    private Animator anim;
    public bool isSelected;
    public float buildSpeed = 10f;

    [Header("State Machine")]
    public UnitBehavior currentState = UnitBehavior.Idle;

    private JobBase currentJob;
    private ITaskable currentOrder;

    [Header("Logistics & Gathering")]
    public int gatheringPower = 10;
    public bool isCarrying;
    public LineRenderer rope;
    public SO_ItemData carriedItem;
    public int carriedAmount;
    private Transform draggedVisualTarget;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        RunStateMachine();
        UpdateRopeVisual();
    }

    private void RunStateMachine()
    {
        switch (currentState)
        {
            case UnitBehavior.Moving:
                // 🟢 เช็คแบบชัวร์ 100%: ใช้ระยะห่างจริงระหว่างตัวยูนิตกับจุด Interaction โดยตรง
                if (currentOrder != null)
                {
                    float distanceToTarget = Vector3.Distance(transform.position, currentOrder.GetInteractionPoint());

                    // ถ้าเดินเข้ามาใกล้ในระยะ 1.5 เมตร (หรือปรับให้เหมาะกับขนาดโมเดล) ถือว่าถึงแล้ว
                    if (distanceToTarget <= 1.5f || (!agent.pathPending && !agent.hasPath && agent.remainingDistance <= 0.5f))
                    {
                        ChangeState(UnitBehavior.Interacting);

                        agent.isStopped = true;
                        agent.ResetPath();

                        Debug.Log($"🎯 {name} เดินถึงเป้าหมายแล้ว กำลังทำภารกิจ!");
                        currentOrder.OnUnitInteract(this);
                    }
                }
                else
                {
                    ChangeState(UnitBehavior.Idle);
                }
                break;

            case UnitBehavior.Patrolling:
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                {
                    if (currentJob is FarmJob farmJob)
                    {
                        farmJob.AssignNextTaskToUnit(this);
                    }
                }
                break;
        }
    }


    private void ChangeState(UnitBehavior next) => currentState = next;

    // =====================================================================
    // Command Interface (รับคำสั่งจาก Manager เท่านั้น)
    // =====================================================================
    public void CommandMoveTo(Vector3 position)
    {
        JobManager.Instance.CancelAllJobsForUnit(this);
        agent.isStopped = false;
        agent.SetDestination(position);
        ChangeState(UnitBehavior.Moving);
    }

    public void AssignGatherTask(GatheringBase node, Transform vault, JobBase job)
    {
        currentJob = job;
        currentOrder = node;
        agent.isStopped = false;
        agent.SetDestination(node.GetInteractionPoint());
        ChangeState(UnitBehavior.Moving);
    }

    public void AssignFarmTask(CropPlots plot, JobBase job)
    {
        currentJob = job;
        currentOrder = plot;
        agent.isStopped = false;
        agent.SetDestination(plot.GetInteractionPoint());
        ChangeState(UnitBehavior.Moving);
    }

    public void StartPatrol()
    {
        agent.isStopped = false;
        Vector3 randomDir = Random.insideUnitSphere * 8f + transform.position;
        if (NavMesh.SamplePosition(randomDir, out NavMeshHit hit, 8f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        ChangeState(UnitBehavior.Patrolling);
    }

    public void StartDragging(Transform targetNode, Transform vault)
    {
        agent.isStopped = false;
        isCarrying = true;
        draggedVisualTarget = targetNode;
        if (rope != null) rope.enabled = true;
        agent.SetDestination(vault.position);
        ChangeState(UnitBehavior.CarryingToVault);
        StartCoroutine(CheckArrivalAtVault(vault));
    }

    IEnumerator CheckArrivalAtVault(Transform vault)
    {
        while (agent.pathPending || agent.remainingDistance > agent.stoppingDistance) yield return null;
        ResourceVault v = vault.GetComponent<ResourceVault>();
        if (v != null) v.OnUnitInteract(this);
    }

    public void DropItemAtVault()
    {
        if (draggedVisualTarget != null)
        {
            GatheringBase gatBase = draggedVisualTarget.GetComponent<GatheringBase>();
            if (gatBase != null)
            {
                gatBase.OnUnitSentResourced();
                return;
            }
        }
        ResetUnitState();
    }

    public void RequestFetchItem(SO_ItemData itemData, int amount = 1)
    {
        Transform vault = FindNearestVault();
        if (vault == null) return;
        agent.isStopped = false;
        agent.SetDestination(vault.position);
        ChangeState(UnitBehavior.FetchingItem);
        StartCoroutine(FetchItemRoutine(vault, itemData, amount));
    }

    IEnumerator FetchItemRoutine(Transform vault, SO_ItemData itemData, int amount)
    {
        while (agent.pathPending || agent.remainingDistance > agent.stoppingDistance) yield return null;

        if (ResourceInventory.Instance.HasResource(itemData.itemName, amount))
        {
            carriedItem = ResourceInventory.Instance.ConsumeAndGetReturn(itemData.itemName, amount);
            carriedAmount = amount;
            isCarrying = true;

            if (currentOrder != null)
            {
                agent.isStopped = false;
                agent.SetDestination(currentOrder.GetInteractionPoint());
                ChangeState(UnitBehavior.Moving);
            }
        }
        else
        {
            ChangeState(UnitBehavior.Idle);
        }
    }

    public void CompleteOrder(ITaskable finishedTask)
    {
        if (currentOrder == finishedTask)
        {
            currentOrder = null;
        }

        // ถ้าอยู่ใน FarmJob ให้ขยับไปทำแปลงถัดไปทันที
        if (currentJob is FarmJob farmJob)
        {
            farmJob.AssignNextTaskToUnit(this);
        }
        else
        {
            agent.isStopped = false;
            ChangeState(UnitBehavior.Idle);
        }
    }

    public void ResetUnitState()
    {
        currentJob = null;
        currentOrder = null;
        isCarrying = false;
        carriedItem = null;
        draggedVisualTarget = null;
        if (rope != null) rope.enabled = false;
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.ResetPath();
            agent.isStopped = false;
        }
        ChangeState(UnitBehavior.Idle);
    }

    public void SetSelected(bool value)
    {
        isSelected = value;
        if (selectionCircle != null) selectionCircle.SetActive(value);
    }

    private Transform FindNearestVault()
    {
        GameObject[] vaults = GameObject.FindGameObjectsWithTag("Vault");
        Transform nearest = null;
        float minDist = Mathf.Infinity;
        foreach (var v in vaults)
        {
            float dist = Vector3.Distance(transform.position, v.transform.position);
            if (dist < minDist) { minDist = dist; nearest = v.transform; }
        }
        return nearest;
    }

    private void UpdateRopeVisual()
    {
        if (rope == null || !rope.enabled) return;
        if (isCarrying && draggedVisualTarget != null)
        {
            rope.SetPosition(0, transform.position);
            rope.SetPosition(1, draggedVisualTarget.position);
        }
        else if (currentOrder != null)
        {
            rope.SetPosition(0, transform.position);
            rope.SetPosition(1, currentOrder.GetInteractionPoint());
        }
    }
}