using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum CropStage { Empty, Growing, ReadyToHarvest }

public class CropPlots : MonoBehaviour
{
    [Header("Current Status")]
    public CropStage currentStage = CropStage.Empty;
    public SO_PlantData plantedPlantData;

    public int daysGrown = 0;
    private GameObject currentVisualObject;

    [Header("Plot Durability Settings")]
    public int maxPlotHealth = 3;
    public int currentPlotHealth = 3;

    [Header("Requirements Info")]
    public SO_ItemData fertilizerItemData;
    public SO_ItemData waterItemData;

    [Header("Soil & Nutrient System")]
    public NutrientData currentSoilNutrients;
    public SO_FertilizerData appliedFertilizer;
    public bool isMutated = false;

    [Header("Visual Settings")]
    public float plantHeightOffset = 0.05f;

    [Header("Quality & Care Tracking")]
    public float careQualityScore = 100f;

    [Header("Perfect Timing Window")]
    [Tooltip("ก่อนถึงเดดไลน์กี่วินาที ถึงจะเริ่มนับว่าเป็นช่วง 'Perfect' ให้รดน้ำ/ใส่ปุ๋ย")]
    public float perfectWindowSeconds = 5f;

    [Header("Independent Timers (นับถอยหลัง)")]
    public float waterCooldownTimer = 0f;
    public float fertilizerCooldownTimer = 0f;

    // 🟢 ธงบอกสถานะ "ต้องการตอนนี้เลย" — CropPlotUI เรียกใช้ตัวนี้โดยตรง
    public bool needsWaterNow { get; private set; }
    public bool needsFertilizerNow { get; private set; }

    private float waterInterval = 30f;
    private float fertilizerInterval = 60f;

    void Start()
    {
        if (DayNightManager.Instance != null)
        {
            DayNightManager.Instance.OnDayChanged -= HandleDayChanged;
            DayNightManager.Instance.OnDayChanged += HandleDayChanged;
        }
    }

    void OnDestroy()
    {
        if (DayNightManager.Instance != null)
        {
            DayNightManager.Instance.OnDayChanged -= HandleDayChanged;
        }
    }

    private void HandleDayChanged(int newDay)
    {
        if (currentStage == CropStage.Growing && plantedPlantData != null)
        {
            daysGrown++;
            if (daysGrown >= plantedPlantData.daysToGrow)
            {
                SwitchStage(CropStage.ReadyToHarvest);
            }
        }
    }

    void Update()
    {
        // 🌙 กลางคืนหยุดโตและหยุดจับเวลาดูแลชั่วคราว
        if (DayNightManager.Instance != null && DayNightManager.Instance.isNightTime) return;
        if (currentStage != CropStage.Growing) return;

        // 💧 นับถอยหลังน้ำ
        waterCooldownTimer -= Time.deltaTime;
        needsWaterNow = waterCooldownTimer <= perfectWindowSeconds;

        // 🧪 นับถอยหลังปุ๋ย
        fertilizerCooldownTimer -= Time.deltaTime;
        needsFertilizerNow = fertilizerCooldownTimer <= perfectWindowSeconds;
    }

    public bool PlantSeed(SO_PlantData plantData)
    {
        if (currentStage != CropStage.Empty) return false;

        plantedPlantData = plantData;
        daysGrown = 0;
        careQualityScore = 100f;

        RecalculateIntervalsFromPlantData();

        waterCooldownTimer = waterInterval;
        fertilizerCooldownTimer = fertilizerInterval;
        needsWaterNow = false;
        needsFertilizerNow = false;

        SwitchStage(CropStage.Growing);
        return true;
    }

    /// <summary>คำนวณ interval ใหม่จากค่าเริ่มต้นใน SO_PlantData (wateringsPerDay / fertilizingsPerDay)</summary>
    private void RecalculateIntervalsFromPlantData()
    {
        if (plantedPlantData == null) return;
        float dayDuration = DayNightManager.Instance != null ? DayNightManager.Instance.dayDuration / 2  : 300f;
        
        // แบ่งเวลาต่อวันตามจำนวนครั้งที่ต้องดูแลต่อวัน
        waterInterval = dayDuration / Mathf.Max(1f, plantedPlantData.wateringsPerDay);
        fertilizerInterval = dayDuration / Mathf.Max(1f, plantedPlantData.fertilizingsPerDay);
    }

    /// <summary>
    /// 🟢 ให้ CropPlotsGroup เรียกตอน "จ้างงาน" เพื่อ override ความถี่จากค่าที่ผู้เล่นตั้งในแผง UI
    /// waterTimesPerCycle / fertilizeTimesPerCycle = กี่รอบต่อ 1 รอบการเติบโตของพืชต้นนี้
    /// </summary>
    public void SetCareFrequencyOverride(bool overrideWater, float waterTimesPerCycle, bool overrideFertilize, float fertilizeTimesPerCycle)
    {
        if (plantedPlantData == null) return;

        if (overrideWater)
            waterInterval = plantedPlantData.timeToGrow / Mathf.Max(0.01f, waterTimesPerCycle);

        if (overrideFertilize)
            fertilizerInterval = plantedPlantData.timeToGrow / Mathf.Max(0.01f, fertilizeTimesPerCycle);
    }

    // เมื่อผู้เล่นมากดโต้ตอบกับแปลง (เช่นคลิกเก็บเกี่ยว)
    public void PlayerInteract()
    {
        if (currentStage == CropStage.ReadyToHarvest)
        {
            HarvestCrop();
        }
    }

    // 💧 รดน้ำ — ประเมินจาก "เวลาจริง" ตอนที่ลงมือ ไม่ใช่สุ่ม
    public void PlayerPerformWater()
    {
        EvaluateCareTiming(waterCooldownTimer, "Water");
        waterCooldownTimer = waterInterval;
        needsWaterNow = false;
    }

    // 🧪 ใส่ปุ๋ย
    public void PlayerPerformFertilize(SO_FertilizerData fertData)
    {
        EvaluateCareTiming(fertilizerCooldownTimer, "Fertilizer");

        if (fertData != null)
        {
            ApplyFertilizer(fertData);
        }
        fertilizerCooldownTimer = fertilizerInterval;
        needsFertilizerNow = false;
    }

    /// <summary>
    /// 🟢 ประเมินจากเวลาถอยหลังจริง ณ ตอนลงมือ:
    /// - timerAtAction &lt;= 0            → สายเกินไปแล้ว (Late)
    /// - 0 &lt; timerAtAction &lt;= perfectWindow → จังหวะสมบูรณ์แบบ (Perfect)
    /// - timerAtAction &gt; perfectWindow  → เร็วไป ยังไม่ถึงช่วงที่ควรทำ (Too Early)
    /// </summary>
    private void EvaluateCareTiming(float timerAtAction, string actionType)
    {
        if (timerAtAction <= 0f)
        {
            RegisterCareEvent(-10f);
            ShowPopupFeedback("⚠️ LATE " + actionType + " (-10)", Color.red);
        }
        else if (timerAtAction <= perfectWindowSeconds)
        {
            RegisterCareEvent(0f);
            ShowPopupFeedback("✨ PERFECT " + actionType + "!", Color.green);
        }
        else
        {
            RegisterCareEvent(-5f);
            ShowPopupFeedback("⏳ TOO EARLY " + actionType + " (-5)", Color.yellow);
        }
    }

    private void RegisterCareEvent(float scoreDelta) => careQualityScore = Mathf.Clamp(careQualityScore + scoreDelta, 0f, 100f);

    private void ShowPopupFeedback(string message, Color color)
    {
        if (FloatingTextManager.Instance != null)
        {
            FloatingTextManager.Instance.ShowText(transform.position + Vector3.up * 1.5f, message, color);
        }
    }

    public void HarvestCrop()
    {
        if (currentStage != CropStage.ReadyToHarvest || plantedPlantData == null) return;

        ItemGrade finalGrade = CalculateFinalGrade();
        Debug.Log($"🌾 [Harvest]: เก็บเกี่ยวสำเร็จ! เกรด: {finalGrade} (คะแนน: {careQualityScore})");

        Color gradeColor = GetGradeColor(finalGrade);
        ShowPopupFeedback($"Grade: {finalGrade}", gradeColor);

        GivePlayerProductWithGrade(plantedPlantData.cropProduct, finalGrade);

        plantedPlantData = null;
        appliedFertilizer = null;
        isMutated = false;
        currentSoilNutrients = new NutrientData();
        needsWaterNow = false;
        needsFertilizerNow = false;
        SwitchStage(CropStage.Empty);
    }

    public void TakeDamageFromEnemy(int damage)
    {
        if (currentStage == CropStage.Empty) return;

        currentPlotHealth = Mathf.Max(0, currentPlotHealth - damage);
        Debug.LogWarning($"⚠ [CropPlots]: แปลง {gameObject.name} ถูกศัตรูโจมตี! HP เหลือ {currentPlotHealth}/{maxPlotHealth}");

        if (currentPlotHealth <= 0)
        {
            DestroyCropCompletely();
        }
    }

    private void DestroyCropCompletely()
    {
        currentStage = CropStage.Empty;
        currentPlotHealth = maxPlotHealth;
        daysGrown = 0;
        waterCooldownTimer = 0f;
        fertilizerCooldownTimer = 0f;
        careQualityScore = 100f;
        plantedPlantData = null;
        appliedFertilizer = null;
        isMutated = false;
        currentSoilNutrients = new NutrientData();
        needsWaterNow = false;
        needsFertilizerNow = false;

        if (currentVisualObject != null)
        {
            Destroy(currentVisualObject);
            currentVisualObject = null;
        }

        Debug.LogError($"💀 [CropPlots]: แปลง {gameObject.name} ถูกทำลายจากศัตรู!");
    }

    private ItemGrade CalculateFinalGrade()
    {
        float diffN = Mathf.Abs(currentSoilNutrients.nitrogen - plantedPlantData.idealNutrients.nitrogen);
        float diffP = Mathf.Abs(currentSoilNutrients.phosphorus - plantedPlantData.idealNutrients.phosphorus);
        float diffK = Mathf.Abs(currentSoilNutrients.potassium - plantedPlantData.idealNutrients.potassium);
        float totalError = diffN + diffP + diffK;

        float finalScore = careQualityScore - (totalError * 0.5f);

        if (finalScore >= 90f) return ItemGrade.S;
        if (finalScore >= 75f) return ItemGrade.A;
        if (finalScore >= 60f) return ItemGrade.B;
        if (finalScore >= 45f) return ItemGrade.C;
        if (finalScore >= 30f) return ItemGrade.D;
        if (finalScore >= 15f) return ItemGrade.E;
        return ItemGrade.F;
    }

    private Color GetGradeColor(ItemGrade grade)
    {
        switch (grade)
        {
            case ItemGrade.S: return new Color(1f, 0.84f, 0f); // ทอง
            case ItemGrade.A: return Color.green;    // เขียว
            case ItemGrade.B: return Color.white;    // ขาว
            case ItemGrade.C: return new Color(1f, 0.5f, 0f); // ส้ม
            case ItemGrade.D: return Color.red;      // แดง
            case ItemGrade.E: return Color.grey;     // เทา
            case ItemGrade.F: return Color.black;    // ดำ
            default: return Color.white;
        }
    }

    private void GivePlayerProductWithGrade(SO_ItemData product, ItemGrade grade)
    {
        if (product == null || ResourceInventory.Instance == null) return;
        int amount = Random.Range(plantedPlantData.minProductAmount, plantedPlantData.maxProductAmount + 1);
        ResourceInventory.Instance.AddResourceWithGrade(product, amount, grade);
    }

    private void SwitchStage(CropStage newStage)
    {
        currentStage = newStage;
        if (currentVisualObject != null) Destroy(currentVisualObject);
        if (plantedPlantData == null) return;

        GameObject prefabToSpawn = (currentStage == CropStage.Growing) ? plantedPlantData.growingPrefab : plantedPlantData.fullyGrownPrefab;
        if (prefabToSpawn != null)
        {
            Vector3 spawnPos = new Vector3(transform.position.x, transform.position.y + plantHeightOffset, transform.position.z);
            currentVisualObject = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity, transform);
        }
    }

    public bool ApplyFertilizer(SO_FertilizerData fertilizer)
    {
        if (fertilizer == null) return false;
        appliedFertilizer = fertilizer;
        currentSoilNutrients.nitrogen += fertilizer.providedNutrients.nitrogen;
        currentSoilNutrients.phosphorus += fertilizer.providedNutrients.phosphorus;
        currentSoilNutrients.potassium += fertilizer.providedNutrients.potassium;
        return true;
    }

}