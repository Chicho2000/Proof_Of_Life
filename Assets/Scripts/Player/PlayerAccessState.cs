using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerAccessState : MonoBehaviour
{
    private readonly HashSet<RestrictedZone> restrictedZones = new HashSet<RestrictedZone>();

    public bool IsInRestrictedZone => restrictedZones.Count > 0;

    public static bool IsRestricted(Transform target)
    {
        PlayerAccessState access = target != null ? target.GetComponentInParent<PlayerAccessState>() : null;
        return access != null && access.IsInRestrictedZone;
    }

    public void Enter(RestrictedZone zone)
    {
        if (zone != null)
        {
            restrictedZones.Add(zone);
        }
    }

    public void Leave(RestrictedZone zone)
    {
        restrictedZones.Remove(zone);
    }

    private void OnDisable()
    {
        restrictedZones.Clear();
    }
}
