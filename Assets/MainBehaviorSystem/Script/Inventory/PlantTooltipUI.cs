using UnityEngine;
using TMPro;

public class PlantTooltipUI : MonoBehaviour
{
    public static PlantTooltipUI Instance { get; private set; }

    [Header("Tooltip Panel & Texts")]
    public GameObject tooltipPanel;       // หน้าต่าง Tooltip ของเมล็ดพืช
    public TMP_Text titleText;            // ชื่อเมล็ด
    public TMP_Text detailText;           // รายละเอียด
    public TMP_Text attractedEnemiesText; // ⚠️ ข้อความแสดงรายชื่อศัตรูที่จะมาบุก

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        HideTooltip(); // ซ่อนตอนเริ่มเกม
    }

    // ฟังก์ชันเปิดและแสดงข้อมูลเฉพาะของเมล็ดพืช
    public void ShowPlantTooltip(SO_PlantData plantData, Vector3 position)
    {
        if (tooltipPanel == null || plantData == null) return;

        tooltipPanel.SetActive(true);

        if (titleText != null) titleText.text = plantData.itemName;
        if (detailText != null) detailText.text = plantData.detail;

        // 🎯 ดึงรายชื่อศัตรูจาก List ที่เราทำไว้มาแสดงตรงนี้โดยเฉพาะ!
        if (attractedEnemiesText != null)
        {
            if (plantData.specificAttractedEnemies != null && plantData.specificAttractedEnemies.Count > 0)
            {
                string enemyNames = "";
                foreach (var enemy in plantData.specificAttractedEnemies)
                {
                    if (enemy != null)
                    {
                        enemyNames += $"- {enemy.enemyName}\n";
                    }
                }
                attractedEnemiesText.text = $"⚠️ Attracted Enemies:\n{enemyNames}";
                attractedEnemiesText.gameObject.SetActive(true);
            }
            else
            {
                attractedEnemiesText.text = "⚠️ Attracted Enemies:\n- None (Safe)";
                attractedEnemiesText.gameObject.SetActive(true);
            }
        }

        // ปรับตำแหน่ง Tooltip ตามตำแหน่งเมาส์หรือปุ่ม
        tooltipPanel.transform.position = position + new Vector3(0, -500f, 0);
    }

    public void HideTooltip()
    {
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
    }
}