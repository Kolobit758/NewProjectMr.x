using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

public enum UIDirection { LeftToRight, RightToLeft, TopToBottom, BottomToTop }
public enum ParticleTriggerTiming { OnStartOpening, OnReachEndpointClose }

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class UIAnimationController : MonoBehaviour
{
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 defaultAnchoredPos;
    private bool isPositionInited = false;

    [Header("Animation Settings")]
    public UIDirection slideDirection = UIDirection.BottomToTop;
    [Tooltip("ระยะทางเผื่อความไกล (พิกเซล) ให้มันพ้นขอบจอชัวร์ๆ")]
    public float extraOffset = 200f; 
    public float duration = 0.4f;

    [Header("Tweening & Easing")]
    public AnimationCurve tweenCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Particle / Dust Effects")]
    public GameObject dustParticlePrefab;
    public ParticleTriggerTiming particleTiming = ParticleTriggerTiming.OnReachEndpointClose;

    private bool isOpen = false;
    private Coroutine currentRoutine;

    void Awake()
    {
        InitPosition();
    }

    private void InitPosition()
    {
        if (isPositionInited) return;
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        defaultAnchoredPos = rectTransform.anchoredPosition;
        isPositionInited = true;
    }

    // 🟢 ฟังก์ชัน Wrapper แบบไม่มีพารามิเตอร์ (เพื่อให้ลากใส่ปุ่ม OnClick ได้ง่ายๆ)
    public void OpenUI() { OpenUI(null); }
    public void CloseUI() { CloseUI(null); }

    public void OpenUI(Action onComplete = null)
    {
        InitPosition();
        if (isOpen) return;
        isOpen = true;
        
        gameObject.SetActive(true);

        if (dustParticlePrefab != null && particleTiming == ParticleTriggerTiming.OnStartOpening)
        {
            SpawnDustEffect();
        }

        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(AnimateUI(true, onComplete));
    }

    public void CloseUI(Action onComplete = null)
    {
        InitPosition();
        if (!isOpen) return;
        isOpen = false;

        if (dustParticlePrefab != null && particleTiming == ParticleTriggerTiming.OnReachEndpointClose)
        {
            SpawnDustEffect();
        }

        if (currentRoutine != null) StopCoroutine(currentRoutine);
        
        if (gameObject.activeInHierarchy)
        {
            currentRoutine = StartCoroutine(AnimateUI(false, () => {
                gameObject.SetActive(false);
                onComplete?.Invoke();
            }));
        }
        else
        {
            gameObject.SetActive(false);
            onComplete?.Invoke();
        }
    }

    public void ToggleUI()
    {
        if (isOpen) CloseUI();
        else OpenUI();
    }

    private IEnumerator AnimateUI(bool opening, Action onComplete)
    {
        // คำนวณตำแหน่งเริ่มต้นและสิ้นสุดแบบสลับทิศทางเป๊ะๆ
        Vector2 hiddenPos = GetHiddenPosition(slideDirection);
        Vector2 startPos = opening ? hiddenPos : defaultAnchoredPos;
        Vector2 endPos = opening ? defaultAnchoredPos : hiddenPos;

        float startAlpha = opening ? 0f : 1f;
        float endAlpha = opening ? 1f : 0f;

        float timer = 0f;
        canvasGroup.blocksRaycasts = opening;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float rawPercent = Mathf.Clamp01(timer / duration);
            float curvePercent = tweenCurve.Evaluate(rawPercent);

            rectTransform.anchoredPosition = Vector2.LerpUnclamped(startPos, endPos, curvePercent);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, curvePercent);

            yield return null;
        }

        rectTransform.anchoredPosition = endPos;
        canvasGroup.alpha = endAlpha;
        canvasGroup.blocksRaycasts = isOpen;

        onComplete?.Invoke();
    }

    // 🟢 ระบบคำนวณตำแหน่ง "นอกจอ/นอกกรอบ Canvas" อัตโนมัติ
    private Vector2 GetHiddenPosition(UIDirection dir)
    {
        Vector2 pos = defaultAnchoredPos;
        RectTransform canvasRect = GetComponentInParent<Canvas>()?.GetComponent<RectTransform>();
        
        // ถ้าหา Canvas ไม่เจอ ให้ใช้ค่าสำรองเผื่อไว้
        float screenWidth = canvasRect != null ? canvasRect.rect.width : 1920f;
        float screenHeight = canvasRect != null ? canvasRect.rect.height : 1080f;

        switch (dir)
        {
            case UIDirection.TopToBottom:
                // ตอนเปิดจะร่วงจากข้างบนลงมา / ตอนปิดจะพุ่งกลับขึ้นไปข้างบนสุดพ้นจอ
                pos.y = defaultAnchoredPos.y + (screenHeight * 0.5f) + extraOffset;
                break;
            case UIDirection.BottomToTop:
                // ตอนเปิดจะเลื่อนจากข้างล่างขึ้นมา / ตอนปิดจะพุ่งลงข้างล่างสุดพ้นจอ
                pos.y = defaultAnchoredPos.y - (screenHeight * 0.5f) - extraOffset;
                break;
            case UIDirection.LeftToRight:
                pos.x = defaultAnchoredPos.x - (screenWidth * 0.5f) - extraOffset;
                break;
            case UIDirection.RightToLeft:
                pos.x = defaultAnchoredPos.x + (screenWidth * 0.5f) + extraOffset;
                break;
        }
        return pos;
    }

    private void SpawnDustEffect()
    {
        if (dustParticlePrefab == null) return;
        GameObject dust = Instantiate(dustParticlePrefab, transform.position, Quaternion.identity);
        Destroy(dust, 2f);
    }
}