using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(CharacterStats))]
public class BuildingHealth : MonoBehaviour, ITaskable
{
    private CharacterStats characterStats;

    [Header("UI Settings")]
    public Slider healthBarSlider;
    public GameObject canvasUIParent;
    public GameObject fixButtonObject;

    [Header("Building Settings")]
    public string buildingName = "สิ่งก่อสร้าง";
    public bool isDestroyedOnDeath = true;

    [Header("Repair Settings")]
    public SO_ItemData repairItemCost;
    public int repairCostAmount = 1;
    public float repairHealRate = 20f;

    private bool isRepairing = false;

    // 🟢 เปลี่ยนมาเป็นตัวแปรเก็บบนสคริปต์ ระบบจะวิ่งไปหาตัวที่เป็นปุ่ม Fix ภายในลูกๆ ให้เองอัตโนมัติ
    private Button fixButtonComponent;
    public Button fixCanvas; // (ยังเก็บตัวแปรนี้ไว้ตามโครงสร้างเดิมของคุณ)

    void Awake()
    {
        characterStats = GetComponent<CharacterStats>();
    }

    void Start()
    {
        DayNightManager.Instance.RegisterBuilding(true);
        int defaultMaxHP = 200;
        characterStats.InitializeStats(defaultMaxHP, defaultMaxHP);

        // 🟢 [ออโต้อัตโนมัติ 1]: ค้นหา Canvas ลูกภายในตึกนี้ทันที
        AutoFindAndSetupCanvasAndButton();

        UpdateHealthUI();
    }

    // ฟังก์ชันค้นหาและตั้งค่า Canvas กับปุ่ม Fix ภายในตึกอัตโนมัติ
    private void AutoFindAndSetupCanvasAndButton()
    {
        // 1. ค้นหา Canvas ทั้งหมดที่เป็นลูกของตึกนี้
        Canvas foundCanvas = GetComponentInChildren<Canvas>(true);
        if (foundCanvas != null)
        {
            // ถ้า Canvas เป็นแบบ World Space และยังไม่มีกล้อง ให้หากล้องมาใส่ให้อัตโนมัติ
            if (foundCanvas.renderMode == RenderMode.WorldSpace && foundCanvas.worldCamera == null)
            {
                Camera mainCam = FindAnyObjectByType<Camera>();
                if (mainCam != null)
                {
                    foundCanvas.worldCamera = mainCam;
                }
            }

            // อ้างอิงเก็บไว้ใช้งาน (แปลงจาก Button เป็น GameObject หรือเก็บ reference ตามที่คุณสะดวก)
            // หมายเหตุ: ตรงนี้ถ้าตัวแปร fixCanvas ในโค้ดคุณเป็น Button (ตามโค้ดล่าสุดที่คุณส่งมา) 
            // ให้เช็คว่าต้องการใช้ Canvas จริงๆ หรือไม่ ถ้าเป็น Canvas ให้เปลี่ยนชนิดตัวแปร fixCanvas เป็น Canvas นะครับ
        }

        // 2. ค้นหาปุ่ม Fix จากลูกๆ ในตึกอัตโนมัติ เพื่อเปิด-ปิดเวลาเลือดลด
        Button[] allButtons = GetComponentsInChildren<Button>(true);
        foreach (var btn in allButtons)
        {
            if (btn.gameObject.name.Contains("Fix") || btn.gameObject.name.Contains("Repair"))
            {
                fixButtonComponent = btn;
                fixButtonObject = btn.gameObject;

                // ผูก Event กดปุ่มให้อัตโนมัติ (ไม่ต้องไปกดบวกใน Inspector)
                fixButtonComponent.onClick.RemoveListener(OnClickFixButton);
                fixButtonComponent.onClick.AddListener(OnClickFixButton);
                break;
            }
        }

        // ถ้าหาไม่เจอด้วยชื่อ ให้หยิบปุ่มตัวแรกที่เจอในตึกแทน
        if (fixButtonObject == null && allButtons.Length > 0)
        {
            fixButtonComponent = allButtons[0];
            fixButtonObject = allButtons[0].gameObject;
            fixButtonComponent.onClick.RemoveListener(OnClickFixButton);
            fixButtonComponent.onClick.AddListener(OnClickFixButton);
        }

        // เริ่มต้นซ่อนปุ่ม Fix ไว้ก่อน
        if (fixButtonObject != null) fixButtonObject.SetActive(false);
    }

    void Update()
    {
        UpdateHealthUI();

        if (characterStats.currentHP <= 0)
        {
            DestroyBuilding();
        }
    }

    void UpdateHealthUI()
    {
        if (healthBarSlider != null)
        {
            int[] stats = characterStats.GetPrivateData();
            int maxHP = stats[0];
            if (maxHP > 0)
            {
                healthBarSlider.value = (float)characterStats.currentHP / maxHP;
            }

            // 🟢 เช็คเงื่อนไขเลือด < 80% เพื่อเปิด-ปิดปุ่ม Fix อัตโนมัติ
            if (maxHP > 0 && fixButtonObject != null && !isRepairing)
            {
                bool isDamaged = characterStats.currentHP < (maxHP * 0.8f);
                fixButtonObject.SetActive(isDamaged);
            }
        }
    }

    public void OnClickFixButton()
    {
        if (isRepairing) return;

        int[] stats = characterStats.GetPrivateData();
        int maxHP = stats[0];

        if (characterStats.currentHP >= maxHP)
        {
            Debug.Log($"✨ [Building]: ตึก {buildingName} เลือดเต็มอยู่แล้ว!");
            return;
        }

        if (repairItemCost != null)
        {
            if (ResourceInventory.Instance != null && ResourceInventory.Instance.HasResource(repairItemCost.itemName, repairCostAmount))
            {
                ResourceInventory.Instance.ConsumeResourceByName(repairItemCost.itemName, repairCostAmount, showFeedback: false);

                // 🟢 Feedback แสดงการเสียทรัพยากรซ่อม ลอยขึ้นเหนือตึก
                if (FloatingTextManager.Instance != null)
                {
                    FloatingTextManager.Instance.ShowResourceSpend(repairItemCost.itemName, repairCostAmount, transform.position + Vector3.up * 2f);
                }
            }
            else
            {
                Debug.LogWarning($"⚠️ [Building]: ไอเทมซ่อมไม่พอ! ต้องการ {repairCostAmount} ชิ้นของ {repairItemCost.itemName}");
                if (FloatingTextManager.Instance != null)
                {
                    FloatingTextManager.Instance.ShowText(transform.position + Vector3.up * 2f, $"Need {repairCostAmount} {repairItemCost.itemName}", Color.red);
                }
                return;
            }
        }

        StartCoroutine(RepairRoutine(maxHP));
    }

    IEnumerator RepairRoutine(int maxHP)
    {
        isRepairing = true;
        if (fixButtonObject != null) fixButtonObject.SetActive(false);

        Debug.Log($"🔨 [Building]: เริ่มซ่อมแซมตึก {buildingName}...");

        while (characterStats.currentHP < maxHP && isRepairing)
        {
            int healAmount = Mathf.RoundToInt(repairHealRate * Time.deltaTime);
            characterStats.Heal(Mathf.Max(1, healAmount));
            UpdateHealthUI();
            yield return null;
        }

        isRepairing = false;
        Debug.Log($"✅ [Building]: ซ่อมแซมตึก {buildingName} เสร็จสิ้น!");

        if (FloatingTextManager.Instance != null)
        {
            FloatingTextManager.Instance.ShowText(transform.position + Vector3.up * 2f, $"🔨 Repaired!", Color.green);
        }
    }

    void DestroyBuilding()
    {
        Debug.LogError($"💥 [Building]: ตึก {buildingName} ถูกทำลายพังทลายลงแล้ว!");

        if (isDestroyedOnDeath)
        {
            Destroy(gameObject, 0.1f);
        }
    }

    void OnDestroy()
    {
        if (DayNightManager.Instance != null)
        {
            DayNightManager.Instance.RegisterBuilding(false);
            DayNightManager.Instance.CheckLossEvent(); // สั่งเช็คทันทีตอนตาย
        }
    }

    public void OnUnitInteract(UnitBase unit)
    {
        Debug.Log($"🔨 ยูนิตกำลังซ่อมแซม/ใช้งานตึก {buildingName}");
    }

    public void OnUnitExit(UnitBase unit) { }
    public Vector3 GetInteractionPoint() => transform.position + transform.forward * 2f;
}