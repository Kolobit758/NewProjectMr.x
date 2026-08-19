using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class UnitProducerBuilding : MonoBehaviour
{
    [Header("Building Identification")]
    public string buildingTypeID = "AnimalSmall_Spawner";

    [Header("Buildable Units (ลาก UnitDataSO มาใส่)")]
    public List<UnitDataSO> availableUnits = new List<UnitDataSO>();

    [Header("Production Queue State")]
    public UnitDataSO currentActiveUnit; // ยูนิตที่กำลังสร้างอยู่ปัจจุบัน
    public int queuedCount = 0;          // จำนวนคิวรอสะสม (กดเบิ้ลได้)
    public bool isProducing = false;
    public float currentProductionProgress = 0f;
    private float currentEffectiveTime = 5f;

    [Header("Production Settings")]
    public Transform spawnPoint;
    public float baseProductionTime = 5f;

    [Header("Red Alert Effect (Stacking Bonus)")]
    [Range(0.05f, 0.5f)]
    public float speedBonusPerBuilding = 0.15f;

    [Header("World Space UI Elements")]
    public GameObject worldSpaceCanvas;
    public GameObject progressPanelObj;
    public Slider worldProgressBar;
    public TMP_Text queueCountText;          // แสดงตัวเลขคิวรอ เช่น "+2"
    [Header("Spawn Configuration")]
    public Transform unitFolder;    // 🟢 โฟลเดอร์เก็บยูนิต (ลากอันเดียวกับที่ใช้ใน SpawnAllUnit มาใส่ได้เลย)
    public string layerName = "Unit"; // 🟢 ชื่อ Layer ของยูนิต
    [Header("Currency Settings")]
    public SO_ItemData goldItemData; // 💰 ลาก SO_ItemData ทองคำอันเดียวกันมาใส่ที่ตึกผลิตยูนิตด้วย

    void Awake()
    {
        if (worldSpaceCanvas == null)
        {
            worldSpaceCanvas = GetComponentInChildren<Canvas>(true)?.gameObject;
        }

        if (progressPanelObj == null && worldSpaceCanvas != null)
        {
            Transform panelTransform = worldSpaceCanvas.transform.Find("ProgressPanel");
            if (panelTransform != null) progressPanelObj = panelTransform.gameObject;
        }

        if (worldProgressBar == null && progressPanelObj != null)
        {
            worldProgressBar = progressPanelObj.GetComponentInChildren<Slider>();
        }
    }

    void Start()
    {
        if (progressPanelObj != null) progressPanelObj.SetActive(false);

        if (worldSpaceCanvas != null)
        {
            Canvas canvas = worldSpaceCanvas.GetComponent<Canvas>();
            if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
            {
                canvas.worldCamera = FindAnyObjectByType<Camera>();
            }
        }

        CalculateProductionSpeed();

        // 🟢 [ทีเด็ดตรงนี้]: สั่งวิ่งไปหาปุ่มใน Canvas ของตึก แล้วผูกฟังก์ชัน OnClickOpenUIButton ให้เองทันที!
        AutoBindOpenButton();
    }

    private void AutoBindOpenButton()
    {
        if (worldSpaceCanvas == null) return;

        // วิ่งไปหาปุ่มทั้งหมดที่เป็นลูกของ World Space Canvas นี้
        Button[] buttons = worldSpaceCanvas.GetComponentsInChildren<Button>(true);
        foreach (var btn in buttons)
        {
            // ถ้าปุ่มนั้นมีชื่อว่า "Open" หรือ "Button" (หรือตั้งชื่อตามปุ่มของคุณ)
            if (btn.gameObject.name.Contains("Open") || btn.gameObject.name.Contains("Button"))
            {
                // ล้างอันเก่ากันเหนียว แล้วผูกเข้ากับฟังก์ชันเปิดหน้าต่าง UI กลาง
                btn.onClick.RemoveListener(OnClickOpenUIButton);
                btn.onClick.AddListener(OnClickOpenUIButton);



                Debug.Log($"🔗 [Auto-Bind]: เชื่อมปุ่ม {btn.gameObject.name} บนตึก {gameObject.name} สำเร็จ!");
                break;
            }
        }
    }


    void Update()
    {
        if (!isProducing || currentActiveUnit == null) return;

        currentProductionProgress += Time.deltaTime;

        if (worldProgressBar != null && currentEffectiveTime > 0)
        {
            worldProgressBar.value = currentProductionProgress / currentEffectiveTime;
        }

        if (currentProductionProgress >= currentEffectiveTime)
        {
            CompleteCurrentProduction();
        }
    }

    void CalculateProductionSpeed()
    {
        UnitProducerBuilding[] allBuildings = Object.FindObjectsByType<UnitProducerBuilding>(FindObjectsSortMode.None);
        int sameTypeCount = 0;

        foreach (var b in allBuildings)
        {
            if (b.buildingTypeID == this.buildingTypeID && b.gameObject.activeInHierarchy)
            {
                sameTypeCount++;
            }
        }

        float multiplier = 1f + ((sameTypeCount - 1) * speedBonusPerBuilding);
        currentEffectiveTime = Mathf.Max(1f, baseProductionTime / multiplier);
    }

    public bool RequestProduceUnit(int unitIndex)
    {
        if (unitIndex < 0 || unitIndex >= availableUnits.Count) return false;

        UnitDataSO targetUnit = availableUnits[unitIndex];
        if (targetUnit == null) return false;

        // 1. 🏠 เช็คความจุคอกสัตว์ (รวมตัวที่กำลัง process และรอคิวอยู่ทั้งหมดแล้ว)
        if (AnimalShelter.IsTotalCapacityFull())
        {
            Debug.LogWarning("⚠️ [Spawner]: คอกสัตว์เต็มแล้ว! (รวมคิวที่กำลังสร้าง) สร้างยูนิตเพิ่มไม่ได้");
            return false;
        }

        // 2. 💰 เช็คเงิน
        if (ResourceInventory.Instance != null && goldItemData != null)
        {
            if (!ResourceInventory.Instance.HasResource(goldItemData.itemName, targetUnit.coinCost))
            {
                Debug.LogWarning("⚠️ [Spawner]: เงินไม่พอซื้อยูนิตตัวนี้!");
                return false;
            }
            ResourceInventory.Instance.ConsumeResource(goldItemData, targetUnit.coinCost);
        }

        CalculateProductionSpeed();

        // 3. จัดการคิวและสถานะการผลิต
        if (!isProducing)
        {
            currentActiveUnit = targetUnit;
            isProducing = true;
            currentProductionProgress = 0f;
            if (progressPanelObj != null) progressPanelObj.SetActive(true);
        }
        else if (currentActiveUnit == targetUnit)
        {
            // ถ้ากำลังสร้างชนิดนี้อยู่ ให้เพิ่มคิวรอสะสม
            queuedCount++;
        }
        else
        {
            Debug.LogWarning("⚠️ [Spawner]: กำลังสร้างยูนิตคนละชนิดอยู่ รอให้เสร็จก่อนครับ!");
            // ถ้ายกเลิกเพราะคนละชนิด อย่าลืมคืนเงินถ้าหักไปแล้วตามระบบเกมของคุณ
            return false;
        }

        UpdateQueueUI();
        Debug.Log($"🏭 [Spawner]: Queued production for {targetUnit.speciesName} (Waiting queue: {queuedCount})");
        return true;
    }

    void CompleteCurrentProduction()
    {
        // (ตอน Spawn จริง ตัวระบบจะเช็ค Capacity จากเงื่อนไขรวมด้านบนเรียบร้อยแล้วตั้งแต่ตอนกดปุ่ม)
        // 1. Spawn ยูนิตออกมา
        if (currentActiveUnit != null && currentActiveUnit.unitPrefab != null)
        {
            Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : transform.position + transform.right * 2f;

            GameObject animalSpawned = Instantiate(currentActiveUnit.unitPrefab, spawnPos, Quaternion.identity);

            if (unitFolder != null)
            {
                animalSpawned.transform.SetParent(unitFolder);
            }

            animalSpawned.tag = "Unit";

            int targetLayer = LayerMask.NameToLayer(layerName);
            if (targetLayer != -1)
            {
                animalSpawned.layer = targetLayer;
            }

            if (animalSpawned.TryGetComponent<AnimalStatsManager>(out var statsMgr))
            {
                statsMgr.SetupFromDataSO(currentActiveUnit);
            }

            UnitBase unitBase = null;
            if (animalSpawned.TryGetComponent<UnitBase>(out unitBase))
            {
                unitBase.enabled = true;
            }
            if (animalSpawned.TryGetComponent<AnimalAIController>(out var aiController))
            {
                aiController.enabled = false;
            }

            if (unitBase != null && RTS_movement.instance != null)
            {
                if (!RTS_movement.instance.allUnits.Contains(unitBase))
                {
                    RTS_movement.instance.allUnits.Add(unitBase);
                }
            }

            if (unitBase != null)
            {
                AnimalShelter targetShelter = AnimalShelter.GetAvailableShelter();
                if (targetShelter != null)
                {
                    targetShelter.RegisterUnit(unitBase);
                }
            }

            Debug.Log($"🎉 [Spawner]: สร้างและตั้งค่า {currentActiveUnit.speciesName} เรียบร้อย พร้อมลุย!");
        }

        // 3. เช็คว่ามีคิวสะสม (queuedCount) เหลืออีกไหม
        if (queuedCount > 0)
        {
            queuedCount--;
            currentProductionProgress = 0f;
            UpdateQueueUI();
        }
        else
        {
            isProducing = false;
            currentProductionProgress = 0f;
            currentActiveUnit = null;

            if (progressPanelObj != null) progressPanelObj.SetActive(false);
        }
    }

    private void UpdateQueueUI()
    {
        if (queueCountText != null)
        {
            queueCountText.text = queuedCount > 0 ? $"+{queuedCount}" : "";
        }
    }

    public void OnClickOpenUIButton()
    {
        if (BuildingProductionUI.Instance != null)
        {
            // สั่งเปิดหน้าต่างกลาง พร้อมส่งข้อมูลตึกหลังนี้เข้าไป
            BuildingProductionUI.Instance.Open(this);
        }
    }

    // ➕ คืนค่าจำนวนยูนิตที่กำลังผลิตอยู่ + คิวรอทั้งหมด (ใช้สำหรับคำนวณโควตารวม)
    public int GetPendingCount()
    {
        int count = 0;
        if (isProducing && currentActiveUnit != null)
        {
            count += 1; // ตัวที่กำลัง process อยู่
        }
        count += queuedCount; // ตัวที่รออยู่ในคิว
        return count;
    }
}