using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public enum ArmyCommandMode
{
    FollowFormation,   // เดินตามผู้เล่นในรูปแบบ Grid ปกติ
    CircleGuard,       // โหมดกด V: ล้อมรอบผู้เล่น
    LineFormation,     // โหมดกด B: จัดแถวหน้ากระดาน
    StayStill,         // โหมดกด C: นั่งรออยู่กับที่
    ManualMoveToPoint  // 🆕 โหมดกด X: วิ่งไปจุดที่สั่ง (ห้ามลูปดึงตัวกลับจนกว่าจะถึง)
}

public class ArmyController : MonoBehaviour
{
    public static ArmyController Instance { get; private set; }

    [Header("Formation Resources")]
    public CustomFormationData formationData;
    public List<NavMeshAgent> armyUnits = new List<NavMeshAgent>();
    public LayerMask groundLayer;

    [System.Serializable]
    public class ArmySlotAssignment
    {
        public int row;
        public int col;
        public UnitDataSO unitData;
        public UnitInstance customTestingInstance;
    }

    [Header("Dynamic Slot Assignments")]
    public List<ArmySlotAssignment> slotAssignments = new List<ArmySlotAssignment>();

    [Header("Preview Settings")]
    public GameObject previewMarkerPrefab;
    public List<GameObject> spawnedPreviewMarkers = new List<GameObject>();

    [Header("Agent Spacing & Guard Settings")]
    public float minSpacingDistance = 0.8f;
    public float slowDownRadius = 1.5f;
    public float unitGuardRadius = 5f;
    public float unitAttackRange = 1.5f;
    public LayerMask enemyLayer;
    public LayerMask unitLayer;

    [Header("Command State & Settings")]
    public ArmyCommandMode currentCommandMode = ArmyCommandMode.FollowFormation;
    [SerializeField] private float circleRadius = 3f;
    [SerializeField] private float lineSpacing = 1f;

    // ⚡ ปรับความถี่ให้เร็วขึ้นเป็น 0.1 วินาที เพื่อการตอบสนองการจัดแถวที่ไวขึ้น ไม่หน่วงช้า
    [SerializeField] private float updateInterval = 0.1f;

    private List<FormationUnitController> unitControllers = new List<FormationUnitController>();
    private GameObject player;
    private Vector3 manualTargetPoint; // จำจุดที่ผู้เล่นคลิกสั่งให้เดินไป

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        InitializeArmy();
        StartCoroutine(UpdateArmyPositionsRoutine());
    }

    public void InitializeArmy()
    {
        armyUnits.Clear();
        unitControllers.Clear();
        if (formationData == null) return;

        for (int y = 0; y < formationData.gridHeight; y++)
        {
            for (int x = 0; x < formationData.gridWidth; x++)
            {
                if (y < formationData.rows.Length && x < formationData.rows[y].cols.Length && formationData.rows[y].cols[x] == true)
                {
                    ArmySlotAssignment localAssign = slotAssignments.Find(s => s.row == y && s.col == x && s.unitData != null);
                    if (localAssign != null) SpawnSingleLocalUnit(localAssign, y, x);
                    else
                    {
                        TeamFormationManager.ActiveSlotAssignment teamAssign = null;
                        if (TeamFormationManager.Instance != null)
                            teamAssign = TeamFormationManager.Instance.activeFormation.Find(a => a.row == y && a.col == x);

                        if (teamAssign != null && teamAssign.unit != null) SpawnSingleTamedUnit(teamAssign.unit, y, x);
                    }
                }
            }
        }
        if (armyUnits.Count == 0) SetupAllAgents();
    }

    public void RespawnArmyBasedOnManager()
    {
        foreach (var agent in armyUnits) if (agent != null && agent.gameObject != null) Destroy(agent.gameObject);
        armyUnits.Clear(); unitControllers.Clear();
        InitializeArmy();
    }

    public void UnregisterArmyUnit(NavMeshAgent agent)
    {
        if (agent == null) return;
        armyUnits.Remove(agent);
        var controller = agent.gameObject.GetComponent<FormationUnitController>();
        if (controller != null) unitControllers.Remove(controller);
    }

    private void SpawnSingleTamedUnit(UnitInstance unit, int row, int col)
    {
        if (unit == null || unit.template == null || unit.template.unitPrefab == null) return;
        Vector3 spawnPos = GetSlotWorldPosition(row, col);
        GameObject spawnedObj = Instantiate(unit.template.unitPrefab, spawnPos, Quaternion.identity);
        spawnedObj.name = $"{unit.customName}_[Tamed]";

        AnimalStatsManager statsManager = spawnedObj.GetComponent<AnimalStatsManager>() ?? spawnedObj.AddComponent<AnimalStatsManager>();
        statsManager.SetupFromInstance(unit);
        ConfigureSpawnedAgent(spawnedObj, unit);
    }

    private void SpawnSingleLocalUnit(ArmySlotAssignment assignment, int row, int col)
    {
        Vector3 spawnPos = GetSlotWorldPosition(row, col);
        GameObject spawnedObj = Instantiate(assignment.unitData.unitPrefab, spawnPos, Quaternion.identity);
        spawnedObj.name = $"{assignment.unitData.speciesName}_[ทหารหลัก]";
        ConfigureSpawnedAgentFromData(spawnedObj, assignment.unitData);
    }

    Vector3 GetSlotWorldPosition(int row, int col)
    {
        if (formationData == null || player == null) return transform.position;

        float xOffset = (col - (formationData.gridWidth - 1) / 2f) * formationData.spacing;
        float zOffset = (((formationData.gridHeight - 1) / 2f) - row) * formationData.spacing;

        Vector3 spawnPos = player.transform.position + new Vector3(xOffset, 0, zOffset);
        if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, formationData.spacing * 2f, NavMesh.AllAreas)) return hit.position;
        return spawnPos;
    }

    void ConfigureSpawnedAgent(GameObject spawnedObj, UnitInstance instance)
    {
        if (spawnedObj == null || instance == null) return;
        spawnedObj.transform.tag = "Unit";
        int targetLayer = LayerMask.NameToLayer("Unit"); // ใส่ชื่อเลเยอร์ของคุณตรงนี้เลย
        SetLayerRecursively(spawnedObj, targetLayer);
        if (spawnedObj.GetComponent<UnitBuffManager>() == null) spawnedObj.AddComponent<UnitBuffManager>();

        NavMeshAgent agent = spawnedObj.GetComponent<NavMeshAgent>() ?? spawnedObj.AddComponent<NavMeshAgent>();

        FormationUnitController formationUnitController = spawnedObj.GetComponent<FormationUnitController>();
        formationUnitController.enabled = true;

        // ⚡ เร่งการตอบสนองระบบของตัวทหารให้ จัดทัพไว เดินไวขึ้น
        agent.radius = 0.45f;
        agent.height = 1.8f;
        agent.stoppingDistance = 0.2f; // ลด Stopping Distance ลงให้เข้าล็อกแถวง่ายขึ้้น
        agent.speed = instance.moveSpeed * 1.3f; // คูณเพิ่มโบนัสความเร็วขบวนทัพเล็กน้อย
        agent.acceleration = 12f; // เพิ่มแรงเร่งความเร็วตัวเพื่อไม่ให้เดินอืดอาดตอนเลี้ยวตัว
        agent.angularSpeed = 360f; // หมุนหน้าหันตัวไปหาทิศทางเดินทันทีรวดเร็ว
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance; // ลดภาระการคำนวณหลบกันเองเพื่อวิ่งเข้าจุดเป้าหมายตรงๆ
        agent.avoidancePriority = Random.Range(30, 70);

        var controller = spawnedObj.GetComponent<FormationUnitController>() ?? spawnedObj.AddComponent<FormationUnitController>();
        controller.minSpacingDistance = minSpacingDistance;
        controller.slowDownRadius = slowDownRadius;
        controller.guardRadius = instance.guardRadius;
        controller.attackRange = instance.guardRadius;

        controller.enemyLayer = enemyLayer;

        armyUnits.Add(agent);
        unitControllers.Add(controller);
    }

    void ConfigureSpawnedAgentFromData(GameObject spawnedObj, UnitDataSO data)
    {
        spawnedObj.transform.tag = "Unit";
        int targetLayer = LayerMask.NameToLayer("Unit"); // ใส่ชื่อเลเยอร์ของคุณตรงนี้เลย
        SetLayerRecursively(spawnedObj, targetLayer);
        NavMeshAgent agent = spawnedObj.GetComponent<NavMeshAgent>() ?? spawnedObj.AddComponent<NavMeshAgent>();

        FormationUnitController formationUnitController = spawnedObj.GetComponent<FormationUnitController>();
        formationUnitController.enabled = true;

        agent.radius = 0.45f;
        agent.height = 1.8f;
        agent.stoppingDistance = 0.2f;
        agent.speed = data.baseMoveSpeed * 1.3f;
        agent.acceleration = 12f;
        agent.angularSpeed = 360f;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
        agent.avoidancePriority = Random.Range(30, 70);

        var controller = spawnedObj.GetComponent<FormationUnitController>() ?? spawnedObj.AddComponent<FormationUnitController>();
        controller.minSpacingDistance = minSpacingDistance;
        controller.slowDownRadius = slowDownRadius;
        controller.guardRadius = unitGuardRadius;
        controller.attackRange = unitAttackRange;
        controller.enemyLayer = enemyLayer;

        armyUnits.Add(agent);
        unitControllers.Add(controller);
    }
    // 🛠️ เพิ่มฟังก์ชันนี้ไว้ด้านล่างสุดของ ArmyController.cs
    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;

        obj.layer = newLayer;

        foreach (Transform child in obj.transform)
        {
            if (child == null) continue;
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }

    void SetupAllAgents()
    {
        unitControllers.Clear();
        foreach (var agent in armyUnits)
        {
            if (agent == null) continue;
            var controller = agent.gameObject.GetComponent<FormationUnitController>() ?? agent.gameObject.AddComponent<FormationUnitController>();
            controller.minSpacingDistance = minSpacingDistance;
            controller.slowDownRadius = slowDownRadius;
            controller.guardRadius = unitGuardRadius;
            controller.attackRange = unitAttackRange;
            controller.enemyLayer = enemyLayer;
            unitControllers.Add(controller);
        }
    }

    void Update()
    {
        HandleCommandInputs();

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Input.GetKey(KeyCode.X))
        {
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayer)) DrawPreview(hit.point);
        }
        if (Input.GetKeyUp(KeyCode.X))
        {
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayer))
            {
                MoveToPoint(hit.point);
                ClearPreviewMarkers();
            }
        }
    }

    private void HandleCommandInputs()
    {
        if (Input.GetKeyDown(KeyCode.V))
            currentCommandMode = (currentCommandMode == ArmyCommandMode.CircleGuard) ? ArmyCommandMode.FollowFormation : ArmyCommandMode.CircleGuard;

        if (Input.GetKeyDown(KeyCode.B))
            currentCommandMode = (currentCommandMode == ArmyCommandMode.LineFormation) ? ArmyCommandMode.FollowFormation : ArmyCommandMode.LineFormation;

        if (Input.GetKeyDown(KeyCode.C))
        {
            if (currentCommandMode == ArmyCommandMode.StayStill || currentCommandMode == ArmyCommandMode.ManualMoveToPoint)
            {
                currentCommandMode = ArmyCommandMode.FollowFormation;
                SetUnitsAnimation(false);

                // 🛠️ ปลุกทหารให้ตื่นจากการตรึงตำแหน่งเดิม แล้วรีเซ็ตสเตตให้กลับมาเดินตาม
                foreach (var controller in unitControllers)
                {
                    if (controller != null) controller.ResetToIdleState();
                }
                Debug.Log("Army Command: Follow Player");
            }
            else
            {
                currentCommandMode = ArmyCommandMode.StayStill;
                SetUnitsAnimation(true);
                StopAllUnitsImmediately();
                Debug.Log("Army Command: Stay Still & Wait");
            }
        }
    }

    private IEnumerator UpdateArmyPositionsRoutine()
    {
        while (true)
        {
            if (player != null && armyUnits.Count > 0)
            {
                switch (currentCommandMode)
                {
                    case ArmyCommandMode.FollowFormation:
                        // โหมดเดินตาม: ส่งพิกัดผู้เล่นทุกๆ 0.1 วินาที (ถูกต้องแล้ว)
                        MoveToPositionInternal(player.transform.position, player.transform.rotation);
                        break;

                    case ArmyCommandMode.CircleGuard:
                        MoveInCircleFormation();
                        break;

                    case ArmyCommandMode.LineFormation:
                        MoveInLineFormation();
                        break;

                    case ArmyCommandMode.ManualMoveToPoint:
                        // 🛠️ แก้ไข: ปล่อยว่างไว้เลยครับ! 
                        // เพราะเราสั่ง MoveToPositionInternal ไป "ครั้งเดียว" ตอนปล่อยปุ่ม X แล้ว
                        // การปล่อยว่างตรงนี้จะทำให้ทหารไม่โดนโค้ดดึงตัวกลับไปที่จุด X ทุกๆ 0.1 วินาที ทำให้ AI สลับไปตีศัตรูได้สำเร็จ
                        break;

                    case ArmyCommandMode.StayStill:
                        break;
                }
            }
            yield return new WaitForSeconds(updateInterval);
        }
    }

    #region Formation Calculations

    private void MoveToPositionInternal(Vector3 centerPoint, Quaternion rotation)
    {
        if (formationData == null) return;
        int unitIndex = 0;

        for (int y = 0; y < formationData.gridHeight; y++)
        {
            for (int x = 0; x < formationData.gridWidth; x++)
            {
                if (y < formationData.rows.Length && x < formationData.rows[y].cols.Length && formationData.rows[y].cols[x] == true)
                {
                    bool hasLocalUnit = slotAssignments.Exists(s => s.row == y && s.col == x && s.unitData != null);
                    bool hasTamedUnit = TeamFormationManager.Instance != null && TeamFormationManager.Instance.activeFormation.Exists(a => a.row == y && a.col == x && a.unit != null);

                    if (hasLocalUnit || hasTamedUnit)
                    {
                        if (unitIndex >= armyUnits.Count) break;

                        float xOffset = (x - (formationData.gridWidth - 1) / 2f) * formationData.spacing;
                        float zOffset = (((formationData.gridHeight - 1) / 2f) - y) * formationData.spacing;
                        Vector3 finalWorldPos = centerPoint + (rotation * new Vector3(xOffset, 0, zOffset));

                        SendUnitToTarget(unitIndex, finalWorldPos);
                        unitIndex++;
                    }
                }
            }
        }
    }

    private void MoveInCircleFormation()
    {
        int totalUnits = armyUnits.Count;
        for (int i = 0; i < totalUnits; i++)
        {
            float angle = i * Mathf.PI * 2f / totalUnits;
            float x = Mathf.Cos(angle) * circleRadius;
            float z = Mathf.Sin(angle) * circleRadius;
            Vector3 targetPos = player.transform.position + new Vector3(x, 0, z);
            SendUnitToTarget(i, targetPos);
        }
    }

    private void MoveInLineFormation()
    {
        int totalUnits = armyUnits.Count;
        float totalWidth = (totalUnits - 1) * lineSpacing;
        float startX = -totalWidth / 2f;
        Vector3 backOffset = -player.transform.forward * 2f;
        Vector3 centerBase = player.transform.position + backOffset;

        for (int i = 0; i < totalUnits; i++)
        {
            float currentXOffset = startX + (i * lineSpacing);
            Vector3 targetPos = centerBase + (player.transform.right * currentXOffset);
            SendUnitToTarget(i, targetPos);
        }
    }

    private void SendUnitToTarget(int index, Vector3 rawPosition)
    {
        if (index >= armyUnits.Count || index >= unitControllers.Count) return;
        if (armyUnits[index] == null || unitControllers[index] == null) return;

        if (NavMesh.SamplePosition(rawPosition, out NavMeshHit navHit, formationData.spacing * 1.5f, NavMesh.AllAreas))
        {
            unitControllers[index].SetFormationTarget(navHit.position);
        }
    }

    private void StopAllUnitsImmediately()
    {
        foreach (var agent in armyUnits) if (agent != null && agent.isOnNavMesh) agent.ResetPath();
    }

    private void SetUnitsAnimation(bool isSitting) { }

    #endregion

    #region Move (ระบบสั่งเคลื่อนทัพคลิกเมาส์ปุ่ม X)

    public void MoveToPoint(Vector3 clickPoint)
    {
        if (armyUnits.Count == 0 || formationData == null) return;

        // 🛠️ 1. เปลี่ยนเข้าสู่สถานะสั่งเคลื่อนทัพแบบแมนนวล เพื่อตัดลูปวิ่งตามผู้เล่น
        currentCommandMode = ArmyCommandMode.ManualMoveToPoint;
        manualTargetPoint = clickPoint;
        SetUnitsAnimation(false);

        Vector3 armyCenter = GetArmyCenter();
        Vector3 dir = (clickPoint - armyCenter).normalized;
        Quaternion formationRotation = dir != Vector3.zero ? Quaternion.LookRotation(dir) : Quaternion.identity;

        // 🛠️ 2. สั่งให้ทหารทุกตัววิ่งไปกางขบวน ณ จุดที่คุณกด X 
        MoveToPositionInternal(clickPoint, formationRotation);
    }

    public void DrawPreview(Vector3 previewPoint)
    {
        if (armyUnits.Count == 0 || formationData == null || previewMarkerPrefab == null) return;
        Vector3 armyCenter = GetArmyCenter();
        Vector3 dir = (previewPoint - armyCenter).normalized;
        Quaternion formationRotation = dir != Vector3.zero ? Quaternion.LookRotation(dir) : Quaternion.identity;

        int unitIndex = 0; int markerIndex = 0;
        for (int y = 0; y < formationData.gridHeight; y++)
        {
            for (int x = 0; x < formationData.gridWidth; x++)
            {
                if (y < formationData.rows.Length && x < formationData.rows[y].cols.Length && formationData.rows[y].cols[x] == true)
                {
                    bool hasLocalUnit = slotAssignments.Exists(s => s.row == y && s.col == x && s.unitData != null);
                    bool hasTamedUnit = TeamFormationManager.Instance != null && TeamFormationManager.Instance.activeFormation.Exists(a => a.row == y && a.col == x && a.unit != null);

                    if (hasLocalUnit || hasTamedUnit)
                    {
                        if (unitIndex >= armyUnits.Count) break;
                        float xOffset = (x - (formationData.gridWidth - 1) / 2f) * formationData.spacing;
                        float zOffset = (((formationData.gridHeight - 1) / 2f) - y) * formationData.spacing;
                        Vector3 finalWorldPos = previewPoint + (formationRotation * new Vector3(xOffset, 0, zOffset));

                        if (NavMesh.SamplePosition(finalWorldPos, out NavMeshHit navhit, formationData.spacing * 1.5f, NavMesh.AllAreas))
                        {
                            if (markerIndex < spawnedPreviewMarkers.Count)
                            {
                                spawnedPreviewMarkers[markerIndex].SetActive(true);
                                spawnedPreviewMarkers[markerIndex].transform.position = navhit.position;
                                spawnedPreviewMarkers[markerIndex].transform.rotation = formationRotation;
                            }
                            else spawnedPreviewMarkers.Add(Instantiate(previewMarkerPrefab, navhit.position, formationRotation));
                            markerIndex++;
                        }
                        unitIndex++;
                    }
                }
            }
        }
    }

    Vector3 GetArmyCenter()
    {
        Vector3 center = Vector3.zero; int activeUnits = 0;
        foreach (var unit in armyUnits) { if (unit != null) { center += unit.transform.position; activeUnits++; } }
        return activeUnits > 0 ? center / activeUnits : transform.position;
    }

    public void ClearPreviewMarkers()
    {
        foreach (GameObject marker in spawnedPreviewMarkers) if (marker != null) marker.SetActive(false);
    }
    #endregion
}