using UnityEngine;

public class ItemDropPickUp : MonoBehaviour
{
    public SO_ItemData itemData;
    public int amount;

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            ResourceInventory.Instance.AddResource(itemData,amount);
            Destroy(gameObject);
        }
    }
}