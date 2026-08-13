using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // ใช้สำหรับ Image (Panel สีดำสำหรับ Fade)
using System.Collections;

public class AirshipVictoryController : MonoBehaviour
{
    [Header("Victory Settings")]
    [Tooltip("ชื่อฉาก (Scene) ที่จะให้ตัดไปเมื่อชนะเกม")]
    public string victorySceneName = "VictoryScene";

    [Tooltip("เวลารอก่อนจะเริ่มเฟดจอ (วินาที) ให้ผู้เล่นได้ชื่นชมเรือเหาะก่อน")]
    public float celebrationDelay = 3f;

    [Tooltip("ระยะเวลาในการทำ Fade หน้าจอให้มืดลง (วินาที)")]
    public float fadeDuration = 1.5f;

    [Header("UI Fade Panel")]
    [Tooltip("ลาก UI Image สีดำเต็มจอมาใส่ตรงนี้ (ถ้าไม่มี ระบบจะสร้างให้อัตโนมัติ)")]
    public Image fadePanel;

    void Start()
    {
        // 🚀 ทันทีที่ Object เรือเหาะถูกสร้าง/Active ขึ้นมาตอนสร้างเสร็จ เริ่มกระบวนการชนะเกมทันที!
        Debug.Log("🎉 [Airship]: สร้างเรือเหาะสำเร็จ! เริ่มกระบวนการนับถอยหลังสู่ชัยชนะ...");
        StartCoroutine(VictorySequenceRoutine());
    }

    IEnumerator VictorySequenceRoutine()
    {
        // 1. รอเวลาให้ผู้เล่นดีใจกับความสำเร็จสักครู่
        yield return new WaitForSeconds(celebrationDelay);

        // 2. เตรียม Panel สีดำสำหรับทำ Background Fade
        if (fadePanel == null)
        {
            fadePanel = CreateRuntimeFadePanel();
        }

        // 3. เริ่มนับเวลา Coroutine เฟดหน้าจอจากใส -> ดำสนิท
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

        // 4. มืดสนิทแล้ว ตัดเข้าสู่ซีนชัยชนะ (Victory Scene)
        Debug.Log($"🎬 [Victory]: โหลดฉากชัยชนะ -> {victorySceneName}");
        SceneManager.LoadScene(victorySceneName);
    }

    // ฟังก์ชันสำรอง: สร้าง UI Panel สีดำให้อัตโนมัติถ้าลืมทำใน Canvas
    private Image CreateRuntimeFadePanel()
    {
        GameObject canvasObj = new GameObject("RuntimeFadeCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999; // ให้อยู่บนสุดเสมอ
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

        DontDestroyOnLoad(canvasObj); // ป้องกัน Canvas หายระหว่างเปลี่ยนซีน
        return img;
    }
}