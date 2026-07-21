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

public class UnitBase : MonoBehaviour
{
    public GameObject selectionCircle;
    public NavMeshAgent agent;
    private Animator anim;
    public bool isSelected;
    public float buildSpeed = 10f;

    [Header("State Machine")]
    public UnitBehavior currentState = UnitBehavior.Idle;

    // 🟢 "คำสั่งหลัก" (Order) ตัวเดียวที่คุมทุกอย่าง
    // ตราบใดที่ตัวนี้ไม่เป็น null แปลว่ายูนิต "ติดเควส" อยู่
    // ห้าม AutoScan / Combat เข้ามาแย่งซีน จนกว่า Order จะเสร็จ หรือมีคำสั่งใหม่มาทับ
    private ITaskable currentOrder;

    [Header("Gathering power")]
    public int gatheringPower = 10;
    public bool isCarrying;

    [Header("Logistics")]
    public LineRenderer rope;
    public SO_ItemData carriedItem;
    public int carriedAmount;
    private Transform draggedVisualTarget;

    [Header("Fetch (สั่งไปหาของระหว่างเควส)")]
    private SO_ItemData fetchItemData;
    private Transform fetchVault;
    private int fetchAmount = 1;

    [Header("Auto Task Scanning (ทำงานเฉพาะตอน Idle และไม่มี Order)")]
    public float autoTaskScanRadius = 8f;
    private float scanCooldown = 0f;

    [Header("Auto-Repeat Gathering (เก็บทรัพยากรซ้ำอัตโนมัติ)")]
    // 🟢 true = เพิ่งไปเก็บทรัพยากร (เช่นต้นไม้) มา ให้ลองหาต้นถัดไปชนิดเดียวกันเองเลย
    // ถูกล้างอัตโนมัติทุกครั้งที่มีคำสั่งใหม่มาทับผ่าน CommandMoveTo / CommandInteract
    public bool autoRepeatEnabled = false;
    private SO_ItemData autoRepeatResourceType;

    [Header("Combat (priority ต่ำสุด)")]
    public float attackRange = 2f;
    public float attackCooldown = 1.5f;
    private float lastAttackTime = 0f;
    public LayerMask attackableLayer;
    private Transform currentAttackTarget;

    // มี Order ค้างอยู่ไหม? ใช้ตัวนี้เป็นเงื่อนไขเดียวในการปิดกั้น AutoScan/Combat
    private bool HasOrder => currentOrder != null;

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

    // =====================================================================
    // STATE MACHINE CORE
    // =====================================================================
    private void RunStateMachine()
    {
        switch (currentState)
        {
            case UnitBehavior.Idle:
                HandleIdleState();
                break;

            case UnitBehavior.MovingToOrder:
                HandleMovingToOrderState();
                break;

            case UnitBehavior.Interacting:
                // งานระหว่างทำจะจบตัวเองผ่าน callback ของ ITaskable (ดูเมธอด CompleteOrder)
                break;

            case UnitBehavior.FetchItem_MoveToVault:
                // คุมด้วย coroutine WaitUntilReachVaultAndReturn
                break;

            case UnitBehavior.FetchItem_ReturnToOrder:
                HandleMovingToOrderState(); // เดินกลับไปหาเป้าหมายเดิม ใช้ logic เดียวกับเดินไป order
                break;

            case UnitBehavior.DraggingToVault:
                // คุมด้วย coroutine CheckArrivalAtVault
                break;

            case UnitBehavior.Attacking:
                HandleCombatBehavior();
                break;
            case UnitBehavior.FarmingPatrol:
                HandleFarmingPatrolState();
                break;
        }
    }

    /// <summary>
    /// Idle คือสถานะเดียวที่ยอมให้ AutoScan / Combat / Auto-Repeat เข้ามาเสนองานได้
    /// เพราะไม่มี Order ค้างอยู่แล้ว
    /// ลำดับความสำคัญ: ศัตรู > งานเก็บของซ้ำที่ค้างไว้ > งานฟาร์ม/ตึกทั่วไป
    /// </summary>
    private void HandleIdleState()
    {
        if (isCarrying) return; // กำลังแบกของอยู่ ไม่ต้องหางานใหม่

        scanCooldown -= Time.deltaTime;
        if (scanCooldown <= 0f)
        {
            scanCooldown = 1f;

            // 1) หาศัตรูก่อน (หรือจะสลับลำดับกับ auto task ก็ได้ตามที่ต้องการ)
            if (TryFindNearbyEnemy(out Transform enemy))
            {
                currentAttackTarget = enemy;
                ChangeState(UnitBehavior.Attacking);
                return;
            }

            // 2) 🟢 [Auto-Repeat]: เพิ่งไปเก็บทรัพยากรมา (เช่นต้นไม้) ให้ลองหาต้นถัดไปชนิดเดียวกัน
            // ก่อนไปรับงานอื่น เพื่อให้ยูนิตวนเก็บของชนิดเดิมต่อเนื่องเองโดยไม่ต้องสั่งซ้ำ
            if (autoRepeatEnabled && TryFindNearbyMatchingResourceNode(out GatheringBase repeatNode))
            {
                Debug.Log($"🌲 [Auto-Repeat]: {name} ไปเก็บทรัพยากรต้นถัดไปเองอัตโนมัติ!");
                CommandGather(repeatNode);
                return;
            }

            // 3) ไม่มีศัตรู ไม่มีงานซ้ำให้ทำ ลองหางานแปลงผัก/ตึกที่ต้องการความช่วยเหลือ
            TryFindAndExecuteNearbyTask();
        }
    }

    private void HandleMovingToOrderState()
    {
        if (agent.pathPending) return;
        if (agent.remainingDistance > agent.stoppingDistance) return;

        // ถึงจุดหมายแล้ว
        if (currentOrder != null)
        {
            ChangeState(UnitBehavior.Interacting);
            currentOrder.OnUnitInteract(this);
            // NOTE: ตัว ITaskable (เช่น CropPlots / GhostBuilding / GatheringBase) เป็นคนเรียก
            // CompleteOrder() หรือ RequestFetchItem() กลับมาเองเมื่อทำงานเสร็จ/ต้องการของ
        }
        else
        {
            // สั่งเดินเฉยๆ ไม่มีเป้าหมายงาน
            ChangeState(UnitBehavior.Idle);
        }
    }

    private void ChangeState(UnitBehavior next)
    {
        currentState = next;
    }

    // =====================================================================
    // PUBLIC COMMANDS — จุดเดียวที่อนุญาตให้ "สั่งงานใหม่" มาทับ Order เดิมได้
    // =====================================================================

    /// <summary>สั่งเดินเฉยๆ ไม่มี task ผูกท้าย (ยกเลิก Order เดิมทันที รวมถึงยกเลิก auto-repeat ด้วย)</summary>
    public void CommandMoveTo(Vector3 position)
    {
        autoRepeatEnabled = false; // 🟢 มีคำสั่งใหม่จากผู้เล่น = เลิกวนงานเก่าอัตโนมัติ
        AbandonCurrentOrder();
        agent.isStopped = false;
        agent.SetDestination(position);
        ChangeState(UnitBehavior.MovingToOrder);
    }

    /// <summary>
    /// สั่งงานหลัก (Order) ใหม่ — เช่น "ไปทำฟาร์ม" / "ไปช่วยสร้างตึก"
    /// จะทำงานนี้ "อย่างเดียว" จนกว่าจะเสร็จ หรือมีคำสั่งใหม่มาทับเท่านั้น
    /// </summary>
    public void CommandInteract(ITaskable task)
    {
        if (task == null) return;

        // 🟢 คำสั่งใหม่ (ไม่ว่าจะเป็นงานอะไรก็ตาม) ให้เคลียร์ auto-repeat เสมอ
        // ถ้าเป็นการสั่งเก็บของแบบ auto-repeat จริงๆ ให้ไปเรียกผ่าน CommandGather() แทน
        // ซึ่งจะตั้งค่ากลับเป็น true อีกทีหลังจากเรียกเมธอดนี้
        autoRepeatEnabled = false;

        AbandonCurrentOrder();
        currentOrder = task;

        agent.isStopped = false;
        agent.SetDestination(task.GetInteractionPoint());
        ChangeState(UnitBehavior.MovingToOrder);
    }

    /// <summary>
    /// สั่งให้ยูนิตไปเก็บทรัพยากร (เช่นต้นไม้) ผ่าน GatheringBase และ "จดจำ" ไว้ว่างานนี้ให้ทำซ้ำอัตโนมัติ
    /// พอส่งของที่โกดังเสร็จแล้ว (ResetUnitState ถูกเรียก) ยูนิตจะพยายามหาทรัพยากรชนิดเดียวกัน
    /// ต้นถัดไปที่ใกล้ที่สุดแล้วไปเก็บต่อเองทันที โดยไม่ต้องสั่งใหม่
    /// จนกว่าผู้เล่นจะสั่ง CommandMoveTo / CommandInteract (งานอื่น) มาทับ
    /// </summary>
    public void CommandGather(GatheringBase node)
    {
        if (node == null) return;

        CommandInteract(node); // ข้างในจะเคลียร์ autoRepeatEnabled ก่อน แล้วค่อยตั้งใหม่ด้านล่างนี้
        autoRepeatEnabled = true;
        autoRepeatResourceType = node.resourceToProduce;
    }

    /// <summary>
    /// เรียกจากตัว ITaskable เมื่อ "เควสของมันจบสมบูรณ์แล้ว"
    /// (เช่น สร้างตึกเสร็จ / รดน้ำแปลงผักเสร็จ) — ค่อยปลด Order
    /// ห้ามที่อื่นเคลียร์ currentOrder เอง นอกจากจุดนี้กับ Abandon/Command ใหม่
    /// </summary>
    public void CompleteOrder(ITaskable finishedTask)
    {
        if (currentOrder == finishedTask)
        {
            currentOrder = null;
        }
        agent.isStopped = false;
        ChangeState(UnitBehavior.Idle);
    }

    /// <summary>
    /// เรียกจากตัว ITaskable ระหว่างทำงาน เมื่อพบว่า "ยังขาดของ" (เช่น ปุ๋ยหมด)
    /// ยูนิตจะวิ่งไปเบิกของแล้ว "กลับมาทำ Order เดิมต่อ" โดยอัตโนมัติ — Order ไม่ถูกยกเลิก
    /// </summary>
    public void RequestFetchItem(SO_ItemData itemData, int amount = 1)
    {
        if (currentOrder == null)
        {
            Debug.LogWarning($"{name}: RequestFetchItem ถูกเรียกทั้งที่ไม่มี Order ค้างอยู่");
            return;
        }

        fetchItemData = itemData;
        fetchAmount = amount;
        fetchVault = FindNearestVault();

        if (fetchVault == null || fetchItemData == null)
        {
            Debug.LogWarning("ไม่พบโกดัง หรือไม่ได้กำหนดข้อมูลไอเทมที่จะเบิก!");
            ChangeState(UnitBehavior.MovingToOrder); // ลองกลับไปทำงานเดิมต่อเผื่อแก้ปัญหาได้เอง
            return;
        }

        agent.isStopped = false;
        agent.SetDestination(fetchVault.position);
        ChangeState(UnitBehavior.FetchItem_MoveToVault);

        StopAllCoroutines();
        StartCoroutine(WaitUntilReachVaultAndReturn());
    }

    /// <summary>
    /// ผูก Order ให้ยูนิตตรงๆ โดยไม่สั่งให้เดิน (compat กับโค้ดเดิมที่เคยเรียก unit.SetTask(ghost))
    /// ใช้ตอนที่ยูนิตเดินถึง/ชนเป้าหมายอยู่แล้ว แค่ต้องการให้จำไว้ว่ากำลังทำงานนี้อยู่
    /// (เพื่อให้ AbandonCurrentOrder เรียก OnUnitExit ถูกตัวตอนยูนิตเลิกงานทีหลัง)
    /// ถ้าต้องการสั่งให้เดินไปด้วย ให้ใช้ CommandInteract แทน
    /// </summary>
    public void SetOrder(ITaskable task)
    {
        currentOrder = task;
    }

    /// <summary>เผื่อโค้ดภายนอกอยากเช็คว่ายูนิตติดเควสอะไรอยู่ (read-only)</summary>
    public ITaskable GetCurrentOrder() => currentOrder;

    /// <summary>ยกเลิก Order ปัจจุบันแบบเงียบๆ (ใช้ก่อนรับคำสั่งใหม่ หรือตอน Reset)</summary>
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

    // =====================================================================
    // Trigger เดินชน — ยอมรับงานอัตโนมัติ "เฉพาะตอนไม่มี Order" หรือ
    // ถ้ามี Order อยู่แล้ว ต้องเป็นเป้าหมายเดียวกับ Order เท่านั้น (กันแย่งซีนเควส)
    // =====================================================================
    private void OnTriggerEnter(Collider other)
    {
        if (isCarrying) return;

        ITaskable task = other.GetComponentInParent<ITaskable>();
        if (task == null) return;

        bool isOurOwnOrder = (currentOrder == task);
        // 🟢 แก้บั๊ก: เดิมเช็ค currentState == Idle ด้วย ทำให้ตอนผู้เล่นสั่ง CommandMoveTo
        // (state = MovingToOrder แต่ currentOrder เป็น null) แล้วเดินถูตึกพอดี กลับไม่ถูกรับงาน
        // เงื่อนไขที่ถูกต้องคือเช็คแค่ "มี Order ค้างอยู่จริงไหม" เท่านั้น ไม่ต้องสนใจ state
        bool freeToAutoAccept = !HasOrder;

        if (!isOurOwnOrder && !freeToAutoAccept) return; // มีเควสอื่นค้างอยู่ ไม่ไปยุ่งกับตึก/แปลงอื่น

        agent.isStopped = true;

        if (freeToAutoAccept)
        {
            currentOrder = task; // เดินชนแล้วรับเป็น Order ใหม่ (auto)

            // 🟢 ถ้าเดินชนต้นไม้/แหล่งทรัพยากร (GatheringBase) แบบไม่ได้ตั้งใจ
            // ให้ถือว่านี่คืองานเก็บของแบบ auto-repeat ด้วยเช่นกัน
            if (task is GatheringBase gatherNode)
            {
                autoRepeatEnabled = true;
                autoRepeatResourceType = gatherNode.resourceToProduce;
            }
        }

        ChangeState(UnitBehavior.Interacting);
        task.OnUnitInteract(this);
    }

    // =====================================================================
    // Auto Task Scanning — ทำงานเฉพาะตอน Idle และไม่มี Order เท่านั้น
    // =====================================================================
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
                Debug.Log($"🤖 [Smart AI]: {name} เจองานแปลงผัก เดินไปจัดการเองออโต้!");
                CommandInteract(plot);
            }
        }
        else if (nearestTask is GhostBuilding)
        {
            Debug.Log($"🤖 [Smart AI]: {name} เดินไปช่วยสร้างตึกออโต้!");
            CommandInteract(nearestTask);
        }
    }

    /// <summary>
    /// 🟢 [Auto-Repeat]: หาแหล่งทรัพยากร (GatheringBase) ที่ใกล้ที่สุด ซึ่งเป็น "ชนิดเดียวกัน"
    /// กับที่เพิ่งไปเก็บมาล่าสุด (autoRepeatResourceType) เท่านั้น กันวิ่งไปเก็บของผิดชนิด
    /// </summary>
    private bool TryFindNearbyMatchingResourceNode(out GatheringBase node)
    {
        node = null;
        Collider[] hits = Physics.OverlapSphere(transform.position, autoTaskScanRadius);
        float minDst = Mathf.Infinity;

        foreach (var hit in hits)
        {
            GatheringBase gb = hit.GetComponentInParent<GatheringBase>();
            if (gb == null) continue;

            // ถ้าไม่ได้ระบุชนิดไว้ (เผื่อกรณีพิเศษ) ก็รับได้ทุกชนิด
            if (autoRepeatResourceType != null && gb.resourceToProduce != autoRepeatResourceType) continue;

            float dst = Vector3.Distance(transform.position, hit.transform.position);
            if (dst < minDst)
            {
                minDst = dst;
                node = gb;
            }
        }

        return node != null;
    }
    #endregion

    // =====================================================================
    // Reset
    // =====================================================================
    public void ResetUnitState()
    {
        AbandonCurrentOrder();

        isCarrying = false;
        carriedItem = null;
        draggedVisualTarget = null;

        if (rope != null) rope.enabled = false;

        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.ResetPath();
            agent.isStopped = false;
        }

        currentAttackTarget = null;
        // 🟢 หมายเหตุ: ตั้งใจ "ไม่" เคลียร์ autoRepeatEnabled / autoRepeatResourceType ตรงนี้
        // เพราะ ResetUnitState ถูกเรียกตอนส่งของที่โกดังเสร็จพอดี ซึ่งเป็นจังหวะที่เรา
        // ต้องการให้ยูนิตจำได้ว่า "เพิ่งเก็บทรัพยากรชนิดนี้มา" แล้วไปหาต้นถัดไปเองใน HandleIdleState
        ChangeState(UnitBehavior.Idle);
    }

    // =====================================================================
    // Resource Gathering (ระบบลากของเดิม — แยกจาก Order system)
    // =====================================================================
    #region Resource Gathering
    public void StartDragging(Transform targetNode, Transform vault)
    {
        AbandonCurrentOrder();

        agent.isStopped = false;
        agent.enabled = true;
        isCarrying = true;
        draggedVisualTarget = targetNode;

        if (rope != null) rope.enabled = true;

        agent.SetDestination(vault.position);
        ChangeState(UnitBehavior.DraggingToVault);

        StopAllCoroutines();
        StartCoroutine(CheckArrivalAtVault(vault));
    }

    public void DropItemAtVault()
    {
        Debug.Log("ส่งของแล้ว รอ ลบข้อมูล");

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

    IEnumerator CheckArrivalAtVault(Transform vault)
    {
        while (agent.remainingDistance > agent.stoppingDistance) yield return null;

        if (isCarrying)
        {
            ResourceVault v = vault.GetComponent<ResourceVault>();
            if (v != null) v.OnUnitInteract(this);
        }
    }
    #endregion

    // =====================================================================
    // Fetch item mid-order (สั่งให้ไปเบิกของ แล้วกลับมาทำ Order เดิมต่อ)
    // =====================================================================
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
            carriedAmount = fetchAmount; // 🟢 บั๊กเดิม: ลืมตั้งค่านี้ ทำให้ carriedAmount ค้างเป็น 0 ตลอด
            isCarrying = true;

            Debug.Log($"{name} เบิก {fetchItemData.itemName} มาแล้ว กำลังเดินกลับไปที่ Order เดิม!");

            if (currentOrder != null)
            {
                // 🟢 แก้บั๊ก: ห้ามปลด isCarrying ตรงนี้! ต้องถือของไว้จนกว่าจะเดินถึงแปลง/ตึกเป้าหมาย
                // แล้วให้ ITaskable (CropPlots) เป็นคนเช็ค unit.isCarrying ตอน OnUnitInteract เอง
                // ก่อนหน้านี้ตั้ง isCarrying = false ตรงนี้ไปด้วย เลยทำให้ถึงที่หมายแล้วดูเหมือน "มือเปล่า"
                agent.isStopped = false;
                agent.SetDestination(currentOrder.GetInteractionPoint());
                ChangeState(UnitBehavior.FetchItem_ReturnToOrder);
            }
            else
            {
                Debug.LogWarning($"{name}: เบิกของมาแล้วแต่ Order เดิมหายไป (ถูกยกเลิกระหว่างทาง)");
                ChangeState(UnitBehavior.Idle);
            }
        }
        else
        {
            Debug.LogWarning($"โกดังไม่มี {fetchItemData.itemName} ให้เบิก!");
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

    // =====================================================================
    // Rope visual
    // =====================================================================
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

    // =====================================================================
    // Combat — priority ต่ำสุด แทรกได้เฉพาะตอนไม่มี Order (HasOrder == false)
    // =====================================================================
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
        // ถ้าระหว่างนี้มี Order เข้ามาแทรก (ผู้เล่นสั่งงานใหม่) ให้เลิกสู้ทันที — ไม่ต้องทำอะไร
        // เพราะ CommandInteract/CommandMoveTo จะ ChangeState ทับอยู่แล้ว

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

        CharacterStats enemyStats = currentAttackTarget.GetComponent<CharacterStats>();
        if (enemyStats == null)
        {
            Debug.LogWarning($"⚠️ เป้าหมาย {currentAttackTarget.name} ไม่มีสคริปต์ CharacterStats!");
            return;
        }

        int damageToDeal = gatheringPower > 0 ? gatheringPower : 10;
        enemyStats.currentHP -= damageToDeal;
        if (enemyStats.currentHP < 0) enemyStats.currentHP = 0;

        Debug.Log($"🗡️ {name} โจมตี {currentAttackTarget.name} สร้างความเสียหาย {damageToDeal}! เลือดศัตรูเหลือ: {enemyStats.currentHP}");

        if (enemyStats.currentHP <= 0)
        {
            Debug.Log($"💀 ศัตรู {currentAttackTarget.name} ถูกกำจัดแล้ว!");
            currentAttackTarget = null;
            agent.isStopped = false;
            ChangeState(UnitBehavior.Idle);
        }
    }
    #endregion
    #region Farming
    public void CommandFarmingPatrol()
    {
        AbandonCurrentOrder();
        ChangeState(UnitBehavior.FarmingPatrol);
        FindAndWalkToNextFarmTask();
    }

    private void HandleFarmingPatrolState()
    {
        if (agent.pathPending) return;

        // ถ้ายูนิตกำลังเดินไปถึงจุดหมายแล้ว (ไม่ว่าจะเดินไปถึงแปลงผัก หรือเดินวนตรวจ)
        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            // ลองมองหาแปลงใกล้ๆ อีกรอบว่ามีอันไหนต้องการความช่วยเหลือไหม
            FindAndWalkToNextFarmTask();
        }
    }

    public void FindAndWalkToNextFarmTask()
    {
        // 1. ค้นหาแปลงผักทั้งหมดในรัศมี
        Collider[] hits = Physics.OverlapSphere(transform.position, 20f);
        CropPlots bestPlot = null;
        float minDst = Mathf.Infinity;

        foreach (var hit in hits)
        {
            CropPlots plot = hit.GetComponentInParent<CropPlots>();

            // 🟢 เงื่อนไขสำคัญ: 
            // - ต้องไม่ใช่แปลงที่ว่างเปล่า หรือกำลังเติบโต
            // - ต้องไม่อยู่ในระหว่างถูกยูนิตตัวอื่นให้บริการ/จองอยู่ (isBeingServiced == false)
            // - หรือถ้าเป็นแปลงที่ตัวเราเองกำลังทำอยู่ ก็อนุญาต
            if (plot != null && plot.currentStage != CropStage.Empty && plot.currentStage != CropStage.Growing)
            {
                // 🟢 แปลง currentOrder เป็น object เพื่อป้องกันการเตือน CS0252
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
            // ถ้าเดิมเคยจับจองแปลงอื่นอยู่ ให้ปลดล็อคแปลงเก่าก่อน
            if (currentOrder is CropPlots oldPlot && oldPlot != bestPlot)
            {
                oldPlot.isBeingServiced = false;
            }

            currentOrder = bestPlot;
            bestPlot.isBeingServiced = true; // 🟢 ล็อคทันทีกันยูนิตตัวอื่นแย่งแปลงนี้!

            agent.isStopped = false;
            agent.SetDestination(bestPlot.GetInteractionPoint());
            ChangeState(UnitBehavior.FarmingPatrol);
        }
        else
        {
            // 🟢 ถ้าแปลงทั้งหมดถูกจับจองหรือเต็มหมดแล้ว ยูนิตที่เหลือจะไม่ไปรุม 
            // แต่จะเดินแยกย้ายไปยืนสแตนด์บาย/เดินตรวจรอบนอกโซนฟาร์มแทน
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
        if (task is GatheringBase gatherNode)
        {
            // 🟢 สั่งให้ยูนิตทุกตัวที่ถูกเลือก วิ่งมารุมล้อมช่วยกันเก็บทรัพยากรกลุ่มนี้
            CommandGather(gatherNode);
        }
        else if (task is CropPlots)
        {
            // สั่งกระจายกำลังทำฟาร์มแบบไม่แย่งกัน
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
        RequestFetchItem(itemData, amount); // ultimateTask ไม่จำเป็นแล้ว เพราะ currentOrder เก็บไว้ในตัวอยู่แล้ว
    }
}