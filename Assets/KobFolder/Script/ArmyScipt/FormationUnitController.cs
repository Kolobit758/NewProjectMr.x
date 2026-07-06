using UnityEngine;
using UnityEngine.AI;

public enum UnitState { Idle, MovingToFormation, HoldAndGuard, Attacking, Fleeing }

public class FormationUnitController : MonoBehaviour
{
    [Header("State Indicator")]
    public UnitState currentState = UnitState.Idle;

    [Header("Guard & Combat Settings")]
    public float guardRadius = 4f;
    public float attackRange = 1.5f;
    public LayerMask enemyLayer;
    [SerializeField] private float attackCooldown = 1.2f; // ระยะเวลาห่างระหว่างการตีแต่ละครั้ง (วินาที)

    [Header("Flee Settings")]
    public float fleeRadius = 10f;
    public float fleeDuration = 6f;
    public float fleeSpeedMultiplier = 1.4f;

    [HideInInspector] public float minSpacingDistance;
    [HideInInspector] public float slowDownRadius;

    private NavMeshAgent agent;
    private Vector3 targetFormationPosition;
    private Transform currentTargetEnemy;
    private AnimalStatsManager statsManager;
    private Animator anim; // 🆕 ดึง Animator ของตัวทหารมาใช้เล่นท่าตี

    private float fleeEndTime;
    private bool isFleeing;
    private float nextAttackTime; // 🆕 จับเวลาสำหรับคูลดาวน์การตีรอบถัดไป

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        statsManager = GetComponent<AnimalStatsManager>();
        anim = GetComponent<Animator>(); // หา Component Animator ในตัว

        if (statsManager != null && statsManager.Stats != null)
        {
            statsManager.Stats.OnHPChanged += HandleHPChanged;
        }
    }

    void OnDestroy()
    {
        if (statsManager != null && statsManager.Stats != null)
        {
            statsManager.Stats.OnHPChanged -= HandleHPChanged;
        }
    }

    void Update()
    {
        if (agent == null) return;

        switch (currentState)
        {
            case UnitState.Idle:
                break;

            case UnitState.MovingToFormation:
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                {
                    currentState = UnitState.HoldAndGuard;
                }
                break;

            case UnitState.HoldAndGuard:
                LookForEnemies();
                break;

            case UnitState.Attacking:
                ExecuteAttackLogic();
                break;

            case UnitState.Fleeing:
                ProcessFleeing();
                break;
        }
    }

    public void SetFormationTarget(Vector3 worldPos)
    {
        targetFormationPosition = worldPos;

        if (agent != null)
        {
            agent.isStopped = false;
            agent.SetDestination(targetFormationPosition);
        }

        currentState = UnitState.MovingToFormation;
        currentTargetEnemy = null;
    }

    private void LookForEnemies()
    {
        // 🛠️ แก้ไข: เปลี่ยนจุดศูนย์กลางในการสแกนหาศัตรูให้เริ่มจาก "รอบตัวทหารเอง" (transform.position) เสมอ
        // วิธีนี้จะทำให้ทหารมองเห็นศัตรูที่อยู่ข้างหน้าตัวมันทันที ไม่ว่าจะเดินอยู่หรือยืนนิ่ง
        Vector3 scanPosition = transform.position;

        Collider[] enemiesInRange = Physics.OverlapSphere(scanPosition, guardRadius, enemyLayer);

        if (enemiesInRange.Length > 0)
        {
            // ล็อกเป้าหมายศัตรูตัวแรกที่เจอ
            currentTargetEnemy = enemiesInRange[0].transform;
            currentState = UnitState.Attacking;
        }
        else
        {
            // ถ้าไม่มีศัตรูรอบตัว และตัวมันยังเดินไปไม่ถึงจุดหมาย ให้มันเดินหน้าต่อไป
            if (targetFormationPosition != Vector3.zero &&
                Vector3.Distance(transform.position, targetFormationPosition) > agent.stoppingDistance)
            {
                agent.isStopped = false;
                agent.SetDestination(targetFormationPosition);
            }
        }
    }

    private void ProcessFleeing()
    {
        if (Time.time >= fleeEndTime)
        {
            StopFleeing();
            return;
        }

        if (agent != null && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            Vector3 fleeTarget = GetRandomFleePoint();
            if (fleeTarget != Vector3.zero)
            {
                agent.isStopped = false;
                agent.SetDestination(fleeTarget);
            }
        }
    }

    private Vector3 GetRandomFleePoint()
    {
        Vector3 randomDirection = Random.insideUnitSphere * fleeRadius;
        randomDirection += transform.position;
        if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, fleeRadius, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return Vector3.zero;
    }

    private void ExecuteAttackLogic()
    {
        if (currentTargetEnemy == null || !currentTargetEnemy.gameObject.activeSelf)
        {
            ReturnToFormation();
            return;
        }

        float distanceToEnemyFromAnchor = Vector3.Distance(targetFormationPosition, currentTargetEnemy.position);
        float distanceToEnemyFromSelf = Vector3.Distance(transform.position, currentTargetEnemy.position);

        // ถ้าศัตรูวิ่งหนีหลุดระยะคุม (Guard Radius) ของจุดตรึงทัพ ให้ทหารถอยกลับฐาน
        if (distanceToEnemyFromAnchor > guardRadius)
        {
            ReturnToFormation();
            return;
        }

        if (agent != null)
        {
            if (distanceToEnemyFromSelf <= attackRange)
            {
                agent.isStopped = true;

                // หันหน้ายูนิตไปทางศัตรูให้ตรงก่อนตี
                Vector3 lookDir = (currentTargetEnemy.position - transform.position).normalized;
                lookDir.y = 0;
                if (lookDir != Vector3.zero) transform.rotation = Quaternion.LookRotation(lookDir);

                // 🛠️ ลอจิกคูลดาวน์สำหรับการโจมตีจริง
                if (Time.time >= nextAttackTime)
                {
                    nextAttackTime = Time.time + attackCooldown; // ล็อกเวลาสำหรับฮิตต่อไป

                    if (statsManager != null && statsManager.ActiveUnitData != null)
                    {
                        int currentDmg = (int)statsManager.ActiveUnitData.attackDamage;
                        Debug.Log($"{gameObject.name} หวดเป้าหมาย {currentTargetEnemy.name} ดาเมจ: {currentDmg}");

                        // 💥 สั่งทำดาเมจจริงเข้าสคริปต์เลือดของศัตรู (CharacterStats ของศัตรู)
                        CharacterStats enemyStats = currentTargetEnemy.GetComponent<CharacterStats>();
                        if (enemyStats != null)
                        {
                            enemyStats.TakeDamage(currentDmg,transform.position);
                        }
                    }

                    // 🎬 สั่งให้แอนิเมชันทหารเล่นท่าโจมตี (ตั้งค่า Trigger ชื่อ "Attack" ใน Animator ของทหารด้วยนะครับ)
                    if (anim != null)
                    {
                        anim.SetTrigger("Attack");
                    }
                }
            }
            else
            {
                agent.isStopped = false;
                agent.SetDestination(currentTargetEnemy.position);
            }
        }
    }

    private void ReturnToFormation()
    {
        currentTargetEnemy = null;
        if (agent != null)
        {
            agent.isStopped = false;
            agent.SetDestination(targetFormationPosition);
        }
        currentState = UnitState.MovingToFormation;
    }

    private void StopFleeing()
    {
        isFleeing = false;
        if (agent != null)
        {
            agent.speed /= fleeSpeedMultiplier;
            agent.isStopped = false;
            agent.SetDestination(transform.position);
        }
        currentState = UnitState.Idle;
    }

    private void HandleHPChanged()
    {
        if (statsManager == null || statsManager.Stats == null) return;
        if (statsManager.Stats.currentHP > 0) return;

        UnitInstance instance = statsManager.ActiveUnitData;
        if (instance != null)
        {
            string id = instance.uniqueId;
            Debug.Log("This animal id : " + id);
            if (AnimalInventory.Instance != null) AnimalInventory.Instance.RemoveUnitFromInventory(id);
            if (TeamFormationManager.Instance != null) TeamFormationManager.Instance.RemoveUnitFromFormationById(id);
            if (ArmyController.Instance != null) ArmyController.Instance.UnregisterArmyUnit(agent);
        }

        StartFleeing();
    }

    private void StartFleeing()
    {
        if (agent == null) return;

        if (!isFleeing)
        {
            agent.speed *= fleeSpeedMultiplier;
            isFleeing = true;
        }

        currentState = UnitState.Fleeing;
        fleeEndTime = Time.time + fleeDuration;
        Vector3 fleeTarget = GetRandomFleePoint();
        if (fleeTarget != Vector3.zero)
        {
            agent.isStopped = false;
            agent.SetDestination(fleeTarget);
        }
    }
    // 🛠️ เพิ่มฟังก์ชันนี้เข้าไปใน FormationUnitController เพื่อใช้รีเซ็ตสเตตตอนกด C
    public void ResetToIdleState()
    {
        currentState = UnitState.Idle;
        currentTargetEnemy = null;
        if (agent != null) agent.isStopped = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(targetFormationPosition, guardRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, fleeRadius);
    }
}