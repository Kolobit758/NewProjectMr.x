using System.Collections;
using UnityEngine;

public class HitFeedback : MonoBehaviour
{
    [Header("Juice / AnimationSettings")]
    [SerializeField] private float knockbackDistance = 0.25f;
    [SerializeField] private float animationDuration = 0.15f;
    [SerializeField] private Vector3 punchScaleAmount = new Vector3(1.2f, 0.8f, 1.2f);
    [SerializeField] private float hitCooldown = 0.1f;

    private Vector3 originalPosition;
    private Vector3 originalScale;
    private bool isHitCooldown = false;
    private Coroutine feedbackCoroutine;

    // ลิงก์ไปยังสคริปต์สเตตัส (ถ้ามี)
    private CharacterStats characterStats;

    void Awake()
    {
        // แอบส่องดูว่าในตัวนี้มีสคริปต์สเตตัสไหม ถ้ามีจะได้ผูกเหตุการณ์อัตโนมัติ
        characterStats = GetComponent<CharacterStats>();
    }

    void OnEnable()
    {
        // หากต้องการผูกกับตัวเปิดรับดาเมจของระบบอื่น หรือจะเรียกผ่านฟังก์ชันตรงๆ ก็ได้
    }

    void Start()
    {
        originalPosition = transform.position;
        originalScale = transform.localScale;
    }

    // ฟังก์ชันหลักที่สั่งให้สั่นสะดุ้ง เรียกใช้จากภายนอกได้เลย
    public void PlayHitFeedback(Vector3 attackerPosition)
    {
        if (isHitCooldown) return;

        // อัปเดตตำแหน่งเริ่มต้นล่าสุด (เผื่อวัตถุมีการเคลื่อนที่ไปที่อื่นแล้ว)
        originalPosition = transform.position;

        if (feedbackCoroutine != null) StopCoroutine(feedbackCoroutine);
        feedbackCoroutine = StartCoroutine(HitFeedbackRoutine(attackerPosition));
    }

    private IEnumerator HitFeedbackRoutine(Vector3 attackerPosition)
    {
        isHitCooldown = true;

        // คำนวณทิศทางเด้งถอยหลัง
        Vector3 pushDirection = (transform.position - attackerPosition);
        pushDirection.y = 0; 
        pushDirection.Normalize();

        Vector3 targetPosition = transform.position + (pushDirection * knockbackDistance);
        Vector3 targetScale = Vector3.Scale(originalScale, punchScaleAmount);

        float elapsedTime = 0f;

        // ขาไป: ยุบตัวและถอยหลัง
        while (elapsedTime < animationDuration * 0.4f)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / (animationDuration * 0.4f);
            transform.position = Vector3.Lerp(originalPosition, targetPosition, t);
            transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            yield return null;
        }

        elapsedTime = 0f;

        // ขากลับ: คืนรูปทรงและตำแหน่งเดิม
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