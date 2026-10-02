using System;
using UnityEngine;

public class NoiseEmitter : MonoBehaviour
{
    [Min(0f)] [SerializeField] private float noiseRadius = 10f;
    [SerializeField] private bool drawDebug = false;

    public static event Action<Vector3, float> OnNoiseEmitted;

    public void EmitNoise(Vector3 position)
    {
        EmitNoise(position, noiseRadius);
    }

    public void EmitNoise(Vector3 position, float radius)
    {
        if (radius > 0f)
        {
            OnNoiseEmitted?.Invoke(position, radius);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDebug) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(0f, noiseRadius));
    }
}
