using UnityEngine;
using System.Collections;
using UnityEngine.AI;

/// <summary>
/// สถานะทั้งหมดของยูนิต — ตัวนี้แหละคือ "แหล่งความจริงเดียว" (single source of truth)
/// ว่าตอนนี้ยูนิตกำลังทำอะไรอยู่ ไม่ต้องมั่วกับ flag หลายตัวเหมือนเดิมแล้ว
/// </summary>
public enum UnitBehavior
{
    Idle, // ว่าง ไม่มีคำสั่งค้าง
    MovingToOrder, // กำลังเดินไปทำ "คำสั่งหลัก" ที่ผู้เล่น/AI สั่งไว้
    Interacting, // ถึงเป้าหมายแล้ว กำลังทำงาน (สร้างตึก/ทำแปลงผัก ฯลฯ)
    FetchItem_MoveToVault, // ระหว่างเควสหลัก ต้องวิ่งไปเบิกของที่โกดังก่อน
    FetchItem_ReturnToOrder, // เบิกของเสร็จ กำลังเดินกลับไปทำเควสหลักต่อ
    DraggingToVault, // กำลังแบกทรัพยากรไปส่งโกดัง (ระบบ gathering เดิม)
    Attacking, // กำลังสู้ศัตรู (priority ต่ำสุด แทรกได้เฉพาะตอนไม่มี Order)
    FarmingPatrol
}
public enum JobType
{
    None,
    Gathering,
    Farming
}

public class UnitBase : MonoBehaviour
{
    public AnimalStatsManager animalStatsManager;
    public GameObject selectionCircle;
    public NavMeshAgent agent;
    private Animator anim;
    public bool isSelected;
    public float buildSpeed = 10f;

    [Header("State Machine")]
    public UnitBehavior currentState = UnitBehavior.Idle;

    // 🟢 "คำสั่งหลัก" (Order) ตัวเดียวที่คุมทุกอย่าง
    private ITaskable currentOrder;

    [Header("Gathering power")]
    public int gatheringPower = 10;
    public bool isCarrying;

    [Header("Logistics")]
    public LineRenderer rope;
    public SO_ItemData carriedItem;
    public int carriedAmount;
    private Transform draggedVisualTarget;

    // 🔒 Guard: กันการตรวจบน arrival เร็วเกินไป (เฟรม 0 ของ SetDestination)
    private bool _waitingForPathCalc = false;

    // 🟢 เปลี่ยนจาก GatheringBase เป็น ResourceNodeBase เพื่อรองรับทั้งต้นไม้และแม่น้ำ
    public ResourceNodeBase gatBase;
    public float defaultSpeed;

    [Header("Fetch (สั่งไปหาของระหว่างเควส)")]
    private SO_ItemData fetchItemData;
    private Transform fetchVault;
    private int fetchAmount = 1;

    [Header("Auto Task Scanning (ทำงานเฉพาะตอน Idle และไม่มี Order)")]
    public float autoTaskScanRadius = 8f;
    private float scanCooldown = 0f;

    [Header("Auto-Repeat Gathering (เก็บทรัพยากรซ้ำอัตโนมัติ)")]
    public JobType currentJobType = JobType.None;
    private SO_ItemData autoRepeatResourceType;

    [Header("Combat (priority ต่ำสุด)")]
    public float attackRange = 2f;
    public float attackCooldown = 1.5f;
    private float lastAttackTime = 0f;
    public float damage = 0;
    public LayerMask attackableLayer;
    private Transform currentAttackTarget;

    private bool HasOrder => currentOrder != null;
    [Header("Sleep & Shift System")]
    public bool isExhausted = false; // อาการล้าจากการอดนอนข้ามคืน
    public bool isSleepingInShelter = false;
    [Header("Combat Faction (Rock-Paper-Scissors)")]
    public FactionType unitFaction = FactionType.AnimalLarge; // เลือกประเภทของยูนิตนี้ใน Inspector ได้เลย


    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        defaultSpeed = agent.speed;

        // AnimalStatsManager animal = transform.GetComponent<AnimalStatsManager>();
    }

    void Start() // 🟢 เปลี่ยนมาใช้ Start หรือสร้างฟังก์ชัน Setup แยก
    {
        animalStatsManager = GetComponent<AnimalStatsManager>();

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
            case UnitBehavior.Idle: HandleIdleState(); break;
            case UnitBehavior.MovingToOrder: HandleMovingToOrderState(); break;
            case UnitBehavior.Interacting: break;
            case UnitBehavior.FetchItem_MoveToVault: break;
            case UnitBehavior.FetchItem_ReturnToOrder: HandleMovingToOrderState(); break;
            case UnitBehavior.DraggingToVault: break;
            case UnitBehavior.Attacking: HandleCombatBehavior(); break;
            case UnitBehavior.FarmingPatrol: HandleFarmingPatrolState(); break;
        }
    }

    private void HandleIdleState()
    {
        if (isCarrying) return;
        if (isSleepingInShelter) return;

        scanCooldown -= Time.deltaTime;
        if (scanCooldown <= 0f)
        {
            scanCooldown = 1f;

            if (TryFindNearbyEnemy(out Transform enemy))
            {
                currentAttackTarget = enemy;
                ChangeState(UnitBehavior.Attacking);
                return;
            }

            // 🟢 [Auto-Repeat]: รองรับ ResourceNodeBase ทั่วไป
            if (currentJobType == JobType.Gathering)
            {
                if (gatBase != null && gatBase.canGathering)
                {
                    CommandGather(gatBase);
                    return;
                }
                else
                {
                    if (TryFindMatchingResourceNearLastBase(out ResourceNodeBase nearbyNode))
                    {
                        CommandGather(nearbyNode);
                        return;
                    }
                }
            }

            // 🟢 [Auto-Repeat]: สำหรับ Farming ให้วนลูปปลูกผักต่อถ้าร่วงกลับมาสถานะ Idle (เช่น ของในโกดังหมดชั่วคราว)
            if (currentJobType == JobType.Farming)
            {
                CommandFarmingPatrol();
                return;
            }

            TryFindAndExecuteNearbyTask();
        }
    }

    /// <summary>
    /// 🟢 ค้นหาแหล่งทรัพยากรชนิดเดิมในบริเวณใกล้เคียง (รองรับคลาสแม่ ResourceNodeBase)
    /// </summary>
    private bool TryFindMatchingResourceNearLastBase(out ResourceNodeBase node)
    {
        node = null;
        if (gatBase == null) return false;

        Collider[] hits = Physics.OverlapSphere(gatBase.transform.position, 12f);
        float minDst = Mathf.Infinity;

        foreach (var hit in hits)
        {
            ResourceNodeBase rb = hit.GetComponentInParent<ResourceNodeBase>();
            if (rb == null || !rb.canGathering) continue;

            if (autoRepeatResourceType != null && rb.resourceToProduce != autoRepeatResourceType) continue;

            float dst = Vector3.Distance(transform.position, rb.transform.position);
            if (dst < minDst)
            {
                minDst = dst;
                node = rb;
            }
        }

        return node != null;
    }

    private void HandleMovingToOrderState()
    {
        // 🔒 รอให้ NavMesh คำนวณ path เสร็จก่อนเสมอ
        if (_waitingForPathCalc)
        {
            if (agent.pathPending) return; // ยังคำนวณอยู่
            _waitingForPathCalc = false;    // คำนวณเสร็จแล้ว ถอด guard
        }

        if (agent.pathPending) return;
        if (agent.remainingDistance > agent.stoppingDistance) return;

        if (currentOrder != null)
        {
            ChangeState(UnitBehavior.Interacting);
            currentOrder.OnUnitInteract(this);
        }
        else
        {
            ChangeState(UnitBehavior.Idle);
        }
    }

    private void ChangeState(UnitBehavior next) => currentState = next;

    public void CommandMoveTo(Vector3 position)
    {
        if (isSleepingInShelter) return; // 🔒 กำลังหลับอยู่ สั่งงานไม่ได้

        currentJobType = JobType.None;
        AbandonCurrentOrder();
        agent.isStopped = false;
        agent.SetDestination(position);
        ChangeState(UnitBehavior.MovingToOrder);
    }

    public void CommandInteract(ITaskable task)
    {
        if (isSleepingInShelter) return; // 🔒 กำลังหลับอยู่ สั่งงานไม่ได้
        if (task == null) return;

        currentJobType = JobType.None;
        AbandonCurrentOrder();
        currentOrder = task;

        agent.isStopped = false;
        agent.SetDestination(task.GetInteractionPoint());
        _waitingForPathCalc = true; // 🔒 Guard: รอ 1 รอบให้ NavMesh เริ่มคำนวณ path
        ChangeState(UnitBehavior.MovingToOrder);
    }


    /// <summary>
    /// 🟢 เปลี่ยนพารามิเตอร์รับ ResourceNodeBase แทน GatheringBase เดิม
    /// </summary>
    public void CommandGather(ResourceNodeBase node)
    {
        if (node == null) return;

        CommandInteract(node);
        currentJobType = JobType.Gathering;
        autoRepeatResourceType = node.resourceToProduce;
    }

    public void CompleteOrder(ITaskable finishedTask)
    {
        if (currentOrder == finishedTask)
        {
            currentOrder = null;
        }
        agent.isStopped = false;
        ChangeState(UnitBehavior.Idle);
    }

    public void RequestFetchItem(SO_ItemData itemData, int amount = 1)
    {
        if (currentOrder == null) return;

        fetchItemData = itemData;
        fetchAmount = amount;
        fetchVault = FindNearestVault();

        if (fetchVault == null || fetchItemData == null)
        {
            ChangeState(UnitBehavior.MovingToOrder);
            return;
        }

        agent.isStopped = false;
        agent.SetDestination(fetchVault.position);
        ChangeState(UnitBehavior.FetchItem_MoveToVault);

        StopAllCoroutines();
        StartCoroutine(WaitUntilReachVaultAndReturn());
    }

    public void SetOrder(ITaskable task) => currentOrder = task;
    public ITaskable GetCurrentOrder() => currentOrder;

    private void AbandonCurrentOrder()
    {
        if (currentOrder != null)
        {
            currentOrder.OnUnitExit(this);
            currentOrder = null;
        }
        StopAllCoroutines();
    }

    public void SetSelected(bool value)
    {
        isSelected = value;
        if (selectionCircle != null)
            selectionCircle.SetActive(value);
    }

    private void OnTriggerEnter(Collider other)
    {
        // 🔒 ถ้ากำลังแบกของ หรือกำลังเดิน Dragging อยู่ อย่าเข้าไปแย่งงานใหม่
        if (isCarrying) return;
        if (currentState == UnitBehavior.DraggingToVault) return;

        ITaskable task = other.GetComponentInParent<ITaskable>();
        if (task == null) return;

        // 🟢 เปลี่ยนเงื่อนไขชน ให้ตรวจจับ ResourceNodeBase แทน
        if (task is ResourceNodeBase resourceNode)
        {
            // เข้าได้เฉพาะตอนไม่มี Order หรือกำลังเดินหา node นี้อยู่
            if (!HasOrder || currentOrder == (object)resourceNode)
            {
                currentOrder = resourceNode;
                currentJobType = JobType.Gathering;
                autoRepeatResourceType = resourceNode.resourceToProduce;

                agent.isStopped = true;
                ChangeState(UnitBehavior.Interacting);
                resourceNode.OnUnitInteract(this);
            }
        }
    }

    #region Auto Task Scanning AI
    private void TryFindAndExecuteNearbyTask()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, autoTaskScanRadius);
        ITaskable nearestTask = null;
        float minDst = Mathf.Infinity;

        foreach (var hit in hits)
        {
            ITaskable task = hit.GetComponentInParent<ITaskable>();
            if (task == null) continue;

            float dst = Vector3.Distance(transform.position, hit.transform.position);
            if (dst < minDst)
            {
                minDst = dst;
                nearestTask = task;
            }
        }

        if (nearestTask == null) return;

        if (nearestTask is CropPlots plot)
        {
            if (plot.currentStage == CropStage.NeedsFertilizer || plot.currentStage == CropStage.NeedsWater)
            {
                CommandInteract(plot);
            }
        }
        else if (nearestTask is GhostBuilding)
        {
            CommandInteract(nearestTask);
        }
    }
    #endregion

    public void ResetUnitState()
    {
        AbandonCurrentOrder();

        isCarrying = false;
        carriedItem = null;
        draggedVisualTarget = null;

        if (currentJobType != JobType.Gathering)
        {
            gatBase = null;
            agent.speed = defaultSpeed;
        }

        if (rope != null) rope.enabled = false;

        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.ResetPath();
            agent.isStopped = false;
        }

        currentAttackTarget = null;
        ChangeState(UnitBehavior.Idle);
    }

    #region Resource Gathering
    public void StartDragging(Transform targetNode, Transform vault, float customSpeed = 3f)
    {
        // 🔒 ไม่ใช้ AbandonCurrentOrder เพราะจะลบ gatBase ที่ gathering
        // แต่ยกเลิก coroutines เก่าทั้งหมดและ clear order reference
        StopAllCoroutines();
        currentOrder = null;

        agent.isStopped = false;
        agent.enabled = true;
        isCarrying = true;
        draggedVisualTarget = targetNode;

        agent.speed = customSpeed;

        if (rope != null) rope.enabled = true;

        agent.SetDestination(vault.position);
        ChangeState(UnitBehavior.DraggingToVault);

        StartCoroutine(CheckArrivalAtVault(vault));
    }

    public void DropItemAtVault()
    {
        if (gatBase != null)
        {
            gatBase.OnUnitSentResourced();
            return;
        }
        ResetUnitState();
    }

    IEnumerator CheckArrivalAtVault(Transform vault)
    {
        yield return new WaitForEndOfFrame();
        while (agent.pathPending) yield return null;

        while (Vector3.Distance(transform.position, vault.position) > (agent.stoppingDistance + 1.5f))
        {
            if (!agent.hasPath && agent.velocity.sqrMagnitude < 0.01f)
                break;

            yield return null;
        }

        if (isCarrying)
        {
            ResourceVault v = vault.GetComponent<ResourceVault>();
            if (v != null)
            {
                v.OnUnitInteract(this);
            }
            else
            {
                ResetUnitState();
            }
        }
    }
    #endregion

    #region Fetch Item Flow
    IEnumerator WaitUntilReachVaultAndReturn()
    {
        while (agent.pathPending || agent.remainingDistance > agent.stoppingDistance)
        {
            yield return null;
        }

        if (ResourceInventory.Instance.HasResource(fetchItemData.itemName, fetchAmount))
        {
            carriedItem = ResourceInventory.Instance.ConsumeAndGetReturn(fetchItemData.itemName, fetchAmount);
            carriedAmount = fetchAmount;
            isCarrying = true;

            if (currentOrder != null)
            {
                agent.isStopped = false;
                agent.SetDestination(currentOrder.GetInteractionPoint());
                ChangeState(UnitBehavior.FetchItem_ReturnToOrder);
            }
            else
            {
                ChangeState(UnitBehavior.Idle);
            }
        }
        else
        {
            ChangeState(UnitBehavior.Idle);
        }
    }

    private Transform FindNearestVault()
    {
        GameObject[] vaults = GameObject.FindGameObjectsWithTag("Vault");
        Transform nearest = null;
        float minDistance = Mathf.Infinity;
        foreach (GameObject v in vaults)
        {
            float dist = Vector3.Distance(transform.position, v.transform.position);
            if (dist < minDistance) { minDistance = dist; nearest = v.transform; }
        }
        return nearest;
    }
    #endregion

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

    #region Combat System
    private bool TryFindNearbyEnemy(out Transform enemy)
    {
        enemy = null;
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, autoTaskScanRadius, attackableLayer);
        float nearestDist = Mathf.Infinity;

        foreach (var hit in hitColliders)
        {
            if (!hit.CompareTag("Enemy")) continue;

            float dist = Vector3.Distance(transform.position, hit.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                enemy = hit.transform;
            }
        }
        return enemy != null;
    }

    private void HandleCombatBehavior()
    {
        if (currentAttackTarget == null || !currentAttackTarget.gameObject.activeInHierarchy)
        {
            currentAttackTarget = null;
            agent.isStopped = false;
            ChangeState(UnitBehavior.Idle);
            return;
        }

        float distanceToEnemy = Vector3.Distance(transform.position, currentAttackTarget.position);

        if (distanceToEnemy > attackRange)
        {
            agent.isStopped = false;
            agent.SetDestination(currentAttackTarget.position);
        }
        else
        {
            agent.isStopped = true;

            if (Time.time >= lastAttackTime + attackCooldown)
            {
                PerformAttack();
                lastAttackTime = Time.time;
            }
        }
    }

    private void PerformAttack()
    {
        if (currentAttackTarget == null) return;

        Vector3 direction = (currentAttackTarget.position - transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        // 1. เช็คว่าเป้าหมายมี CharacterStats หรือเป็นศัตรูไหม
        CharacterStats enemyStats = currentAttackTarget.GetComponent<CharacterStats>();
        if (enemyStats == null) return;

        // 2. ดึงประเภทของศัตรูจาก EnemyController (ถ้ามี)
        FactionType enemyFaction = FactionType.AnimalLarge; // ค่าสำรอง
        EnemyController enemyController = currentAttackTarget.GetComponent<EnemyController>();
        if (enemyController != null && enemyController.enemyData != null)
        {
            enemyFaction = enemyController.enemyData.factionType;
        }

        // 3. 🌟 คำนวณดาเมจตามหลักแพ้ทางชนะทาง (CombatSystem จาก UnitType.cs)
        float baseDamage = animalStatsManager != null ? animalStatsManager.damage : 10f;
        float multiplier = CombatSystem.GetDamageMultiplier(unitFaction, enemyFaction);
        int finalDamage = Mathf.RoundToInt(baseDamage * multiplier);

        Debug.Log($"⚔️ [Unit Attack]: ยูนิตฝ่ายเรา ({unitFaction}) โจมตีศัตรู ({enemyFaction}) | ตัวคูณ: {multiplier}x | ดาเมจสุทธิ: {finalDamage}");

        // 4. ส่งดาเมจไปยังศัตรู (ผ่าน EnemyController หรือ CharacterStats โดยตรง)
        if (enemyController != null)
        {
            // ส่งตำแหน่งผู้โจมตีไปด้วย เพื่อให้ HitFeedback เด้งถอยหลังถูกทิศทาง
            enemyController.TakeDamage(finalDamage, unitFaction, transform.position);
        }
        else
        {
            enemyStats.TakeDamage(finalDamage, transform.position);
        }

        if (enemyStats.currentHP <= 0)
        {
            currentAttackTarget = null;
            
            agent.isStopped = false;
            ChangeState(UnitBehavior.Idle);
        }
    }
    #endregion

    #region Farming
    public void CommandFarmingPatrol()
    {
        if (isSleepingInShelter) return; // 🔒 กำลังหลับอยู่ สั่งงานไม่ได้

        AbandonCurrentOrder();
        currentJobType = JobType.Farming;
        ChangeState(UnitBehavior.FarmingPatrol);
        FindAndWalkToNextFarmTask();
    }

    private void HandleFarmingPatrolState()
    {
        if (agent.pathPending) return;

        // เช็คว่าถึงเป้าหมาย (แปลงผัก) หรือยัง
        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            agent.isStopped = true;

            // ถ้ามี Order เป็น CropPlots อยู่ และยังไม่ได้จัดการ ให้สั่ง Interact ทันที
            if (currentOrder is CropPlots plot)
            {
                // ป้องกันการเรียกซ้ำรัวๆ ถ้ากำลังทำงานอยู่
                if (currentState != UnitBehavior.Interacting)
                {
                    ChangeState(UnitBehavior.Interacting);
                    plot.OnUnitInteract(this);
                }
                return;
            }

            // ถ้าไม่มีเป้าหมาย ให้หาแปลงถัดไปเดินไปเรื่อยๆ (Patrol)
            FindAndWalkToNextFarmTask();
        }
    }

    public void FindAndWalkToNextFarmTask()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, 20f);
        CropPlots bestPlot = null;
        float minDst = Mathf.Infinity;

        foreach (var hit in hits)
        {
            CropPlots plot = hit.GetComponentInParent<CropPlots>();

            if (plot != null && plot.currentStage != CropStage.Empty && plot.currentStage != CropStage.Growing)
            {
                bool isMyCurrentPlot = ((object)currentOrder == (object)plot);

                if (!plot.isBeingServiced || isMyCurrentPlot)
                {
                    float dst = Vector3.Distance(transform.position, plot.transform.position);
                    if (dst < minDst)
                    {
                        minDst = dst;
                        bestPlot = plot;
                    }
                }
            }
        }

        if (bestPlot != null)
        {
            if (currentOrder is CropPlots oldPlot && oldPlot != bestPlot)
            {
                oldPlot.isBeingServiced = false;
            }

            currentOrder = bestPlot;
            bestPlot.isBeingServiced = true;

            agent.isStopped = false;
            agent.SetDestination(bestPlot.GetInteractionPoint());
            ChangeState(UnitBehavior.FarmingPatrol);
        }
        else
        {
            currentOrder = null;
            agent.isStopped = false;

            Vector3 randomDirection = Random.insideUnitSphere * 6f;
            randomDirection += transform.position;

            if (NavMesh.SamplePosition(randomDirection, out NavMeshHit navHit, 6f, NavMesh.AllAreas))
            {
                agent.SetDestination(navHit.position);
            }

            ChangeState(UnitBehavior.FarmingPatrol);
        }
    }
    #endregion

    public void MoveTo(Vector3 position, ITaskable task = null)
    {
        // 🟢 เปลี่ยนจากการเช็ค GatheringBase ตรงๆ เป็น ResourceNodeBase
        if (task is ResourceNodeBase resourceNode)
        {
            CommandGather(resourceNode);
        }
        else if (task is CropPlots)
        {
            CommandFarmingPatrol();
        }
        else if (task != null)
        {
            CommandInteract(task);
        }
        else
        {
            CommandMoveTo(position);
        }
    }

    public void GoFetchItemAndReturn(SO_ItemData itemData, ITaskable ultimateTask, int amount = 1)
    {
        RequestFetchItem(itemData, amount);
    }


    public void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;

        Gizmos.DrawSphere(transform.position, autoTaskScanRadius);

    }


}