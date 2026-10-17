using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealthUI : MonoBehaviour
{
    [Header("Referencia al Jugador")]
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Elementos de UI")]
    [SerializeField] private GameObject healthContainer;
    [SerializeField] private Image healthFillImage;
    [SerializeField] private Image trailFillImage;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private Image damageFlashOverlay;

    [SerializeField] private float flashDuration = 0.2f;

    private float flashTimer = 0f;

    private void Awake()
    {
        if (playerHealth == null)
        {
            playerHealth = FindFirstObjectByType<PlayerHealth>();
        }

        AutoFindReferences();
    }

    private void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += HandleHealthChanged;
            playerHealth.OnDamaged += HandleDamaged;
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= HandleHealthChanged;
            playerHealth.OnDamaged -= HandleDamaged;
        }
    }

    private void Start()
    {
        if (playerHealth != null)
        {
            HandleHealthChanged(playerHealth.CurrentHealth, playerHealth.MaxHealth);
        }
    }

    private void Update()
    {
        if (MissionManager.Instance != null && (MissionManager.Instance.IsMissionCompleted || MissionManager.Instance.IsMissionFailed))
        {
            if (healthContainer != null && healthContainer.activeSelf)
            {
                healthContainer.SetActive(false);
            }
            return;
        }

        AnimateDamageFlash();
    }

    private void AutoFindReferences()
    {
        if (healthContainer == null)
        {
            Transform containerTransform = transform.Find("PlayerHealth_Panel");
            if (containerTransform != null)
            {
                healthContainer = containerTransform.gameObject;
            }
            else
            {
                healthContainer = gameObject;
            }
        }

        if (healthContainer != null)
        {
            if (healthFillImage == null)
            {
                Transform fillTr = healthContainer.transform.Find("Health_Fill");
                if (fillTr == null) fillTr = healthContainer.transform.Find("Bar_Bg/Health_Fill");
                if (fillTr != null) healthFillImage = fillTr.GetComponent<Image>();
            }

            if (trailFillImage == null)
            {
                Transform trailTr = healthContainer.transform.Find("Health_Trail");
                if (trailTr == null) trailTr = healthContainer.transform.Find("Bar_Bg/Health_Trail");
                if (trailTr != null) trailFillImage = trailTr.GetComponent<Image>();
            }

            if (healthText == null)
            {
                healthText = healthContainer.GetComponentInChildren<TMP_Text>();
            }
        }
    }

    private void HandleHealthChanged(int current, int max)
    {
        if (max <= 0) return;

        if (healthText != null)
        {
            healthText.text = current.ToString() + " HP";
        }
    }

    private void HandleDamaged(int damageAmount)
    {
        flashTimer = flashDuration;
    }

    private void AnimateDamageFlash()
    {
        if (damageFlashOverlay == null) return;

        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
        }
    }
}