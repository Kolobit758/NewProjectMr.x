using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))] // บังคับให้ Object นี้ต้องมี Component Button
public class AnimalButtonUI : MonoBehaviour
{
    public UnitInstance unitInstance;
    
    [Header("UI Component References")]
    public Image animalIconImage;       // ลากช่อง Image บน Prefab มาใส่ตรงนี้
    public TextMeshProUGUI nameText;    // ลากช่อง Text แสดงชื่อสัตว์มาใส่ (ถ้ามี)
    public TextMeshProUGUI hpText;      // ลากช่อง Text แสดงเลือดมาใส่ (ถ้ามี)

    private UITeamManager managerRef;
    private Button myButton;

    private void Awake()
    {
        myButton = GetComponent<Button>();
        // ผูกฟังก์ชันคลิกปุ่มเข้ากับระบบดักฟัง
        myButton.onClick.AddListener(HandleClick);
    }

    /// <summary>
    /// ฟังก์ชันสำหรับให้ UITeamManager เรียกใช้เพื่อยัดข้อมูลและเปลี่ยนหน้าตาปุ่ม
    /// </summary>
    public void SetupButton(UnitInstance instance, UITeamManager manager)
    {
        unitInstance = instance;
        managerRef = manager;

        // 🖼️ เปลี่ยนไอคอนตามข้อมูลสัตว์ที่สุ่มมา
        if (animalIconImage != null && unitInstance.template != null)
        {
            animalIconImage.sprite = unitInstance.template.unitIcon;
        }

        // 📝 อัปเดตข้อความบนการ์ด UI (ถ้าเพื่อนตั้งช่องไว้)
        if (nameText != null) nameText.text = unitInstance.customName;
        if (hpText != null) hpText.text = $"HP: {unitInstance.maxHP}";
    }

    private void HandleClick()
    {
        // เมื่อปุ่มนี้โดนคลิก ให้ส่งข้อมูลสัตว์ตัวนี้กลับไปบอก UITeamManager ทันที
        if (managerRef != null && unitInstance != null)
        {
            managerRef.OnAnimalCardClicked(unitInstance);
        }
    }
}