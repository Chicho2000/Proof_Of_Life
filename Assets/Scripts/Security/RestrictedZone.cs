using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class RestrictedZone : MonoBehaviour
{
    private readonly Dictionary<PlayerAccessState, int> occupants = new Dictionary<PlayerAccessState, int>();

    private void Reset()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerAccessState access = other.GetComponentInParent<PlayerAccessState>();
        if (access == null)
        {
            return;
        }

        occupants.TryGetValue(access, out int count);
        occupants[access] = count + 1;
        access.Enter(this);
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerAccessState access = other.GetComponentInParent<PlayerAccessState>();
        if (access == null || !occupants.TryGetValue(access, out int count))
        {
            return;
        }

        if (count > 1)
        {
            occupants[access] = count - 1;
        }
        else
        {
            occupants.Remove(access);
            access.Leave(this);
        }
    }

    private void OnDisable()
    {
        foreach (PlayerAccessState access in occupants.Keys)
        {
            if (access != null)
            {
                access.Leave(this);
            }
        }
        occupants.Clear();
    }
}
