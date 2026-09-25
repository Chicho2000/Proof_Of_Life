using UnityEngine;

public class BodyInteractable : Interactable
{
    public static BodyInteractable CurrentCarriedBody { get; private set; }

    [Header("Configuración de Arrastre")]
    [SerializeField] private float carryDistance = 1.5f;
    [SerializeField] private float carryHeightOffset = -0.4f;
    [SerializeField] private float followSpeed = 14f;

    [Header("Audio")]
    [SerializeField] private AudioClip grabSound;
    [SerializeField] private AudioClip dropSound;

    [Header("Control de Soltado")]
    [SerializeField] private float dropCooldown = 0.35f;
    private float grabTime = -999f;

    private bool isBeingCarried = false;
    private bool isHidden = false;
    private Transform carrier;
    private Camera playerCamera;
    private Collider[] bodyColliders;
    private string npcName;

    public bool IsBeingCarried => isBeingCarried;
    public bool IsHidden => isHidden;
    public string NPCName => npcName;

    private void Awake()
    {
        npcName = gameObject.name;
        bodyColliders = GetComponentsInChildren<Collider>();
        canInteract = true;
        UpdatePrompt();
    }

    private void Start()
    {
        UpdatePrompt();
    }

    private void UpdatePrompt()
    {
        if (isHidden)
        {
            canInteract = false;
            interactionPrompt = string.Empty;
            return;
        }

        canInteract = true;
        if (isBeingCarried)
        {
            interactionPrompt = "Soltar cuerpo";
        }
        else
        {
            interactionPrompt = $"Arrastrar cuerpo ({npcName})";
        }
    }

    public override void Interact(GameObject interactor)
    {
        if (isHidden) return;

        if (isBeingCarried)
        {
            if (Time.time - grabTime < dropCooldown) return;
            DropBody();
        }
        else
        {
            // Si ya cargaba otro cuerpo, soltar el anterior
            if (CurrentCarriedBody != null && CurrentCarriedBody != this)
            {
                CurrentCarriedBody.DropBody();
            }

            CarryBody(interactor);
        }
    }

    public void CarryBody(GameObject interactor)
    {
        if (isHidden || interactor == null) return;

        grabTime = Time.time;
        isBeingCarried = true;
        CurrentCarriedBody = this;
        carrier = interactor.transform;
        playerCamera = interactor.GetComponentInChildren<Camera>();
        if (playerCamera == null) playerCamera = Camera.main;

        // Evitar que el cuerpo bloquee físicamente el paso del jugador
        SetCollidersTrigger(true);

        if (grabSound != null)
        {
            AudioSource.PlayClipAtPoint(grabSound, transform.position);
        }

        UpdatePrompt();
        Debug.Log($"✋ [BodyInteractable] Arrastrando cuerpo de {npcName}. Mirá un Placard o Tacho para esconderlo, o presiona E/G para soltar.");
    }

    public void DropBody()
    {
        if (!isBeingCarried) return;
        if (Time.time - grabTime < dropCooldown) return;

        isBeingCarried = false;
        if (CurrentCarriedBody == this)
        {
            CurrentCarriedBody = null;
        }

        carrier = null;
        playerCamera = null;

        // Apoyar en el piso mediante raycast hacia abajo
        Vector3 rayStart = transform.position + Vector3.up * 0.5f;
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 3.0f, ~0, QueryTriggerInteraction.Ignore))
        {
            transform.position = hit.point + Vector3.up * 0.12f;
        }

        // Restaurar colisiones sólidas
        SetCollidersTrigger(false);

        if (dropSound != null)
        {
            AudioSource.PlayClipAtPoint(dropSound, transform.position);
        }

        UpdatePrompt();
        Debug.Log($"📦 [BodyInteractable] Cuerpo de {npcName} soltado en el suelo.");
    }

    public void OnHiddenInSpot(HideSpot spot)
    {
        if (isBeingCarried)
        {
            isBeingCarried = false;
            if (CurrentCarriedBody == this)
            {
                CurrentCarriedBody = null;
            }
        }

        isHidden = true;
        canInteract = false;

        // Alojar en el punto interno del escondite si está asignado
        if (spot != null && spot.HidePoint != null)
        {
            transform.position = spot.HidePoint.position;
            transform.rotation = spot.HidePoint.rotation;
            transform.SetParent(spot.HidePoint);
        }
        else if (spot != null)
        {
            transform.position = spot.transform.position;
            transform.SetParent(spot.transform);
        }

        // Desactivar el GameObject para que quede completamente oculto
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!isBeingCarried || isHidden) return;

        if (carrier == null)
        {
            DropBody();
            return;
        }

        Transform targetTransform = playerCamera != null ? playerCamera.transform : carrier;
        Vector3 targetPos = targetTransform.position + targetTransform.forward * carryDistance + Vector3.up * carryHeightOffset;

        // Posicionar suavemente frente al jugador
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSpeed);

        // Orientación acostada acompañando la rotación del jugador
        Quaternion targetRot = Quaternion.Euler(0f, targetTransform.eulerAngles.y, -90f);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, Time.deltaTime * followSpeed);

        // No procesar soltar en el mismo instante en que se levantó
        if (Time.time - grabTime < dropCooldown) return;

        // Tecla alternativa para soltar (G) o tecla E si no está apuntando a otro interactable
        if (Input.GetKeyDown(KeyCode.G))
        {
            DropBody();
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            PlayerInteraction playerInteraction = carrier.GetComponent<PlayerInteraction>();
            if (playerInteraction != null && (playerInteraction.currentInteractable == null || playerInteraction.currentInteractable == this))
            {
                DropBody();
            }
        }
    }

    private void SetCollidersTrigger(bool isTrigger)
    {
        if (bodyColliders == null || bodyColliders.Length == 0)
        {
            bodyColliders = GetComponentsInChildren<Collider>();
        }

        foreach (Collider col in bodyColliders)
        {
            if (col != null)
            {
                col.isTrigger = isTrigger;
            }
        }
    }
}
