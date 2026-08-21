using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
public class OutpostAnimalButtonUI : MonoBehaviour
{
    [Header("UI Components")]
    public TMP_Text unitNameText;        // ช่องใส่ชื่อสัตว์เลี้ยง (TextMeshPro)
    public TMP_Text unitHpAtkText;       // ช่องใส่ HP/ATK (TextMeshPro)
    public Image unitIconImage;         // ช่องใส่รูปสัตว์เลี้ยง

    private UnitInstance myUnit;
    private OutpostUIController uiControllerRef;
    private Button myButton;

    // 🟢 ฟังก์ชัน Setup ข้อมูลสดๆ จากกระเป๋ามาโชว์บนการ์ดค่ายย่อย
    public void SetupOutpostButton(UnitInstance unit, OutpostUIController controller)
    {
        myUnit = unit;
        uiControllerRef = controller;
        myButton = GetComponent<Button>();

        // 📝 หยอดตัวหนังสือและสเตตัส
        if (unitNameText != null) unitNameText.text = unit.customName;
        if (unitHpAtkText != null && unit.template != null)
        {
            unitHpAtkText.text = $"{unit.template.speciesName}\nHP: {unit.currentHP}/{unit.maxHP} | ATK: {unit.attackDamage}";
        }

        // 🖼 หหยอดรูปภาพ Icon สัตว์ Chibi
        if (unitIconImage != null && unit.template != null)
        {
            unitIconImage.sprite = unit.template.unitIcon;
        }

        // 🔗 ผูกสัญญานปุ่มคลิก: พอกดจิ้มการ์ดใบนี้ ให้ส่งข้อมูลไปเปิดกล่อง Confirm ที่ค่ายย่อย!
        myButton.onClick.RemoveAllListeners();
        myButton.onClick.AddListener(HandleCardClick);
    }

    private void HandleCardClick()
    {
        if (uiControllerRef != null && myUnit != null)
        {
            // ยิงข้อมูลสัตว์ตัวนี้ส่งไปให้หน้าต่างยืนยันค่ายย่อยประมวลผล!
            uiControllerRef.OnSelectUnitToDeploy(myUnit);
        }
    }
}