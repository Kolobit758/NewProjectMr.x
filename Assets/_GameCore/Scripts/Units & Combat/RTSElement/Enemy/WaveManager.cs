

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

    // 🟢 ระบบปล่อยมอนสเตอร์เป็นเฟส พร้อมยิง UI แจ้งเตือน
    IEnumerator TriggerPhasedWaveRoutine()
    {
        int currentDay = DayNightManager.Instance != null ? DayNightManager.Instance.currentDay : 1;
        Debug.Log($"🚨 [WaveManager]: คลื่นศัตรูบุกฐานคืนที่ {currentDay} เริ่มต้นแล้ว!");

        for (int phase = 1; phase <= wavesPerNight; phase++)
        {
            if (DayNightManager.Instance != null && !DayNightManager.Instance.isNightTime) yield break;

            // 🌟 แสดง UI แจ้งเตือนกลางจอ (เช่น "⚠️ WAVE 1 / 3")
            StartCoroutine(ShowWaveAlertRoutine($"⚠️ WAVE {phase} / {wavesPerNight}"));

            Debug.Log($"⚔️ [WaveManager]: เข้าสู่เวฟย่อยที่ <b>{phase} / {wavesPerNight}</b> ของคืนนี้!");

            SpawnWaveBatch(phase, currentDay);

            // ถ้าระหว่างเวฟยังไม่ใช่เวฟสุดท้าย ให้รอก่อนจะขึ้นเวฟถัดไป (ช่วงพักหายใจ)
            if (phase < wavesPerNight)
            {
                StartCoroutine(ShowWaveAlertRoutine($"☕ BREAK TIME... (Wave {phase + 1} Incoming)", Color.cyan));
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

    private void SpawnWaveBatch(int phase, int currentDay)
    {
        List<SO_EnemyData> enemiesToSpawn = DetermineAttractedEnemiesFromFarm();

        if (enemiesToSpawn == null || enemiesToSpawn.Count == 0)
        {
            if (allAvailableEnemies.Count > 0)
            {
                enemiesToSpawn.Add(allAvailableEnemies[Random.Range(0, allAvailableEnemies.Count)]);
            }
            else return;
        }

        int baseCount = 2 + (currentDay * 1);
        int waveCount = baseCount + (phase * 2);

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

            float dayMultiplier = 1f + (currentDay * 0.1f);
            float phaseMultiplier = 1f + (phase * 0.15f);

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