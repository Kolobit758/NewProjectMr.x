using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(CharacterStats))]
[RequireComponent(typeof(HitFeedback))]
public class EnemyController : MonoBehaviour
{
    public SO_EnemyData enemyData;

    private CharacterStats characterStats;
    private NavMeshAgent agent;
    private Collider selfCollider; // 🟢 เก็บ collider ตัวเองไว้เผื่อใช้คำนวณระยะแบบ closest point ในอนาคต

    [Header("Targeting & Radius Scan")]
    public float scanRadius = 15f;
    public float unitInterruptRange = 6f;
    public Transform currentTarget;
    private Transform primaryTarget;
    private bool isDistractedByUnit = false;

    private float lastAttackTime;
    private float reevaluationTimer = 0f;

    // 🟢 ค่าเหล่านี้ตอนนี้ "ไม่ตั้งตายตัว" อีกแล้ว จะถูก override จาก enemyData ใน Start()
    private float cooldownDuration = 0.5f;
    private float attackYTolerance = 3f;

    void Awake()
    {
        characterStats = GetComponent<CharacterStats>();
        agent = GetComponent<NavMeshAgent>();
        selfCollider = GetComponent<Collider>();
    }

    void Start()
    {
        if (enemyData != null)
        {
            characterStats.InitializeStats((int)enemyData.maxHealth, (int)enemyData.maxHealth);
            if (agent != null) agent.speed = enemyData.moveSpeed;

            // 🟢 [FIX 1] ดึง attack speed จาก SO แทนค่าตายตัว ตัวเล็กตีถี่ ตัวใหญ่ตีช้าได้ตามต้องการ
            cooldownDuration = enemyData.attackCooldown;

            // 🟢 [FIX 2] ดึง yTolerance จาก SO ตามขนาดตัว กันปัญหาตัวใหญ่ตีไม่เข้า
            attackYTolerance = enemyData.attackYTolerance;
        }

        FindBestTargetInRadius();
    }

    void Update()
    {
        if (enemyData == null) return;
        if (characterStats.currentHP <= 0) return;

        reevaluationTimer += Time.deltaTime;
        if (reevaluationTimer >= 0.5f)
        {
            reevaluationTimer = 0f;
            DynamicTargetReevaluation();
        }

        if (!IsTargetStillValid(currentTarget))
        {
            if (isDistractedByUnit)
            {
                isDistractedByUnit = false;
                currentTarget = IsTargetStillValid(primaryTarget) ? primaryTarget : null;
            }
            else
            {
                currentTarget = null;
            }

            if (currentTarget == null)
            {
                FindBestTargetInRadius();
            }

            if (currentTarget == null)
            {
                if (agent != null) agent.isStopped = true;
                return;
            }
        }

        // 🟢 [FIX 3] เปลี่ยนมาเช็คระยะแบบ collider-to-collider (closest point) แทน pivot-to-pivot ตรงๆ
        bool inRange = IsInAttackRange(currentTarget, enemyData.attackRange, attackYTolerance);

        if (inRange)
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
            }

            if (Time.time >= lastAttackTime + cooldownDuration)
            {
                AttackCurrentTarget();
                lastAttackTime = Time.time;
            }
        }
        else
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(currentTarget.position);
            }
        }
    }

    bool IsTargetStillValid(Transform target)
    {
        if (target == null || !target.gameObject.activeInHierarchy) return false;

        if (target.TryGetComponent<CropPlots>(out var plot) && plot.currentStage == CropStage.Empty)
            return false;

        if (target.TryGetComponent<CharacterStats>(out var stats) && stats.currentHP <= 0)
            return false;

        return true;
    }

    void FindBestTargetInRadius()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, scanRadius);

        Transform nearestDummy = null;
        float minDummyDst = Mathf.Infinity;

        Transform nearestBuilding = null;
        float minBuildingDst = Mathf.Infinity;

        Transform nearestCrop = null;
        float minCropDst = Mathf.Infinity;

        Transform nearestUnit = null;
        float minUnitDst = Mathf.Infinity;

        foreach (var hit in hits)
        {
            BuildingHealth building = hit.GetComponentInParent<BuildingHealth>();

            bool isDummy = hit.CompareTag("Dummy");

            if (isDummy)
            {
                Transform targetT = building != null ? building.transform : hit.transform;
                float dst = Vector3.Distance(transform.position, targetT.position);
                if (dst < minBuildingDst)
                {
                    minDummyDst = dst;
                    nearestDummy = targetT;
                }
                continue;
            }

            bool isVault = hit.CompareTag("Vault") || hit.GetComponentInParent<ResourceVault>() != null;

            if (building != null || isVault)
            {
                Transform targetT = building != null ? building.transform : hit.transform;
                float dst = Vector3.Distance(transform.position, targetT.position);
                if (dst < minBuildingDst)
                {
                    minBuildingDst = dst;
                    nearestBuilding = targetT;
                }
                continue;
            }

            CropPlots plot = hit.GetComponentInParent<CropPlots>();
            if (plot != null && plot.currentStage != CropStage.Empty)
            {
                Transform targetT = plot.transform;
                float dst = Vector3.Distance(transform.position, targetT.position);
                if (dst < minCropDst)
                {
                    minCropDst = dst;
                    nearestCrop = targetT;
                }
                continue;
            }

            UnitBase unit = hit.GetComponentInParent<UnitBase>();
            bool isMyUnit = hit.CompareTag("Unit") || unit != null;
            if (isMyUnit)
            {
                Transform targetT = unit != null ? unit.transform : hit.transform;
                float dst = Vector3.Distance(transform.position, targetT.position);
                if (dst < minUnitDst)
                {
                    minUnitDst = dst;
                    nearestUnit = targetT;
                }
            }
        }

        if (nearestBuilding != null)
        {
            primaryTarget = nearestBuilding;
        }
        else if (nearestCrop != null)
        {
            primaryTarget = nearestCrop;
        }
        else if (nearestUnit != null)
        {
            primaryTarget = nearestUnit;
        }
        else
        {
            FindAbsoluteNearestTargetGlobal();
            return;
        }

        isDistractedByUnit = false;
        currentTarget = primaryTarget;
    }

    void FindAbsoluteNearestTargetGlobal()
    {
        float minDst = Mathf.Infinity;
        Transform nearest = null;

        GameObject[] buildings = GameObject.FindGameObjectsWithTag("Building");
        foreach (var b in buildings)
        {
            float dst = Vector3.Distance(transform.position, b.transform.position);
            if (dst < minDst)
            {
                minDst = dst;
                nearest = b.transform;
            }
        }

        GameObject[] vaults = GameObject.FindGameObjectsWithTag("Vault");
        foreach (var v in vaults)
        {
            float dst = Vector3.Distance(transform.position, v.transform.position);
            if (dst < minDst) { minDst = dst; nearest = v.transform; }
        }

        if (nearest != null)
        {
            primaryTarget = nearest;
            currentTarget = nearest;
            isDistractedByUnit = false;
            return;
        }

        CropPlots[] allPlots = Object.FindObjectsByType<CropPlots>(FindObjectsSortMode.None);
        foreach (var plot in allPlots)
        {
            if (plot == null || plot.currentStage == CropStage.Empty) continue;

            Transform targetT = plot.transform;
            float dst = Vector3.Distance(transform.position, targetT.position);
            if (dst < minDst)
            {
                minDst = dst;
                nearest = targetT;
            }
        }

        primaryTarget = nearest;
        currentTarget = nearest;
        isDistractedByUnit = false;
    }

    void DynamicTargetReevaluation()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, unitInterruptRange);

        Transform nearestUnit = null;
        float minUnitDst = Mathf.Infinity;

        foreach (var hit in hits)
        {
            bool isMyUnit = hit.CompareTag("Unit") || hit.GetComponentInParent<UnitBase>() != null;
            if (!isMyUnit) continue;

            float dst = Vector3.Distance(transform.position, hit.transform.position);
            if (dst < minUnitDst)
            {
                minUnitDst = dst;
                nearestUnit = hit.GetComponentInParent<UnitBase>() != null
                    ? hit.GetComponentInParent<UnitBase>().transform
                    : hit.transform;
            }
        }

        if (nearestUnit != null)
        {
            if (!isDistractedByUnit || currentTarget != nearestUnit)
            {
                Debug.Log($"⚡ [Enemy] มี {nearestUnit.name} เดินเข้ามาขวางทาง! หันไปฟันก่อนชั่วคราว");
            }
            isDistractedByUnit = true;
            currentTarget = nearestUnit;
            agent.isStopped = false;
        }
        else if (isDistractedByUnit)
        {
            isDistractedByUnit = false;
            currentTarget = IsTargetStillValid(primaryTarget) ? primaryTarget : null;
            agent.isStopped = false;

            if (currentTarget == null)
                FindBestTargetInRadius();
            else
                Debug.Log($"↩️ [Enemy] ยูนิตที่ขวางถอยไปแล้ว กลับไปบุก {currentTarget.name} ต่อ!");
        }
    }

    void AttackCurrentTarget()
    {
        if (currentTarget == null) return;

        int damageInt = Mathf.RoundToInt(enemyData.attackDamage);

        if (currentTarget.TryGetComponent<BuildingHealth>(out var buildingHealth))
        {
            Debug.Log($"🏢 [Enemy]: {enemyData.enemyName} กำลังถล่มสิ่งก่อสร้าง! ดาเมจ: {damageInt}");
            if (currentTarget.TryGetComponent<CharacterStats>(out var buildingStats))
            {
                buildingStats.TakeDamage(damageInt, transform.position);
            }
        }
        else if (currentTarget.TryGetComponent<UnitBase>(out var unitBase)
                 && currentTarget.TryGetComponent<CharacterStats>(out var targetStats)
                 && !currentTarget.CompareTag("Enemy"))
        {
            Debug.Log($"⚔️ [Enemy]: {enemyData.enemyName} โจมตียูนิตฝ่ายเรา! ดาเมจ: {damageInt}");
            targetStats.TakeDamage(damageInt, transform.position);
        }
    }

    public void TakeDamage(int rawDamage, FactionType attackerType, Vector3 attackerPos)
    {
        if (enemyData == null) return;

        float multiplier = CombatSystem.GetDamageMultiplier(attackerType, enemyData.factionType);
        int finalDamage = Mathf.RoundToInt(rawDamage * multiplier);

        characterStats.TakeDamage(finalDamage, attackerPos);

        if (characterStats.currentHP <= 0)
        {
            Die();
            return;
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, scanRadius);
        Transform attackerTransform = null;
        float minDst = Mathf.Infinity;

        foreach (var hit in hits)
        {
            bool isMyUnit = hit.CompareTag("Unit") || hit.GetComponentInParent<UnitBase>() != null;
            if (!isMyUnit) continue;

            Transform unitTransform = hit.GetComponentInParent<UnitBase>() != null
                ? hit.GetComponentInParent<UnitBase>().transform
                : hit.transform;

            float dst = GetHorizontalDistance(transform.position, hit.transform.position);
            if (dst < minDst)
            {
                minDst = dst;
                attackerTransform = unitTransform;
            }
        }

        if (attackerTransform != null)
        {
            isDistractedByUnit = true;
            currentTarget = attackerTransform;
            agent.isStopped = false;
            Debug.Log($"🔄 [Enemy] โดนตี! หันมาสวนกลับที่ {attackerTransform.name} ก่อน!");
        }
    }

    void Die()
    {
        Debug.Log($"💀 [Enemy]: {enemyData.enemyName} ถูกกำจัดเรียบร้อย!");

        if (enemyData != null && enemyData.dropItem != null && ResourceInventory.Instance != null)
        {
            int dropAmount = Random.Range(enemyData.minDropAmount, enemyData.maxDropAmount + 1);
            ResourceInventory.Instance.AddResource(enemyData.dropItem, dropAmount);

            if (FloatingTextManager.Instance != null)
            {
                string lootText = $"+{dropAmount} {enemyData.dropItem.itemName}";
                FloatingTextManager.Instance.ShowText(transform.position + Vector3.up * 1.5f, lootText, Color.green);
            }

            Debug.Log($"🎁 [Loot Drop]: ได้รับ {enemyData.dropItem.itemName} จำนวน {dropAmount} ชิ้น!");
        }

        if (agent != null) agent.enabled = false;
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
        Destroy(gameObject, 0.2f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, scanRadius);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, unitInterruptRange);
    }

    #region Hit Range
    private float GetHorizontalDistance(Vector3 a, Vector3 b)
    {
        Vector3 flatA = new Vector3(a.x, 0f, a.z);
        Vector3 flatB = new Vector3(b.x, 0f, b.z);
        return Vector3.Distance(flatA, flatB);
    }

    // 🟢 [FIX] เช็คระยะโจมตีจาก "จุดที่ใกล้ที่สุดบน collider ของเป้าหมาย" แทน pivot-to-pivot
    // แก้ปัญหามอนสเตอร์ตัวใหญ่/เล็ก pivot ไม่ตรงกัน ตีไม่เข้าหรือตีทะลุ
    bool IsInAttackRange(Transform target, float attackRange, float yTolerance)
    {
        Vector3 targetPoint = target.position;

        if (target.TryGetComponent<Collider>(out var targetCol))
        {
            targetPoint = targetCol.ClosestPoint(transform.position);
        }
        else
        {
            // ถ้าไม่มี collider บน root ลองหาใน children (เผื่อ collider อยู่บนลูก)
            Collider childCol = target.GetComponentInChildren<Collider>();
            if (childCol != null)
            {
                targetPoint = childCol.ClosestPoint(transform.position);
            }
        }

        float horizontalDst = GetHorizontalDistance(transform.position, targetPoint);
        float yDiff = Mathf.Abs(transform.position.y - targetPoint.y);
        return horizontalDst <= attackRange && yDiff <= yTolerance;
    }
    #endregion
}