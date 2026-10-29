using UnityEngine;

public class BrokenProp : MonoBehaviour
{
    [Header("Configuración de Fragmentos")]
    [SerializeField, Min(0f)] private float explosionForce = 1.5f;
    [SerializeField, Min(0.1f)] private float explosionRadius = 0.4f;
    [SerializeField, Min(0.5f)] private float lifetime = 3f;

    private void Start()
    {
        Vector3 explosionCenter = transform.position + Vector3.down * 0.05f;
        Rigidbody[] fragments = GetComponentsInChildren<Rigidbody>();

        for (int i = 0; i < fragments.Length; i++)
        {
            Rigidbody rb = fragments[i];
            if (rb == null) continue;

            rb.AddExplosionForce(explosionForce, explosionCenter, explosionRadius, 0.1f, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * 0.5f, ForceMode.Impulse);
        }

        Destroy(gameObject, lifetime);
    }
}
