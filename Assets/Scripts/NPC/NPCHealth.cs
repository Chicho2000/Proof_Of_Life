using System;
using UnityEngine;

public class NPCHealth : MonoBehaviour
{
    [Header("Configuración de Salud")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;

    [Header("Orientacion")]
    [SerializeField] private Transform facingReference;

    [Header("Audio")]
    [SerializeField] private AudioClip hurtSound;
    [SerializeField] private AudioClip deathSound;

    private bool isDead = false;

    // Eventos
    public event Action<int, int> OnHealthChanged;
    public event Action<int> OnDamaged;
    public event Action OnDeath;

    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public bool IsDead => isDead;
    public Vector3 Forward => facingReference != null ? facingReference.forward : transform.forward;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    private void Start()
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// Aplica daño al NPC.
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (isDead || damage <= 0) return;

        currentHealth = Mathf.Max(0, currentHealth - damage);
        Debug.Log($"[NPCHealth] {gameObject.name} recibió {damage} de daño. Salud restante: {currentHealth}/{maxHealth}");

        if (hurtSound != null)
        {
            AudioSource.PlayClipAtPoint(hurtSound, transform.position);
        }

        OnDamaged?.Invoke(damage);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Eliminación silenciosa instantánea (usada por Cable de Fibra).
    /// </summary>
    public void ExecuteSilentTakedown()
    {
        if (isDead) return;

        Debug.Log($"[NPCHealth] {gameObject.name} fue eliminado silenciosamente.");
        currentHealth = 0;
        Die();
    }

    /// <summary>
    /// Gestiona la muerte del NPC y lo convierte en cuerpo interactuable.
    /// </summary>
    private void Die()
    {
        if (isDead) return;

        isDead = true;
        Debug.Log($"[NPCHealth] {gameObject.name} ha muerto.");

        if (deathSound != null)
        {
            AudioSource.PlayClipAtPoint(deathSound, transform.position);
        }

        // Desactivar IA / Scripts de movimiento si existen
        NPCBase npcBase = GetComponent<NPCBase>();
        if (npcBase != null)
        {
            npcBase.enabled = false;
        }

        UnityEngine.AI.NavMeshAgent navAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (navAgent != null)
        {
            navAgent.enabled = false;
        }

        // Animator: verificar si tiene controller y el parámetro "IsDead"
        Animator anim = GetComponentInChildren<Animator>();
        bool hasDeathAnim = false;

        if (anim != null && anim.runtimeAnimatorController != null && HasParameter(anim, "IsDead"))
        {
            anim.SetBool("IsDead", true);
            hasDeathAnim = true;
        }

        // Si no hay animación de muerte configurada, hacer colapso físico al suelo
        if (!hasDeathAnim)
        {
            // Desactivar el Animator para que no obligue al modelo a quedarse parado en pose idle/T-pose
            if (anim != null)
            {
                anim.enabled = false;
            }

            // Voltear el modelo hacia el suelo para que se vea claramente abatido
            transform.Rotate(Vector3.right, -90f, Space.Self);
            transform.position = new Vector3(transform.position.x, 0.2f, transform.position.z);

            CapsuleCollider col = GetComponent<CapsuleCollider>();
            if (col != null)
            {
                col.direction = 2; // Eje Z (alineado horizontalmente con el cuerpo acostado)
                col.center = new Vector3(0f, 0.2f, 0f);
            }
        }

        // Convertir al NPC en cuerpo interactuable para esconder o cambiar ropa
        SetupBodyInteractable();
        SetupDisguiseInteractable();

        OnDeath?.Invoke();
    }

    private bool HasParameter(Animator animator, string paramName)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return false;
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName)
                return true;
        }
        return false;
    }

    private void SetupBodyInteractable()
    {
        BodyInteractable body = GetComponent<BodyInteractable>();
        if (body == null)
        {
            body = gameObject.AddComponent<BodyInteractable>();
        }

        body.canInteract = true;
        body.interactionPrompt = $"Arrastrar cuerpo ({gameObject.name})";
    }

    private void SetupDisguiseInteractable()
    {
        // Solo los guardias sueltan disfraces por ahora
        GuardNPC guard = GetComponent<GuardNPC>();
        if (guard == null) return;

        // Añadimos el script directamente al cuerpo
        if (GetComponent<GuardDisguise>() == null)
        {
            gameObject.AddComponent<GuardDisguise>();
        }

        // Actualizar el prompt del BodyInteractable
        BodyInteractable body = GetComponent<BodyInteractable>();
        if (body != null)
        {
            body.RefreshPrompt();
        }
    }
}
