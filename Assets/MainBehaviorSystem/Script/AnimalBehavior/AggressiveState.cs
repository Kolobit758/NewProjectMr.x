using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

// ==========================================
// 🔴 1. พฤติกรรม: ดุร้าย (Aggressive) ไล่กัดคน
// ==========================================
[System.Serializable]
public class AggressiveState : AnimalBehaviorState
{
    [Header("Movement Settings")]
    public float attackSpeed = 4.5f;

    [Header("Combat Settings")]
    public float targetScanRadius = 15f;    // รัศมีในการกวาดหาตัวเป้าหมายที่จะวิ่งไปกัด
    public float attackRadius = 1.8f;       // ระยะโจมตี (ถ้าใกล้ขนาดนี้จะลงมือตี)
    public float attackCooldown = 2f;       // ความเร็วในการตี (กี่วินาทีตีที)
    public int damageAmount = 10;           // พลังโจมตีของสัตว์ตัวนี้
    
    [Tooltip("ใส่ Layer ของผู้เล่นและยูนิตทหาร (เช่น Player และ Unit) เพื่อลดภาระการสแกนฟิสิกส์")]
    public LayerMask attackableLayer = ~0;  // ค่าเริ่มต้นคือสแกนทุกเลเยอร์ ถ้าต้องการเจาะจงให้เลือกใน Inspector

    private float attackTimer = 0f;
    private Transform currentTarget = null; // เป้าหมายที่มันกำลังล็อกเป้าไล่กัดอยู่ปัจจุบัน

    public override void OnEnterState()
    {
        if (agent != null) agent.speed = attackSpeed;
        // 🟢 ตั้งค่าเริ่มต้นให้พร้อมโจมตีทันทีที่เจอหน้า (หรือจะตั้งเป็น 0 เพื่อให้รอคูลดาวน์ก่อนฮิตแรกก็ได้)
        attackTimer = attackCooldown; 
        currentTarget = null;
        Debug.Log($"{animalGo.name} : 💢 โหมดดุร้ายทำงาน! แยกเขี้ยวพร้อมขย้ำทุกคนที่ขวางหน้า!");
    }

    public override void UpdateState(float distanceToPlayer)
    {
        if (agent == null || animalGo == null) return;

        // 1. ค้นหาและล็อกเป้าหมายอัจฉริยะ (หาคน/ยูนิตที่ใกล้ที่สุด)
        FindClosestTarget();

        // ถ้าไม่มีเป้าหมายเหลือรอดอยู่แถวนี้เลย ให้หยุดเดิน
        if (currentTarget == null)
        {
            if (agent.hasPath) agent.ResetPath();
            return;
        }

        // 2. สั่ง NavMeshAgent วิ่งกวดตามเป้าหมายที่ล็อกไว้
        agent.SetDestination(currentTarget.position);

        // 3. 🟢 [FIXED TIMER] ลอจิกการนับเวลาถอยหลังแบบปลอดภัย ไม่หลุดลูปรัวเฟรมต่อเฟรม
        if (attackTimer < attackCooldown)
        {
            attackTimer += Time.deltaTime; // สะสมเวลาเพิ่มขึ้นเรื่อยๆ จนกว่าจะเท่ากับขีดคูลดาวน์
        }

        // ตรวจสอบระยะห่างจากตัวสัตว์ถึงเป้าหมายปัจจุบัน
        float distanceToTarget = Vector3.Distance(animalGo.transform.position, currentTarget.position);
        
        // เงื่อนไข: ระยะถึง + เวลาพร้อมคูลดาวน์ครบกำหนดจริงๆ เท่านั้นถึงจะลงมือทุบ
        if (distanceToTarget <= attackRadius && attackTimer >= attackCooldown)
        {
            ExecuteOverlapAttack();
            attackTimer = 0f; // 🔥 รีเซ็ตเวลากลับไปเริ่มนับ 0 ใหม่ตรงนี้อย่างมั่นคง
        }
    }

    /// <summary>
    /// สแกนหาเป้าหมายที่ใกล้ที่สุดรอบตัวสัตว์ ไม่ว่าจะเป็น Player หรือทหารยูนิต (Tag: Unit)
    /// </summary>
    private void FindClosestTarget()
    {
        // 🟢 อัปเดตปรับมาใช้ระบบ LayerMask ร่วมด้วยเพื่อความลื่นไหลของหน่วยความจำเครื่อง
        Collider[] hitColliders = Physics.OverlapSphere(animalGo.transform.position, targetScanRadius, attackableLayer);
        
        float closestDistance = Mathf.Infinity;
        Transform bestTarget = null;

        foreach (var col in hitColliders)
        {
            if (col.CompareTag("Player") || col.CompareTag("Unit"))
            {
                float dist = Vector3.Distance(animalGo.transform.position, col.transform.position);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    bestTarget = col.transform;
                }
            }
        }

        currentTarget = bestTarget;
    }

    /// <summary>
    /// สั่งระเบิดวงเวทฟิสิกส์กวาดดาเมจใส่เป้าหมายทั้งหมดในระยะโจมตี
    /// </summary>
    private void ExecuteOverlapAttack()
    {
        Debug.Log($"{animalGo.name} : 🦖 ตวัดกรงเล็บ/อ้าปากงับ! (Overlap Attack)");

        // 🟢 [FIXED CS0103] แก้ไขจากพิกัดตัวแปรเอ๋อ มาดึงตำแหน่งแท้จริงของตัวสัตว์ผ่านคอมโพเนนต์แม่ animalGo
        Vector3 attackerPos = animalGo.transform.position;

        Collider[] targetsInHitbox = Physics.OverlapSphere(attackerPos, attackRadius, attackableLayer);

        foreach (var col in targetsInHitbox)
        {
            if (col.CompareTag("Player") || col.CompareTag("Unit"))
            {
                CharacterStats character = col.GetComponent<CharacterStats>();
                if (character != null)
                {
                    // ส่งค่าความแรงดาเมจ และตำแหน่งจุดทุบไปกระตุ้นระบบสั่นสะดุ้ง HitFeedback ของเป้าหมาย
                    character.TakeDamage(damageAmount, attackerPos);
                    Debug.Log($"💥 {animalGo.name} โจมตีโดน {col.name} ทำดาเมจ {damageAmount} หน่วย!");
                }
            }
        }
    }
}

// ==========================================
// 🟢 2. พฤติกรรม: รักสงบ กินหญ้าชิวๆ (Grazing/Idling)
// ==========================================
[System.Serializable]
public class PeacefulState : AnimalBehaviorState
{
    public float walkSpeed = 1.5f;
    public float wanderRadius = 8f;
    private float wanderTimer = 0f;
    private float nextWanderTime = 3f;

    public override void OnEnterState()
    {
        if (agent != null) agent.speed = walkSpeed;
        wanderTimer = 0f;
    }

    public override void UpdateState(float distanceToPlayer)
    {
        if (agent == null) return;

        wanderTimer += Time.deltaTime;
        if (wanderTimer >= nextWanderTime)
        {
            wanderTimer = 0f;
            nextWanderTime = Random.Range(3f, 6f); 

            Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
            randomDirection += animalGo.transform.position;
            
            if (UnityEngine.AI.NavMesh.SamplePosition(randomDirection, out UnityEngine.AI.NavMeshHit navHit, wanderRadius, UnityEngine.AI.NavMesh.AllAreas))
            {
                agent.SetDestination(navHit.position);
                Debug.Log($"{animalGo.name} : 🌿 เดินเล็มกินหญ้าชิวๆ ไปที่จุดใหม่");
            }
        }
    }
}

// ==========================================
// 🔵 3. พฤติกรรม: ขี้กลัว (Coward) เจอหน้าแล้ววิ่งหนีกระเจิง
// ==========================================
[System.Serializable]
public class CowardState : AnimalBehaviorState
{
    public float runSpeed = 5f;
    public float escapeDistance = 12f;

    public override void OnEnterState()
    {
        if (agent != null) agent.speed = runSpeed;
        Debug.Log($"{animalGo.name} : 😨 ตื่นตระหนก! วิ่งหนีเร็ว!");
    }

    public override void UpdateState(float distanceToPlayer)
    {
        if (playerTransform == null || agent == null) return;

        Vector3 dirToPlayer = animalGo.transform.position - playerTransform.position;
        dirToPlayer.y = 0; 
        dirToPlayer.Normalize();

        Vector3 runTargetPos = animalGo.transform.position + (dirToPlayer * escapeDistance);

        if (UnityEngine.AI.NavMesh.SamplePosition(runTargetPos, out UnityEngine.AI.NavMeshHit navHit, escapeDistance, UnityEngine.AI.NavMesh.AllAreas))
        {
            agent.SetDestination(navHit.position);
        }
    }
}