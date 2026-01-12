using UnityEngine;

public class Rotate : MonoBehaviour
{
    [SerializeField] private float speed = 50f;

    void Update()
    {
        transform.Rotate(0, speed * Time.deltaTime, 0);
    }
}
