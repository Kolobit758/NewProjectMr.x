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
        // ค่อยๆ ฟื้นฟู Stamina ของตัวเองเรื่อยๆ ตลอดเวลา (ยกเว้นตอนกำลังชาร์จ/ตี)
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
                Vector3 targetFlankPos = target.position + flockOffset;
                float distanceToTarget = Vector3.Distance(transform.position, target.position);

                // จะชาร์จได้ก็ต่อเมื่อเข้าระยะล้อม และมี Stamina เพียงพอเท่านั้น
                if (distanceToTarget <= attackRange * 2.5f && myStats != null && myStats.currentStamina >= dashStaminaCost)
                {
                    myStats.UseStamina(dashStaminaCost); // หักสเตมินาจริง!
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
                    if (Vector3.Distance(transform.position, dashTargetPos) < 0.2f)
                    {
                        isDashing = false; 
                    }
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

            case EnemyState.Cooldown:
                stateTimer -= Time.deltaTime;
                Vector3 retreatPos = target.position + (flockOffset * 1.5f);
                MoveTowards(retreatPos);

                if (stateTimer <= 0)
                {
                    currentState = EnemyState.Chasing;
                }
                break;

            case EnemyState.Dead:
                // นอนนิ่งๆ หรือทำลายตัวเองทิ้งตรงนี้ได้เลย
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
                // ส่งดาเมจเข้าระบบสเตตัสของเป้าหมายจริง พร้อมระบุตำแหน่งผู้โจมตี (ตัวเราเอง)
                if (hit.TryGetComponent<CharacterStats>(out var targetStats))
                {
                    targetStats.TakeDamage(attackDamage, transform.position);
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (currentState == EnemyState.Attacking) Gizmos.color = Color.red;
        else Gizmos.color = Color.yellow;

        Vector3 attackPoint = transform.position + transform.forward * 1f;
        Gizmos.DrawWireSphere(attackPoint, attackRange);
        Gizmos.DrawLine(transform.position, attackPoint);

        if (target != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, target.position + flockOffset);
        }
    }
}