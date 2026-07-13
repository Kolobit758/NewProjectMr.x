using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// เอาไว้แสดงสถานะของ MotherTreeController ผ่าน UI (Canvas)
/// วิธีใช้: 
/// 1) สร้าง Canvas ใน Scene แล้วสร้าง TMP_Text / Slider ตามช่องด้านล่าง
/// 2) เอาสคริปต์นี้ไปแปะที่ GameObject ไหนก็ได้ (เช่น GameObject ชื่อ "UIManager")
/// 3) ลาก MotherTreeController ตัวจริงใน Scene มาใส่ช่อง Mother Tree
/// 4) ลาก UI Element ที่สร้างไว้มาใส่ในแต่ละช่อง
/// </summary>
public class MotherTreeUIManager : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private MotherTreeController motherTree;

    [Header("Day / Night UI")]
    [SerializeField] private TMP_Text dayText;          // เช่น "วันที่ 3"
    [SerializeField] private TMP_Text phaseText;        // เช่น "☀️ กลางวัน" / "🌙 กลางคืน"
    [SerializeField] private Image phaseIcon;           // (ถ้ามี) เปลี่ยนสี/ไอคอนตามกลางวัน-กลางคืน
    [SerializeField] private Color dayColor = new Color(1f, 0.85f, 0.2f);
    [SerializeField] private Color nightColor = new Color(0.2f, 0.25f, 0.6f);
    [SerializeField] private Slider phaseTimerSlider;   // นับถอยหลังในเฟสปัจจุบัน (0-1)

    [Header("Tree Mood UI")]
    [SerializeField] private Slider happinessSlider;    // 0-100
    [SerializeField] private TMP_Text happinessText;    // เช่น "อารมณ์: 82%"
    [SerializeField] private Image happinessFillImage;  // เปลี่ยนสีตามอารมณ์ (ถ้ามี)
    [SerializeField] private Color happyColor = new Color(0.3f, 0.9f, 0.4f);
    [SerializeField] private Color sadColor = new Color(0.9f, 0.3f, 0.3f);

    [Header("Daily Quest UI")]
    [SerializeField] private TMP_Text questTitleText;   // หัวข้อเควส
    [SerializeField] private TMP_Text questDetailText;  // รายละเอียด/เกาะเป้าหมาย
    [SerializeField] private GameObject questCompleteBadge; // ป้าย "สำเร็จแล้ว" (โชว์/ซ่อน)

    [Header("Energy UI (World Space - ลอยเหนือแต่ละเกาะ)")]
    [Tooltip("Prefab ที่มี World Space Canvas ติดตัวเอง (ดูโครงสร้างที่ IslandEnergyRow ด้านล่างไฟล์) จะถูกวางลอยเหนือเกาะแต่ละเกาะอัตโนมัติ")]
    [SerializeField] private IslandEnergyRow islandRowPrefab;
    [Tooltip("ความสูงที่ยกขึ้นจากตำแหน่งเกาะ (แกน Y) เพื่อไม่ให้ UI ทับตัวเกาะ")]
    [SerializeField] private float worldUIHeightOffset = 15f;
    [Tooltip("แสดง UI พลังงานเหนือเกาะเฉพาะตอนอยู่ God Mode เท่านั้น (สลับด้วยปุ่ม C) ถ้าไม่ติ๊ก จะโชว์ตลอดเวลา")]
    [SerializeField] private bool onlyShowInGodMode = true;

    private readonly Dictionary<IslandController, IslandEnergyRow> islandRows = new Dictionary<IslandController, IslandEnergyRow>();

    [Header("Refresh Rate")]
    [Tooltip("อัปเดต UI ทุกกี่วินาที (0 = ทุกเฟรม)")]
    [SerializeField] private float refreshInterval = 0.2f;
    private float refreshTimer = 0f;

    void Reset()
    {
        // เผื่อความสะดวก: พยายามหา MotherTreeController ใน Scene ให้อัตโนมัติตอนแปะสคริปต์ครั้งแรก
        if (motherTree == null)
            motherTree = FindAnyObjectByType<MotherTreeController>();
    }

    void Start()
    {
        BuildIslandRows();
    }

    // สร้าง World Space UI 1 อันต่อ 1 เกาะ (เกาะแม่ + เกาะบริวารทั้งหมด) แค่ครั้งเดียวตอนเริ่มเกม
    // แต่ละอันจะถูกฝังเป็นลูกของตัวเกาะเอง แล้วยกขึ้นไปลอยเหนือหัวเกาะ
    void BuildIslandRows()
    {
        if (motherTree == null || islandRowPrefab == null) return;

        islandRows.Clear();

        IslandController home = motherTree.GetHomeIsland();
        if (home != null)
            CreateRowForIsland(home, "🏠 " + home.islandName);

        foreach (var outpost in motherTree.GetOutpostIslands())
        {
            if (outpost == null) continue;
            CreateRowForIsland(outpost, outpost.islandName);
        }
    }

    void CreateRowForIsland(IslandController island, string displayName)
    {
        // ฝังเป็นลูกของตัวเกาะเอง เพื่อให้ลอยตามเกาะไปตลอด ถ้าเกาะขยับ/ถูกย้ายตำแหน่ง
        IslandEnergyRow row = Instantiate(islandRowPrefab, island.transform);
        row.transform.localPosition = new Vector3(0f, worldUIHeightOffset, 0f);

        // หมุนให้ Canvas หงายหน้าขึ้นด้านบน เพื่อให้กล้อง God Mode (มองตั้งฉากลงมา) เห็นได้ชัดเจน
        row.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        row.SetName(displayName);
        islandRows[island] = row;
    }

    void Update()
    {
        if (motherTree == null) return;

        refreshTimer += Time.deltaTime;
        if (refreshTimer < refreshInterval) return;
        refreshTimer = 0f;

        UpdateDayNightUI();
        UpdateMoodUI();
        UpdateQuestUI();
        UpdateEnergyUI();
    }

    void UpdateDayNightUI()
    {
        if (dayText != null)
            dayText.text = $"Day {motherTree.currentDay}";

        if (phaseText != null)
            phaseText.text = motherTree.isNightTime ? "🌙 Night" : "☀️ Day";

        if (phaseIcon != null)
            phaseIcon.color = motherTree.isNightTime ? nightColor : dayColor;

        if (phaseTimerSlider != null)
        {
            // ต้องเข้าถึง phaseDuration ซึ่งเป็น public อยู่แล้วใน MotherTreeController
            float progress = motherTree.dayNightTimer / Mathf.Max(motherTree.phaseDuration, 0.0001f);
            phaseTimerSlider.value = Mathf.Clamp01(progress);
        }
    }

    void UpdateMoodUI()
    {
        if (happinessSlider != null)
            happinessSlider.value = motherTree.treeHappiness / 100f;

        if (happinessText != null)
            happinessText.text = $"Tree Mood: {motherTree.treeHappiness:0}%";

        if (happinessFillImage != null)
            happinessFillImage.color = motherTree.isTreeHappy ? happyColor : sadColor;
    }

    void UpdateQuestUI()
    {
        string islandName = motherTree.targetIslandForDesire != null ? motherTree.targetIslandForDesire.islandName : "any island";

        if (questTitleText != null)
        {
            questTitleText.text = motherTree.currentDesire switch
            {
                TreeDesireType.WantToConnectIsland => $"🌐 Connect with: {islandName}",
                TreeDesireType.WantToSeeGreenIsland => $"🌱 Make {islandName} fully green",
                TreeDesireType.WantPoopFertilizer => "💩 Wants animal fertilizer",
                _ => "-"
            };
        }

        if (questDetailText != null)
        {
            questDetailText.text = motherTree.isDesireCompleted
                ? "Completed! Waiting for next day"
                : "In progress...";
        }

        if (questCompleteBadge != null)
            questCompleteBadge.SetActive(motherTree.isDesireCompleted);
    }

    void UpdateEnergyUI()
    {
        bool shouldShow = !onlyShowInGodMode || motherTree.IsGodMode;

        // วนอัปเดตทุกเกาะที่สร้าง UI ลอยไว้ (เกาะแม่ + เกาะบริวารทั้งหมด)
        foreach (var kvp in islandRows)
        {
            IslandController island = kvp.Key;
            IslandEnergyRow row = kvp.Value;
            if (island == null || row == null) continue;

            row.gameObject.SetActive(shouldShow);
            if (!shouldShow) continue;

            row.SetEnergy(island.currentEnergy, island.maxEnergy, island.isConnectedToNet);

            // ไฮไลต์แถวนี้ถ้ามันคือเกาะเป้าหมายของเควสปัจจุบัน (และเควสยังไม่เสร็จ)
            bool isQuestTarget = !motherTree.isDesireCompleted
                && motherTree.targetIslandForDesire == island
                && motherTree.currentDesire != TreeDesireType.WantPoopFertilizer; // เควสนี้ไม่ผูกกับเกาะไหนโดยเฉพาะ

            row.SetHighlight(isQuestTarget);
        }
    }
}