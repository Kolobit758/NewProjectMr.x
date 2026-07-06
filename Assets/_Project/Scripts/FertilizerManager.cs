using UnityEngine;
using TMPro;

public enum FertilizerType
{
    Basic,
    Ash
}
public class FertilizerManager : MonoBehaviour
{
    public static FertilizerManager Instance { get; private set; }
    [Header("Current Fertilizer")]
    public FertilizerType currentFertilizer = FertilizerType.Basic;

    [Header("UI")]
    public TextMeshProUGUI fertilizerText;
    public FertilizerType CurrentFertilizer => currentFertilizer;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance  = this;
    }

    private void Start()
    {
        RefreshUI();
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Alpha4))
        {
            ToggleFertilizer();
        }
    }

    private void ToggleFertilizer()
    {
        currentFertilizer = currentFertilizer == FertilizerType.Basic
            ? FertilizerType.Ash
            : FertilizerType.Basic;

        Debug.Log("Current Fertilizer: " + currentFertilizer);
        RefreshUI();
    }

    private  void RefreshUI()
    {
        if (fertilizerText != null)
        {
            fertilizerText.text = "Fertilizer: " + currentFertilizer;
        }
    }
}
