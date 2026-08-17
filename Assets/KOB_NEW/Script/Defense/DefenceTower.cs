using UnityEngine;

public class DefenceTower : MonoBehaviour
{
    [Header("Basic")]
    public string towerName = "Turret";
    public int buildCost = 20;
    public float maxHP = 150f;
    public float currentHP;

    [Header("Combat")]
    public float range = 12f;
    public float fireRate = 1.2f;
    public float damage = 20f;
    public float projectileSpeed = 18f;
    public float rotationSpeed = 8f;

    [Header("References")]
    public Transform firePoint;
    public GameObject projectilePrefab;
    public LayerMask enemyLayer;

    private float cooldown;

    void Start()
    {
        currentHP = maxHP;
    }

    void Update()
    {
        if (cooldown > 0f)
        {
            cooldown -= Time.deltaTime;
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, range, enemyLayer);
        if (hits.Length == 0) return;

        Transform target = GetClosestEnemy(hits);
        if (target == null) return;

        Vector3 lookTarget = new Vector3(target.position.x, transform.position.y, target.position.z);
        Quaternion lookRot = Quaternion.LookRotation(lookTarget - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, rotationSpeed * Time.deltaTime);

        if (cooldown <= 0f)
        {
            Fire(target);
            cooldown = 1f / Mathf.Max(fireRate, 0.1f);
        }
    }

    private Transform GetClosestEnemy(Collider[] hits)
    {
        Transform bestTarget = null;
        float bestDistance = Mathf.Infinity;

        foreach (var hit in hits)
        {
            if (hit == null) continue;
            if (!hit.CompareTag("Enemy")) continue;

            float distance = Vector3.Distance(transform.position, hit.transform.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestTarget = hit.transform;
            }
        }

        return bestTarget;
    }

    private void Fire(Transform target)
    {
        if (firePoint == null) return;
        if (projectilePrefab == null) return;

        Vector3 direction = (target.position - firePoint.position).normalized;
        GameObject projectileObject = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(direction));
        Projectile projectile = projectileObject.GetComponent<Projectile>();

        if (projectile != null)
        {
            projectile.Setup(direction, damage, projectileSpeed, enemyLayer);
        }
    }

    public void TakeDamage(float amount)
    {
        currentHP -= amount;
        if (currentHP <= 0f)
        {
            Destroy(gameObject);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
