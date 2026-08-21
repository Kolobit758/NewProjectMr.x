using UnityEngine;

public class Projectile : MonoBehaviour
{
    private Vector3 direction;
    private float damage;
    private float speed;
    private LayerMask hitMask;

    public void Setup(Vector3 dir, float dmg, float spd, LayerMask mask)
    {
        direction = dir.normalized;
        damage = dmg;
        speed = spd;
        hitMask = mask;

        Destroy(gameObject, 3f);
    }

    void Update()
    {
        transform.position += direction * speed * Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        if ((hitMask.value & (1 << other.gameObject.layer)) == 0)
        {
            return;
        }

        if (other.CompareTag("Enemy"))
        {
            CharacterStats enemy = other.GetComponent<CharacterStats>();
            if (enemy != null)
            {
                enemy.TakeDamage(Mathf.RoundToInt(damage),transform.position);
            }
        }

        Destroy(gameObject);
    }
}
