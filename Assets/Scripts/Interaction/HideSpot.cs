using System.Collections.Generic;
using UnityEngine;

public class HideSpot : Interactable
{
    private const int MaxCapacity = 2;

    [Header("Configuración de HideSpot")]
    [Tooltip("Nombre descriptivo para la UI (ej: Placard, Tacho de Basura)")]
    [SerializeField] private string spotName = "Placard";

    [Tooltip("Punto interno donde se aloja el cuerpo o el jugador escondido")]
    [SerializeField] private Transform hidePoint;

    [Tooltip("Punto exterior por donde reaparece el jugador al salir del escondite")]
    [SerializeField] private Transform exitPoint;

    [Tooltip("Punto de cámara / mirilla para espiar desde adentro (por defecto usa punto interior)")]
    [SerializeField] private Transform cameraPoint;

    [Tooltip("Radio para detectar cuerpos caídos en el piso cerca de este HideSpot")]
    [SerializeField] private float detectionRadius = 3.5f;

    [Header("Audio")]
    [SerializeField] private AudioClip hideSound;

    [Header("Estado")]
    [SerializeField] private List<BodyInteractable> storedBodies = new List<BodyInteractable>();
    [SerializeField] private bool isPlayerInside = false;

    private Transform internalCameraPoint;

    public bool IsPlayerInside => isPlayerInside;
    public int StoredBodyCount => storedBodies?.Count ?? 0;
    public int TotalOccupants => StoredBodyCount + (isPlayerInside ? 1 : 0);
    public bool IsOccupied => TotalOccupants > 0;
    public bool IsFull => TotalOccupants >= MaxCapacity;
    public bool CanPlayerHide => !isPlayerInside && TotalOccupants < MaxCapacity;
    public bool CanBodyHide => TotalOccupants < MaxCapacity;
    public string SpotName => spotName;
    public AudioClip HideSound => hideSound;

    public bool IsCloset => (GetComponent<BoxCollider>()?.size.y ?? 0f) >= 1.9f;

    /// <summary>
    /// Devuelve el offset local en X del lado de la puerta abierta (Puerta izquierda / lado negativo)
    /// </summary>
    public float OpenDoorSideLocalX
    {
        get
        {
            Transform openDoor = transform.Find("Puerta izquierda");
            if (openDoor == null)
            {
                foreach (Transform child in transform)
                {
                    string lower = child.name.ToLower();
                    if (lower.Contains("izq") || lower.Contains("left") || lower.Contains("open"))
                    {
                        openDoor = child;
                        break;
                    }
                }
            }

            if (openDoor != null)
            {
                Renderer r = openDoor.GetComponent<Renderer>();
                if (r != null)
                {
                    Vector3 localCenter = transform.InverseTransformPoint(r.bounds.center);
                    if (Mathf.Abs(localCenter.x) > 0.05f)
                    {
                        return Mathf.Sign(localCenter.x) * Mathf.Min(Mathf.Abs(localCenter.x), 0.35f);
                    }
                }
                if (Mathf.Abs(openDoor.localPosition.x) > 0.05f)
                {
                    return Mathf.Sign(openDoor.localPosition.x) * Mathf.Min(Mathf.Abs(openDoor.localPosition.x), 0.35f);
                }
            }

            BoxCollider box = GetComponent<BoxCollider>();
            return box != null ? -box.size.x * 0.25f : -0.35f;
        }
    }

    /// <summary>
    /// Devuelve el offset local en X del lado de la puerta CERRADA (lado opuesto a la puerta abierta)
    /// </summary>
    public float ClosedDoorSideLocalX => -OpenDoorSideLocalX;

    public Transform HidePoint
    {
        get
        {
            if (hidePoint == null)
            {
                Transform directPoint = transform.Find("HidePoint");
                if (directPoint != null && !IsCloset)
                {
                    hidePoint = directPoint;
                }
                else
                {
                    GameObject autoPoint = new GameObject("HidePoint");
                    autoPoint.transform.SetParent(transform);

                    BoxCollider box = GetComponent<BoxCollider>();
                    if (IsCloset)
                    {
                        // En placard: el jugador se ubica del lado de la puerta abierta mirando hacia la habitación (-Z)
                        autoPoint.transform.localPosition = new Vector3(OpenDoorSideLocalX, 0.9f, 0.05f);
                        autoPoint.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                    }
                    else
                    {
                        // En contenedor: ubicado adentro mirando hacia la apertura frontal (+Z)
                        autoPoint.transform.localPosition = new Vector3(0f, 0.7f, 0.25f);
                        autoPoint.transform.localRotation = Quaternion.identity;
                    }

                    hidePoint = autoPoint.transform;
                }
            }
            return hidePoint;
        }
    }

    public bool HasCustomExitPoint => exitPoint != null;
    public Transform CustomExitPoint => exitPoint;

    public Transform CameraPoint
    {
        get
        {
            if (cameraPoint != null) return cameraPoint;

            Transform directCam = transform.Find("CameraPoint");
            if (directCam == null) directCam = transform.Find("Mirilla");
            if (directCam != null) return directCam;

            if (internalCameraPoint == null)
            {
                GameObject camObj = new GameObject("HidingViewCameraPoint");
                camObj.transform.SetParent(transform);

                if (IsCloset)
                {
                    // Placard: ubicado del lado de la puerta abierta, dentro del mueble
                    // mirando hacia afuera a la habitación (-Z), enmarcado por las puertas ("medio escondido")
                    camObj.transform.localPosition = new Vector3(
                        OpenDoorSideLocalX,
                        1.55f,
                        0.05f
                    );
                    camObj.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                }
                else
                {
                    // Contenedor: ubicado adentro mirando hacia la abertura frontal (+Z)
                    // a la altura del gap de la tapa semi-abierta (Y = 1.42m, Z = +0.25m),
                    // con vista nítida al exterior ("medio escondido")
                    camObj.transform.localPosition = new Vector3(0f, 1.42f, 0.25f);
                    camObj.transform.localRotation = Quaternion.identity;
                }

                internalCameraPoint = camObj.transform;
            }

            return internalCameraPoint;
        }
    }

    public Vector3 ExitPointPosition
    {
        get
        {
            if (exitPoint != null) return exitPoint.position;
            return transform.position;
        }
    }

    private void Awake()
    {
        CleanupStoredBodies();
        EnsureColliderExists();
        UpdatePrompt();
    }

    private void EnsureColliderExists()
    {
        if (GetComponentInChildren<Collider>() == null)
        {
            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            box.size = new Vector3(1.2f, 2.0f, 1.0f);
            box.center = new Vector3(0f, 1.0f, 0f);
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
        CleanupStoredBodies();
        int total = TotalOccupants;

        if (total >= MaxCapacity)
        {
            interactionPrompt = $"{spotName} (Lleno {total}/{MaxCapacity})";
            canInteract = false;
            return;
        }

        bool carryingBody = BodyInteractable.CurrentCarriedBody != null;

        if (carryingBody)
        {
            canInteract = true;
            interactionPrompt = $"Esconder cuerpo en {spotName} ({total}/{MaxCapacity})";
            return;
        }

        // Jugador sin cuerpo en mano
        canInteract = true;
        interactionPrompt = $"Esconderse en {spotName} ({total}/{MaxCapacity})";

        // Si además hay un cuerpo en el suelo cercano, ofrecer opción secundaria
        BodyInteractable nearby = FindNearbyBody();
        if (nearby != null && CanBodyHide)
        {
            interactionPrompt += $"\n[F] Esconder cadáver ({nearby.NPCName})";
        }
    }

    public override void Interact(GameObject interactor)
    {
        CleanupStoredBodies();

        if (IsFull)
        {
            Debug.Log($"[HideSpot] {spotName} está lleno ({TotalOccupants}/{MaxCapacity}).");
            return;
        }

        // 1. Si el jugador está arrastrando un cuerpo, meter ese cuerpo
        BodyInteractable carriedBody = BodyInteractable.CurrentCarriedBody;
        if (carriedBody != null)
        {
            HideBody(carriedBody);
            return;
        }

        // 2. Si no arrastra cuerpo, el jugador se esconde
        if (CanPlayerHide)
        {
            PlayerHidingSystem hidingSystem = interactor.GetComponent<PlayerHidingSystem>();
            if (hidingSystem == null)
            {
                hidingSystem = interactor.GetComponentInParent<PlayerHidingSystem>();
            }

            if (hidingSystem != null)
            {
                hidingSystem.EnterHideSpot(this);
            }
        }
    }

    public override void SecondaryInteract(GameObject interactor)
    {
        CleanupStoredBodies();

        if (IsFull || !CanBodyHide)
        {
            return;
        }

        // La interacción secundaria [F] esconde un cuerpo cercano del suelo si lo hay
        BodyInteractable nearby = FindNearbyBody();
        if (nearby != null)
        {
            HideBody(nearby);
        }
    }

    public bool HideBody(BodyInteractable body)
    {
        CleanupStoredBodies();

        if (body == null || TotalOccupants >= MaxCapacity || storedBodies.Contains(body))
        {
            return false;
        }

        storedBodies.Add(body);
        body.OnHiddenInSpot(this);

        if (hideSound != null)
        {
            AudioSource.PlayClipAtPoint(hideSound, transform.position);
        }

        UpdatePrompt();
        Debug.Log($"🔒 [HideSpot] ¡Cuerpo de {body.gameObject.name} escondido en {spotName}! ({TotalOccupants}/{MaxCapacity})");
        return true;
    }

    public void OnPlayerEntered(PlayerHidingSystem player)
    {
        isPlayerInside = true;
        UpdatePrompt();
    }

    public void OnPlayerExited(PlayerHidingSystem player)
    {
        isPlayerInside = false;
        UpdatePrompt();
    }

    private BodyInteractable FindNearbyBody()
    {
        CleanupStoredBodies();

        if (IsFull)
        {
            return null;
        }

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

    private void CleanupStoredBodies()
    {
        if (storedBodies == null)
        {
            storedBodies = new List<BodyInteractable>();
            return;
        }

        storedBodies.RemoveAll(body => body == null);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        if (hidePoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(hidePoint.position, new Vector3(0.5f, 1.8f, 0.5f));
        }

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(CameraPoint.position, 0.15f);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(ExitPointPosition, 0.3f);
    }

    /// <summary>
    /// Detecta únicamente objetos de escondite específicos en la escena que contengan un hijo directo 'HidePoint',
    /// protegiendo contra objetos de nivel o ambiente.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoDetectHidePoints()
    {
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject obj in allObjects)
        {
            if (obj == null) continue;

            // Ignorar explícitamente objetos de entorno/nivel/escenario
            string lowerName = obj.name.ToLower();
            if (lowerName.Contains("enviroment") || lowerName.Contains("environment") ||
                lowerName.Contains("level") || lowerName.Contains("map") ||
                lowerName.Contains("walls") || lowerName.Contains("floors") ||
                lowerName.Contains("scene") || lowerName.Contains("room"))
            {
                continue;
            }

            // Buscar ÚNICAMENTE como hijo DIRECTO (no en toda la jerarquía de descendientes)
            Transform directPoint = obj.transform.Find("HidePoint");
            if (directPoint == null)
            {
                continue;
            }

            HideSpot existingSpot = obj.GetComponent<HideSpot>();
            if (existingSpot != null)
            {
                if (existingSpot.hidePoint == null)
                {
                    existingSpot.hidePoint = directPoint;
                }
                continue;
            }

            // Crear el componente HideSpot en el contenedor específico
            HideSpot spot = obj.AddComponent<HideSpot>();
            spot.hidePoint = directPoint;
            spot.spotName = (lowerName.Contains("trash") || lowerName.Contains("tacho"))
                ? "Tacho de Basura"
                : "Escondite";

            Debug.Log($"[HideSpot] Escondite configurado exitosamente en '{obj.name}' con su HidePoint directo.");
        }
    }
}
