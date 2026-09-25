using UnityEngine;

public class HideSpot : Interactable
{
    [Header("Configuración de HideSpot")]
    [Tooltip("Nombre descriptivo para la UI (ej: Placard, Tacho de Basura)")]
    [SerializeField] private string spotName = "Placard";

    [Tooltip("Punto interno opcional donde se aloja el cuerpo escondido")]
    [SerializeField] private Transform hidePoint;

    [Tooltip("Radio para detectar cuerpos caídos en el piso cerca de este HideSpot")]
    [SerializeField] private float detectionRadius = 3.5f;

    [Header("Audio")]
    [SerializeField] private AudioClip hideSound;

    [Header("Estado")]
    [SerializeField] private bool isOccupied = false;
    [SerializeField] private GameObject hiddenBody;

    public bool IsOccupied => isOccupied;
    public string SpotName => spotName;
    public Transform HidePoint => hidePoint;

    private void Awake()
    {
        EnsureColliderExists();
        UpdatePrompt();
    }

    private void Start()
    {
        EnsureColliderExists();
        UpdatePrompt();
    }

    private void EnsureColliderExists()
    {
        // Si no tiene collider propio ni en hijos, agregarle uno para que el Raycast del jugador pueda interactuar
        if (GetComponentInChildren<Collider>() == null)
        {
            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            string lowerName = (spotName + " " + gameObject.name).ToLower();
            if (lowerName.Contains("tacho") || lowerName.Contains("trash"))
            {
                box.size = new Vector3(0.9f, 1.2f, 0.9f);
                box.center = new Vector3(0f, 0.6f, 0f);
            }
            else
            {
                box.size = new Vector3(1.4f, 2.3f, 1.0f);
                box.center = new Vector3(0f, 1.15f, 0f);
            }
        }
    }

    public override void OnFocus(GameObject interactor)
    {
        UpdatePrompt();
    }

    private void Update()
    {
        UpdatePrompt();
    }

    private void UpdatePrompt()
    {
        if (isOccupied)
        {
            interactionPrompt = $"{spotName} (Ocupado)";
            canInteract = false;
            return;
        }

        // Si el jugador está arrastrando un cuerpo en este momento
        if (BodyInteractable.CurrentCarriedBody != null)
        {
            interactionPrompt = $"Esconder cuerpo en {spotName}";
            canInteract = true;
            return;
        }

        // Si hay un cuerpo abatido cerca en el suelo
        BodyInteractable nearbyBody = FindNearbyBody();
        if (nearbyBody != null)
        {
            interactionPrompt = $"Esconder cuerpo cercano en {spotName}";
            canInteract = true;
            return;
        }

        interactionPrompt = $"{spotName} (Vacío)";
        canInteract = true;
    }

    public override void Interact(GameObject interactor)
    {
        if (isOccupied)
        {
            Debug.Log($"[HideSpot] {spotName} ya está ocupado con un cuerpo.");
            return;
        }

        // 1. Prioridad: el cuerpo que el jugador está arrastrando
        BodyInteractable bodyToHide = BodyInteractable.CurrentCarriedBody;

        // 2. Si no arrastra ninguno, buscar si hay un cuerpo en el suelo cerca
        if (bodyToHide == null)
        {
            bodyToHide = FindNearbyBody();
        }

        if (bodyToHide != null)
        {
            HideBody(bodyToHide);
        }
        else
        {
            Debug.Log($"[HideSpot] No hay ningún cuerpo para esconder en {spotName}. Arrastra un cuerpo hasta aquí con [E].");
        }
    }

    public bool HideBody(BodyInteractable body)
    {
        if (isOccupied || body == null)
        {
            return false;
        }

        isOccupied = true;
        hiddenBody = body.gameObject;

        body.OnHiddenInSpot(this);

        if (hideSound != null)
        {
            AudioSource.PlayClipAtPoint(hideSound, transform.position);
        }

        UpdatePrompt();
        Debug.Log($"🔒 [HideSpot] ¡Cuerpo de {hiddenBody.name} escondido exitosamente en {spotName}!");
        return true;
    }

    private BodyInteractable FindNearbyBody()
    {
        BodyInteractable[] allBodies = Object.FindObjectsByType<BodyInteractable>(FindObjectsSortMode.None);
        BodyInteractable closest = null;
        float minDist = detectionRadius;

        foreach (BodyInteractable body in allBodies)
        {
            if (body == null || body.IsHidden || !body.gameObject.activeInHierarchy) continue;

            float dist = Vector3.Distance(transform.position, body.transform.position);
            if (dist <= minDist)
            {
                minDist = dist;
                closest = body;
            }
        }

        return closest;
    }

    /// <summary>
    /// Auto-configuración en tiempo de ejecución para los Placards y Tachos de basura existentes en la escena.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoSetupSceneHideSpots()
    {
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject obj in allObjects)
        {
            string lower = obj.name.ToLower();
            if (obj.GetComponent<HideSpot>() != null) continue;

            if (lower.Contains("closet") || lower.Contains("placard"))
            {
                HideSpot spot = obj.AddComponent<HideSpot>();
                spot.spotName = "Placard";
                Debug.Log($"[HideSpot] Configurado automáticamente {spot.spotName} en {obj.name}");
            }
            else if (lower.Contains("trashcan") || lower.Contains("tacho") || lower.Contains("trash"))
            {
                HideSpot spot = obj.AddComponent<HideSpot>();
                spot.spotName = "Tacho de Basura";
                Debug.Log($"[HideSpot] Configurado automáticamente {spot.spotName} en {obj.name}");
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
