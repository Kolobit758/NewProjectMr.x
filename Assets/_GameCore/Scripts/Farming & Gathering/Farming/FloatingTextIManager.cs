using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class FloatingTextManager : MonoBehaviour
{
    public static FloatingTextManager Instance { get; private set; }

    [Header("Prefab สำหรับ Text (ถ้ามี)")]
    public GameObject floatingTextPrefab;

    private Canvas uiCanvas;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        EnsureCanvasSetup();
    }

    private void EnsureCanvasSetup()
    {
        if (uiCanvas == null)
        {
            // ค้นหา Canvas ที่เป็น ScreenSpaceOverlay ในฉาก
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var c in canvases)
            {
                if (c != null && c.renderMode == RenderMode.ScreenSpaceOverlay && c.enabled)
                {
                    uiCanvas = c;
                    break;
                }
            }

            // ถ้าหาไม่เจอ ให้สร้าง Canvas Overlay สำหรับยิง UI Popup ขึ้นมาออโต้
            if (uiCanvas == null)
            {
                GameObject canvasObj = new GameObject("ResourceFeedbackCanvas");
                uiCanvas = canvasObj.AddComponent<Canvas>();
                uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                uiCanvas.sortingOrder = 9999; // ให้อยู่ชั้นบนสุดเสมอ

                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);

                canvasObj.AddComponent<GraphicRaycaster>();
                DontDestroyOnLoad(canvasObj);
            }
        }
    }

    /// <summary>
    /// 🟢 แสดง Floating Text ทั่วไปตามพิกัดโลก หรือพิกัดหน้าจอ
    /// </summary>
    public void ShowText(Vector3 worldPos, string text, Color color)
    {
        if (floatingTextPrefab != null)
        {
            GameObject obj = Instantiate(floatingTextPrefab, worldPos, Quaternion.identity);
            FloatingTextItem item = obj.GetComponent<FloatingTextItem>();
            if (item != null)
            {
                item.Initialize(text, color);
            }
            return;
        }

        SpawnCanvasPopupText(worldPos, text, color);
    }

    /// <summary>
    /// 🟢 แสดง Feedback ได้รับทรัพยากร (+จำนวน ชื่อ) สีเขียวสว่าง สไตล์ Shop Popup
    /// </summary>
    public void ShowResourceGain(string resourceName, int amount, Vector3? worldPos = null)
    {
        Color gainColor = new Color(0.2f, 1f, 0.4f, 1f); // สีเขียวสว่าง
        string text = $"+{amount} {resourceName}";

        if (worldPos.HasValue)
        {
            ShowText(worldPos.Value, text, gainColor);
        }
        else
        {
            SpawnCanvasPopupTextAtScreenCenter(text, gainColor);
        }
    }

    /// <summary>
    /// 🟢 แสดง Feedback เสียทรัพยากร (-จำนวน ชื่อ) สีแดงส้ม สไตล์ Shop Popup
    /// </summary>
    public void ShowResourceSpend(string resourceName, int amount, Vector3? worldPos = null)
    {
        Color spendColor = new Color(1f, 0.35f, 0.35f, 1f); // สีแดงส้ม
        string text = $"-{amount} {resourceName}";

        if (worldPos.HasValue)
        {
            ShowText(worldPos.Value, text, spendColor);
        }
        else
        {
            SpawnCanvasPopupTextAtScreenCenter(text, spendColor);
        }
    }

    private void SpawnCanvasPopupText(Vector3 worldPos, string text, Color color)
    {
        EnsureCanvasSetup();

        Vector2 screenPos;
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            Vector3 viewportPos = mainCam.WorldToScreenPoint(worldPos);
            screenPos = new Vector2(viewportPos.x, viewportPos.y);
        }
        else
        {
            screenPos = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        }

        CreateUIPopupElement(screenPos, text, color);
    }

    private void SpawnCanvasPopupTextAtScreenCenter(string text, Color color)
    {
        EnsureCanvasSetup();
        Vector2 screenPos = new Vector2(Screen.width * 0.5f, Screen.height * 0.55f);
        CreateUIPopupElement(screenPos, text, color);
    }

    private void CreateUIPopupElement(Vector2 screenPoint, string text, Color color)
    {
        if (uiCanvas == null) return;

        GameObject textObj = new GameObject("ResourcePopupText");
        textObj.transform.SetParent(uiCanvas.transform, false);

        RectTransform rectTrans = textObj.AddComponent<RectTransform>();
        
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            uiCanvas.transform as RectTransform,
            screenPoint,
            uiCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : Camera.main,
            out Vector2 localPoint
        );

        // สุ่มเยื้องตำแหน่งเล็กน้อยไม่ให้ข้อความซ้อนทับกันกรณีเด้งติดๆ กัน
        localPoint += new Vector2(Random.Range(-25f, 25f), Random.Range(-10f, 10f));
        rectTrans.anchoredPosition = localPoint;
        rectTrans.sizeDelta = new Vector2(400f, 60f);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 32;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        StartCoroutine(AnimateUIPopupRoutine(textObj, rectTrans, tmp, color));
    }

    private IEnumerator AnimateUIPopupRoutine(GameObject textObj, RectTransform rectTrans, TextMeshProUGUI tmp, Color startColor)
    {
        Vector2 startPos = rectTrans.anchoredPosition;
        Vector2 targetPos = startPos + new Vector2(0f, 80f); // ลอยขึ้น 80 pixel บน UI Canvas

        float duration = 1.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (textObj == null) yield break;

            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Ease Out Quadrant (ลอยขึ้นอย่างสมูท สไตล์ Shop UI)
            rectTrans.anchoredPosition = Vector2.Lerp(startPos, targetPos, 1f - (1f - t) * (1f - t));

            // ค่อยๆ จางหายไปช่วงครึ่งหลัง (Fade Out)
            if (t > 0.5f)
            {
                float alpha = Mathf.Lerp(1f, 0f, (t - 0.5f) / 0.5f);
                tmp.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            }

            yield return null;
        }

        if (textObj != null)
        {
            Destroy(textObj);
        }
    }
}