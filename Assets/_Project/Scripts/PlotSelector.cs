using UnityEngine;

public class PlotSelector : MonoBehaviour
{
    [Header("Raycast")]
    public Camera mainCamera;

    private CropPlot selectedPlot;

    private void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TrySelectPlot();
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            PlantPepperOnSelectedPlot();
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            WaterSelectedPlot();
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            HarvestPepperOnSelectedPlot();
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            ApplyFertilizerToSelectedPlot();
        }
    }

    private void TrySelectPlot()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if(Physics.Raycast(ray, out RaycastHit hit))
        {
            CropPlot plot = hit.collider.GetComponent<CropPlot>();

            if (plot != null)
            {
                SelectPlot(plot);
            }
        }
    }

    private void SelectPlot(CropPlot plot)
    {
        if (selectedPlot != null)
        {
            selectedPlot.Deselect();
        }

        selectedPlot = plot;
        selectedPlot.Select();
    }

    private void PlantPepperOnSelectedPlot()
    {
        if (selectedPlot == null)
        {
            Debug.Log("Select a plot first.");
            ShowHUDPopup("Select a plot first", "Click a crop plot before planting.");
            return;
        }

        selectedPlot.PlantPepper();
    }

    private void WaterSelectedPlot()
    {
        if(selectedPlot == null)
        {
            Debug.Log("Slect a plot first.");
            ShowHUDPopup("Select a plot first", "Click a crop plot before watering.");
            return;
        }

        selectedPlot.WaterPlot();
    }

    private void HarvestPepperOnSelectedPlot()
    {
        if (selectedPlot == null)
        {
            Debug.Log("Select a plot first");
            ShowHUDPopup("Select a plot first", "Click a crop plot before harvesting.");
            return;
        }

        selectedPlot.HarvestPepper();
    }

    private void ApplyFertilizerToSelectedPlot()
    {
        if (selectedPlot == null)
        {
            Debug.Log("Select a plot first.");
            ShowHUDPopup("Select a plot first", "Click a crop plot before applying fertilizer.");
            return;
        }

        if (FertilizerManager.Instance == null)
        {
            Debug.LogWarning("FertilizerManager not found.");
            return;
        }

        selectedPlot.ApplyFertilizer(FertilizerManager.Instance.CurrentFertilizer);
    }

    private void ShowHUDPopup(string title, string detail)
    {
        if (HUDController.Instance != null)
        {
            HUDController.Instance.ShowPopup(title, detail);
        }
    }
}
