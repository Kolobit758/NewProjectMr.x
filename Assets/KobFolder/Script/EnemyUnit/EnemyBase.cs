using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public enum EnemyState { Idle, Chasing, Attacking, Cooldown, Dead }

public abstract class EnemyBase : MonoBehaviour
{
    [Header("Base Settings")]
    public float detectRadius = 15f;
    public LayerMask targetLayers;
    public float moveSpeed = 4f;

    protected Transform target;
    protected EnemyState currentState = EnemyState.Idle;
    protected float stateTimer = 0f;
    protected Vector3 flockOffset;

    // ดึง CharacterStats ของตัวเองมาใช้งาน
    protected CharacterStats myStats;
    public OutPosManager myOutPos;

    protected virtual void Awake()
    {
        myStats = GetComponent<CharacterStats>();
    }

    protected virtual void Start()
    {
        float randomAngle = Random.Range(0f, 360f);
        float randomRadius = Random.Range(2f, 5f);
        flockOffset = new Vector3(Mathf.Cos(randomAngle), 0, Mathf.Sin(randomAngle)) * randomRadius;
    }

    // ใน EnemyBase.cs ตรงฟังก์ชัน Update()
    protected virtual void Update()
    {
        if (myStats != null && myStats.currentHP <= 0)
        {
            if (currentState != EnemyState.Dead)
            {
                currentState = EnemyState.Dead;

                // 🔥 เปลี่ยนมาใช้ Coroutine เพื่อจัดระเบียบลำดับการตายให้จบหล่อๆ ท้ายเฟรม
                StartCoroutine(HandleDeathRoutine());
            }
            return;
        }

        FindTarget();
        UpdateStateMachine();
    }

    private IEnumerator HandleDeathRoutine()
    {
        // 1. ถอนชื่อออกจากระบบค่ายก่อน
        if (myOutPos != null)
        {
            myOutPos.RemoveEnemy(gameObject);
        }

        // 2. ⏳ ไม้ตาย: สั่งให้โค้ดหยุดรอจนกว่ากวาดล้างลูป Update ของวัตถุทุกตัวในเฟรมนี้เสร็จสิ้นก่อน!
        yield return new WaitForEndOfFrame();

        // 3. 💥 พ้นเฟรมไปแล้ว ไม่มีใครเรียกหาเราแล้ว สั่งทำลายตัวเองทิ้งแบบไร้เออร์เรอร์รบกวน!
        Destroy(gameObject);
    }
    protected void FindTarget()
    {
        if (target != null)
        {
            // ถ้าเป้าหมายเดิมตายแล้ว ให้เลิกสนใจ
            if (target.TryGetComponent<CharacterStats>(out var targetStats) && targetStats.currentHP <= 0)
            {
                target = null;
                return;
            }
            return;
        }

        Collider[] cols = Physics.OverlapSphere(transform.position, detectRadius, targetLayers);
        foreach (var col in cols)
        {
            if (col.CompareTag("Player") || col.CompareTag("Unit"))
            {
                // เช็คด้วยว่าเป้าหมายที่เราเจอนั้นยังไม่ตาย
                if (col.TryGetComponent<CharacterStats>(out var targetStats) && targetStats.currentHP > 0)
                {
                    target = col.transform;
                    break;
                }
            }
        }
    }

    protected void MoveTowards(Vector3 destination)
    {
        Vector3 direction = (destination - transform.position).normalized;
        direction.y = 0;
        transform.position += direction * moveSpeed * Time.deltaTime;
        if (direction != Vector3.zero)
        {
            transform.forward = direction;
        }
    }

    protected abstract void UpdateStateMachine();

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectRadius);
    }
}