using UnityEngine;

public class TentacleVisual3D : MonoBehaviour
{
    public float targetHeight = 5f;
    public float popUpSpeed = 18f;
    public float duration = 0.8f;

    private Vector3 startPos;
    private Vector3 endPos;
    private float timer = 0f;
    private bool movingUp = true;

    // 🟢 ปรับหนวดให้อ้วนผอมตามรัศมีแรงฟาดของบอสเพื่อความสมจริง
    public void SetupTentacleSize(float radius)
    {
        // ปรับความหนาของโคนหนวด 3D ให้สมดุลกับวงรัศมีดาเมจ
        float tentacleThickness = radius * 0.4f; 
        transform.localScale = new Vector3(tentacleThickness, transform.localScale.y, tentacleThickness);
    }

    void Start()
    {
        startPos = transform.position;
        endPos = startPos + (Vector3.up * targetHeight); 
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (movingUp)
        {
            transform.position = Vector3.MoveTowards(transform.position, endPos, popUpSpeed * Time.deltaTime);
            if (timer >= 0.25f) movingUp = false;
        }
        else
        {
            if (timer >= duration)
            {
                transform.position = Vector3.MoveTowards(transform.position, startPos, (popUpSpeed * 0.6f) * Time.deltaTime);
                if (Vector3.Distance(transform.position, startPos) < 0.1f)
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}