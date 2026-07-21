using UnityEngine;
using System.Collections;
using UnityEngine.AI;

public enum UnitBehavior { Idle, Moving, Interacting, Taming, Attack }
public class UnitBase : MonoBehaviour
{
    public GameObject selectionCircle;
    public UnitBehavior currentBehavior = UnitBehavior.Idle;
    public NavMeshAgent agent;
    private Animator anim;
    public ITaskable targetTask;
    public bool isSelected;
    public float buildSpeed = 10f;
    private GhostBuilding currentTargetGhost;

    [Header("Gathering power")]
    public int gatheringPower = 10;
    public bool isCarrying;
    [Header("Logistics")]
    public LineRenderer rope;
    public SO_ItemData carriedItem;
    public SO_ItemData itemToFetchData;

    public int carriedAmount;
    private Transform draggedVisualTarget;
    [Header("Smart Tasking")]
    public ITaskable pendingTask; // จำไว้ว่าต้องกลับไปทำงานที่แปลงไหน
    private string itemToFetchKey;
    [Header("Auto AI Tasking")]
    public float autoTaskScanRadius = 8f; // รัศมีที่ยูนิตจะมองหางานอัตโนมัติเมื่อว่าง
    private float scanCooldown = 0f;
    [Header("RTS Task Locking")]
    public bool isManualTask = false; // ตัวเช็คว่าผู้เล่นสั่งมาเองหรือเปล่า ถ้าใช่ ห้ามเปลี่ยนงานมั่วซั่ว

    [Header("hit enemy")]
    public float attackRange = 2f;         // ระยะโจมตีประชิด
    public float attackCooldown = 1.5f;     // คูลดาวน์การโจมตีแต่ละครั้ง
    private float lastAttackTime = 0f;
    public LayerMask attackableLayer;       // เลเยอร์ของเป้าหมายที่โจมตีได้ (เช่น Enemy)
    private Transform currentAttackTarget;  // เป้าหมายที่กำลังจะตี

    public void SetTask(GhostBuilding target)
    {
        currentTargetGhost = target;
    }

    public void StopTask()
    {
        if (currentTargetGhost != null)
        {
            currentTargetGhost.OnUnitExit(this);
            currentTargetGhost = null;
        }
    }

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        if (currentBehavior == UnitBehavior.Moving && !agent.pathPending)
        {
            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                if (targetTask != null)
                {
                    targetTask.OnUnitInteract(this);

                    if (targetTask is GhostBuilding ghost)
                    {
                        SetTask(ghost);
                    }
                    targetTask = null;
                }

                // 🟢 เมื่องานที่สั่งเสร็จสิ้น ให้ปลดล็อคสถานะ Manual
                isManualTask = false;
                currentBehavior = UnitBehavior.Idle;
            }
        }

        HandleCombatBehavior();

        // 🟢 2. ระบบ AI อัจฉริยะ Auto Scan: จะทำงานก็ต่อเมื่อ [ว่างจริง] [ไม่ได้ถือของ] และ [ไม่ได้ติดงานค้างคาจากผู้เล่น (isManualTask = false)] เท่านั้น!
        if (currentBehavior == UnitBehavior.Idle && !isCarrying && !isManualTask)
        {
            scanCooldown -= Time.deltaTime;
            if (scanCooldown <= 0f)
            {
                TryFindAndExecuteNearbyTask();
                scanCooldown = 1f;
            }
        }



        // 3. ระบบวาดเชือก (Logistics)
        if (rope != null && rope.enabled)
        {
            if (isCarrying && draggedVisualTarget != null)
            {
                rope.SetPosition(0, transform.position);
                rope.SetPosition(1, draggedVisualTarget.position);
            }
            else if (targetTask != null)
            {
                rope.SetPosition(0, transform.position);
                rope.SetPosition(1, targetTask.GetInteractionPoint());
            }
        }
    }

    public void SetSelected(bool value)
    {
        isSelected = value;
        if (selectionCircle != null)
            selectionCircle.SetActive(value);
    }

    public void MoveTo(Vector3 position, ITaskable targetTask = null)
    {
        if (agent.isActiveAndEnabled) agent.isStopped = false;

        agent.SetDestination(position);
        currentBehavior = UnitBehavior.Moving;
        StopTask();

        this.targetTask = targetTask;

        // 🟢 ถ้ามีคนสั่ง (ไม่ว่าจะคลิกเดินหรือคลิกที่ตึก) ให้ตีตราว่าเป็นงานบังคับ (Manual) ห้าม AI ออโต้เปลี่ยนงานเอง
        if (targetTask != null)
        {
            isManualTask = true;
        }

        if (targetTask is GhostBuilding ghost)
        {
            SetTask(ghost);
        }
    }


    public void StartInteract(Vector3 targetPos)
    {
        currentBehavior = UnitBehavior.Interacting;
        agent.SetDestination(targetPos);
    }

    private void OnTriggerEnter(Collider other)
    {
        ITaskable task = other.GetComponentInParent<ITaskable>();

        // 🟢 แก้: ปลดล็อคเงื่อนไขกลับมาให้ "เดินชนปุ๊บ ทำงานปั๊บ" เหมือนเดิม (แต่ต้องไม่ได้แบกของอยู่)
        if (task != null && !isCarrying)
        {
            // หยุดงานเก่าก่อนที่จะไปรับงานใหม่ที่เดินชน
            if (targetTask != task) StopTask();

            agent.isStopped = true;
            task.OnUnitInteract(this);

            // 🟢 สำคัญมาก: ถ้าเดินชนตึก ต้องจำไว้ด้วยว่าทำตึกนี้อยู่ 
            // ไม่งั้นเวลาเดินหนีออกมา OnUnitExit จะไม่ทำงาน แล้วตึกจะบัคสร้างเองต่อ!
            if (task is GhostBuilding ghost)
            {
                SetTask(ghost);
            }

            targetTask = null; // เคลียร์เป้าหมายเดิน
            currentBehavior = UnitBehavior.Idle;
        }
    }


    #region Auto Task Scanning AI
    private void TryFindAndExecuteNearbyTask()
    {
        // กวาดหา Collider ในรัศมีรอบตัว
        Collider[] hits = Physics.OverlapSphere(transform.position, autoTaskScanRadius);
        ITaskable nearestTask = null;
        float minDst = Mathf.Infinity;

        foreach (var hit in hits)
        {
            ITaskable task = hit.GetComponentInParent<ITaskable>();
            if (task != null)
            {
                // เช็คว่าเป็นงานที่ทำได้จริงไหม (เช่น แปลงผักที่ต้องการปุ๋ย/รดน้ำ หรือตึกที่กำลังสร้าง)
                // ตรงนี้เราเช็คระยะทางหาตัวที่ใกล้ที่สุด
                float dst = Vector3.Distance(transform.position, hit.transform.position);
                if (dst < minDst)
                {
                    minDst = dst;
                    nearestTask = task;
                }
            }
        }

        // ถ้าเจองานใกล้ๆ และยูนิตว่าง สั่งให้เดินไปทำอัตโนมัติทันที!
        if (nearestTask != null)
        {
            // ถ้าเป็นแปลงผัก ให้เช็คสถานะก่อนว่ามันต้องการความช่วยเหลือจริงๆ ไหม (ไม่ใช่แปลงว่างเปล่า)
            if (nearestTask is CropPlots plot)
            {
                if (plot.currentStage == CropStage.NeedsFertilizer || plot.currentStage == CropStage.NeedsWater)
                {
                    MoveTo(plot.GetInteractionPoint(), plot);
                    Debug.Log($"🤖 [Smart AI]: {name} เจองานแปลงผัก เดินไปจัดการเองออโต้!");
                }
            }
            else if (nearestTask is GhostBuilding)
            {
                // ถ้าเป็นตึกกำลังสร้าง เดินเข้าไปช่วยสร้างเลย
                MoveTo(nearestTask.GetInteractionPoint(), nearestTask);
                Debug.Log($"🤖 [Smart AI]: {name} เดินไปช่วยสร้างตึกออโต้!");
            }
        }
    }
    #endregion

    public void ResetUnitState()
    {
        isCarrying = false;
        carriedItem = null;
        carriedAmount = 0;
        draggedVisualTarget = null;
        isManualTask = false; // 🟢 ล้างล็อคงาน

        if (rope != null) rope.enabled = false;

        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.ResetPath();
            agent.isStopped = false;
        }

        currentBehavior = UnitBehavior.Idle;
        StopAllCoroutines();
    }
    #region Resource Gathering
    public void StartDragging(Transform targetNode, Transform vault)
    {
        agent.isStopped = false;
        agent.enabled = true;
        isCarrying = true;

        draggedVisualTarget = targetNode;
        targetTask = null;

        if (rope != null) rope.enabled = true;

        currentBehavior = UnitBehavior.Moving;
        agent.SetDestination(vault.position);

        StopAllCoroutines();
        StartCoroutine(CheckArrivalAtVault(vault));
    }

    public void DropItemAtVault()
    {
        Debug.Log("ส่งเสดแล้วว รอ ลบข้อมูล");

        if (draggedVisualTarget != null)
        {
            GatheringBase gatBase = draggedVisualTarget.GetComponent<GatheringBase>();
            if (gatBase != null)
            {
                gatBase.OnUnitSentResourced();
            }
        }
        else
        {
            ResetUnitState();
        }
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
    #region Farming smart behavior
    // 🟢 ฟังก์ชันสั่งให้วิ่งไปหยิบของที่โกดัง แล้วกลับมาทำงานเดิมที่ค้างอยู่
    public void GoFetchItemAndReturn(SO_ItemData itemData, ITaskable ultimateTask)
    {
        pendingTask = ultimateTask;
        itemToFetchData = itemData;

        // 🟢 เปลี่ยนสถานะเป็น Moving ทันที และเปิดใช้ระบบล็อค Manual เพื่อไม่ให้ระบบ Auto Scan มากวนใจ
        currentBehavior = UnitBehavior.Moving;
        isManualTask = true;

        Transform nearestVault = FindNearestVault();

        if (nearestVault != null && itemToFetchData != null)
        {
            Debug.Log($"{name} กำลังวิ่งไปเบิก {itemToFetchData.itemName} ที่โกดัง...");
            MoveTo(nearestVault.position);
            StartCoroutine(WaitUntilReachVaultAndReturn(nearestVault));
        }
        else
        {
            Debug.LogWarning("ไม่พบโกดัง หรือไม่ได้กำหนดข้อมูลไอเทมที่จะเบิก!");
            currentBehavior = UnitBehavior.Idle;
            isManualTask = false;
        }
    }

    IEnumerator WaitUntilReachVaultAndReturn(Transform vault)
    {
        // รอจนกว่าจะเดินถึงโกดัง
        while (agent.pathPending || agent.remainingDistance > agent.stoppingDistance)
        {
            yield return null;
        }

        // 3. หักของออกจาก Inventory กลาง และเอามาถือไว้
        if (ResourceInventory.Instance.HasResource(itemToFetchData.itemName, 1))
        {
            carriedItem = ResourceInventory.Instance.ConsumeAndGetReturn(itemToFetchData.itemName, 1);

            // จำลองการถือของ
            isCarrying = true;
            // ดึงข้อมูลไอเทมมาใส่ carriedItem (สมมติว่าคุณมีฟังก์ชันค้นหาไอเทมใน Database)



            Debug.Log($"{name} เบิก {itemToFetchKey} มาแล้ว กำลังเดินกลับไปที่แปลงผัก!");

            // 4. เดินกลับไปทำภารกิจเดิมที่จำไว้
            if (pendingTask != null)
            {
                MoveTo(pendingTask.GetInteractionPoint(), pendingTask);
                pendingTask = null;
            }
        }
        else
        {
            Debug.LogWarning($"โกดังไม่มี {itemToFetchKey} ให้เบิก!");
            currentBehavior = UnitBehavior.Idle;
        }
    }

    // ฟังก์ชันช่วยหาโกดัง (เผื่อคุณไม่มีใน UnitBase ให้ก๊อปไปวางไว้ใน UnitBase ด้วยครับ)
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

    #region Combat System (Attack with Physics OverlapSphere)
    private void HandleCombatBehavior()
    {
        // ถ้ากำลังลากของหรือทำงานสำคัญอยู่ ข้ามการต่อสู้ชั่วคราว
        if (isCarrying) return;

        // ถ้าไม่มีเป้าหมายโจมตี ให้ลองสแกนหาศัตรูรอบตัวในรัศมี autoTaskScanRadius
        if (currentAttackTarget == null)
        {
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, autoTaskScanRadius, attackableLayer);
            float nearestDist = Mathf.Infinity;
            Transform bestTarget = null;

            foreach (var hit in hitColliders)
            {
                // เช็ค Tag ให้ชัวร์ว่าเป็น Enemy ตามต้องการ
                if (hit.CompareTag("Enemy"))
                {
                    float dist = Vector3.Distance(transform.position, hit.transform.position);
                    if (dist < nearestDist)
                    {
                        nearestDist = dist;
                        bestTarget = hit.transform;
                    }
                }
            }

            if (bestTarget != null)
            {
                currentAttackTarget = bestTarget;
                currentBehavior = UnitBehavior.Attack;
                Debug.Log($"⚔️ [Combat]: {name} พบศัตรู {currentAttackTarget.name} กำลังพุ่งเข้าโจมตี!");
            }
        }

        // ถ้ามีเป้าหมายศัตรูแล้ว
        if (currentAttackTarget != null)
        {
            // ถ้าศัตรูตายหรือหายไปจากฉาก ให้รีเซ็ตเป้าหมาย
            if (currentAttackTarget == null || !currentAttackTarget.gameObject.activeInHierarchy)
            {
                currentAttackTarget = null;
                currentBehavior = UnitBehavior.Idle;
                if (agent.isActiveAndEnabled) agent.isStopped = false;
                return;
            }

            float distanceToEnemy = Vector3.Distance(transform.position, currentAttackTarget.position);

            // ถ้ายังเดินไปไม่ถึงระยะโจมตี ให้สั่ง NavMesh วิ่งไล่ตามศัตรู
            if (distanceToEnemy > attackRange)
            {
                currentBehavior = UnitBehavior.Attack;
                if (agent.isActiveAndEnabled)
                {
                    agent.isStopped = false;
                    agent.SetDestination(currentAttackTarget.position);
                }
            }
            else
            {
                // ถึงระยะโจมตีแล้ว! หยุดเดินแล้วฟัน
                if (agent.isActiveAndEnabled) agent.isStopped = true;

                // เช็คคูลดาวน์การโจมตี
                if (Time.time >= lastAttackTime + attackCooldown)
                {
                    PerformAttack();
                    lastAttackTime = Time.time;
                }
            }
        }
    }

    private void PerformAttack()
    {
        if (currentAttackTarget == null) return;

        // หันหน้าไปหาศัตรู
        Vector3 direction = (currentAttackTarget.position - transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        // ดึงคอมโพเนนต์ CharacterStats ของศัตรูมาตัดเลือด
        CharacterStats enemyStats = currentAttackTarget.GetComponent<CharacterStats>();
        if (enemyStats != null)
        {
            // สมมติใช้ค่า gatheringPower มาคิดเป็นดาเมจ หรือกำหนดค่าดาเมจแยกต่างหาก (เช่น 10 หน่วย)
            int damageToDeal = gatheringPower > 0 ? gatheringPower : 10;

            // สมมติว่ามีฟังก์ชัน TakeDamage หรือปรับค่า currentHP โดยตรงใน CharacterStats ของคุณ
            // enemyStats.TakeDamage(damageToDeal); 

            // ตัวอย่างการลดเลือดผ่าน CharacterStats (ปรับชื่อฟังก์ชันตามสคริปต์จริงของคุณถ้ามี เช่น TakeDamage)
            enemyStats.currentHP -= damageToDeal;
            if (enemyStats.currentHP < 0) enemyStats.currentHP = 0;

            Debug.Log($"🗡️ {name} โจมตี {currentAttackTarget.name} สร้างความเสียหาย {damageToDeal}! เลือดศัตรูเหลือ: {enemyStats.currentHP}");

            // ถ้าเลือดศัตรูหมด ให้ลบเป้าหมายทิ้งแล้วกลับสู่สถานะปกติ
            if (enemyStats.currentHP <= 0)
            {
                Debug.Log($"💀 ศัตรู {currentAttackTarget.name} ถูกกำจัดแล้ว!");
                currentAttackTarget = null;
                currentBehavior = UnitBehavior.Idle;
                if (agent.isActiveAndEnabled) agent.isStopped = false;
            }
        }
        else
        {
            Debug.LogWarning($"⚠️ เป้าหมาย {currentAttackTarget.name} ไม่มีสคริปต์ CharacterStats!");
        }
    }
    #endregion

}