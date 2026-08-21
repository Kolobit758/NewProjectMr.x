using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Game/Enemy Data")]
public class SO_EnemyData : ScriptableObject
{
    public string enemyName = "ศัตรูทั่วไป";
    public GameObject enemyPrefab;
    public FactionType factionType;

    [Header("Base Stats")]
    public float maxHealth = 100f;
    public float attackDamage = 15f;
    public float attackRange = 2f;
    public float moveSpeed = 3.5f;

    [Header("Loot Drop Settings (ของที่ดรอปเมื่อตาย)")]
    public SO_ItemData dropItem;       // ไอเทมที่จะได้เข้ากระเป๋าตรงๆ
    public int minDropAmount = 1;      // จำนวนต่ำสุด
    public int maxDropAmount = 3;      // จำนวนสูงสุด

    [Header("Attack")]
    public float attackCooldown = 0.5f;   // ยิ่งน้อยยิ่งตีถี่ — ตัวเล็กตั้ง 0.3-0.5, ตัวใหญ่ตั้ง 1.0-2.0
    public float attackYTolerance = 3f;   // ตัวใหญ่ตั้งสูงกว่านี้ เช่น 5-8 ถ้ายังตีไม่เข้า
}