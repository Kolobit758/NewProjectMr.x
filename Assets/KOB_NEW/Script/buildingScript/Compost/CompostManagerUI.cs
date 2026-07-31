using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.EventSystems; // จำเป็นสำหรับการเช็คการชี้เมาส์ (Hover)

public class CompostManagerUI : MonoBehaviour
{
    public static CompostManagerUI Instance { get; private set; }

    [Header("References")]
    public GameObject mainPanel;               // หน้าต่าง Screen Space UI หลัก
    public Transform recipeButtonContainer;     // Content ของ ScrollView สำหรับวางปุ่มสูตร
    public GameObject recipeButtonPrefab;       // Prefab ปุ่มสูตรปุ๋ย

    [Header("Tooltip UI")]
    public GameObject tooltipPanel;            // Panel สำหรับแสดง Tooltip
    public TMP_Text tooltipText;               // ข้อความรายละเอียดวัตถุดิบใน Tooltip

    [Header("All Available Recipes")]
    public List<SO_CompostRecipe> allMasterRecipes = new List<SO_CompostRecipe>(); // รายการสูตรปุ๋ยทั้งหมดในเกม (ลากมาใส่ใน Inspector)

    private CompostBuilding selectedCompost;    // ตึกตัวที่กำลังถูกกดสั่งงานอยู่ปัจจุบัน

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (tooltipPanel != null) tooltipPanel.SetActive(false);

        // สร้างปุ่มสูตรปุ๋ยทั้งหมดตั้งแต่เริ่มเกมอัตโนมัติ
        GenerateMasterRecipeButtons();
    }

    // เปิดหน้าต่าง UI กลาง และจดจำตึกตัวที่ถูกคลิก
    public void OpenCompostPanel(CompostBuilding targetCompost)
    {
        selectedCompost = targetCompost;
        if (mainPanel != null) mainPanel.SetActive(true);
    }

    // ปิดหน้าต่าง UI กลาง
    public void CloseCompostPanel()
    {
        selectedCompost = null;
        if (mainPanel != null) mainPanel.SetActive(false);
        if (tooltipPanel != null) tooltipPanel.SetActive(false);
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

            // ตั้งชื่อปุ่มแสดงชื่อสูตร
            TMP_Text btnText = btnObj.GetComponentInChildren<TMP_Text>();
            if (btnText != null)
            {
                btnText.text = $"{recipe.recipeName}\nYield: {recipe.resultingFertilizer.itemName}";
            }

            // ผูก Event กดปุ่มสร้าง (เปิดให้กดซ้ำๆ เพื่อ Stack คิวได้โดยไม่ปิด UI)
            SO_CompostRecipe targetRecipe = recipe;
            btn.onClick.AddListener(() =>
            {
                OnSelectRecipe(targetRecipe);
            });

            // ผูก Event ชี้เมาส์เพื่อแสดง Tooltip
            AddTooltipTriggers(btnObj, recipe);
        }
    }

    // จัดการระบบ Tooltip เมื่อเอาเมาส์ชี้เข้า/ออก
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

        // ป้องกันกรณีที่รายชื่อวัตถุดิบเป็นค่าว่าง
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

                // เช็คความพร้อมของทรัพยากรในคลังอย่างปลอดภัย
                bool hasEnough = false;
                if (ResourceInventory.Instance != null)
                {
                    hasEnough = ResourceInventory.Instance.HasResource(itemName, requiredAmount);
                }

                string colorTag = hasEnough ? "green" : "red";

                // ใช้การต่อสตริงแบบปลอดภัย
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

    // เมื่อผู้เล่นกดปุ่มเลือกสร้างสูตร (สามารถกดรัวๆ เพื่อเพิ่ม Stack คิวได้)
    void OnSelectRecipe(SO_CompostRecipe recipe)
    {
        if (selectedCompost != null)
        {
            bool success = selectedCompost.StartProduction(recipe);
            if (success)
            {
                Debug.Log($"✅ Successfully stacked production order for {recipe.recipeName}!");
                // หมายเหตุ: เอา CloseCompostPanel() ออกแล้ว เพื่อให้ผู้เล่นกดคลิกซ้ำหลายๆ ครั้งเพื่อสะสมคิวได้สะดวก
            }
            else
            {
                Debug.Log("❌ Not enough ingredients to produce!");
            }
        }
    }
}