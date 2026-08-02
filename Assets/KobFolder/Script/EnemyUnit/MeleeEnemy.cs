using UnityEngine;

public class MeleeEnemy : EnemyBase
{
    [Header("Melee Settings")]
    public float attackRange = 2f;
    public float dashSpeed = 12f;
    public float attackCooldown = 3f;
    public int attackDamage = 10;

    [Header("Stamina Cost")]
    public int dashStaminaCost = 30;
    public int staminaRegenRate = 15;

    private int attackCount = 0;
    private bool isDashing = false;
    private Vector3 dashTargetPos;

    protected override void UpdateStateMachine()
    {
        int maxHp = myStats.GetPrivateData()[0];
        // ใน MeleeEnemy.cs
        if (myStats.currentHP <  maxHp * 0.3f) // ถ้าเลือดเหลือน้อยกว่า 30%
        {
            // เปลี่ยนพฤติกรรม: เลิกบุก แล้ววิ่งหนีออกจากระยะโจมตี
            currentState = EnemyState.Cooldown;
            MoveTowards(transform.position - (target.position - transform.position).normalized * 5f);
            return;
        }
        if (currentState != EnemyState.Attacking && myStats != null)
        {
            myStats.RegenerateStamina((int)(staminaRegenRate * Time.deltaTime));
        }

        if (target == null)
        {
            currentState = EnemyState.Idle;
            return;
        }

        switch (currentState)
        {
            case EnemyState.Idle:
                currentState = EnemyState.Chasing;
                break;

            case EnemyState.Chasing:
                // 🟢 ใช้ tacticalOffset ที่ Manager เป็นคนสั่ง!
                Vector3 targetFlankPos = target.position + tacticalOffset;
                float distanceToTarget = Vector3.Distance(transform.position, target.position);

                if (distanceToTarget <= attackRange * 2.5f && myStats != null && myStats.currentStamina >= dashStaminaCost)
                {
                    myStats.UseStamina(dashStaminaCost);
                    currentState = EnemyState.Attacking;
                    isDashing = true;
                    dashTargetPos = target.position;
                    attackCount = 0;
                }
                else
                {
                    MoveTowards(targetFlankPos);
                }
                break;

            case EnemyState.Attacking:
                if (isDashing)
                {
                    transform.position = Vector3.MoveTowards(transform.position, dashTargetPos, dashSpeed * Time.deltaTime);
                    if (Vector3.Distance(transform.position, dashTargetPos) < 0.2f) isDashing = false;
                }
                else
                {
                    stateTimer -= Time.deltaTime;
                    if (stateTimer <= 0)
                    {
                        ExecuteOverlapAttack();
                        attackCount++;
                        stateTimer = 0.2f;
                        if (attackCount >= 5)
                        {
                            stateTimer = attackCooldown;
                            currentState = EnemyState.Cooldown;
                        }
                    }
                }
                break;

            // ใน MeleeEnemy.cs ตรงส่วน Cooldown
            case EnemyState.Cooldown:
                stateTimer -= Time.deltaTime;

                // 🟢 เปลี่ยนจาก flockOffset เป็น tacticalOffset ครับ!
                MoveTowards(target.position + (tacticalOffset * 1.5f));

                if (stateTimer <= 0) currentState = EnemyState.Chasing;
                break;
        }
    }

    void ExecuteOverlapAttack()
    {
        Vector3 attackPoint = transform.position + transform.forward * 1f;
        Collider[] hitTargets = Physics.OverlapSphere(attackPoint, attackRange, targetLayers);
        foreach (var hit in hitTargets)
        {
            if (hit.CompareTag("Player") || hit.CompareTag("Unit"))
            {
                if (hit.TryGetComponent<CharacterStats>(out var targetStats))
                    targetStats.TakeDamage(attackDamage, transform.position);
            }
        }
    }
}