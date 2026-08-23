

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [Header("Enemy Database (สำรองกรณีฟาร์มว่างเปล่า)")]
    public List<SO_EnemyData> allAvailableEnemies = new List<SO_EnemyData>();

    [Header("Spawn Points")]
    public Transform[] enemySpawnPoints;

    [Header("Wave Phase Settings")]
    [Tooltip("จำนวนระลอกย่อยใน 1 คืน (เช่น 3 เวฟ)")]
    public int wavesPerNight = 3;
    [Tooltip("เวลารอระหว่างแต่ละเวฟย่อย (วินาที) ให้ผู้เล่นพักหายใจ")]
    public float timeBetweenPhases = 5f;

    [Header("Raid Settings")]
    [Tooltip("วันที่จะมี Raid (ศัตรูบุกหนักกว่าปกติ) เช่น [4, 6]")]
    public int[] raidDays = new int[] { 4, 6 };
    [Tooltip("ตัวคูณจำนวนศัตรูในคืน Raid (เช่น 2.5 = เยอะกว่าปกติ 2.5 เท่า)")]
    public float raidMultiplier = 2.5f;
    [Tooltip("จำนวนเวฟย่อยในคืน Raid (มากกว่าคืนปกติ)")]
    public int raidWavesPerNight = 5;

    [Header("Wave UI Alert Settings")]
    [Tooltip("ลาก TextMeshProUGUI บนหน้าจอ Canvas มาใส่ตรงนี้ (ถ้าไม่ใส่ ระบบจะสร้างให้เองอัตโนมัติ)")]
    public TextMeshProUGUI waveAlertText;
    [Tooltip("ระยะเวลาที่ข้อความเตือนจะแสดงบนหน้าจอ (วินาที)")]
    public float alertDisplayDuration = 2.5f;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (DayNightManager.Instance != null)
        {
            DayNightManager.Instance.OnTimeChanged += HandleTimeChanged;
        }

        // ตรวจสอบหรือสร้าง UI เตือนอัตโนมัติถ้ายังไม่ได้ลากใส่
        CheckAndCreateWaveUI();
    }

    private void HandleTimeChanged(bool isNight)
    {
        if (isNight)
        {
            StartCoroutine(TriggerPhasedWaveRoutine());
        }
    }

    // 🟢 เช็คว่าวันที่กำหนดเป็น Raid Day หรือไม่
    private bool IsRaidDay(int day)
    {
        if (raidDays == null) return false;
        foreach (int raidDay in raidDays)
        {
            if (day == raidDay) return true;
        }
        return false;
    }

    // 🟢 ระบบปล่อยมอนสเตอร์เป็นเฟส พร้อมยิง UI แจ้งเตือน
    IEnumerator TriggerPhasedWaveRoutine()
    {
        int currentDay = DayNightManager.Instance != null ? DayNightManager.Instance.currentDay : 1;
        bool isRaid = IsRaidDay(currentDay);
        int totalWaves = isRaid ? raidWavesPerNight : wavesPerNight;

        if (isRaid)
        {
            // 🔴 แจ้งเตือนพิเศษก่อนเริ่ม Raid
            Debug.LogWarning($"🔴 [RAID NIGHT]: คืน RAID บุกวันที่ {currentDay}! เตรียมรับมือ!");
            yield return StartCoroutine(ShowWaveAlertRoutine($"🔴 !! RAID NIGHT - DAY {currentDay} !!", new Color(1f, 0.3f, 0f)));
            yield return new WaitForSeconds(1.5f);
        }
        else
        {
            Debug.Log($"🚨 [WaveManager]: คลื่นศัตรูบุกฐานคืนที่ {currentDay} เริ่มต้นแล้ว!");
        }

        for (int phase = 1; phase <= totalWaves; phase++)
        {
            if (DayNightManager.Instance != null && !DayNightManager.Instance.isNightTime) yield break;

            // 🌟 แสดง UI แจ้งเตือนกลางจอ
            Color alertColor = isRaid ? new Color(1f, 0.3f, 0f) : Color.red;
            string alertPrefix = isRaid ? "🔴 RAID" : "⚠️ WAVE";
            StartCoroutine(ShowWaveAlertRoutine($"{alertPrefix} {phase} / {totalWaves}", alertColor));

            Debug.Log($"⚔️ [WaveManager]: เข้าสู่เวฟย่อยที่ <b>{phase} / {totalWaves}</b> ของคืนนี้! (Raid: {isRaid})");

            SpawnWaveBatch(phase, currentDay, isRaid);

            // ถ้าระหว่างเวฟยังไม่ใช่เวฟสุดท้าย ให้รอก่อนจะขึ้นเวฟถัดไป (ช่วงพักหายใจ)
            if (phase < totalWaves)
            {
                Color breakColor = isRaid ? new Color(1f, 0.6f, 0f) : Color.cyan;
                StartCoroutine(ShowWaveAlertRoutine($"☕ BREAK... (Wave {phase + 1} Incoming)", breakColor));
                yield return new WaitForSeconds(timeBetweenPhases);
            }
        }
    }

    // 🟢 โคโรทีนสำหรับโชว์ข้อความเตือนกลางจอแบบเฟดขยาย
    IEnumerator ShowWaveAlertRoutine(string message, Color textColor = default)
    {
        if (waveAlertText == null) yield break;

        if (textColor == default) textColor = Color.red; // ค่าเริ่มต้นสีแดงเตือนภัย

        waveAlertText.text = message;
        waveAlertText.color = textColor;
        waveAlertText.gameObject.SetActive(true);

        float timer = 0f;
        RectTransform rect = waveAlertText.rectTransform;

        // ทำแอนิเมชันขยายตัวหนังสือเล็กน้อยตอนเด้งขึ้นมา
        while (timer < alertDisplayDuration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Sin((timer / alertDisplayDuration) * Mathf.PI); // ค่อยๆ ชัดแล้วจางลง

            Color c = waveAlertText.color;
            c.a = alpha > 0 ? 1f : 0f; // ควบคุมความโปร่งใส
            waveAlertText.color = c;

            yield return null;
        }

        waveAlertText.gameObject.SetActive(false);
    }

    private void CheckAndCreateWaveUI()
    {
        if (waveAlertText != null) return;

        // ถ้าใน Scene ไม่มี UI Text ให้สร้างให้อัตโนมัติทันที จะได้ไม่ Error
        GameObject canvasObj = GameObject.Find("Canvas");
        if (canvasObj == null)
        {
            canvasObj = new GameObject("RuntimeCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        GameObject textObj = new GameObject("WaveAlertText");
        textObj.transform.SetParent(canvasObj.transform, false);

        waveAlertText = textObj.AddComponent<TextMeshProUGUI>();
        waveAlertText.fontSize = 48;
        waveAlertText.alignment = TextAlignmentOptions.Center;
        waveAlertText.fontStyle = FontStyles.Bold;

        RectTransform rect = waveAlertText.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.7f); // วางไว้ค่อนไปทางด้านบนกลางจอ
        rect.anchorMax = new Vector2(0.5f, 0.7f);
        rect.sizeDelta = new Vector2(600, 100);

        waveAlertText.gameObject.SetActive(false);
    }

    private void SpawnWaveBatch(int phase, int currentDay, bool isRaid = false)
    {
        List<SO_EnemyData> enemiesToSpawn = DetermineAttractedEnemiesFromFarm();

        if (enemiesToSpawn == null || enemiesToSpawn.Count == 0)
        {
            if (allAvailableEnemies.Count > 0)
            {
                enemiesToSpawn = new List<SO_EnemyData>();
                enemiesToSpawn.Add(allAvailableEnemies[Random.Range(0, allAvailableEnemies.Count)]);
            }
            else return;
        }

        // เริ่มต้นเบาๆ วันที่ 1 อาจจะมีแค่ 1-2 ตัว แล้วค่อยๆ ไต่ขึ้น
        int baseCount = Mathf.Max(1, currentDay - 1);
        int waveCount = baseCount + phase;

        // 🔴 ถ้าเป็น Raid Night ให้คูณจำนวนศัตรูด้วย raidMultiplier
        if (isRaid)
        {
            waveCount = Mathf.RoundToInt(waveCount * raidMultiplier);
            Debug.LogWarning($"🔴 [RAID]: เวฟ {phase} — spawn {waveCount} ตัว (×{raidMultiplier})");
        }

        for (int i = 0; i < waveCount; i++)
        {
            SO_EnemyData chosenEnemy = enemiesToSpawn[Random.Range(0, enemiesToSpawn.Count)];
            SpawnEnemy(chosenEnemy, currentDay, phase);
        }
    }

    private List<SO_EnemyData> DetermineAttractedEnemiesFromFarm()
    {
        CropPlots[] allPlots = Object.FindObjectsByType<CropPlots>(FindObjectsSortMode.None);
        List<SO_EnemyData> attractedEnemiesList = new List<SO_EnemyData>();

        if (allPlots == null || allPlots.Length == 0) return null;

        foreach (var plot in allPlots)
        {
            if (plot.currentStage != CropStage.Empty && plot.plantedPlantData != null)
            {
                SO_PlantData plantData = plot.plantedPlantData as SO_PlantData;

                if (plantData != null && plantData.specificAttractedEnemies != null)
                {
                    foreach (var enemyData in plantData.specificAttractedEnemies)
                    {
                        if (enemyData != null && !attractedEnemiesList.Contains(enemyData))
                        {
                            attractedEnemiesList.Add(enemyData);
                        }
                    }
                }
            }
        }

        return attractedEnemiesList;
    }

    private void SpawnEnemy(SO_EnemyData enemyData, int currentDay, int phase)
    {
        if (enemySpawnPoints == null || enemySpawnPoints.Length == 0 || enemyData == null || enemyData.enemyPrefab == null) return;

        Transform spawnPoint = enemySpawnPoints[Random.Range(0, enemySpawnPoints.Length)];
        GameObject enemyObj = Instantiate(enemyData.enemyPrefab, spawnPoint.position, Quaternion.identity);
        MinimapMarker icon = enemyObj.GetComponent<MinimapMarker>();
        if (icon == null)
        {
            icon = enemyObj.AddComponent<MinimapMarker>();
            icon.teamType = TeamType.Enemy;
        }

        EnemyController controller = enemyObj.GetComponent<EnemyController>();
        if (controller != null)
        {
            controller.enemyData = enemyData;

            // ลดความรุนแรงของการคูณสเตตัสในช่วงวันแรกๆ
            float dayMultiplier = 1f + ((currentDay - 1) * 0.05f); // ลดสเกลความอึกถึก
            float phaseMultiplier = 1f + (phase * 0.1f);

            CharacterStats enemyStats = enemyObj.GetComponent<CharacterStats>();
            if (enemyStats != null)
            {
                int[] privateData = enemyStats.GetPrivateData();
                int maxHP = privateData[0];
                maxHP = Mathf.RoundToInt(maxHP * dayMultiplier * phaseMultiplier);
                enemyStats.currentHP = maxHP;

            }
        }
    }
}