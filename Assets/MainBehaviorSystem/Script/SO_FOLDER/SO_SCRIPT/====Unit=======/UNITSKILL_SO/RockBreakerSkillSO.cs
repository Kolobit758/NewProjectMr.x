using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[CreateAssetMenu(fileName = "NewRockBreakerSkill", menuName = "Combat/Skills/Rock Breaker")]
public class RockBreakerSkillSO : UnitSkillSO
{
    [Header("Skill Mechanics")]
    public float windUpDistance = 1.5f; // ระยะที่ถอยหลังไปตั้งหลัก
    public float windUpDuration = 0.5f; // เวลาที่ใช้ในการถอยหลังชาร์จ
    
    public float dashSpeed = 25f;       // ความเร็วตอนพุ่งชน
    public float maxDashDuration = 0.4f;// เวลาสูงสุดที่จะพุ่งไปข้างหน้า (กันบั๊กพุ่งทะลุโลก)

    [Header("Destruction Settings")]
    public LayerMask breakableLayer;    // เลือก Layer ที่สามารถทำลายได้ (เช่น Breakable)
    public float breakRadius = 2.5f;    // รัศมีการระเบิดทำลายรอบตัวเมื่อพุ่งไปสุดทาง

    public override void ExecuteSkill(GameObject user)
    {
        // ส่งต่อลอจิกให้ MonoBehaviour ในยูนิตตัวนั้นรัน Coroutine (เนื่องจาก ScriptableObject รัน Coroutine เองโดยตรงไม่ได้)
        MonoBehaviour runtimeRunner = user.GetComponent<MonoBehaviour>();
        if (runtimeRunner != null)
        {
            runtimeRunner.StartCoroutine(RockBreakerRoutine(user));
        }
    }

    private IEnumerator RockBreakerRoutine(GameObject user)
    {
        Debug.Log($"{user.name} 🎬 เริ่มชาร์จสกิล Rock Breaker!");

        // 1. ตรวจสอบและปิดการทำงานของระบบขบวนทัพชั่วคราว
        NavMeshAgent agent = user.GetComponent<NavMeshAgent>();
        FormationUnitController controller = user.GetComponent<FormationUnitController>();
        
        if (controller != null) controller.enabled = false;
        if (agent != null && agent.isOnNavMesh) agent.isStopped = true;

        // ค้นหาเป้าหมายหินที่ใกล้ที่สุดในระยะสายตา เพื่อหันหน้าไปให้ตรงจุด
        Vector3 dashDirection = user.transform.forward;
        Collider[] initialCheck = Physics.OverlapSphere(user.transform.position, 10f, breakableLayer);
        if (initialCheck.Length > 0)
        {
            // หันหน้าเข้าหาหินก้อนแรกที่เจอ
            Vector3 targetDir = (initialCheck[0].transform.position - user.transform.position);
            targetDir.y = 0; // ล็อกแกน Y ไว้ไม่ให้ตัวเอียงทิ่มลงดิน
            if (targetDir != Vector3.zero)
            {
                user.transform.rotation = Quaternion.LookRotation(targetDir.normalized);
                dashDirection = targetDir.normalized;
            }
        }

        // 2. [Phase 1: ถอยหลังชาร์จตั้งหลัก (Wind-up)]
        Vector3 startPos = user.transform.position;
        Vector3 windUpTargetPos = startPos - (dashDirection * windUpDistance);
        
        // ลองเช็ค NavMesh เผื่อข้างหลังเป็นเหวหรือกำแพง จะได้ไม่ถอยทะลุฉาก
        if (NavMesh.SamplePosition(windUpTargetPos, out NavMeshHit hit, windUpDistance, NavMesh.AllAreas))
        {
            windUpTargetPos = hit.position;
        }

        float timer = 0f;
        while (timer < windUpDuration)
        {
            if (user == null) yield break;
            timer += Time.deltaTime;
            float t = timer / windUpDuration;
            
            // ใช้ Lerp ค่อยๆ ย้ายตัวถอยหลัง
            user.transform.position = Vector3.Lerp(startPos, windUpTargetPos, t);
            yield return null;
        }

        // หยุดรอจังหวะเสี้ยววินาทีก่อนพุ่ง (สร้างแรงอิมแพคสะใจ)
        yield return new WaitForSeconds(0.1f);

        // 3. [Phase 2: พุ่งทะยานไปข้างหน้า (Dash Charge)]
        Debug.Log($"{user.name} ⚡ พุ่งชน!!");
        if (agent != null) agent.enabled = false; // ปิด Agent สนิทป้องกันแรงต้านความเร็ว

        timer = 0f;
        bool hitSomething = false;

        while (timer < maxDashDuration && !hitSomething)
        {
            if (user == null) yield break;
            timer += Time.deltaTime;

            // เคลื่อนที่ไปข้างหน้าตามทิศทางเดช
            user.transform.position += dashDirection * dashSpeed * Time.deltaTime;

            // ตรวจจับสิ่งกีดขวางด้านหน้าแบบ Realtime ระหว่างพุ่ง
            Collider[] hitColliders = Physics.OverlapSphere(user.transform.position, 1.2f, breakableLayer);
            if (hitColliders.Length > 0)
            {
                hitSomething = true; // ชนเป้าหมายแล้ว ให้หลุดลูปพุ่งทันที
            }

            yield return null;
        }

        // 4. [Phase 3: ระเบิดพลังทำลาย Object (Explosion / Break)]
        Debug.Log($"{user.name} 💥 กระแทกเป้าหมาย! ส่งแรงทำลายล้าง");
        
        // กวาดรอบตัวผู้เล่นตามรัศมี breakRadius
        Collider[] objectsToBreak = Physics.OverlapSphere(user.transform.position, breakRadius, breakableLayer);
        foreach (var col in objectsToBreak)
        {
            if (col != null)
            {
                Debug.Log($"🔥 ทำลายวัตถุ: {col.gameObject.name}");
                
                // คืนค่าความเสียหาย หรือสั่งลายวัตถุตรงนี้
                // คิวแนะนำ: สามารถสปอว์น Effect ฝุ่น/หินแตก (Instantiate) ตรงนี้ได้เลยครับ
                Destroy(col.gameObject); 
            }
        }

        // 5. คืนค่าระบบกลับสู่สภาวะปกติ เพื่อให้เดินเข้าขบวนทัพต่อได้
        if (user != null)
        {
            if (agent != null)
            {
                agent.enabled = true;
                // อัปเดตพิกัด NavMesh ล่าสุดหลังพุ่งเสร็จเพื่อป้องกันอาการตัวเอ๋อวาร์ปกลับจุดเดิม
                if (NavMesh.SamplePosition(user.transform.position, out NavMeshHit navHit, 3f, NavMesh.AllAreas))
                {
                    agent.Warp(navHit.position);
                }
                agent.isStopped = false;
            }
            if (controller != null) controller.enabled = true;
        }
    }
}