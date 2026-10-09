using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Configuración de Salud")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;

    [Header("Invulnerabilidad Temporal")]
    [Tooltip("Tiempo en segundos durante el cual el jugador no recibe daño consecutivo inmediatamente")]
    [SerializeField] private float invulnerabilityDuration = 0.5f;
    private float lastDamageTime = -999f;

    [Header("Audio")]
    [SerializeField] private AudioClip hurtSound;
    [SerializeField] private AudioClip deathSound;

    private bool isDead = false;

    // Eventos desacoplados para UI y sistemas de juego
    public static event Action<int, bool, Vector3> OnPlayerDamagedWithSource; // (damageAmount, hasAttacker, attackerPosition)
    public static event Action<int> OnPlayerDamaged;
    public static event Action<int, int> OnPlayerHealthChangedStatic; // (currentHealth, maxHealth)
    public event Action<int, int> OnHealthChanged; // (currentHealth, maxHealth)
    public event Action<int> OnDamaged;           // (damageAmount)
    public event Action<int, Vector3> OnDamagedWithSource; // (damageAmount, attackerPosition)
    public event Action<int> OnHealed;            // (healAmount)
    public event Action OnDeath;
    public event Action OnPlayerDeath;            // Alias por compatibilidad

    // Propiedades públicas de consulta
    public int MaxHealth
    {
        get { return maxHealth; }
    }

    public int CurrentHealth
    {
        get { return currentHealth; }
    }

    public bool IsDead
    {
        get { return isDead; }
    }

    public float HealthNormalized
    {
        get
        {
            if (maxHealth > 0)
            {
                return (float)currentHealth / maxHealth;
            }
            return 0f;
        }
    }

    private void Awake()
    {
        currentHealth = maxHealth;
        EnsureUIExists();
    }

    private void EnsureUIExists()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas != null)
        {
            if (FindFirstObjectByType<PlayerHealthUI>() == null)
            {
                canvas.gameObject.AddComponent<PlayerHealthUI>();
            }

            if (FindFirstObjectByType<DamageIndicatorUI>() == null)
            {
                canvas.gameObject.AddComponent<DamageIndicatorUI>();
            }
        }
    }

    private void Start()
    {
        if (OnHealthChanged != null)
        {
            OnHealthChanged.Invoke(currentHealth, maxHealth);
        }

        if (OnPlayerHealthChangedStatic != null)
        {
            OnPlayerHealthChangedStatic.Invoke(currentHealth, maxHealth);
        }
    }

    /// <summary>
    /// Aplica daño al jugador sin origen específico de posición.
    /// </summary>
    public void TakeDamage(int damage)
    {
        TakeDamage(damage, false, Vector3.zero);
    }

    /// <summary>
    /// Aplica daño al jugador registrando la posición del atacante para feedback direccional.
    /// </summary>
    public void TakeDamage(int damage, Vector3 attackerPosition)
    {
        TakeDamage(damage, true, attackerPosition);
    }

    /// <summary>
    /// Aplica daño al jugador indicando explícitamente si proviene de una fuente direccional.
    /// </summary>
    public void TakeDamage(int damage, bool hasAttacker, Vector3 attackerPosition)
    {
        if (isDead || damage <= 0) return;

        // Verificar período de invulnerabilidad
        if (Time.time < lastDamageTime + invulnerabilityDuration)
        {
            return;
        }

        lastDamageTime = Time.time;
        currentHealth = Mathf.Max(0, currentHealth - damage);

        Debug.Log($"[PlayerHealth] Jugador recibió {damage} de daño. Salud restante: {currentHealth}/{maxHealth}");

        if (hurtSound != null)
        {
            AudioSource.PlayClipAtPoint(hurtSound, transform.position);
        }

        if (OnDamaged != null)
        {
            OnDamaged.Invoke(damage);
        }

        if (OnDamagedWithSource != null)
        {
            OnDamagedWithSource.Invoke(damage, attackerPosition);
        }

        if (OnPlayerDamaged != null)
        {
            OnPlayerDamaged.Invoke(damage);
        }

        if (OnPlayerDamagedWithSource != null)
        {
            OnPlayerDamagedWithSource.Invoke(damage, hasAttacker, attackerPosition);
        }

        if (OnHealthChanged != null)
        {
            OnHealthChanged.Invoke(currentHealth, maxHealth);
        }

        if (OnPlayerHealthChangedStatic != null)
        {
            OnPlayerHealthChangedStatic.Invoke(currentHealth, maxHealth);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Restaura una cantidad de vida al jugador sin sobrepasar el máximo.
    /// </summary>
    public void Heal(int healAmount)
    {
        if (isDead || healAmount <= 0) return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + healAmount);
        Debug.Log($"[PlayerHealth] Jugador curado +{healAmount}. Salud actual: {currentHealth}/{maxHealth}");

        if (OnHealed != null)
        {
            OnHealed.Invoke(healAmount);
        }

        if (OnHealthChanged != null)
        {
            OnHealthChanged.Invoke(currentHealth, maxHealth);
        }

        if (OnPlayerHealthChangedStatic != null)
        {
            OnPlayerHealthChangedStatic.Invoke(currentHealth, maxHealth);
        }
    }

    /// <summary>
    /// Gestiona la muerte del jugador y desactiva los controles.
    /// </summary>
    private void Die()
    {
        if (isDead) return;

        isDead = true;
        Debug.Log("[PlayerHealth] ¡El jugador ha muerto!");

        if (deathSound != null)
        {
            AudioSource.PlayClipAtPoint(deathSound, transform.position);
        }

        // 1. Desbloquear el cursor para interactuar con botones de la UI
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 2. Desactivar componentes del jugador para bloquear totalmente inputs
        FPSPlayerController movement = GetComponent<FPSPlayerController>();
        if (movement != null) movement.enabled = false;

        PlayerCombat combat = GetComponent<PlayerCombat>();
        if (combat != null) combat.enabled = false;

        PlayerInteraction interaction = GetComponent<PlayerInteraction>();
        if (interaction != null) interaction.enabled = false;

        PlayerHotbar hotbar = GetComponent<PlayerHotbar>();
        if (hotbar != null) hotbar.enabled = false;

        // 3. Disparar eventos
        if (OnDeath != null)
        {
            OnDeath.Invoke();
        }

        if (OnPlayerDeath != null)
        {
            OnPlayerDeath.Invoke();
        }

        // 4. Notificar a MissionManager si existe
        if (MissionManager.Instance != null && !MissionManager.Instance.IsMissionCompleted)
        {
            MissionManager.Instance.FailMission("El agente ha caído en combate.");
        }
    }

    /// <summary>
    /// Reinicia la vida y estado del jugador, rehabilitando sus controles.
    /// </summary>
    public void ResetHealth()
    {
        isDead = false;
        currentHealth = maxHealth;

        FPSPlayerController movement = GetComponent<FPSPlayerController>();
        if (movement != null) movement.enabled = true;

        PlayerCombat combat = GetComponent<PlayerCombat>();
        if (combat != null) combat.enabled = true;

        PlayerInteraction interaction = GetComponent<PlayerInteraction>();
        if (interaction != null) interaction.enabled = true;

        PlayerHotbar hotbar = GetComponent<PlayerHotbar>();
        if (hotbar != null) hotbar.enabled = true;

        if (OnHealthChanged != null)
        {
            OnHealthChanged.Invoke(currentHealth, maxHealth);
        }

        if (OnPlayerHealthChangedStatic != null)
        {
            OnPlayerHealthChangedStatic.Invoke(currentHealth, maxHealth);
        }
    }
}
