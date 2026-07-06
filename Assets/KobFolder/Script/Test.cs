using UnityEngine;

public class Test : MonoBehaviour
{
    public string text;
    public int number;
    public int handsome;
    public GameObject door;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        text = "dfgdfgdfgdfg";
        number = 5;
    }

    // Update is called once per frameS
    void Update()
    {

    }

    public void Gay()
    {
        Debug.Log("excaliber");
    }
    public void CloseDoor()
    {
        door.SetActive(false);
    }

    public void OpenDoor()
    {
        door.SetActive(true);
    }
}
