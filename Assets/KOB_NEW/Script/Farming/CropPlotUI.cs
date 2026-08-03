using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CropPlotUI : MonoBehaviour
{
    public CropPlots cropPlot;          
    public GameObject uiContainer;      
    
    [Header("UI แสดงผลเวลารดน้ำ")]
    public Slider waterSlider;          
    public TextMeshProUGUI waterText;   

    [Header("UI แสดงผลเวลาใส่ปุ๋ย")]
    public Slider fertilizerSlider;     
    public TextMeshProUGUI fertilizerText; 

    void Update()
    {
        if (cropPlot == null) return;

        if (cropPlot.currentStage == CropStage.Empty)
        {
            if (uiContainer != null) uiContainer.SetActive(false);
            return;
        }

        if (uiContainer != null) uiContainer.SetActive(true);

        if (cropPlot.currentStage == CropStage.Growing)
        {
            if (waterText != null)
            {
                if (cropPlot.needsWaterNow)
                    waterText.text = "💧 Needs Water!";
                else
                    waterText.text = $"Water: {Mathf.Ceil(cropPlot.waterCooldownTimer)}s";
            }

            if (fertilizerText != null)
            {
                if (cropPlot.needsFertilizerNow)
                    fertilizerText.text = "🧪 Needs Fert!";
                else
                    fertilizerText.text = $"Fert: {Mathf.Ceil(cropPlot.fertilizerCooldownTimer)}s";
            }
        }
        else if (cropPlot.currentStage == CropStage.ReadyToHarvest)
        {
            if (waterText != null) waterText.text = "✨ Ready to Harvest!";
            if (fertilizerText != null) fertilizerText.text = "✨ Ready!";
        }
    }
}