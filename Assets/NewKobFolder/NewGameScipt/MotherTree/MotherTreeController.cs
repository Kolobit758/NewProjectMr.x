using UnityEngine;
using System.Collections.Generic;

public enum TreeDesireType
{
    WantToConnectIsland,
    WantToSeeGreenIsland,
    WantPoopFertilizer
}

public class MotherTreeController : MonoBehaviour
{
    public static MotherTreeController Instance { get; private set; }
    [Header("Growth Settings (สุ่มจำนวนท่อนกิ่งไม้ของกอบ)")]
    [SerializeField] private GameObject branchPrefab;
    [Range(0, 100)] public float spawnNodeChance = 30f;
    public float turnAngle = 30f;
    public float energyCostToGrow = 10f;

    // 🟢 [ตัวแปรสุ่มความยาวกิ่ง]: ตั้งค่าได้เลยว่าอยากให้สุ่มออกมากี่ท่อนต่อการกด 1 ครั้ง
    public int minSegmentsPerGrow = 1;
    public int maxSegmentsPerGrow = 3;

    [Header("Ecosystem & Energy Net")]
    [SerializeField] private IslandController homeIsland;
    [SerializeField] private List<IslandController> outpostIslands = new List<IslandController>();

    public IslandController GetHomeIsland() => homeIsland;
    public List<IslandController> GetOutpostIslands() => outpostIslands;

    [Header("Day / Night Time System")]
    public int currentDay = 1;
    public bool isNightTime = false;
    public float dayNightTimer = 0f;
    public float phaseDuration = 60f;

    [Header("Giant Tree Mood System")]
    public float treeHappiness = 100f;
    public bool isTreeHappy = true;

    [Header("Production Cooldown")]
    public float energyProductionAmount = 5f;
    private float productionCooldownTimer = 0f;

    [Header("Active Daily Quest (สุ่มเฉพาะตอนขึ้นวันใหม่!)")]
    public TreeDesireType currentDesire;
    public IslandController targetIslandForDesire;
    public bool isDesireCompleted = false;
    private float desireCheckTimer = 0f;

    [Header("Camera Configurations")]
    [SerializeField] private GameObject godCamera;
    [SerializeField] private GameObject humanCamera;
    [SerializeField] private Vector3 godCamPosition = new Vector3(0, 40, 0);
    private Vector3 humanCamPosition;
    private Quaternion humanCamRotation;
    private bool isGodMode = false;
    public bool IsGodMode => isGodMode;

    [Header("God Mode Camera Drag & Zoom")]
    [SerializeField] private float panSpeed = 20f;
    [SerializeField] private float zoomSpeed = 10f;
    [SerializeField] private float minZoomY = 10f;
    [SerializeField] private float maxZoomY = 80f;
    private Vector3 dragOrigin;

    private float currentZAngle = 0f;
    private List<BranchSegment> allSegments = new List<BranchSegment>();
    private bool isLeftBlocked = false;
    private bool isRightBlocked = false;
    private bool isWorldFrozen = false;
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    void Start()
    {
        if (humanCamera != null)
        {
            humanCamPosition = humanCamera.transform.position;
            humanCamRotation = humanCamera.transform.rotation;
        }

        GenerateNewDailyDesire();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C)) ToggleCameraView();

        // 🟢 ปุ่มส่งปุ๋ยด่วนสำหรับคนเล่น (กดดึงจากคลังจริงไปส่งให้น้อนต้นไม้ยักษ์)
        if (Input.GetKeyDown(KeyCode.P) && currentDesire == TreeDesireType.WantPoopFertilizer && !isDesireCompleted)
        {
            TryDeliverFertilizerFromInventory();
        }

        HandleTimeSystem();
        HandleTreeQuestAndMoodLogic();
        HandleEnergyProductionInterval();
        HandleEnergyTransferLogic();

        if (isGodMode)
        {
            HandleGodCameraPan();
            HandleGodCameraZoom();
        }
    }

    void HandleTimeSystem()
    {
        if (isWorldFrozen) return;

        dayNightTimer += Time.deltaTime;

        if (dayNightTimer >= phaseDuration)
        {
            dayNightTimer = 0f;
            isNightTime = !isNightTime;

            if (!isNightTime)
            {
                currentDay++;
                Debug.LogWarning($"☀️ [อรุณสวัสดิ์]: เข้าสู่วันที่ {currentDay}!");
                GenerateNewDailyDesire();
            }
            else
            {
                Debug.LogWarning($"🌙 [พลบค่ำ]: เข้าสู่ช่วงกลางคืนแล้วมึงกอบ!");
            }
        }
    }

    void HandleTreeQuestAndMoodLogic()
    {
        if (isWorldFrozen) return;

        desireCheckTimer += Time.deltaTime;
        if (desireCheckTimer >= 1f)
        {
            desireCheckTimer = 0f;
            CheckDesireProgress();
        }

        if (!isDesireCompleted)
        {
            treeHappiness -= 1.5f * Time.deltaTime;
        }
        else
        {
            treeHappiness += 4f * Time.deltaTime;
        }

        treeHappiness = Mathf.Clamp(treeHappiness, 0f, 100f);
        isTreeHappy = (treeHappiness >= 50f);
    }

    public void GenerateNewDailyDesire()
    {
        isDesireCompleted = false;

        int desireIndex = Random.Range(0, 3);
        currentDesire = (TreeDesireType)desireIndex;

        if (outpostIslands.Count > 0)
        {
            int randomIslandIndex = Random.Range(0, outpostIslands.Count);
            targetIslandForDesire = outpostIslands[randomIslandIndex];
        }

        string targetName = targetIslandForDesire != null ? targetIslandForDesire.islandName : "เกาะไหนก็ได้";

        switch (currentDesire)
        {
            case TreeDesireType.WantToConnectIsland:
                Debug.LogWarning($"👑 [ภารกิจวันที่ {currentDay}]: ต้นไม้อยากเชื่อมเน็ตเวิร์กกับเกาะ [{targetName}]");
                break;
            case TreeDesireType.WantToSeeGreenIsland:
                Debug.LogWarning($"👑 [ภารกิจวันที่ {currentDay}]: ต้นไม้อยากเห็นเกาะ [{targetName}] เขียวขจี 100%!");
                break;
            case TreeDesireType.WantPoopFertilizer:
                Debug.LogWarning($"👑 [ภารกิจวันที่ {currentDay}]: ต้นไม้อยากได้ 'ปุ๋ยมูลสัตว์' (กด P เพื่อหักส่งจากคลังจริง!)");
                break;
        }
    }

    void CheckDesireProgress()
    {
        if (isDesireCompleted) return;

        switch (currentDesire)
        {
            case TreeDesireType.WantToConnectIsland:
                if (targetIslandForDesire != null && targetIslandForDesire.isConnectedToNet) CompleteCurrentDesire();
                break;

            case TreeDesireType.WantToSeeGreenIsland:
                if (targetIslandForDesire != null)
                {
                    float energyPercent = targetIslandForDesire.currentEnergy / targetIslandForDesire.maxEnergy;
                    if (energyPercent >= 0.95f) CompleteCurrentDesire();
                }
                break;

            case TreeDesireType.WantPoopFertilizer:
                // เควสปุ๋ยจะให้คนเล่นกดปุ่มกดส่งของ (P) เพื่อคัดแยกทรัพยากรตรงจากกระเป๋า
                break;
        }
    }

    // 🟢 🛠️ ฟังก์ชันพึ่งพาคลังกระเป๋าจริงในการส่งปุ๋ย
    void TryDeliverFertilizerFromInventory()
    {
        if (ResourceInventory.Instance != null)
        {
            // ค้นหาและตรวจสอบว่าในกระเป๋ามีไอเทมชื่อแนวปุ๋ย/มูลสัตว์ไหม (ปรับแมตช์ตามชื่อ String หรือ Class ของมึงได้เลย)
            // สมมติว่าในกระเป๋ามีฟังก์ชันดักเช็คและหักของอยู่แล้ว
            bool hasFertilizer = ResourceInventory.Instance.HasResource("Fertilizer", 1) || ResourceInventory.Instance.HasResource("Poop", 1);

            if (hasFertilizer)
            {
                // สั่งหักไอเทมปุ๋ยออกจากคลังผู้เล่น 1 ชิ้นจริง ๆ ผ่าน God View
                ResourceInventory.Instance.ConsumeResourceByName("Fertilizer", 1);
                CompleteCurrentDesire();
            }
            else
            {
                Debug.LogWarning("⚠️ ไม่มีปุ๋ยมูลสัตว์ในกระเป๋าเลยมึงกอบ! ไปเก็บจากเกาะที่มีสัตว์ป่ามาก่อน!");
            }
        }
        else
        {
            // บล็อก Failsafe เผื่อยังไม่เชื่อมคลังตอนรันโปรโตไทป์ กดผ่านได้เลย
            CompleteCurrentDesire();
        }
    }

    void CompleteCurrentDesire()
    {
        isDesireCompleted = true;
        treeHappiness = 100f;
        isTreeHappy = true;
        Debug.Log($"✨ [เควสวันที่ {currentDay} สำเร็จ!]: น้อนแฮปปี้มว๊ากก!");
    }

    // 🟢 🛠️ [แก้ไขระบบสุ่มการโตของกิ่ง]: งอกแบบสุ่มท่อนจำนวนลูป Prefab ไม่ให้น่าเบื่อ!
    public void GrowBranchFromHead(BranchSegment oldHead)
    {
        if (isWorldFrozen || oldHead == null) return;
        if (homeIsland == null || homeIsland.currentEnergy < energyCostToGrow) return;
        if (branchPrefab == null) return;

        // สุ่มเลือกว่าการกดครั้งนี้จะงอกออกมากี่ท่อน (Prefab)
        int segmentsToSpawn = Random.Range(minSegmentsPerGrow, maxSegmentsPerGrow + 1);
        BranchSegment currentHead = oldHead;

        for (int i = 0; i < segmentsToSpawn; i++)
        {
            // เช็คไฟก่อนงอกแต่ละท่อนย่อย
            if (homeIsland.currentEnergy < energyCostToGrow) break;

            homeIsland.currentEnergy -= energyCostToGrow;
            currentHead.SetHeadState(false);
            currentHead.UpdateShadowVisual(false, false);

            if (isLeftBlocked) currentZAngle += turnAngle;
            else if (isRightBlocked) currentZAngle -= turnAngle;

            Quaternion spawnRotation = Quaternion.Euler(90f, 0f, currentZAngle);
            Vector3 spawnPos = currentHead.endPoint.position;

            GameObject newSegObj = Instantiate(branchPrefab, spawnPos, spawnRotation);
            BranchSegment newSegment = newSegObj.GetComponent<BranchSegment>();

            if (newSegment != null)
            {
                Vector3 offset = newSegObj.transform.position - newSegment.startPoint.position;
                newSegObj.transform.position += offset;

                allSegments.Add(newSegment);
                newSegment.SetHeadState(true);
                newSegment.UpdateShadowVisual(isLeftBlocked, isRightBlocked);

                if (Random.Range(0f, 100f) <= spawnNodeChance) newSegment.SpawnNode();

                // อัปเดตตัวแปรเพื่อใช้ท่อนใหม่นี้เป็นฐานสำหรับลูปรอบถัดไป
                currentHead = newSegment;
            }
        }
        Debug.Log($"🌿 [กิ่งไม้สุ่มงอก]: พรวดเดียวออกมาสะใจ {segmentsToSpawn} ท่อน!");
    }

    void HandleEnergyProductionInterval()
    {
        if (isWorldFrozen || homeIsland == null) return;
        if (isNightTime)
        {
            productionCooldownTimer = 0f;
            return;
        }

        float currentCooldownLimit = isTreeHappy ? 1f : 10f;

        productionCooldownTimer += Time.deltaTime;
        if (productionCooldownTimer >= currentCooldownLimit)
        {
            productionCooldownTimer = 0f;

            if (homeIsland.currentEnergy < homeIsland.maxEnergy)
            {
                Debug.Log("Home island pump energy Cooldawn = " + currentCooldownLimit);
                homeIsland.currentEnergy += energyProductionAmount;
            }
        }
    }

    void HandleEnergyTransferLogic()
    {
        if (homeIsland == null || isWorldFrozen) return;

        foreach (IslandController outpost in outpostIslands)
        {
            if (outpost == null) continue;

            if (outpost.isConnectedToNet && outpost.isReceivingEnergy)
            {
                float transferAmount = outpost.energyReceiveRate * Time.deltaTime;

                if (homeIsland.currentEnergy > transferAmount)
                {
                    homeIsland.currentEnergy -= transferAmount;
                    outpost.currentEnergy += transferAmount;
                }
                else
                {
                    outpost.isReceivingEnergy = false;
                }
            }
        }
    }

    public void SpawnSubBranch(BranchSegment parentSegment)
    {
        if (isWorldFrozen || parentSegment == null || homeIsland == null || homeIsland.currentEnergy < energyCostToGrow) return;

        homeIsland.currentEnergy -= energyCostToGrow;
        parentSegment.hasNode = false;

        float subBranchAngle = parentSegment.transform.eulerAngles.z + 45f;
        Quaternion spawnRotation = Quaternion.Euler(90f, 0f, subBranchAngle);
        Vector3 spawnPos = parentSegment.transform.position;

        GameObject newSegObj = Instantiate(branchPrefab, spawnPos, spawnRotation);
        BranchSegment newSegment = newSegObj.GetComponent<BranchSegment>();

        if (newSegment != null)
        {
            allSegments.Add(newSegment);
            newSegment.SetHeadState(true);
            newSegment.UpdateShadowVisual(false, false);
            if (Random.Range(0f, 100f) <= spawnNodeChance) newSegment.SpawnNode();
        }
    }

    public void PlantBatteryTree(BranchSegment segment, IslandController targetIsland)
    {
        if (isWorldFrozen || segment == null || targetIsland == null) return;
        segment.ApplyGraft(GraftType.BatteryTree);
        targetIsland.IncreaseMaxEnergy(30f);
    }

    void HandleGodCameraPan()
    {
        if (Input.GetMouseButtonDown(2))
        {
            dragOrigin = godCamera.GetComponent<Camera>().ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, godCamera.GetComponent<Camera>().transform.position.y));
            return;
        }
        if (Input.GetMouseButton(2))
        {
            Vector3 currentMouseWorld = godCamera.GetComponent<Camera>().ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, godCamera.GetComponent<Camera>().transform.position.y));
            Vector3 difference = dragOrigin - currentMouseWorld;
            difference.y = 0;
            godCamera.transform.position += difference;
        }
    }

    void HandleGodCameraZoom()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (scrollInput != 0f)
        {
            Vector3 currentPos = godCamera.transform.position;
            currentPos.y -= scrollInput * zoomSpeed * Time.deltaTime * 100f;
            currentPos.y = Mathf.Clamp(currentPos.y, minZoomY, maxZoomY);
            godCamera.transform.position = currentPos;
        }
    }

    void ToggleCameraView()
    {
        isGodMode = !isGodMode;
        if (isGodMode)
        {
            godCamera.SetActive(true);
            humanCamera.SetActive(false);
            godCamera.transform.position = godCamPosition;
            godCamera.transform.rotation = Quaternion.Euler(90, 0, 0);
        }
        else
        {
            godCamera.SetActive(false);
            humanCamera.SetActive(true);
        }
    }
    // 🟢 🔥 [เสกฟังก์ชันบังเงาผ้าคลุมกลับคืนมาให้แล้วมึงกอบ!]
    // ฟังก์ชันนี้จะถูกเรียกจาก GodModeUIManager ตอนคนเล่นสั่งคุมทิศทางเงาผ้าคลุมซ้าย-ขวา
    public void SetShadowDirection(bool left, bool right, BranchSegment headSegment)
    {
        if (isWorldFrozen) return;

        isLeftBlocked = left;
        isRightBlocked = right;

        // ส่งคำสั่งไปอัปเดตกราฟิกเงาที่ตัวหัวกิ่งไม้โดยตรง
        if (headSegment != null)
        {
            headSegment.UpdateShadowVisual(isLeftBlocked, isRightBlocked);
        }
    }
}