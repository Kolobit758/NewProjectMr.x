using UnityEngine;

public class RangedEnemy : EnemyBase
{
    [Header("Ranged Settings")]
    public float fireRate = 1.5f;
    public float minKeepDistance = 7f;
    public GameObject bulletPrefab;
    public Transform firePoint;
    public int bulletDamage = 15; // พลังโจมตีของกระสุน

    protected override void Start()
    {
        base.Start();
        tacticalOffset = tacticalOffset.normalized * Random.Range(minKeepDistance, minKeepDistance + 3f);
    }

    protected override void UpdateStateMachine()
    {
        if (target == null)
        {
            currentState = EnemyState.Idle;
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        switch (currentState)
        {
            case EnemyState.Idle:
                currentState = EnemyState.Chasing;
                break;

            case EnemyState.Chasing:
                Vector3 lookDir = (target.position - transform.position).normalized;
                lookDir.y = 0;
                transform.forward = lookDir;

                // เดินตามตำแหน่งที่ Manager สั่งไว้ใน tacticalOffset
                MoveTowards(target.position + tacticalOffset);

                currentState = EnemyState.Attacking;
                break;

            case EnemyState.Attacking:
                stateTimer -= Time.deltaTime;

                if (target != null)
                {
                    Vector3 targetDir = (target.position - transform.position).normalized;
                    targetDir.y = 0;
                    transform.forward = targetDir;
                }

                if (stateTimer <= 0)
                {
                    Shoot();
                    stateTimer = fireRate;
                }

                if (distanceToTarget < minKeepDistance || distanceToTarget > minKeepDistance + 4f)
                {
                    currentState = EnemyState.Chasing;
                }
                break;

            case EnemyState.Dead:
                break;
        }
    }

    void Shoot()
    {
        if (bulletPrefab != null && firePoint != null)
        {
            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, transform.rotation);

            // ส่งข้อมูลดาเมจให้ตัวกระสุนโง่ๆ ของเราไปจัดการต่อ
            if (bullet.TryGetComponent<GreyboxBullet>(out var bulletScript))
            {
                bulletScript.SetupBullet(bulletDamage, transform.position, targetLayers);
            }

            Rigidbody rb = bullet.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = transform.forward * 15f;
            }
            Destroy(bullet, 3f);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, minKeepDistance);
        Gizmos.color = new Color(1f, 0.6f, 0f);
        Gizmos.DrawWireSphere(transform.position, minKeepDistance + 4f);

        if (target != null && firePoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(firePoint.position, target.position);
        }
    }
}