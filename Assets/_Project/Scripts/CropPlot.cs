using Unity.VisualScripting;
using UnityEngine;

public class CropPlot : MonoBehaviour
{
    [Header("Viusal")]
    public Material normalMaterials;
    public Material selectedMaterial;
    public Material wateredMaterial;

    [Header("Crop")]
    public GameObject pepperPrefab;
    public Transform cropSpawnPoint;

    private Renderer plotRenderer;
    private GameObject currentCrop;

    private bool isSelected =false;
    private bool isWatered = false;

    public bool IsEmpty => currentCrop == null;
    public bool IsWatered => isWatered;

    private FertilizerType currentFertilizer = FertilizerType.Basic;
    public FertilizerType CurrentFertilizer => currentFertilizer;

    private void Awake()
    {
        plotRenderer = GetComponent<Renderer>();

        // if (normalMaterials != null)
        // {
        //     plotRenderer.material = normalMaterials;
        // }

        if (cropSpawnPoint == null)
        {
            cropSpawnPoint = transform;
        }

        UpdateVisual();
    }

    public void Select()
    {

        isSelected = true;
        UpdateVisual();
        // if (selectedMaterial != null)
        // {
        //     plotRenderer.material = selectedMaterial;
        // }

        Debug.Log("Selected plot:" + gameObject.name);
    }

    public void Deselect()
    {
        isSelected = false;
        UpdateVisual();
        // if (normalMaterials != null)
        // {
        //     plotRenderer.material = normalMaterials;
        // }
    }

    public void PlantPepper()
    {
        if (!IsEmpty)
        {
            Debug.Log("This plot already has a crop.");
            ShowHUDPopup("Plot is not empty", "Harvest the current crop before planting.");
            return;
        }

        if (pepperPrefab == null)
        {
            Debug.LogWarning("Pepeer prefab is missing on " + gameObject.name);
            return;
        }
        
        Vector3 spawnPosition = transform.position + new Vector3(0f, 0.35f, 0f);
        currentCrop = Instantiate(pepperPrefab, spawnPosition, Quaternion.identity);

        isWatered = false;
        currentFertilizer = FertilizerType.Basic;
        UpdateVisual();

        Debug.Log("Plated Pepper on " + gameObject.name);
    }

    public void WaterPlot()
    {
        if (IsEmpty)
        {
            Debug.Log("Cannot water. this plot is empty");
            ShowHUDPopup("Plot is empty", "Plant Pepper before watering.");
            return;
        }

        if (isWatered)
        {
            Debug.Log("this plot is already wated.");
            ShowHUDPopup("Already watered", "This crop has already been watered.");
            return;
        }

        isWatered = true;
        UpdateVisual();

        Debug.Log("Waterd plot: " + gameObject.name);
    }

    public void HarvestPepper()
    {
        if (IsEmpty)
        {
            Debug.Log("Plot is Empty");
            ShowHUDPopup("Plot is empty", "Plant and grow Pepper before harvesting.");
            return;
        }

        PepperCrop pepperCrop = currentCrop.GetComponent<PepperCrop>();

        if (pepperCrop == null)
        {
            Debug.LogWarning("Current crop is not a PepperCrop");
            return;
        }

        if (!pepperCrop.IsReady)
        {
            Debug.Log("Pepper is not ready yet");
            ShowHUDPopup("Crop is not ready", "Wait for Pepper to finish growing.");
            return;
        }

        int qualityStars = pepperCrop.GetQualityStars(isWatered);
        bool isFlamePepper = CanHaverstFlamePepper();

        Destroy(currentCrop);
        currentCrop = null;

        isWatered = false;
        currentFertilizer = FertilizerType.Basic;
        UpdateVisual();

        if (InventoryManager.Instance != null)
        {
            if (isFlamePepper)
            {
                InventoryManager.Instance.AddFlamePepper(1);
                InventoryManager.Instance.ShowDiscoveryMessage("Special Crop Discovered!");

                Debug.Log("Special Crop Harvested: Flame pepper!");
            }
            else
            {
                InventoryManager.Instance.AddPepper(1);
            }
        }
        else
        {
            Debug.LogWarning("InventoryManager not found");
        }

        if (PlantMasteryManager.Instance != null)
        {
            PlantMasteryManager.Instance.AddPepperMasteryXPByQuality(qualityStars);
        }
        else
        {
            Debug.LogWarning("PlantMasteryManger not found");
        }
        if (isFlamePepper)
        {
            Debug.Log("Harvested Flame Pepper from " + gameObject.name + " | Quality: " + qualityStars + " Stars");
        }
        else
        {
            Debug.Log("Harvested Pepper from " + gameObject.name + " | Quality: " + qualityStars + " Stars");
        }
    }

    private void UpdateVisual()
    {
        if (plotRenderer == null)
            return;

        if (isSelected && selectedMaterial != null)
        {
            plotRenderer.material = selectedMaterial;
            return;
        }

        if (isWatered && wateredMaterial != null)
        {
            plotRenderer.material = wateredMaterial;
            return;
        }

        if (normalMaterials != null)
        {
            plotRenderer.material = normalMaterials;
        }
    }

    public void ApplyFertilizer(FertilizerType fertilizerType)
    {
        if (IsEmpty)
        {
            Debug.Log("Cannot fertilize. this plot is empty.");
            ShowHUDPopup("Plot is empty", "Plant Pepper before applying fertilizer.");
            return;
        }

        currentFertilizer = fertilizerType;
        Debug.Log("Applied " + currentFertilizer + " fertilizer to " + gameObject.name);
    }

    private bool CanHaverstFlamePepper()
    {
        bool hasMastery = PlantMasteryManager.Instance != null && PlantMasteryManager.Instance.GetPepperMasteryLevel() >= 3;
        bool IsHeatWeather = WeatherManager.Instance != null && WeatherManager.Instance.IsHeat();
        bool hasHeatLamp = UpgradeManager.Instance != null && UpgradeManager.Instance.HasHeatlamp;
        bool hasHeatCondition = IsHeatWeather || hasHeatLamp;
        bool hasAshFertilizer = currentFertilizer == FertilizerType.Ash;
        bool hasLowWater = !isWatered;
        return hasMastery && hasHeatCondition && hasAshFertilizer && hasLowWater;
    }

    private void ShowHUDPopup(string title, string detail)
    {
        if (HUDController.Instance != null)
        {
            HUDController.Instance.ShowPopup(title, detail);
        }
    }
}
