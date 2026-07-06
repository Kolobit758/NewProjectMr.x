using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemDropBox : MonoBehaviour
{
    [System.Serializable]
    public struct DropableItem
    {
        public GameObject itemPrefab;
        [Range(0, 100)] public float dropChance; 
    }

    [Header("Box Settings")]
    public int brokeAttackCount = 3;
    private int attackCount = 0;
    private bool isBroken = false; 
    private bool isHitCooldown = false; // เปลี่ยนเป็นแบบ private กันบั๊กโดนสคริปต์อื่นแทรกแซง

    [Header("Juice / Animation (โค้ดสั่นเด้งถอยหลัง)")]
    [SerializeField] private float knockbackDistance = 0.25f; // ระยะเด้งหนีคนตี
    [SerializeField] private float animationDuration = 0.15f; // ความเร็วในการสั่นสะดุ้ง
    [SerializeField] private Vector3 punchScaleAmount = new Vector3(1.2f, 0.8f, 1.2f); // โดนทุบแล้วแบนลงกระจายออกข้าง

    [Header("Drop Settings")]
    [SerializeField] private List<DropableItem> itemList = new List<DropableItem>(); 
    [SerializeField] private int minDropCount = 1; 
    [SerializeField] private int maxDropCount = 3; 
    [SerializeField] private float dropRadius = 1.5f; 

    private Vector3 originalPosition;
    private Vector3 originalScale;
    private Coroutine feedbackCoroutine;

    void Start()
    {
        originalPosition = transform.position;
        originalScale = transform.localScale;
    }

    // อัปเดต: รับค่าตำแหน่งคนตีเข้ามาเพื่อดันกล่องไปข้างหลังด้วย
    public void Broken(Vector3 attackerPosition)
    {
        if (isBroken || isHitCooldown) return;

        attackCount++;
        Debug.Log("hit count : " + attackCount);

        if (attackCount >= brokeAttackCount)
        {
            isBroken = true;
            DropItems();
            Destroy(gameObject); 
        }
        else
        {
            // 🛠️ สั่งรันคูลดาวน์พร้อมทำแอนิเมชันด้วยวิธีที่ถูกต้องผ่าน StartCoroutine
            if (feedbackCoroutine != null) StopCoroutine(feedbackCoroutine);
            feedbackCoroutine = StartCoroutine(HitFeedbackRoutine(attackerPosition));
        }
    }

    private IEnumerator HitFeedbackRoutine(Vector3 attackerPosition)
    {
        isHitCooldown = true; // เปิดระบบกันการโจมตีซ้ำซ้อนซ้ำถัง

        // คำนวณทิศทางเด้งถอยหลังหนีผู้เล่น
        Vector3 pushDirection = (transform.position - attackerPosition);
        pushDirection.y = 0; 
        pushDirection.Normalize();

        Vector3 targetPosition = originalPosition + (pushDirection * knockbackDistance);
        Vector3 targetScale = Vector3.Scale(originalScale, punchScaleAmount);

        float elapsedTime = 0f;

        // ขาไป: ยุบตัวลงและถอยหลังอย่างรวดเร็ว
        while (elapsedTime < animationDuration * 0.4f)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / (animationDuration * 0.4f);
            transform.position = Vector3.Lerp(originalPosition, targetPosition, t);
            transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            yield return null;
        }

        elapsedTime = 0f;

        // ขากลับ: ดีดเด้งดึ๋งคืนรูปทรงเดิม
        while (elapsedTime < animationDuration * 0.6f)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / (animationDuration * 0.6f);
            transform.position = Vector3.Lerp(targetPosition, originalPosition, t);
            transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
            yield return null;
        }

        // รีเซ็ตค่าตำแหน่งกลับสู่สภาวะปกติให้เป๊ะ
        transform.position = originalPosition;
        transform.localScale = originalScale;

        // คูลดาวน์สั้น ๆ ก่อนจะรับดาเมจฮิตถัดไปได้ (ปรับเวลาตรงนี้ได้ตามใจชอบ)
        yield return new WaitForSeconds(0.1f); 
        isHitCooldown = false; // ปลดล็อกให้พร้อมรับการโจมตีครั้งต่อไป
    }

    private void DropItems()
    {
        if (itemList == null || itemList.Count == 0) return;

        int actualDropCount = UnityEngine.Random.Range(minDropCount, maxDropCount + 1);

        for (int i = 0; i < actualDropCount; i++)
        {
            GameObject selectedItem = GetRandomItem();
            if (selectedItem != null)
            {
                Vector3 randomOffset = new Vector3(
                    UnityEngine.Random.Range(-dropRadius, dropRadius),
                    0.2f, 
                    UnityEngine.Random.Range(-dropRadius, dropRadius)
                );

                Vector3 spawnPosition = originalPosition + randomOffset;
                Instantiate(selectedItem, spawnPosition, Quaternion.identity);
            }
        }
    }

    private GameObject GetRandomItem()
    {
        float totalChance = 0;
        foreach (var item in itemList) totalChance += item.dropChance;
        if (totalChance <= 0) return null;

        float randomValue = UnityEngine.Random.Range(0f, totalChance);
        float cumulativeChance = 0f;

        foreach (var item in itemList)
        {
            cumulativeChance += item.dropChance;
            if (randomValue <= cumulativeChance) return item.itemPrefab;
        }
        return null;
    }
}