using Unity.VisualScripting;
using UnityEngine;

public class TeamManageHome : MonoBehaviour
{
    public GameObject TeamManageUI;
    private bool canOpenUI;


    public void OnTriggerStay(Collider other)
    {
        if (other.gameObject.CompareTag("TeamManageHome"))
        {
            TeamManageUI.SetActive(true);
            
        }
    }
    public void OnTriggerExit(Collider other)
    {
        TeamManageUI.SetActive(false);
    }


    
}
