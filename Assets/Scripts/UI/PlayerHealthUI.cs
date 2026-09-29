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

    [Header("Colores")]
    [SerializeField] private Color fullHealthColor = new Color(0.15f, 0.85f, 0.35f, 1f); // Verde nítido
    [SerializeField] private Color midHealthColor = new Color(0.95f, 0.75f, 0.1f, 1f);   // Amarillo
    [SerializeField] private Color lowHealthColor = new Color(0.9f, 0.2f, 0.2f, 1f);     // Rojo
    [SerializeField] private Color flashColor = new Color(0.8f, 0f, 0f, 0.35f);

    [Header("Animación")]
    [SerializeField] private float fillLerpSpeed = 8f;
    [SerializeField] private float trailDelay = 0.4f;
    [SerializeField] private float trailLerpSpeed = 3f;
    [SerializeField] private float flashDuration = 0.2f;

    private float targetFill = 1f;
    private float currentFill = 1f;
    private float trailFill = 1f;
    private float trailTimer = 0f;
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
        if (damageFlashOverlay != null)
        {
            Color c = damageFlashOverlay.color;
            c.a = 0f;
            damageFlashOverlay.color = c;
            damageFlashOverlay.gameObject.SetActive(true);
        }

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

        AnimateBars();
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

        targetFill = Mathf.Clamp01((float)current / max);

        if (healthText != null)
        {
            healthText.text = current.ToString() + " HP";
        }

        trailTimer = trailDelay;
    }

    private void HandleDamaged(int damageAmount)
    {
        flashTimer = flashDuration;
    }

    private void AnimateBars()
    {
        // Animar barra frontal
        currentFill = Mathf.MoveTowards(currentFill, targetFill, fillLerpSpeed * Time.deltaTime);
        if (healthFillImage != null)
        {
            healthFillImage.fillAmount = currentFill;
            healthFillImage.color = GetColorForFill(currentFill);
        }

        // Animar barra de rastro de daño
        if (trailTimer > 0f)
        {
            trailTimer -= Time.deltaTime;
        }
        else
        {
            trailFill = Mathf.MoveTowards(trailFill, targetFill, trailLerpSpeed * Time.deltaTime);
            if (trailFillImage != null)
            {
                trailFillImage.fillAmount = trailFill;
            }
        }
    }

    private void AnimateDamageFlash()
    {
        if (damageFlashOverlay == null) return;

        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            float alpha = Mathf.Lerp(0f, flashColor.a, flashTimer / flashDuration);
            Color c = flashColor;
            c.a = alpha;
            damageFlashOverlay.color = c;
        }
        else
        {
            if (damageFlashOverlay.color.a > 0f)
            {
                Color c = flashColor;
                c.a = 0f;
                damageFlashOverlay.color = c;
            }
        }
    }

    private Color GetColorForFill(float fill)
    {
        if (fill > 0.5f)
        {
            float t = (fill - 0.5f) / 0.5f;
            return Color.Lerp(midHealthColor, fullHealthColor, t);
        }
        else
        {
            float t = fill / 0.5f;
            return Color.Lerp(lowHealthColor, midHealthColor, t);
        }
    }
}
