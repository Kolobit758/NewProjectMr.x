using TMPro;
using UnityEngine;

public enum DogStatus
{
    Idle,
    Exploring
}

public class PetManager : MonoBehaviour
{
    public static PetManager Instance { get; private set; }

    [Header("Dog")]
    public string dogName = "Dog";
    public DogStatus dogStatus = DogStatus.Idle;

    [Header("Expedition")]
    public float explorationDuration = 30f;

    private float remainingExplorationTime = 0f;

    [Header("Energy")]
    public int dogEnergy = 3;
    public int dogMaxEnergy = 3;
    public int explorationEnergyCost = 1;

    [Header("Scene References")]
    public GameObject dogObject;

    [Header("UI")]
    public TextMeshProUGUI dogStatusText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        dogEnergy = Mathf.Clamp(dogEnergy, 0, dogMaxEnergy);
        RefreshUI();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            SendDogExploring();
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            TryFeedDogPepper();
        }

        UpdateExplorationTimer();
    }

    public void SendDogExploring()
    {
        if (dogStatus == DogStatus.Exploring)
        {
            Debug.Log(dogName + " is already exploring.");
            return;
        }

        if (dogEnergy < explorationEnergyCost)
        {
            Debug.Log(dogName + " is too tired to explore. Feed Pepper with O.");
            RefreshUI();
            return;
        }

        dogEnergy -= explorationEnergyCost;
        dogStatus = DogStatus.Exploring;
        remainingExplorationTime = explorationDuration;
        Debug.Log(dogName + " sent into the fog.");
        RefreshUI();
    }

    public void TryFeedDogPepper()
    {
        if (dogStatus == DogStatus.Exploring)
        {
            Debug.Log(dogName + " is exploring and cannot eat right now.");
            return;
        }

        if (dogEnergy >= dogMaxEnergy)
        {
            Debug.Log(dogName + " already has full energy.");
            RefreshUI();
            return;
        }

        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("InventoryManager not found. Cannot feed " + dogName + ".");
            return;
        }

        bool success = InventoryManager.Instance.SpendPepper(1);
        if (!success)
        {
            Debug.Log("Need Pepper x1 to feed " + dogName + ".");
            return;
        }

        dogEnergy = Mathf.Min(dogEnergy + 1, dogMaxEnergy);
        InventoryManager.Instance.ShowDiscoveryMessage(dogName + " ate Pepper. Energy restored!");
        Debug.Log(dogName + " energy: " + dogEnergy + "/" + dogMaxEnergy);
        RefreshUI();
    }

    public void IncreaseDogMaxEnergy(int amount)
    {
        dogMaxEnergy += amount;
        dogEnergy = Mathf.Clamp(dogEnergy, 0, dogMaxEnergy);
        RefreshUI();
    }

    private void UpdateExplorationTimer()
    {
        if (dogStatus != DogStatus.Exploring)
        {
            return;
        }

        remainingExplorationTime -= Time.deltaTime;

        if (remainingExplorationTime <= 0f)
        {
            CompleteExploration();
        }

        RefreshUI();
    }

    private void CompleteExploration()
    {
        remainingExplorationTime = 0f;
        dogStatus = DogStatus.Idle;

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddFogDust(1);
            InventoryManager.Instance.ShowDiscoveryMessage(dogName + " returned with Fog Dust x1!");
        }
        else
        {
            Debug.LogWarning("InventoryManager not found. Fog Dust reward was not added.");
        }

        Debug.Log(dogName + " returned from the fog with Fog Dust x1.");
    }

    private void RefreshUI()
    {
        if (dogStatusText == null)
        {
            return;
        }

        if (dogStatus == DogStatus.Idle)
        {
            if (dogEnergy <= 0)
            {
                dogStatusText.text = dogName + ": Idle | Energy " + dogEnergy + "/" + dogMaxEnergy + " | Press O to feed Pepper";
            }
            else
            {
                dogStatusText.text = dogName + ": Idle | Energy " + dogEnergy + "/" + dogMaxEnergy + " | Press P to explore";
            }
        }
        else
        {
            int secondsLeft = Mathf.CeilToInt(remainingExplorationTime);
            dogStatusText.text = dogName + ": Exploring | " + secondsLeft + "s | Energy " + dogEnergy + "/" + dogMaxEnergy;
        }
    }
}
