using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

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
    private bool isHitCooldown = false; 

    [Header("Random Spawn On Start (สุ่มที่ซ่อนตอนเริ่มเกม)")]
    public bool randomPositionOnStart = true;
    public float minRandomRadius = 15f;
    public float maxRandomRadius = 40f;

    [Header("Juice / Animation")]
    [SerializeField] private float knockbackDistance = 0.25f; 
    [SerializeField] private float animationDuration = 0.15f; 
    [SerializeField] private Vector3 punchScaleAmount = new Vector3(1.2f, 0.8f, 1.2f); 

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
        originalScale = transform.localScale;

        // 🟢 ถ้าเปิดตั้งค่าไว้ ให้มันสุ่มตำแหน่งแอบบนพื้นดินตั้งแต่เริ่มโหลดฉากเลย!
        if (randomPositionOnStart)
        {
            RandomizeLocationOnGround();
        }
        else
        {
            originalPosition = transform.position;
        }
    }

    // 🟢 ฟังก์ชันเลือกที่ซ่อนสุ่มบน NavMesh เงียบๆ ตั้งแต่ต้นเกม
    private void RandomizeLocationOnGround()
    {
        Vector3 centerPos = transform.position; // ใช้จุดที่วางไว้ใน Editor เป็นจุดศูนย์กลางการสุ่ม
        bool foundValidGround = false;

        for (int attempt = 0; attempt < 50; attempt++)
        {
            Vector2 randomCircle = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(minRandomRadius, maxRandomRadius);
            Vector3 randomTargetPos = centerPos + new Vector3(randomCircle.x, 0f, randomCircle.y);

            if (NavMesh.SamplePosition(randomTargetPos, out NavMeshHit navHit, 10f, NavMesh.AllAreas))
            {
                transform.position = navHit.position;
                foundValidGround = true;
                break;
            }
        }

        // เซ็ตพิกัดอ้างอิงหลักไว้ตรงจุดที่สุ่มเจอ
        originalPosition = transform.position;

        if (!foundValidGround)
        {
            Debug.LogWarning($"[ItemDropBox] {gameObject.name} สุ่มหาพื้น NavMesh รอบๆ ไม่เจอ จึงใช้ตำแหน่งเดิมใน Editor");
        }
    }

    public void Broken(Vector3 attackerPosition)
    {
        if (isBroken || isHitCooldown) return;

        attackCount++;
        if (attackCount >= brokeAttackCount)
        {
            isBroken = true;
            DropItems();
            Destroy(gameObject); 
        }
        else
        {
            if (feedbackCoroutine != null) StopCoroutine(feedbackCoroutine);
            feedbackCoroutine = StartCoroutine(HitFeedbackRoutine(attackerPosition));
        }
    }

    private IEnumerator HitFeedbackRoutine(Vector3 attackerPosition)
    {
        isHitCooldown = true; 

        Vector3 pushDirection = (transform.position - attackerPosition);
        pushDirection.y = 0; 
        pushDirection.Normalize();

        Vector3 targetPosition = originalPosition + (pushDirection * knockbackDistance);
        Vector3 targetScale = Vector3.Scale(originalScale, punchScaleAmount);

        float elapsedTime = 0f;
        float phase1Duration = animationDuration * 0.4f;

        while (elapsedTime < phase1Duration)
        {
            elapsedTime += Time.unscaledDeltaTime; 
            float t = elapsedTime / phase1Duration;
            transform.position = Vector3.Lerp(originalPosition, targetPosition, t);
            transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            yield return null;
        }

        elapsedTime = 0f;
        float phase2Duration = animationDuration * 0.6f;

        while (elapsedTime < phase2Duration)
        {
            elapsedTime += Time.unscaledDeltaTime; 
            float t = elapsedTime / phase2Duration;
            transform.position = Vector3.Lerp(targetPosition, originalPosition, t);
            transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
            yield return null;
        }

        transform.position = originalPosition;
        transform.localScale = originalScale;

        yield return new WaitForSeconds(0.1f); 
        isHitCooldown = false; 
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