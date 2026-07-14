using System.Collections;
using UnityEngine;

public class HitFeedback : MonoBehaviour
{
    [Header("Juice / AnimationSettings")]
    [SerializeField] private float knockbackDistance = 0.25f;
    [SerializeField] private float animationDuration = 0.15f;
    [SerializeField] private Vector3 punchScaleAmount = new Vector3(1.2f, 0.8f, 1.2f);
    [SerializeField] private float hitCooldown = 0.1f;

    [Header("Ragnarok Damage Popup")]
    [SerializeField] private GameObject damagePopupPrefab; // ลาก Prefab ตัวเลขมาใส่ตรงนี้

    private Vector3 originalPosition;
    private Vector3 originalScale;
    private bool isHitCooldown = false;
    private Coroutine feedbackCoroutine;
    private CharacterStats characterStats;

    void Awake()
    {
        characterStats = GetComponent<CharacterStats>();
    }

    void Start()
    {
        originalPosition = transform.position;
        originalScale = transform.localScale;
    }

    // เพิ่มพารามิเตอร์ int damageAmount เข้ามา
    public void PlayHitFeedback(Vector3 attackerPosition, int damageAmount)
    {
        // 1. สร้างตัวเลขดาเมจทันที (ต่อให้ติดคูลดาวน์สั่นสะดุ้ง แต่ตัวเลขต้องเด้งทุกครั้งที่โดนตี!)
        SpawnDamagePopup(damageAmount);

        if (isHitCooldown) return;

        originalPosition = transform.position;

        if (feedbackCoroutine != null) StopCoroutine(feedbackCoroutine);
        feedbackCoroutine = StartCoroutine(HitFeedbackRoutine(attackerPosition));
    }

    // ฟังก์ชันสร้างตัวเลขลอย
    private void SpawnDamagePopup(int damage)
    {
        if (damagePopupPrefab == null) return;

        // สร้างตัวเลขขึ้นมาเหนือนิ้ว/หัวของโมเดลนิดหน่อย
        Vector3 spawnPos = transform.position + Vector3.up * 2f; 
        GameObject popupObj = Instantiate(damagePopupPrefab, spawnPos, Quaternion.identity);
        
        if (popupObj.TryGetComponent<DamagePopup>(out var popup))
        {
            popup.Setup(damage);
        }
    }

    private IEnumerator HitFeedbackRoutine(Vector3 attackerPosition)
    {
        isHitCooldown = true;

        Vector3 pushDirection = (transform.position - attackerPosition);
        pushDirection.y = 0; 
        pushDirection.Normalize();

        Vector3 targetPosition = transform.position + (pushDirection * knockbackDistance);
        Vector3 targetScale = Vector3.Scale(originalScale, punchScaleAmount);

        float elapsedTime = 0f;

        while (elapsedTime < animationDuration * 0.4f)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / (animationDuration * 0.4f);
            transform.position = Vector3.Lerp(originalPosition, targetPosition, t);
            transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            yield return null;
        }

        elapsedTime = 0f;

        while (elapsedTime < animationDuration * 0.6f)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / (animationDuration * 0.6f);
            transform.position = Vector3.Lerp(targetPosition, originalPosition, t);
            transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
            yield return null;
        }

        transform.localScale = originalScale;

        yield return new WaitForSeconds(hitCooldown); 
        isHitCooldown = false;
    }
}