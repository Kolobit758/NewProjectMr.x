using System;
using UnityEngine;

public class PepperCrop : MonoBehaviour
{
    [Header("Growth")]
    public float growthTime = 10f;

    [Header("Scale")]
    public Vector3 smallScale = new Vector3(0.15f, 0.2f, 0.15f);
    public Vector3 fullScale = new Vector3(0.35f, 0.6f, 0.35f);

    [Header("Quality")]
    public int baseQualityStars = 1;
    public int wateredBonusStars = 2;
    public int maxQualityStars = 5;

    private float currentGrowthTime = 0f;
    private bool isReady = false;

    public bool IsReady => isReady;

    private void Start()
    {
        transform.localScale = smallScale;
    }

    private void Update()
    {
        if(isReady)
            return;
        
        currentGrowthTime += Time.deltaTime;

        float progress = currentGrowthTime / growthTime;
        progress = Mathf.Clamp01(progress);

        transform.localScale = Vector3.Lerp(smallScale, fullScale, progress);

        if (progress >= 1f)
        {
            isReady = true;
            Debug.Log("Pepper is ready to harvest");
        }

    }

    public int GetQualityStars(bool wasWatered)
    {
        int qualityStars = baseQualityStars;

        if (wasWatered)
        {
            qualityStars += wateredBonusStars;
        }

        return Mathf.Clamp(qualityStars, 1, maxQualityStars);
    }
}
