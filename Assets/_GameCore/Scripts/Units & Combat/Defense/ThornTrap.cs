using UnityEngine;

public class ThornTrap : MonoBehaviour
{
    [Header("Trap Settings")]
    public float triggerRadius = 2.2f;
    public float damagePerTick = 8f;
    public float tickInterval = 1f;
    public LayerMask enemyLayer;

    private float tickTimer;

    void Start()
    {
        tickTimer = tickInterval;
    }

    void Update()
    {
        tickTimer -= Time.deltaTime;
        if (tickTimer > 0f) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, triggerRadius, enemyLayer);

        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;

            CharacterStats enemy = hit.GetComponent<CharacterStats>();
            if (enemy != null)
            {
                enemy.TakeDamage(Mathf.RoundToInt(damagePerTick),transform.position);
            }
        }

        tickTimer = tickInterval;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
    }
}
