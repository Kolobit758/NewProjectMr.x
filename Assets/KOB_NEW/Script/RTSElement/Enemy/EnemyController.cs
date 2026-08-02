using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(CharacterStats))]
[RequireComponent(typeof(HitFeedback))]
public class EnemyController : MonoBehaviour
{
    public SO_EnemyData enemyData;

    private CharacterStats characterStats;
    private NavMeshAgent agent;

    [Header("Targeting & Radius Scan")]
    public float scanRadius = 15f;              // รัศมีในการกวาดหาเป้าหมายหลัก (ตึก/แปลงพืช) รอบตัว
    public float unitInterruptRange = 6f;       // 🟢 ถ้ายูนิตเข้ามาใกล้กว่านี้ ถือว่า "ขวางทาง" ให้หันไปฟันก่อน
    public Transform currentTarget;             // เป้าหมายที่กำลังตีอยู่ ณ ตอนนี้ (อาจเป็นยูนิตที่มาขวาง หรือเป้าหมายหลักก็ได้)
    private Transform primaryTarget;            // 🟢 เป้าหมายหลักที่ตั้งใจจะบุก (ตึก/โกดัง/แปลงพืช) จำไว้เพื่อกลับมาบุกต่อ
    private bool isDistractedByUnit = false;    // 🟢 สถานะตอนนี้กำลังโดนยูนิตขวางอยู่หรือเปล่า

    private float lastAttackTime;
    private float reevaluationTimer = 0f;
    public float cooldownDuration = 0.5f;

    void Awake()
    {
        characterStats = GetComponent<CharacterStats>();
        agent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        if (enemyData != null)
        {
            characterStats.InitializeStats((int)enemyData.maxHealth, (int)enemyData.maxHealth);
            if (agent != null) agent.speed = enemyData.moveSpeed;
        }

        FindBestTargetInRadius();
    }

    void Update()
    {
        if (enemyData == null) return;
        if (characterStats.currentHP <= 0) return;

        reevaluationTimer += Time.deltaTime;
        if (reevaluationTimer >= 0.5f) // เช็คถี่ขึ้นหน่อยเพื่อจับยูนิตที่เดินเข้ามาขวางไวๆ
        {
            reevaluationTimer = 0f;
            DynamicTargetReevaluation();
        }

        // 1. เช็คว่าเป้าหมายปัจจุบันยังใช้งานได้อยู่ไหม
        if (!IsTargetStillValid(currentTarget))
        {
            // ถ้าเป้าที่หายไปคือยูนิตที่มาขวาง (isDistractedByUnit) ให้กลับไปหาเป้าหมายหลักก่อน
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

        bool inRange = IsInAttackRange(transform.position, currentTarget.position, enemyData.attackRange);

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

    // 🩺 เช็คว่าเป้าหมายที่ถืออยู่ "ยังมีค่าให้ตี" จริงไหม
    bool IsTargetStillValid(Transform target)
    {
        if (target == null || !target.gameObject.activeInHierarchy) return false;

        if (target.TryGetComponent<CropPlots>(out var plot) && plot.currentStage == CropStage.Empty)
            return false;

        if (target.TryGetComponent<CharacterStats>(out var stats) && stats.currentHP <= 0)
            return false;

        return true;
    }

    // 🔍 หาเป้าหมายหลัก: 🏢 ตึก/โกดัง ➡️ แปลงพืช ➡️ ยูนิต
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
            // 🟢 ป้องกัน: ข้ามวัตถุลูกที่เป็น Trigger หรือไม่ใช่ตัวหลัก ถ้าจำเป็น
            // เช็คตึกจากแม่ข่าย

            BuildingHealth building = hit.GetComponentInParent<BuildingHealth>();

            bool isDummy = hit.CompareTag("Dummy");

            if (isDummy)
            {

                Transform targetT = building != null ? building.transform : hit.transform;
                float dst = Vector3.Distance(transform.position, targetT.position);
                if (dst < minBuildingDst)
                {
                    Debug.Log("อยู่ในระยะ ตี dummy");
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
                    Debug.Log("อยู่ในระยะ ตี vault");
                    minBuildingDst = dst;
                    nearestBuilding = targetT;
                }
                continue;
            }

            // 🟢 แปลงพืช: บังคับดึง Component จากแม่ข่าย และอ้างอิงไปที่ transform ของแม่ข่าย (CropPlots) เสมอ!
            CropPlots plot = hit.GetComponentInParent<CropPlots>();
            if (plot != null && plot.currentStage != CropStage.Empty)
            {
                Transform targetT = plot.transform; // ใช้ transform ของตัวแม่ข่ายที่เป็นเจ้าของสคริปต์เท่านั้น!
                float dst = Vector3.Distance(transform.position, targetT.position);
                if (dst < minCropDst)
                {
                    Debug.Log("อยู่ในระยะ ตี cropplots");
                    minCropDst = dst;
                    nearestCrop = targetT;
                }
                continue;
            }

            // ยูนิตฝ่ายเรา
            UnitBase unit = hit.GetComponentInParent<UnitBase>();
            bool isMyUnit = hit.CompareTag("Unit") || unit != null;
            if (isMyUnit)
            {
                Transform targetT = unit != null ? unit.transform : hit.transform;
                float dst = Vector3.Distance(transform.position, targetT.position);
                if (dst < minUnitDst)
                {
                    Debug.Log("อยู่ในระยะ ตี unit");
                    minUnitDst = dst;
                    nearestUnit = targetT;
                }
            }
        }

        // 🎯 ลำดับความสำคัญ: ตึก/โกดัง ➡️ แปลงพืช ➡️ ยูนิต
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

    // 🌍 ฟังก์ชันสำรองหากไม่เจออะไรในรัศมี ให้หากวาดทั้งแม็ป
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
                nearest = b.transform; // 🟢 ลบตัวที่เกินออก เหลือแค่นี้พอครับ
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

        // 🟢 แปลงพืชทั่วแม็ป: บังคับใช้ plot.transform ของแม่ข่ายเท่านั้น
        CropPlots[] allPlots = Object.FindObjectsByType<CropPlots>(FindObjectsSortMode.None);
        foreach (var plot in allPlots)
        {
            if (plot == null || plot.currentStage == CropStage.Empty) continue;

            Transform targetT = plot.transform; // อ้างอิงที่ Root ของแปลงผักเสมอ
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

    // ⚡ ระบบเช็คว่ามียูนิตเดิน "เข้ามาขวาง" ใกล้ตัวไหม ถ้าใช่ให้หันไปฟันก่อนชั่วคราว
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
            // 🟢 มียูนิตเข้ามาขวางในระยะประชิด ให้หันไปฟันก่อน (จำ primaryTarget เดิมไว้กลับมาบุกต่อ)
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
            // 🟢 ยูนิตที่มาขวางเดินออกไปพ้นระยะแล้ว (แต่ยังไม่ตาย) ให้กลับไปบุกเป้าหมายหลักต่อทันที
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

        // 🏢 เช็คตึกก่อน
        if (currentTarget.TryGetComponent<BuildingHealth>(out var buildingHealth))
        {
            Debug.Log($"🏢 [Enemy]: {enemyData.enemyName} กำลังถล่มสิ่งก่อสร้าง! ดาเมจ: {damageInt}");
            if (currentTarget.TryGetComponent<CharacterStats>(out var buildingStats))
            {
                buildingStats.TakeDamage(damageInt, transform.position);
            }
        }
        // 🌾 แล้วค่อยเช็คแปลงพืช
        else if (currentTarget.TryGetComponent<CropPlots>(out var plot))
        {
            Debug.Log($"🌾 [Enemy]: {enemyData.enemyName} กำลังแทะแปลงพืช! ดาเมจ: {damageInt}");
            plot.TakeDamageFromEnemy(damageInt);
        }
        // ⚔️ สุดท้ายค่อยเช็คยูนิตเรา (ต้องมีทั้ง UnitBase และ CharacterStats)
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

        // 🟢 โดนตีปุ๊บ = ถือว่ายูนิตนั้น "มาขวาง/รังแก" แล้ว สวนกลับ 100% ทันที
        // (ยังจำ primaryTarget เดิมไว้ พอฆ่ายูนิตนี้เสร็จจะกลับไปบุกต่อเอง)
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
    // เพิ่มฟังก์ชันนี้ในคลาส EnemyController
    #region Hit Range
    private float GetHorizontalDistance(Vector3 a, Vector3 b)
    {
        Vector3 flatA = new Vector3(a.x, 0f, a.z);
        Vector3 flatB = new Vector3(b.x, 0f, b.z);
        return Vector3.Distance(flatA, flatB);
    }

    bool IsInAttackRange(Vector3 selfPos, Vector3 targetPos, float attackRange, float yTolerance = 3f)
    {
        float horizontalDst = GetHorizontalDistance(selfPos, targetPos);
        float yDiff = Mathf.Abs(selfPos.y - targetPos.y);
        return horizontalDst <= attackRange && yDiff <= yTolerance;
    }
    #endregion
}