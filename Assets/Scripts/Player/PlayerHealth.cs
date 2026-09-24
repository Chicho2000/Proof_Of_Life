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
    public event Action<int, int> OnHealthChanged; // (currentHealth, maxHealth)
    public event Action<int> OnDamaged;           // (damageAmount)
    public event Action<int> OnHealed;            // (healAmount)
    public event Action OnDeath;
    public event Action OnPlayerDeath;            // Alias por compatibilidad

    // Propiedades públicas de consulta
    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public bool IsDead => isDead;
    public float HealthNormalized => maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;

    private void Awake()
    {
        currentHealth = maxHealth;
        EnsureUIExists();
    }

    private void EnsureUIExists()
    {
        if (FindFirstObjectByType<PlayerHealthUI>() == null)
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                canvas.gameObject.AddComponent<PlayerHealthUI>();
            }
        }
    }

    private void Start()
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// Aplica daño al jugador, descontando de su vida actual.
    /// </summary>
    public void TakeDamage(int damage)
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

        OnDamaged?.Invoke(damage);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

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

        OnHealed?.Invoke(healAmount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
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

        OnDeath?.Invoke();
        OnPlayerDeath?.Invoke();

        // Desactivar componentes del jugador si existen
        FPSPlayerController movement = GetComponent<FPSPlayerController>();
        if (movement != null) movement.enabled = false;

        PlayerCombat combat = GetComponent<PlayerCombat>();
        if (combat != null) combat.enabled = false;

        PlayerInteraction interaction = GetComponent<PlayerInteraction>();
        if (interaction != null) interaction.enabled = false;
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

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}
