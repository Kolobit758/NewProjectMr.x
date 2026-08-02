using UnityEngine;
using System;
using System.Collections.Generic;

public class CharacterStats : MonoBehaviour
{
    [Header("Base Max Stats")]
    [SerializeField] private int maxHP = 100;
    [SerializeField] private int maxMana = 50;
    [SerializeField] private int maxStamina = 100;

    [Header("Current Stats")]
    public int currentHP;
    public int currentMana;
    public int currentStamina;

    // Event สำหรับให้ UI มาเกาะดักฟัง
    public event Action OnHPChanged;
    public event Action OnStaminaChanged;

    private void Awake()
    {
        // ค่าเริ่มต้นเผื่อกรณีฉุกเฉิน
        currentHP = maxHP;
        currentMana = maxMana;
        currentStamina = maxStamina;
    }

    // ฟังก์ชันสำคัญ: ใช้รับค่าสเตตัสที่สุ่มมาจากด้านนอก (ใช้ทั้งตอนสัตว์ป่าเกิด และตอนทหารเกิด)
    public void InitializeStats(int newMaxHP, int newCurrentHP)
    {
        maxHP = newMaxHP;
        currentHP = newCurrentHP;

        // แจ้งเตือน UI ให้รีเฟรชค่าพลังทันที
        OnHPChanged?.Invoke();
    }

    // เพิ่มพารามิเตอร์ Vector3 attackerPosition เข้าไปใน TakeDamage
    public void TakeDamage(int amount, Vector3 attackerPosition)
    {
        currentHP -= amount;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);
        OnHPChanged?.Invoke();

        // เรียกใช้ระบบสั่นสะดุ้งถ้าวัตถุนั้นมีคอมโพเนนต์นี้ติดอยู่
        // ใน CharacterStats.cs ตรงฟังก์ชัน TakeDamage ให้แก้บรรทัดนี้:
        if (TryGetComponent<HitFeedback>(out HitFeedback feedback))
        {
            // เปลี่ยนจาก feedback.PlayHitFeedback(attackerPosition); เป็นเวอร์ชันนี้
            feedback.PlayHitFeedback(attackerPosition, amount);
        }

        if (currentHP <= 0)
        {
            Debug.Log($"{gameObject.name} ตายแล้ว!");
        }
    }
    public void Die()
    {
        Debug.Log("is Died");
        Destroy(gameObject);
    }


    public void UseStamina(int amount)
    {
        currentStamina -= amount;
        currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
        OnStaminaChanged?.Invoke();
    }

    public void RegenerateStamina(int amount)
    {
        currentStamina += amount;
        currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
        OnStaminaChanged?.Invoke();
    }

    [ContextMenu("GetPrivateData")]
    public int[] GetPrivateData()
    {
        int[] privateData = { maxHP, maxMana, maxStamina};
        // Debug.Log("maxHP : " + maxHP + " maxMana :" + maxMana + " maxStamina : " + maxStamina);
        return privateData;
    }

    #region Test Button
    [ContextMenu("TumDamage")]
    public void TumDamage()
    {
        TakeDamage(100, gameObject.transform.position);
    }
    [ContextMenu("UseAllStamina")]
    public void StaminaUsed()
    {
        UseStamina(100);
    }
    [ContextMenu("FullFillStramina")]
    public void FullfillStamina()
    {
        RegenerateStamina(100);
    }
    #endregion
}