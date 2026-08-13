using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.EventSystems;

[RequireComponent(typeof(UIAnimationController))] // 🟢 บังคับให้ต้องมีสคริปต์อนิเมชันร่วมด้วย
public class CompostManagerUI : MonoBehaviour
{
    public static CompostManagerUI Instance { get; private set; }

    [Header("References")]
    public GameObject mainPanel;                  // หน้าต่าง Screen Space UI หลัก
    public Transform recipeButtonContainer;     // Content ของ ScrollView สำหรับวางปุ่มสูตร
    public GameObject recipeButtonPrefab;       // Prefab ปุ่มสูตรปุ๋ย

    [Header("Tooltip UI")]
    public GameObject tooltipPanel;             // Panel สำหรับแสดง Tooltip
    public TMP_Text tooltipText;                // ข้อความรายละเอียดวัตถุดิบใน Tooltip

    [Header("All Available Recipes")]
    public List<SO_CompostRecipe> allMasterRecipes = new List<SO_CompostRecipe>(); // รายการสูตรปุ๋ยทั้งหมด

    private CompostBuilding selectedCompost;    // ตึกตัวที่กำลังถูกกดสั่งงานอยู่ปัจจุบัน
    private UIAnimationController animController; // 🟢 ตัวควบคุมแอนิเมชันและฝุ่น

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        // ดึงคอมโพเนนต์แอนิเมชันจากหน้าต่าง Main Panel
        if (mainPanel != null)
        {
            animController = mainPanel.GetComponent<UIAnimationController>();
        }
    }

    void Start()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (tooltipPanel != null) tooltipPanel.SetActive(false);

        // สร้างปุ่มสูตรปุ๋ยทั้งหมดตั้งแต่เริ่มเกมอัตโนมัติ
        GenerateMasterRecipeButtons();
    }

    // 🟢 เปิดหน้าต่าง UI กลาง ผ่าน UIAnimationController (มีสไลด์ + ฝุ่น)
    public void OpenCompostPanel(CompostBuilding targetCompost)
    {
        selectedCompost = targetCompost;
        if (mainPanel != null)
        {
            if (animController != null)
            {
                animController.OpenUI(); // เปิดพร้อมสไลด์และฝุ่น
            }
            else
            {
                mainPanel.SetActive(true);
            }
        }
    }

    // 🔴 ปิดหน้าต่าง UI กลาง ผ่าน UIAnimationController (สไลด์ออก + ฝุ่น แล้วปิดตัวเอง)
    public void CloseCompostPanel()
    {
        selectedCompost = null;
        if (tooltipPanel != null) tooltipPanel.SetActive(false);

        if (mainPanel != null)
        {
            if (animController != null)
            {
                animController.CloseUI(); // ปิดพร้อมสไลด์เก็บและฝุ่น
            }
            else
            {
                mainPanel.SetActive(false);
            }
        }
    }

    // Auto-Generate ปุ่มสูตรปุ๋ยทั้งหมดจาก List
    void GenerateMasterRecipeButtons()
    {
        foreach (Transform child in recipeButtonContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (SO_CompostRecipe recipe in allMasterRecipes)
        {
            if (recipe == null) continue;

            GameObject btnObj = Instantiate(recipeButtonPrefab, recipeButtonContainer);
            Button btn = btnObj.GetComponent<Button>();

            TMP_Text btnText = btnObj.GetComponentInChildren<TMP_Text>();
            if (btnText != null)
            {
                btnText.text = $"{recipe.recipeName}\nYield: {recipe.resultingFertilizer.itemName}";
            }

            SO_CompostRecipe targetRecipe = recipe;
            btn.onClick.AddListener(() =>
            {
                OnSelectRecipe(targetRecipe);
            });

            AddTooltipTriggers(btnObj, recipe);
        }
    }

    void AddTooltipTriggers(GameObject buttonObj, SO_CompostRecipe recipe)
    {
        EventTrigger trigger = buttonObj.GetComponent<EventTrigger>();
        if (trigger == null) trigger = buttonObj.AddComponent<EventTrigger>();

        EventTrigger.Entry entryEnter = new EventTrigger.Entry();
        entryEnter.eventID = EventTriggerType.PointerEnter;
        entryEnter.callback.AddListener((eventData) => { ShowTooltip(recipe); });
        trigger.triggers.Add(entryEnter);

        EventTrigger.Entry entryExit = new EventTrigger.Entry();
        entryExit.eventID = EventTriggerType.PointerExit;
        entryExit.callback.AddListener((eventData) => { HideTooltip(); });
        trigger.triggers.Add(entryExit);
    }

    void ShowTooltip(SO_CompostRecipe recipe)
    {
        if (tooltipPanel == null || tooltipText == null || recipe == null) return;

        string recipeName = !string.IsNullOrEmpty(recipe.recipeName) ? recipe.recipeName : "Unknown Recipe";
        string yieldName = (recipe.resultingFertilizer != null) ? recipe.resultingFertilizer.itemName : "Unknown Item";

        string details = $"<b>{recipeName}</b>\nYield: {yieldName}\nTime: {recipe.productionTime}s\n\n<b>Required Ingredients:</b>\n";

        if (recipe.requiredIngredients != null)
        {
            foreach (var ing in recipe.requiredIngredients)
            {
                if (ing.item == null) continue;

                string itemName = ing.item.itemName;
                int requiredAmount = ing.amount;

                bool hasEnough = false;
                if (ResourceInventory.Instance != null)
                {
                    hasEnough = ResourceInventory.Instance.HasResource(itemName, requiredAmount);
                }

                string colorTag = hasEnough ? "green" : "red";
                details += "- " + itemName + ": <color=" + colorTag + ">" + requiredAmount + "</color>\n";
            }
        }

        tooltipText.text = details;
        tooltipPanel.SetActive(true);
    }

    void HideTooltip()
    {
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
    }

    void OnSelectRecipe(SO_CompostRecipe recipe)
    {
        if (selectedCompost != null)
        {
            bool success = selectedCompost.StartProduction(recipe);
            if (success)
            {
                Debug.Log($"✅ Successfully stacked production order for {recipe.recipeName}!");
            }
            else
            {
                Debug.Log("❌ Not enough ingredients to produce!");
            }
        }
    }
}