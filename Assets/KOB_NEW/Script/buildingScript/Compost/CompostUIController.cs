using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class CompostUIController : MonoBehaviour
{
    [Header("References")]
    public CompostBuilding compostBuilding; // ลากตึก Compost มาใส่
    public Transform recipeButtonContainer;   // Layout Group สำหรับวางปุ่มสูตร
    public GameObject recipeButtonPrefab;     // Prefab ปุ่ม (ต้องมี Button, Image, TextMeshPro)

    [Header("Production UI Status")]
    public GameObject productionPanel;       // Panel ที่แสดงตอนกำลังผลิต
    public TMP_Text statusText;              // ข้อความแสดงสถานะ / ชื่อสูตรที่กำลังทำ
    public Slider progressBar;               // หลอดโหลดเวลา

    private void Start()
    {
        if (compostBuilding == null) compostBuilding = GetComponentInParent<CompostBuilding>();
        
        // สร้างปุ่มสูตรปุ๋ยทั้งหมดที่มีในตึกอัตโนมัติ
        GenerateRecipeButtons();
    }

    private void Update()
    {
        UpdateProductionStatusUI();
    }

    // 🟢 1. ฟังก์ชันสร้างปุ่มเมนูสูตรปุ๋ยตาม List ที่ตั้งไว้
    void GenerateRecipeButtons()
    {
        // เคลียร์ปุ่มเก่าทิ้งก่อน (ถ้ามี)
        foreach (Transform child in recipeButtonContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (SO_CompostRecipe recipe in compostBuilding.availableRecipes)
        {
            GameObject btnObj = Instantiate(recipeButtonPrefab, recipeButtonContainer);
            
            // ดึงคอมโพเนนต์ภายในปุ่ม (ปรับชื่อตาม UI Prefab ของคุณได้เลยครับ)
            Button btn = btnObj.GetComponent<Button>();
            TMP_Text btnText = btnObj.GetComponentInChildren<TMP_Text>();
            
            // ตั้งชื่อปุ่มแสดงชื่อสูตรและผลลัพธ์
            if (btnText != null)
            {
                string info = $"{recipe.recipeName}\n(ใช้: ";
                foreach(var ing in recipe.requiredIngredients)
                {
                    info += $"{ing.item.itemName} x{ing.amount} ";
                }
                info += $")";
                btnText.text = info;
            }

            // ผูก Event กดปุ่มเพื่อสั่งสร้างปุ๋ย
            SO_CompostRecipe currentRecipeRef = recipe; // ป้องกันตัวแปรถูกเปลี่ยนค่าในลูป
            btn.onClick.AddListener(() => {
                OnRecipeButtonClicked(currentRecipeRef);
            });
        }
    }

    // 🟢 2. เมื่อผู้เล่นกดปุ่มเลือกสูตรปุ๋ย
    void OnRecipeButtonClicked(SO_CompostRecipe recipe)
    {
        if (compostBuilding.isProducing)
        {
            Debug.LogWarning("⚠️ กำลังผลิตปุ๋ยอยู่นะ รอให้เสร็จก่อน!");
            return;
        }

        // สั่งตึกเริ่มผลิต (ข้างในจะเช็คทรัพยากรและหักของให้เอง)
        bool success = compostBuilding.StartProduction(recipe);
        if (success)
        {
            Debug.Log($"✅ สั่งผลิต {recipe.recipeName} สำเร็จ!");
        }
        else
        {
            Debug.Log("❌ วัตถุดิบในคลังไม่พอ สร้างไม่ได้!");
        }
    }

    // 🟢 3. อัปเดต UI หลอดโหลดและสถานะการผลิตในหน้าจอ
    void UpdateProductionStatusUI()
    {
        if (compostBuilding.isProducing && compostBuilding.currentActiveRecipe != null)
        {
            if (productionPanel != null && !productionPanel.activeSelf) productionPanel.SetActive(true);

            SO_CompostRecipe activeRecipe = compostBuilding.currentActiveRecipe;
            
            if (statusText != null)
            {
                statusText.text = $"กำลังหมัก: {activeRecipe.recipeName}\nเหลือเวลา: {Mathf.Ceil(activeRecipe.productionTime - compostBuilding.currentProductionProgress)} วินาที";
            }

            if (progressBar != null)
            {
                progressBar.value = compostBuilding.currentProductionProgress / activeRecipe.productionTime;
            }
        }
        else
        {
            if (productionPanel != null && productionPanel.activeSelf) productionPanel.SetActive(false);
        }
    }
}