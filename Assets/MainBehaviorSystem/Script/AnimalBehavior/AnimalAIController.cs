using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class AnimalAIController : MonoBehaviour
{
    // 🟢 enum ตัวช่วยเลือกนิสัยหลักตั้งต้นในหน้า Inspector สะดวกๆ
    public enum AnimalNature { PeacefulGrassEater, AggressiveHunter, CowardFlee }
    
    [Header("Animal Setup")]
    public AnimalNature defaultNature = AnimalNature.PeacefulGrassEater;
    public float alertRadius = 7f; // รัศมีวงเวทตรวจจับผู้เล่น (เข้าใกล้ระยะนี้จะเริ่มเปลี่ยนใจ)

    private NavMeshAgent agent;
    public Transform playerTransform;
    private AnimalBehaviorState currentState;

    // ประกาศกล่องพฤติกรรมรอสวมรอย
    private PeacefulState peacefulState = new PeacefulState();
    private AggressiveState aggressiveState = new AggressiveState();
    private CowardState cowardState = new CowardState();

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;

        // สั่งเชื่อมระบบโครงสร้างให้ทุกรัฐพฤติกรรมรู้จักตัวตนของสัตว์ตัวนี้
        peacefulState.InitState(gameObject, agent);
        aggressiveState.InitState(gameObject, agent);
        cowardState.InitState(gameObject, agent);

        // บังคับสวมนิสัยดั้งเดิมตามที่เราติ๊กไว้ใน Inspector
        ResetToDefaultNature();
    }

    void Update()
    {
        if (playerTransform == null || currentState == null) return;

        // 1. คำนวณระยะห่างระหว่างสัตว์ตัวนี้กับผู้เล่น
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // 2. 🧠 ลอจิกการตัดสินใจเปลี่ยนพฤติกรรมตามสภาพแวดล้อม
        HandleBehaviorTransition(distanceToPlayer);

        // 3. รันพฤติกรรมปัจจุบันที่สวมอยู่ต่อเนื่อง
        currentState.UpdateState(distanceToPlayer);
    }

    private void HandleBehaviorTransition(float distanceToPlayer)
    {
        switch (defaultNature)
        {
            case AnimalNature.PeacefulGrassEater:
                // สัตว์กินหญ้าปกติจะกินชิวๆ ตลอดเวลา (หรือเดฟจะเขียนให้สลับเป็นสเตตัสตื่นตูมตอนโดนโจมตีเพิ่มทีหลังได้)
                if (currentState != peacefulState) ChangeState(peacefulState);
                break;

            case AnimalNature.AggressiveHunter:
                // ถ้าเป็นนักล่า: ผู้เล่นเข้าใกล้รัศมีตื่นตัว ➡️ เปลี่ยนเป็นสเตตัสไล่กัดดุร้ายทันที!
                if (distanceToPlayer <= alertRadius)
                {
                    if (currentState != aggressiveState) ChangeState(aggressiveState);
                    currentState.SetFlukeTransform(transform);
                }
                else
                {
                    // ถ้าผู้เล่นวิ่งหนีไปไกลเกินวง ➡️ กลับไปเดินเล่นกินหญ้าชิวๆ คลายเครียด
                    if (currentState != peacefulState) ChangeState(peacefulState);
                }
                break;

            case AnimalNature.CowardFlee:
                // ถ้าเป็นสัตว์ขี้กลัว: ผู้เล่นเข้าใกล้รัศมีตื่นตัว ➡️ วิ่งหนีกระเจิง!
                if (distanceToPlayer <= alertRadius)
                {
                    if (currentState != cowardState) ChangeState(cowardState);
                }
                else
                {
                    // ปลอดภัยแล้ว ➡️ กลับมาเดินเล็มหญ้าต่อ
                    if (currentState != peacefulState) ChangeState(peacefulState);
                }
                break;
        }
    }

    public void ChangeState(AnimalBehaviorState newState)
    {
        currentState = newState;
        currentState.OnEnterState(); // กระตุ้นแอนิเมชันหรือสปีดรอบใหม่
    }

    public void ResetToDefaultNature()
    {
        if (defaultNature == AnimalNature.AggressiveHunter || defaultNature == AnimalNature.CowardFlee)
        {
            ChangeState(peacefulState); // เริ่มเกมมาให้เดินชิวๆ ปลอมตัวไว้ก่อน
        }
        else
        {
            ChangeState(peacefulState);
        }
    }

    // วาดวงกลมสีแดงโชว์รัศมีสายตาตรวจจับสัตว์ในหน้าต่าง Scene จะได้ปรับแต่งระยะง่ายๆ
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, alertRadius);
    }
}