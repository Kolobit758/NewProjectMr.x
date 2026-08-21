using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossHealthBarUI : MonoBehaviour
{
    [Header("UI Components")]
    public Image healthFillImage;       // ลากวัตถุ HealthFill (สีแดง) มาใส่ช่องนี้
    public TextMeshProUGUI bossNameText; // ลาก BossNameText มาใส่ช่องนี้

    [Header("Target Boss")]
    public CharacterStats targetBossStats; // ลากตัวบอสแม่ Kraken จากในฉากมาใส่ช่องนี้เลยเพื่อน!

    void Start()
    {
        // ซ่อน UI ไว้ก่อนจนกว่าบอสจะตื่น (หรือจะเปิดโชว์ตลอดเวลาก็ได้ครับ)
        // gameObject.SetActive(false);
    }

    // ฟังก์ชันเปิดจอ UI ตอนบอสเริ่มคำรามสู้
    public void ShowBossUI(string bossName)
    {
        gameObject.SetActive(true);
        if (bossNameText != null) bossNameText.text = bossName;
        UpdateHealthBar();
    }

    void Update()
    {
        // อัปเดตหลอดเลือดให้เป๊ะตามเวลาจริง (Real-time)
        if (targetBossStats != null)
        {
            UpdateHealthBar();
        }
    }

    private void UpdateHealthBar()
    {
        if (targetBossStats == null || healthFillImage == null) return;

        // 🟢 ดึงข้อมูลเลือดหลังบ้านจากสคริปต์ CharacterStats ของนาย
        int[] statsData = targetBossStats.GetPrivateData();
        float currentHP = targetBossStats.currentHP; // เลือดปัจจุบัน
        float maxHP = statsData[0];     // เลือดสูงสุด

        // คำนวณอัตราส่วน 0.0 ถึง 1.0 ยิงเข้าหลอด Filled UI
        healthFillImage.fillAmount = Mathf.Clamp01(currentHP / maxHP);
    }

    public void HideBossUI()
    {
        gameObject.SetActive(false);
    }
}