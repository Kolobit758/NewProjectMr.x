using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public enum EnemyState { Idle, Chasing, Attacking, Cooldown, Dead }
public enum EnemyRole { Tank, Melee, Ranged }

public abstract class EnemyBase : MonoBehaviour
{
    public EnemyRole role;
    public Vector3 tacticalOffset; // ตำแหน่งที่ Manager จะสั่งให้ไปยืน
    protected float stateTimer = 0f;
    public float detectRadius = 15f;
    public LayerMask targetLayers;
    public float moveSpeed = 4f;

    protected Transform target;
    protected EnemyState currentState = EnemyState.Idle;
    protected CharacterStats myStats;
    public OutPosManager myOutPos;

    protected virtual void Awake() => myStats = GetComponent<CharacterStats>();

    // เปลี่ยนจาก Start เดิมที่เป็นการสุ่ม ให้เป็นว่างไว้ หรือใช้เซ็ตค่าเริ่มต้น
    protected virtual void Start() { }

    public void SetTacticalPosition(Vector3 newOffset)
    {
        tacticalOffset = newOffset;
    }

    protected void MoveTowards(Vector3 destination)
    {
        Vector3 separation = GetSeparationForce();
        Vector3 direction = (destination - transform.position).normalized + (separation * 0.5f);
        direction.y = 0;
        transform.position += direction.normalized * moveSpeed * Time.deltaTime;

        direction.y = 0;
        transform.position += direction * moveSpeed * Time.deltaTime;
        if (direction != Vector3.zero) transform.forward = direction;
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
    // เพิ่มฟังก์ชันนี้ใน EnemyBase.cs
    protected Vector3 GetSeparationForce()
    {
        Vector3 separation = Vector3.zero;
        float separationRadius = 1.5f;

        // ตรวจสอบโจรตัวอื่นรอบๆ
        Collider[] neighbors = Physics.OverlapSphere(transform.position, separationRadius, LayerMask.GetMask("Enemy")); // สมมติว่าโจรอยู่ Layer "Enemy"
        foreach (var n in neighbors)
        {
            if (n.gameObject != this.gameObject)
            {
                separation += (transform.position - n.transform.position).normalized;
            }
        }
        return separation;
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


    protected abstract void UpdateStateMachine();

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectRadius);
    }
}