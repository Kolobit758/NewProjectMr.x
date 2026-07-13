using UnityEngine;
using UnityEngine.UI;

public class PlayerStatusUI : MonoBehaviour
{
    [Header("Target Player Reference")]
    [Tooltip("ลาก Object ตัวละครผู้เล่นที่มีคอมโพเนนต์ PlayerManager มาใส่ช่องนี้")]
    public PlayerManager playerManager;

    [Header("Radial UI Images")]
    [Tooltip("Image หลอดเลือด (ต้องปรับ Image Type เป็น Filled และ Fill Method เป็น Radial 360)")]
    public Image hpRadialFill;

    [Tooltip("Image หลอดความอึด (ต้องปรับ Image Type เป็น Filled และ Fill Method เป็น Radial 360)")]
    public Image staminaRadialFill;

    private CharacterStats targetStats;

    private void Start()
    {
        // 🟢 1. ตรวจสอบและดึงข้อมูลลิงก์กับ CharacterStats ของผู้เล่น
        if (playerManager != null && playerManager.Stats != null)
        {
            targetStats = playerManager.Stats;

            // 🟢 2. ทำการ Assign/Subscribe ฟังก์ชันไปดักฟัง Event ของแรมหลัก
            targetStats.OnHPChanged += UpdateHPDisplay;
            targetStats.OnStaminaChanged += UpdateStaminaDisplay;

            // สั่งอัปเดตวาดภาพเฟรมแรกตอนเปิดเกมทันที
            UpdateHPDisplay();
            UpdateStaminaDisplay();
        }
        else
        {
            Debug.LogError("[PlayerStatusUI] ❌ กรุณาลากวัตถุที่มี PlayerManager มาหยอดใส่ช่องใน Inspector ด้วยครับ!");
        }
    }

    private void OnDestroy()
    {
        // 🔒 คืนสิทธิ์ถอดถอนชื่อออกจากท่อสัญญานป้องกันบั๊กหน่วยความจำค้างข้ามซีน
        if (targetStats != null)
        {
            targetStats.OnHPChanged -= UpdateHPDisplay;
            targetStats.OnStaminaChanged -= UpdateStaminaDisplay;
        }
    }

    /// <summary>
    /// ฟังก์ชันคำนวณและปรับลด-เพิ่มระดับวงกลมของหลอดเลือด (Radial HP)
    /// </summary>
    public void UpdateHPDisplay()
    {
        if (targetStats == null || hpRadialFill == null) return;

        // ดึงข้อมูลอาเรย์ค่าสูงสุดของตัวละครผ่านมาเฟังก์ชัน GetPrivateData [0] = maxHP
        int[] privateData = targetStats.GetPrivateData();
        float maxHP = privateData[0];

        if (maxHP > 0)
        {
            // สูตรคำนวณสัดส่วนวงกลม: เลือดปัจจุบัน หารด้วย เลือดสูงสุด (ได้ค่าระหว่าง 0.0 ถึง 1.0)
            float fillRatio = (float)targetStats.currentHP / maxHP;
            
            // บังคับให้รูปภาพตัดการแสดงผลหมุนตามตัวเลข Ratio ทันที
            hpRadialFill.fillAmount = Mathf.Clamp01(fillRatio);
        }
    }

    /// <summary>
    /// ฟังก์ชันคำนวณและปรับลด-เพิ่มระดับวงกลมของหลอดสตามิน่า (Radial Stamina)
    /// </summary>
    public void UpdateStaminaDisplay()
    {
        if (targetStats == null || staminaRadialFill == null) return;

        int[] privateData = targetStats.GetPrivateData();
        float maxStamina = privateData[2]; // ดึงค่าช่องอาเรย์ที่ 2 = maxStamina

        if (maxStamina > 0)
        {
            // สูตรคำนวณสัดส่วนวงกลมสตามิน่า
            float fillRatio = (float)targetStats.currentStamina / maxStamina;
            
            staminaRadialFill.fillAmount = Mathf.Clamp01(fillRatio);
        }
    }
}