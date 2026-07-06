using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Weapon Data")]
    [SerializeField] private WeaponData currentWeapon;
    [SerializeField] private Transform weaponHandle;
    [SerializeField] private Rigidbody rb;

    [Header("References")]
    public GameObject playerModel;

    [Header("Collision Settings")]
    // แนะนำให้ตั้ง Layer แยกสำหรับ สิ่งกีดขวาง/กล่อง และ ศัตรู เพื่อความแม่นยำและประหยัดประสิทธิภาพ
    [SerializeField] private LayerMask attackableLayers;

    private GameObject spawnedWeaponModel;
    private Animator anim;

    private int currentComboIndex = 0;
    private float lastAttackTime = 0f;
    private bool canAttack = true;

    void Start()
    {
        anim = GetComponent<Animator>();
        if (currentWeapon != null) EquipWeapon(currentWeapon);
    }

    void Update()
    {
        if (currentWeapon != null && Time.time - lastAttackTime > currentWeapon.comboResetTime)
        {
            currentComboIndex = 0;
        }

        if (Input.GetMouseButtonDown(0) && canAttack && currentWeapon != null)
        {
            Attack();
        }
    }

    public void EquipWeapon(WeaponData newWeapon)
    {
        currentWeapon = newWeapon;
        currentComboIndex = 0;

        if (spawnedWeaponModel != null) Destroy(spawnedWeaponModel);
        if (currentWeapon.weaponPrefab != null && weaponHandle != null)
        {
            spawnedWeaponModel = Instantiate(currentWeapon.weaponPrefab, weaponHandle.position, weaponHandle.rotation, weaponHandle);
        }

        if (anim != null && currentWeapon.weaponAnimatorOverride != null)
        {
            anim.runtimeAnimatorController = currentWeapon.weaponAnimatorOverride;
        }
    }

    void Attack()
    {
        lastAttackTime = Time.time;
        ComboStep currentStep = currentWeapon.comboSteps[currentComboIndex];

        if (anim != null && !string.IsNullOrEmpty(currentStep.animationTrigger))
        {
            anim.SetTrigger(currentStep.animationTrigger);
        }

        switch (currentWeapon.weaponType)
        {
            case WeaponType.Melee:
                StartCoroutine(AttackRoutine(() => MeleeAttack(currentStep)));
                break;
            case WeaponType.Ranged:
                StartCoroutine(AttackRoutine(RangedAttack));
                break;
            case WeaponType.Magic:
                StartCoroutine(AttackRoutine(MagicAttack));
                break;
        }

        currentComboIndex++;
        if (currentComboIndex >= currentWeapon.comboSteps.Length)
        {
            currentComboIndex = 0;
        }
    }

    IEnumerator AttackRoutine(Action attackAction)
    {
        canAttack = false;
        attackAction?.Invoke();

        yield return new WaitForSeconds(currentWeapon.attackCooldown);
        canAttack = true;
    }

    #region Specific Attacks

    void MeleeAttack(ComboStep step)
    {
        if (rb != null && playerModel != null)
        {
            Vector3 dashDir = playerModel.transform.forward;
            dashDir.y = 0f;
            dashDir.Normalize();

            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            rb.AddForce(dashDir * step.dashForce, ForceMode.Impulse);
        }

        // ลดระยะเยื้องลงมาหน่อย (เช่น * 0.5f) เพื่อไม่ให้วงกลม Overlap ลอยหลุดไปข้างหน้าตัวผู้เล่นมากเกินไป
        Vector3 attackCenter = transform.position + (playerModel != null ? playerModel.transform.forward : transform.forward) * 0.5f;
        Collider[] hitColliders = Physics.OverlapSphere(attackCenter, step.attackRange, attackableLayers);

        // 🛠️ ล็อกเป้าหมาย: ในการโจมตีเฟรมนี้ วัตถุชิ้นไหนโดนไปแล้วห้ามโดนซ้ำ
        List<GameObject> hitObjectsInThisFrame = new List<GameObject>();

        foreach (Collider hit in hitColliders)
        {
            // หาวัตถุหลักขั้นสูงสุด เพื่อป้องกันกรณีตัวโมเดลมี Collider ย่อย ๆ ซ้อนกันอยู่ด้านใน
            GameObject rootTarget = hit.transform.root.gameObject;
            if (hitObjectsInThisFrame.Contains(rootTarget)) continue;

            if (hit.CompareTag("Enemy"))
            {
                Debug.Log($"ตีโดนศัตรู: {hit.name} ทำดาเมจ: {currentWeapon.baseDamage * step.damageMultiplier}");

                CharacterStats enemy = hit.GetComponent<CharacterStats>();
                if (enemy != null) enemy.TakeDamage((int)(currentWeapon.baseDamage * step.damageMultiplier),transform.position);

                hitObjectsInThisFrame.Add(rootTarget);
            }
            else if (hit.CompareTag("Box"))
            {
                Debug.Log($"ตีโดนกล่อง: {hit.name}!");

                ItemDropBox box = hit.GetComponent<ItemDropBox>();
                if (box != null)
                {
                    // 🛠️ ส่งค่าตำแหน่งของผู้เล่นไปด้วย เพื่อให้กล่องสั่นสะดุ้งและเด้งหนีทิศทางฟันได้สมจริง
                    box.Broken(transform.position);
                }

                hitObjectsInThisFrame.Add(rootTarget);
            }
        }
    }

    void RangedAttack()
    {
        Debug.Log($"ใช้ {currentWeapon.weaponName} ยิงธนู/ปืน!");
        SpawnProjectile();
    }

    void MagicAttack()
    {
        Debug.Log($"ร่ายเวทมนตร์ด้วย {currentWeapon.weaponName}! เสียมานา: {currentWeapon.manaCost}");
        SpawnProjectile();
    }

    void SpawnProjectile()
    {
        if (currentWeapon.projectilePrefab == null) return;

        GameObject projectile = Instantiate(currentWeapon.projectilePrefab, weaponHandle.position, transform.rotation);
        Rigidbody projRb = projectile.GetComponent<Rigidbody>();
        if (projRb != null)
        {
            projRb.linearVelocity = transform.forward * currentWeapon.projectileSpeed;
        }
    }

    #endregion

    private void OnDrawGizmosSelected()
    {
        if (currentWeapon != null && currentWeapon.weaponType == WeaponType.Melee && currentWeapon.comboSteps.Length > 0)
        {
            Gizmos.color = Color.red;
            float range = currentWeapon.comboSteps[currentComboIndex < currentWeapon.comboSteps.Length ? currentComboIndex : 0].attackRange;

            // ปรับ Gizmos ให้ตรงกับจุดโจมตีจริงในฟังก์ชัน MeleeAttack
            Vector3 attackCenter = transform.position + (playerModel != null ? playerModel.transform.forward : transform.forward) * 1.0f;
            Gizmos.DrawWireSphere(attackCenter, range);
        }
    }
}