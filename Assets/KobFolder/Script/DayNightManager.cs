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
    public int currentDay = 1;          // วันที่ปัจจุบันในเกม
    public int daysPerSeason = 4;       // 1 ฤดู มีกี่วัน
    public Season currentSeason = Season.Spring; // ฤดูกาลปัจจุบัน

    public bool isNightTime;
    [Header("Lighting Settings")]
    public Light directionalLight;
    public float dayLightRotationX = 50f;
    public float nightLightRotationX = -89.9f;
    public float lightRotationY = 0f;
    public float lightRotationZ = 0f;

    // Events สำหรับแจ้งเตือนระบบอื่น
    public event Action<bool> OnTimeChanged;
    public event Action<int> OnDayChanged;
    public event Action<Season> OnSeasonChanged;

    [Header("Global Volume Settings")]
    public Volume dayGlobalVolume;
    public Volume nightGlobalVolume;

    [Header("Victory & Defeat Settings")]
    [Tooltip("ชื่อฉาก (Scene) ที่จะให้ตัดไปเมื่อชนะเกม")]
    public string victorySceneName = "VictoryScene";
    [Tooltip("ชื่อฉาก (Scene) ที่จะให้ตัดไปเมื่อแพ้เกม")]
    public string defeatSceneName = "VictoryScene"; // 🟢 เพิ่มชื่อฉากแพ้

    [Tooltip("เวลารอก่อนจะเริ่มเฟดจอ (วินาที)")]
    public float celebrationDelay = 3f;
    [Tooltip("ระยะเวลาในการทำ Fade หน้าจอให้มืดลง (วินาที)")]
    public float fadeDuration = 1.5f;

    [Header("UI Fade Panel")]
    public Image fadePanel;

    // 🟢 ตัวแปรสำหรับเช็คเงื่อนไขแพ้
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

        bool nightStatus = timeNormalized >= 0.5f;

        if (nightStatus != isNightTime)
        {
            isNightTime = nightStatus;
            UpdateGlobalVolumeBlend(isNightTime);
            OnTimeChanged?.Invoke(isNightTime);
            Debug.Log(isNightTime ? "🌙 [Time]: Night time started." : "☀️ [Time]: Day time started.");
        }

        // 🟢 ตรวจสอบเงื่อนไขแพ้ทุกๆ 2 วินาที (ประหยัด Performance ไม่ต้องเช็คทุกเฟรม)
    }

    // 💀 ฟังก์ชันเช็คว่า Unit และ Building หมดเกลี้ยงแม็ปหรือยัง
    public void CheckLossEvent()
    {
        if (activeBuildingCount <= 0 && activeUnitCount <= 0 && !isGameOverTriggered)
        {
            isGameOverTriggered = true;
            Debug.LogError("💀 [Game Over]: ตึกและยูนิตหมดแล้ว... พ่ายแพ้!");
            TriggerDefeat();
        }
    }

    private void TriggerDefeat()
    {
        StartCoroutine(DefeatSequenceRoutine());
    }

    // 🎬 โคโรทีนสำหรับเฟดจอดำแล้วตัดไปหน้าจอแพ้ (Defeat Scene)
    IEnumerator DefeatSequenceRoutine()
    {
        yield return new WaitForSeconds(0.5f);

        if (fadePanel == null)
        {
            fadePanel = CreateRuntimeFadePanel();
        }

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

        Debug.Log($"🎬 [Defeat]: โหลดฉากพ่ายแพ้ -> {defeatSceneName}");
        SceneManager.LoadScene(defeatSceneName);
    }

    private void UpdateLightingRotation(float timeNormalized)
    {
        if (directionalLight == null) return;

        float targetRotationX;
        if (timeNormalized < 0.5f)
        {
            float t = timeNormalized * 2f;
            targetRotationX = Mathf.Lerp(dayLightRotationX, nightLightRotationX, t);
        }
        else
        {
            float t = (timeNormalized - 0.5f) * 2f;
            targetRotationX = Mathf.Lerp(nightLightRotationX, dayLightRotationX + 360f, t);
        }

        directionalLight.transform.rotation = Quaternion.Euler(targetRotationX, lightRotationY, lightRotationZ);
    }

    private void AdvanceDay()
    {
        currentDay++;
        OnDayChanged?.Invoke(currentDay);
        Debug.Log($"🗓️ [Calendar]: New day started! Day {currentDay}");

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

    [ContextMenu("Force Test / Switch to Day")]
    public void ForceTestDay()
    {
        currentTime = Mathf.Floor(currentTime / dayDuration) * dayDuration;
        bool oldNightStatus = isNightTime;
        isNightTime = false;

        float t = (currentTime % dayDuration) / dayDuration;
        UpdateLightingRotation(t);
        UpdateGlobalVolumeBlend(isNightTime);

        if (oldNightStatus != isNightTime) OnTimeChanged?.Invoke(isNightTime);
        Debug.Log("☀️ [ContextMenu]: บังคับเปลี่ยนเป็น 'กลางวัน' สำเร็จ!");
    }

    [ContextMenu("Force Test / Switch to Night")]
    public void ForceTestNight()
    {
        currentTime = (Mathf.Floor(currentTime / dayDuration) * dayDuration) + (dayDuration * 0.75f);
        bool oldNightStatus = isNightTime;
        isNightTime = true;

        float t = (currentTime % dayDuration) / dayDuration;
        UpdateLightingRotation(t);
        UpdateGlobalVolumeBlend(isNightTime);

        if (oldNightStatus != isNightTime) OnTimeChanged?.Invoke(isNightTime);
        Debug.Log("🌙 [ContextMenu]: บังคับเปลี่ยนเป็น 'กลางคืน' สำเร็จ!");
    }

    [ContextMenu("Force Test / Next Day")]
    public void ForceTestNextDay()
    {
        AdvanceDay();
        currentTime = (currentDay - 1) * dayDuration;
        Debug.Log($"🗓️ [ContextMenu]: บังคับข้ามมาที่ Day {currentDay} สำเร็จ!");
    }

    private void UpdateGlobalVolumeBlend(bool isNight)
    {
        if (dayGlobalVolume != null)
            dayGlobalVolume.gameObject.SetActive(!isNight);

        if (nightGlobalVolume != null)
            nightGlobalVolume.gameObject.SetActive(isNight);
    }

    private void TriggerGameOver()
    {
        Debug.LogError("💀 [Game Over]: ครบกำหนด 30 วันแล้วแต่ยังสร้างเรือเหาะไม่เสร็จ... คุณแพ้แล้ว!");
        StartCoroutine(VictorySequenceRoutine());
    }

    IEnumerator VictorySequenceRoutine()
    {
        yield return new WaitForSeconds(0.5f);

        if (fadePanel == null)
        {
            fadePanel = CreateRuntimeFadePanel();
        }

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

        Debug.Log($"🎬 [Victory]: โหลดฉากชัยชนะ -> {victorySceneName}");
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

    #region Test
    // [Header("🧪 Test & Debug Tools (ContextMenu)")]

    // 🔍 ปุ่มที่ 1: ตรวจสอบสถานะปัจจุบันของตึกและยูนิตในระบบ
    [ContextMenu("Debug / Check Active Buildings & Units")]
    public void ContextMenuCheckStatus()
    {
        Debug.Log($"📊 [Debug Status]: จำนวนตึกที่เหลืออยู่ = {activeBuildingCount} | จำนวนยูนิตที่เหลืออยู่ = {activeUnitCount}");
    }

    // 🧹 ปุ่มที่ 2: จำลองเหตุการณ์ล้างข้อมูล / เคลียร์ Counter ทั้งหมดให้เป็นศูนย์ (เพื่อเทสฉาก Game Over)
    [ContextMenu("Debug / Clear All Registers (Force Defeat)")]
    public void ContextMenuClearAllRegisters()
    {
        activeBuildingCount = 0;
        activeUnitCount = 0;
        Debug.LogWarning("⚠️ [Debug Test]: ล้างข้อมูล Register ทั้งหมดเรียบร้อย (บังคับให้จำนวนตึกและยูนิตเป็น 0)");

        // สั่งเช็คความพ่ายแพ้ทันที
        CheckLossEvent();
    }

    // 🔄 ปุ่มที่ 3: รีเซ็ตค่า Counter กลับมาเป็นค่าเริ่มต้น (เผื่ออยากเทสต่อไม่ให้เกมตัดจบ)
    [ContextMenu("Debug / Reset Counters to Default")]
    public void ContextMenuResetCounters()
    {
        // สั่งกวาดนับจำนวนใหม่หรือเซ็ตค่าจำลอง
        BuildingHealth[] buildings = FindObjectsByType<BuildingHealth>(FindObjectsSortMode.None);
        UnitBase[] units = FindObjectsByType<UnitBase>(FindObjectsSortMode.None);

        activeBuildingCount = buildings != null ? buildings.Length : 0;
        activeUnitCount = units != null ? units.Length : 0;

        Debug.Log($"🔄 [Debug Test]: รีเซ็ต Counter สำเร็จ! ตึก: {activeBuildingCount}, ยูนิต: {activeUnitCount}");
    }
    #endregion
}