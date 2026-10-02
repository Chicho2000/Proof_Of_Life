using UnityEngine;

public class RotarCartel : MonoBehaviour
{
    public float velocidad = 45f;

    void Update()
    {
        transform.Rotate(Vector3.up * velocidad * Time.deltaTime, Space.World);
    }
}