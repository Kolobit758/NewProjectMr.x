using System.Collections;
using UnityEngine; // เอาอันอื่นออกให้หมด เหลือแค่อันนี้พอ!

[RequireComponent(typeof(CharacterStats))]
[RequireComponent(typeof(PlayerActionMovement))]
public class PlayerManager : MonoBehaviour
{
    public CharacterStats Stats { get; private set; }
    public PlayerActionMovement Movement { get; private set; }
    public PlayerAttack Attack { get; private set; }
    [Header("Stamina Regeneration Settings")]
    [Tooltip("ความเร็วในการฟื้นฟู (สตามิน่าเด้งกี่หน่วยต่อ 1 วินาที)")]
    public float staminaRegenRate = 15f;

    [Tooltip("ดีเลย์หลังจากใช้สตามิน่าหมด/ลดลง ต้องรอเวลากี่วินาทีก่อนหลอดจะเริ่มเด้งใหม่")]
    public float regenDelay = 1.0f;

    private float regenCooldownTimer = 0f;
    private float staminaBuffer = 0f; // ตัวช่วยสะสมทศนิยมของ deltaTime
    private int lastCheckStamina;

    private void Awake()
    {
        Stats = GetComponent<CharacterStats>();
        Movement = GetComponent<PlayerActionMovement>();
        Attack = GetComponent<PlayerAttack>();
    }

    private void Start()
    {
        if (Stats != null)
        {
            lastCheckStamina = Stats.currentStamina;
        }
    }

    public void Update()
    {
        if (Stats == null) return;

        // 🟢 1. ระบบตรวจจับความเคลื่อนไหว: ถ้าสตามิน่าลดลงในเฟรมนี้ (เช่นจากการกดวิ่ง/โจมตี) ให้สั่งรีเซ็ตดีเลย์ห้ามเด้งพลังทันที
        if (Stats.currentStamina < lastCheckStamina)
        {
            regenCooldownTimer = regenDelay; // บังคับติดคูลดาวน์ห้ามฟื้นฟู
        }
        lastCheckStamina = Stats.currentStamina; // อัปเดตค่าไว้เช็คเฟรมถัดไป

        // 🟢 2. ทำการนับเวลาถอยหลังดีเลย์ห้ามเด้ง
        if (regenCooldownTimer > 0f)
        {
            regenCooldownTimer -= Time.deltaTime;
            return; // ถ้ายังติดเวลารออยู่ ให้เด้งหลุดฟังก์ชันออกไปก่อน ยังไม่ต้องเติมพลัง
        }

        // ดึงค่าสตามิน่าสูงสุดจากอาเรย์มาใช้งาน (Index 2 คือ maxStamina)
        int maxStamina = Stats.GetPrivateData()[2];

        // 🟢 3. ลอจิกการฟื้นฟูค่าพลังเมื่อหลอดยังไม่เต็ม
        if (Stats.currentStamina < maxStamina)
        {
            // สูตรคำนวณแบบละเอียดอิงเวลาจริง: บวกค่าทศนิยมสะสมตาม deltaTime
            staminaBuffer += staminaRegenRate * Time.deltaTime;

            // เมื่อสะสมค่าทศนิยมจนครบ 1 แต้มเต็ม ให้แปลงเป็นตัวเลขจำนวนเต็มส่งเข้าคลังแรมหลัก
            if (staminaBuffer >= 1f)
            {
                int regenAmount = Mathf.FloorToInt(staminaBuffer);
                staminaBuffer -= regenAmount; // หักเศษที่ใช้ไปเหลือทศนิยมไว้รอเฟรมหน้า

                Stats.RegenerateStamina(regenAmount); // ยิงคำสั่งไปสั่ง CharacterStats ให้เติมแต้มพร้อม Invoke UI
            }
        }
        else
        {
            staminaBuffer = 0f; // เต็มแล้วล้างขยะบัฟเฟอร์ทิ้ง
        }
    }
}