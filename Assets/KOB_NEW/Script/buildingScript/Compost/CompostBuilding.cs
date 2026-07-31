using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class CompostBuilding : MonoBehaviour, ITaskable
{
    [Header("Compost Recipes")]
    public List<SO_CompostRecipe> availableRecipes = new List<SO_CompostRecipe>();

    [Header("Production Queue State")]
    public SO_CompostRecipe currentActiveRecipe;
    public int queuedCount = 0; // จำนวนชิ้นที่รอผลิตสะสม (กดเบิ้ลได้)
    public bool isProducing = false;
    public float currentProductionProgress = 0f;

    [Header("World Space UI Elements")]
    public GameObject worldSpaceCanvas;      
    public GameObject openUIButtonObj;       
    public GameObject progressPanelObj;      
    public Slider worldProgressBar;          
    public TMP_Text popupResultText;         
    public TMP_Text queueCountText;          // แสดงตัวเลขคิวรอ เช่น "+2"

    void Awake()
    {
        // ค้นหา Canvas และองค์ประกอบ UI ใน World Space อัตโนมัติ (รองรับตอน Instantiate จาก Prefab)
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

        if (popupResultText == null && worldSpaceCanvas != null)
        {
            popupResultText = worldSpaceCanvas.GetComponentInChildren<TMP_Text>(true);
        }
    }

    void Start()
    {
        if (progressPanelObj != null) progressPanelObj.SetActive(false);
        if (popupResultText != null) popupResultText.gameObject.SetActive(false);

        // เซ็ต World Camera ให้ Canvas แบบ World Space แสดงผลถูกต้อง
        if (worldSpaceCanvas != null)
        {
            Canvas canvas = worldSpaceCanvas.GetComponent<Canvas>();
            if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
            {
                canvas.worldCamera = FindAnyObjectByType<Camera>();
            }
        }
    }

    // เมื่อผู้เล่นกดปุ่มบนหัวตึก เพื่อเปิดหน้าต่าง UI กลาง
    public void OnClickOpenUIButton()
    {
        if (CompostManagerUI.Instance != null)
        {
            CompostManagerUI.Instance.OpenCompostPanel(this);
        }
    }

    // 🟢 สั่งเพิ่มจำนวนคิวการผลิต (กดรัวๆ เพื่อ Stack เพิ่มได้ ถ้าวัตถุดิบพอ)
    public bool StartProduction(SO_CompostRecipe targetRecipe)
    {
        if (targetRecipe == null) return false;

        // 1. เช็คว่าวัตถุดิบในคลังพอสำหรับทำเพิ่มอีก 1 ชิ้นไหม
        foreach (var ingredient in targetRecipe.requiredIngredients)
        {
            if (!ResourceInventory.Instance.HasResource(ingredient.item.itemName, ingredient.amount))
            {
                Debug.Log($"❌ [Compost]: Not enough ingredients for {targetRecipe.recipeName}!");
                return false;
            }
        }

        // 2. หักวัตถุดิบออกจากคลังทันทีที่กดสั่ง
        foreach (var ingredient in targetRecipe.requiredIngredients)
        {
            ResourceInventory.Instance.ConsumeResource(ingredient.item, ingredient.amount);
        }

        // 3. จัดการคิวและสถานะการผลิต
        if (!isProducing)
        {
            currentActiveRecipe = targetRecipe;
            isProducing = true;
            currentProductionProgress = 0f;
            if (progressPanelObj != null) progressPanelObj.SetActive(true);
            if (openUIButtonObj != null) openUIButtonObj.SetActive(false);
        }
        else if (currentActiveRecipe == targetRecipe)
        {
            // ถ้ากำลังทำสูตรนี้อยู่แล้ว ให้เพิ่มคิวรอสะสม
            queuedCount++;
        }
        else
        {
            Debug.LogWarning("⚠️ Already producing a different recipe! Please wait.");
            // คืนวัตถุดิบกลับเพราะสั่งคนละสูตรไม่ได้
            foreach (var ingredient in targetRecipe.requiredIngredients)
            {
                ResourceInventory.Instance.AddResource(ingredient.item, ingredient.amount);
            }
            return false;
        }

        UpdateQueueUI();
        Debug.Log($"🔥 [Compost]: Queued production for {targetRecipe.recipeName} (Waiting queue: {queuedCount})");
        return true;
    }

    void Update()
    {
        if (isProducing && currentActiveRecipe != null)
        {
            currentProductionProgress += Time.deltaTime;

            if (worldProgressBar != null && currentActiveRecipe.productionTime > 0)
            {
                worldProgressBar.value = currentProductionProgress / currentActiveRecipe.productionTime;
            }

            // ถ้าครบเวลาของชิ้นปัจจุบัน
            if (currentProductionProgress >= currentActiveRecipe.productionTime)
            {
                CompleteCurrentProduction();
            }
        }
    }

    private void CompleteCurrentProduction()
    {
        // 1. เพิ่มปุ๋ยสำเร็จรูปเข้าคลัง 1 ชิ้น
        ResourceInventory.Instance.AddResource(currentActiveRecipe.resultingFertilizer, 1);
        string craftedName = currentActiveRecipe.resultingFertilizer.itemName;

        // 2. แสดง Popup เด้งลอยขึ้นด้านบน (x1 [Item Name])
        StartCoroutine(ShowPopupResultRoutine(craftedName, 1));

        // 3. เช็คว่ามีคิวสะสม (queuedCount) เหลืออีกไหม
        if (queuedCount > 0)
        {
            queuedCount--; // ลดคิวลง
            currentProductionProgress = 0f; // รีเซ็ตเวลาทำชิ้นถัดไปทันที
            UpdateQueueUI();
            Debug.Log($"🔄 [Compost]: Starting next queue automatically (Remaining queue: {queuedCount})");
        }
        else
        {
            // หมดคิวแล้ว: ปิดหลอดโหลด กลับสู่สถานะว่าง
            isProducing = false;
            currentProductionProgress = 0f;
            currentActiveRecipe = null;

            if (progressPanelObj != null) progressPanelObj.SetActive(false);
            if (openUIButtonObj != null) openUIButtonObj.SetActive(true);
        }
    }

    private void UpdateQueueUI()
    {
        if (queueCountText != null)
        {
            queueCountText.text = queuedCount > 0 ? $"+{queuedCount}" : "";
        }
    }

    // อニメชัน Popup เด้งลอยขึ้นด้านบนและจางหายไปแบบ Ease-In
    private IEnumerator ShowPopupResultRoutine(string itemName, int amount)
    {
        if (popupResultText != null)
        {
            popupResultText.text = $"x{amount} {itemName}";
            popupResultText.gameObject.SetActive(true);

            RectTransform rectTrans = popupResultText.GetComponent<RectTransform>();
            Vector3 startPos = rectTrans.localPosition;
            Vector3 targetPos = startPos + new Vector3(0f, 50f, 0f);

            Color startColor = popupResultText.color;
            float duration = 1.5f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                rectTrans.localPosition = Vector3.Lerp(startPos, targetPos, t * t);

                if (t > 0.7f)
                {
                    float alpha = Mathf.Lerp(1f, 0f, (t - 0.7f) / 0.3f);
                    popupResultText.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                }

                yield return null;
            }

            popupResultText.gameObject.SetActive(false);
            rectTrans.localPosition = startPos;
            popupResultText.color = startColor;
        }
    }

    public void OnUnitInteract(UnitBase unit) { }
    public void OnUnitExit(UnitBase unit) { }
    public Vector3 GetInteractionPoint() => transform.position + transform.forward * 2f;
}