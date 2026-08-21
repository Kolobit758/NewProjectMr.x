using UnityEngine;
using UnityEngine.AI;

// กำหนดกลุ่มของประเภทอาวุธ
public enum WeaponType
{
    Melee,      // โจมตีใกล้ (ดาบ, ขวาน)
    Ranged,     // โจมตีไกล (ธนู, ปืน)
    Magic       // เวทมนตร์
}

// สร้างคลาสย่อยเพื่อเก็บรายละเอียดของคอมโบแต่ละฮิต
[System.Serializable]
public class ComboStep
{
    public string comboName;          // ตั้งชื่อจำง่ายๆ เช่น "Light Attack 1"
    public string animationTrigger;    // ชื่อ Trigger ใน Animator เช่น "Attack1", "Attack2"
    public float damageMultiplier = 1f; // ตัวคูณดาเมจ (ท่าท้ายๆ อาจจะแรงขึ้น)
    public float dashForce = 8f;       // แรงพุ่งในฮิตนี้ (แต่ละท่าพุ่งแรงไม่เท่ากันได้)
    public float attackRange = 2f;     // ระยะฟันของฮิตนี้
}
[CreateAssetMenu(fileName = "New weapon", menuName = "ACTION/Weapon_Data")]
public class WeaponData : SO_ItemData
{
    [Header("Name and Apearence")]
    public string weaponName;
    public GameObject weaponPrefab;
    public string historyDetail;
    [Header("Animation Setup")]
    public AnimatorOverrideController weaponAnimatorOverride;

    [Header("Combo System")]
    // เปลี่ยนจากท่าเดี่ยว เป็นอาเรย์เก็บคอมโบต่อเนื่อง!
    public ComboStep[] comboSteps;
    public float comboResetTime = 1.2f; // ระยะเวลาสูงสุดที่ถ้าไม่กดตีต่อ คอมโบจะรีเซ็ตกลับไปท่าแรก
    [Header("Weapon Stats")]
    public WeaponType weaponType;
    public int quality; // ของดีรึป่าว
    public int baseDamage;
    public int attackRange;
    public float attackDash;
    public float attackCooldown;

    [Header("Range weapon Setting")]
    public GameObject projectilePrefab;
    public int projectileSpeed;
    [Header("Magic weapon Setting")]
    public int manaCost;

    public override bool UseItem(GameObject user)
    {
        if (itemType == ItemType.Weapon || itemType == ItemType.Armor)
        {
            Debug.Log($"[Item] สวมใส่ {itemName} แล้ว! (เพิ่มสเตตัสให้ตัวละคร)");
            PlayerAttack playerAttack = FindAnyObjectByType<PlayerAttack>();
            playerAttack.EquipWeapon(this);
            // ลอจิกสวมใส่เกราะ/อาวุธของคุณ...
            return true;
        }

        Debug.LogWarning($"[Item] {itemName} เป็นวัตถุดิบ ไม่สามารถกดใช้ตรงๆ ได้");
        return false;
    }
}