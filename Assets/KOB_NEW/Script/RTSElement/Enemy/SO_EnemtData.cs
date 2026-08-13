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
}