using UnityEngine;
using UnityEngine.Rendering;
using System;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;

public enum Season { Spring, Summer, Autumn, Winter }

public class DayNightManager : MonoBehaviour
{
    public static DayNightManager Instance { get; private set; }

    [Header("Time Settings")]
    public float dayDuration = 300f; // 5 นาทีต่อ 1 วัน
    private float currentTime = 0f;

    [Header("Calendar Settings")]
    public int currentDay = 1;
    public int daysPerSeason = 4;
    public Season currentSeason = Season.Spring;

    public bool isNightTime;

    [Header("Monster Attack Settings (24h format)")]
    [Tooltip("เปิด/ปิดระบบมอนสเตอร์บุกตามเวลา")]
    public bool enableMonsterAttacks = true;
    [Tooltip("เวลาเริ่มต้นที่มอนสเตอร์จะบุก (เช่น 2.0 คือ ตี 2)")]
    public float monsterAttackStartHour = 2.0f;
    [Tooltip("เวลาสิ้นสุดที่มอนสเตอร์จะหยุดบุก (เช่น 5.0 คือ ตี 5)")]
    public float monsterAttackEndHour = 5.0f;

    [Header("Lighting Settings")]
    public Light directionalLight;
    public float dayLightRotationX = 50f;
    public float nightLightRotationX = -89.9f;
    public float lightRotationY = 0f;
    public float lightRotationZ = 0f;

    [Header("Lighting Color & Intensity")]
    public Gradient sunColorGradient;
    public AnimationCurve sunIntensityCurve = AnimationCurve.EaseInOut(0f, 0.05f, 1f, 0.05f);
    public float maxSunIntensity = 1.3f;

    [Header("Ambient / Fog (Optional)")]
    public bool controlAmbientAndFog = true;
    public Gradient ambientColorGradient;
    public Gradient fogColorGradient;

    // Events สำหรับแจ้งเตือนระบบอื่น
    public event Action<bool> OnTimeChanged;
    public event Action<int> OnDayChanged;
    public event Action<Season> OnSeasonChanged;

    [Header("Global Volume Settings")]
    public Volume dayGlobalVolume;
    public Volume nightGlobalVolume;
    public float volumeBlendDuration = 2.5f;
    private Coroutine volumeBlendRoutine;

    [Header("Victory & Defeat Settings")]
    public string victorySceneName = "VictoryScene";
    public string defeatSceneName = "VictoryScene";
    public float celebrationDelay = 3f;
    public float fadeDuration = 1.5f;
    public Image fadePanel;

    public int activeBuildingCount = 0;
    public int activeUnitCount = 0;
    private bool isGameOverTriggered = false;

    public void RegisterBuilding(bool isAdding) => activeBuildingCount += isAdding ? 1 : -1;
    public void RegisterUnit(bool isAdding) => activeUnitCount += isAdding ? 1 : -1;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 🟢 ตั้งค่าให้เวลาเริ่มต้นตอน 06:00 น. (6 โมงเช้า) ของวันแรก แทนที่จะเป็น 00:00 น.
        currentTime = dayDuration * (6.0f / 24.0f);

        if (sunColorGradient == null || sunColorGradient.colorKeys.Length == 0)
            sunColorGradient = BuildDefaultSunGradient();

        if (ambientColorGradient == null || ambientColorGradient.colorKeys.Length == 0)
            ambientColorGradient = BuildDefaultAmbientGradient();

        if (fogColorGradient == null || fogColorGradient.colorKeys.Length == 0)
            fogColorGradient = BuildDefaultAmbientGradient();
    }

    void Start()
    {
        if (GameFlowManager.Instance != null)
        {
            currentSeason = GameFlowManager.SelectedSeason;
        }

        OnDayChanged?.Invoke(currentDay);
        OnSeasonChanged?.Invoke(currentSeason);
        OnTimeChanged?.Invoke(isNightTime);

        if (dayGlobalVolume != null) dayGlobalVolume.gameObject.SetActive(true);
        if (nightGlobalVolume != null) nightGlobalVolume.gameObject.SetActive(true);
        ApplyVolumeWeightsInstant(isNightTime ? 1f : 0f);
    }

    void Update()
    {
        if (isGameOverTriggered) return;

        float previousTime = currentTime;
        currentTime += Time.deltaTime;

        if (Mathf.FloorToInt(currentTime / dayDuration) > Mathf.FloorToInt(previousTime / dayDuration))
        {
            AdvanceDay();
        }

        float timeNormalized = (currentTime % dayDuration) / dayDuration;
        UpdateLightingRotation(timeNormalized);
        UpdateLightingColor(timeNormalized);
        UpdateAmbientAndFog(timeNormalized);

        var (currentHour, currentMinute) = GetGameTime();
        float decimalHour = currentHour + (currentMinute / 60f);

        // วันที่ 1 ปลอดภัย (Free Farm), วันถัดไปบุกช่วง 02:00 - 05:00 น.
        bool shouldBeNight = enableMonsterAttacks &&
                             (decimalHour >= monsterAttackStartHour && decimalHour < monsterAttackEndHour) &&
                             (currentDay > 1);

        if (shouldBeNight != isNightTime)
        {
            isNightTime = shouldBeNight;
            UpdateGlobalVolumeBlend(isNightTime);
            OnTimeChanged?.Invoke(isNightTime);
            Debug.Log(isNightTime ? $"🌙 [Time]: ช่วงเวลาบุกเริ่มแล้ว ({currentHour:00}:{currentMinute:00} น.)" : $"☀️ [Time]: กลับสู่เวลาปกติ ({currentHour:00}:{currentMinute:00} น.)");
        }
    }

    public float GetDayProgress()
    {
        return (currentTime % dayDuration) / dayDuration;
    }

    public (int hour, int minute) GetGameTime()
    {
        float progress = GetDayProgress();
        float totalMinutes = progress * 24f * 60f;
        int hour = Mathf.FloorToInt(totalMinutes / 60f);
        int minute = Mathf.FloorToInt(totalMinutes % 60f);
        return (hour, minute);
    }

    public void CheckLossEvent()
    {
        if (activeBuildingCount <= 0 && activeUnitCount <= 0 && !isGameOverTriggered)
        {
            isGameOverTriggered = true;
            Debug.LogError("💀 [Game Over]: ตึกและยูนิตหมดแล้ว... พ่ายแพ้!");
            TriggerDefeat();
        }
    }

    private void TriggerDefeat() => StartCoroutine(DefeatSequenceRoutine());

    IEnumerator DefeatSequenceRoutine()
    {
        yield return new WaitForSeconds(0.5f);
        if (fadePanel == null) fadePanel = CreateRuntimeFadePanel();

        float elapsedTime = 0f;
        Color panelColor = fadePanel.color;
        panelColor.a = 0f;
        fadePanel.color = panelColor;
        fadePanel.gameObject.SetActive(true);

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            panelColor.a = Mathf.Clamp01(elapsedTime / fadeDuration);
            fadePanel.color = panelColor;
            yield return null;
        }

        SceneManager.LoadScene(defeatSceneName);
    }

    private void UpdateLightingRotation(float timeNormalized)
    {
        if (directionalLight == null) return;
        float targetRotationX = Mathf.Lerp(-90f, 270f, timeNormalized);
        directionalLight.transform.rotation = Quaternion.Euler(targetRotationX, lightRotationY, lightRotationZ);
    }

    private void UpdateLightingColor(float timeNormalized)
    {
        if (directionalLight == null) return;
        directionalLight.color = sunColorGradient.Evaluate(timeNormalized);
        directionalLight.intensity = sunIntensityCurve.Evaluate(timeNormalized) * maxSunIntensity;
    }

    private void UpdateAmbientAndFog(float timeNormalized)
    {
        if (!controlAmbientAndFog) return;
        RenderSettings.ambientLight = ambientColorGradient.Evaluate(timeNormalized);
        if (RenderSettings.fog) RenderSettings.fogColor = fogColorGradient.Evaluate(timeNormalized);
    }

    private void AdvanceDay()
    {
        currentDay++;
        OnDayChanged?.Invoke(currentDay);
        Debug.Log($"🗓️ [Calendar]: เริ่มต้นวันที่ {currentDay}");

        if (currentDay > 30)
        {
            TriggerGameOver();
            return;
        }

        CalculateSeason();
    }

    private void CalculateSeason()
    {
        int seasonIndex = ((currentDay - 1) / daysPerSeason) % 4;
        Season newSeason = (Season)seasonIndex;

        if (newSeason != currentSeason)
        {
            currentSeason = newSeason;
            OnSeasonChanged?.Invoke(currentSeason);
            Debug.Log($"🌾 [Calendar]: Season changed to ➡️ <b>{currentSeason}</b>!");

            if (ShopOrderManager.Instance != null)
            {
                ShopOrderManager.Instance.GenerateDailyProceduralOrders();
            }
        }
    }

    private void UpdateGlobalVolumeBlend(bool isNight)
    {
        if (dayGlobalVolume == null || nightGlobalVolume == null) return;
        if (volumeBlendRoutine != null) StopCoroutine(volumeBlendRoutine);
        volumeBlendRoutine = StartCoroutine(BlendVolumesRoutine(isNight ? 1f : 0f));
    }

    IEnumerator BlendVolumesRoutine(float targetNightWeight)
    {
        float startNightWeight = nightGlobalVolume.weight;
        float elapsed = 0f;

        while (elapsed < volumeBlendDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / volumeBlendDuration);
            float smoothT = t * t * (3f - 2f * t);
            float nightWeight = Mathf.Lerp(startNightWeight, targetNightWeight, smoothT);
            ApplyVolumeWeightsInstant(nightWeight);
            yield return null;
        }

        ApplyVolumeWeightsInstant(targetNightWeight);
        volumeBlendRoutine = null;
    }

    private void ApplyVolumeWeightsInstant(float nightWeight)
    {
        nightWeight = Mathf.Clamp01(nightWeight);
        if (nightGlobalVolume != null) nightGlobalVolume.weight = nightWeight;
        if (dayGlobalVolume != null) dayGlobalVolume.weight = 1f - nightWeight;
    }

    private void TriggerGameOver()
    {
        Debug.Log("🎉 [Victory]: ครบกำหนดวันแล้ว... ชัยชนะ!");
        StartCoroutine(VictorySequenceRoutine());
    }

    IEnumerator VictorySequenceRoutine()
    {
        yield return new WaitForSeconds(0.5f);
        if (fadePanel == null) fadePanel = CreateRuntimeFadePanel();

        float elapsedTime = 0f;
        Color panelColor = fadePanel.color;
        panelColor.a = 0f;
        fadePanel.color = panelColor;
        fadePanel.gameObject.SetActive(true);

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            panelColor.a = Mathf.Clamp01(elapsedTime / fadeDuration);
            fadePanel.color = panelColor;
            yield return null;
        }

        SceneManager.LoadScene(victorySceneName);
    }

    private Image CreateRuntimeFadePanel()
    {
        GameObject canvasObj = new GameObject("RuntimeFadeCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject panelObj = new GameObject("FadePanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        Image img = panelObj.AddComponent<Image>();
        img.color = Color.black;

        RectTransform rect = img.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        return img;
    }

    private Gradient BuildDefaultSunGradient()
    {
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.01f, 0.02f, 0.08f), 0.00f), // 00.00 น. - มืดสนิท
                new GradientColorKey(new Color(0.15f, 0.20f, 0.35f), 0.30f), // 07.00 น. - เริ่มสว่าง
                new GradientColorKey(new Color(1.00f, 0.95f, 0.85f), 0.35f), // 08.00 น. - สว่าง (เริ่มกลางวัน)
                new GradientColorKey(new Color(1.00f, 1.00f, 1.00f), 0.50f), // 12.00 น. - เที่ยงวัน
                new GradientColorKey(new Color(1.00f, 0.85f, 0.60f), 0.75f), // 18.00 น. - เย็น (สิ้นสุดกลางวัน)
                new GradientColorKey(new Color(0.30f, 0.15f, 0.30f), 0.80f), // 19.00 น. - พลบค่ำ
                new GradientColorKey(new Color(0.01f, 0.02f, 0.08f), 0.85f), // 20.00 น. - มืด
                new GradientColorKey(new Color(0.01f, 0.02f, 0.08f), 1.00f),
            },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
        );
        return g;
    }

    private Gradient BuildDefaultAmbientGradient()
    {
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.02f, 0.02f, 0.05f), 0.00f),
                new GradientColorKey(new Color(0.60f, 0.60f, 0.65f), 0.35f),
                new GradientColorKey(new Color(0.80f, 0.80f, 0.80f), 0.50f),
                new GradientColorKey(new Color(0.50f, 0.45f, 0.45f), 0.75f),
                new GradientColorKey(new Color(0.02f, 0.02f, 0.05f), 0.85f),
                new GradientColorKey(new Color(0.02f, 0.02f, 0.05f), 1.00f),
            },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
        );
        return g;
    }

    #region ContextMenu Test
    [ContextMenu("Debug / Check Active Buildings & Units")]
    public void ContextMenuCheckStatus()
    {
        Debug.Log($"📊 [Debug Status]: ตึก = {activeBuildingCount} | ยูนิต = {activeUnitCount}");
    }

    [ContextMenu("Debug / Clear All Registers (Force Defeat)")]
    public void ContextMenuClearAllRegisters()
    {
        activeBuildingCount = 0;
        activeUnitCount = 0;
        CheckLossEvent();
    }

    [ContextMenu("Debug / Reset Counters")]
    public void ContextMenuResetCounters()
    {
        BuildingHealth[] buildings = FindObjectsByType<BuildingHealth>(FindObjectsSortMode.None);
        UnitBase[] units = FindObjectsByType<UnitBase>(FindObjectsSortMode.None);
        activeBuildingCount = buildings != null ? buildings.Length : 0;
        activeUnitCount = units != null ? units.Length : 0;
    }

    [ContextMenu("Force Test / Switch to Day (12:00)")]
    public void ForceTestDay() { currentTime = dayDuration * 0.5f; }
    [ContextMenu("Force Test / Switch to Midnight (00:00)")]
    public void ForceTestMidnight() { currentTime = 0f; }
    [ContextMenu("Force Test / Switch to Attack Time (02:00)")]
    public void ForceTestAttackTime() { currentTime = dayDuration * (2.0f / 24.0f); }
    [ContextMenu("Force Test / Next Day")]
    public void ForceTestNextDay() { AdvanceDay(); currentTime = (currentDay - 1) * dayDuration; }
    #endregion
}