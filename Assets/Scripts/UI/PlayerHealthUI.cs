using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
    [Header("Referencia al Jugador")]
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Elementos de UI")]
    [SerializeField] private GameObject healthContainer;
    [SerializeField] private Image healthFillImage;
    [SerializeField] private Image trailFillImage; // Barra fantasma / rezagada (muestra el daño recibido)
    [SerializeField] private Text healthText;
    [SerializeField] private Image damageFlashOverlay;

    [Header("Configuración Visual")]
    [SerializeField] private Color fullHealthColor = new Color(0.2f, 0.85f, 0.3f, 1f); // Verde
    [SerializeField] private Color midHealthColor = new Color(1f, 0.8f, 0.1f, 1f);    // Amarillo
    [SerializeField] private Color lowHealthColor = new Color(0.9f, 0.15f, 0.15f, 1f); // Rojo
    [SerializeField] private Color trailColor = new Color(1f, 1f, 1f, 0.85f);      // Blanco brillante impacto

    [Header("Animación y Dinamismo")]
    [Tooltip("Velocidad de interpolación de la barra frontal")]
    [SerializeField] private float frontLerpSpeed = 7f;
    [Tooltip("Tiempo de pausa en segundos antes de que la barra de rastro caiga")]
    [SerializeField] private float trailDelay = 0.3f;
    [Tooltip("Velocidad de caída de la barra de rastro rezagada")]
    [SerializeField] private float trailLerpSpeed = 3.5f;
    [Tooltip("Velocidad del contador numérico en texto (HP por segundo)")]
    [SerializeField] private float textRollSpeed = 90f;
    [Tooltip("Efecto de pequeño golpe/rebote elástico al recibir daño")]
    [SerializeField] private bool enablePunchEffect = true;

    [Header("Flash de Daño en Pantalla")]
    [SerializeField] private Color flashColor = new Color(1f, 0f, 0f, 0.35f);
    [SerializeField] private float flashDuration = 0.25f;

    // Estados internos
    private float currentFill = 1f;
    private float trailFill = 1f;
    private float targetFill = 1f;
    private float trailDelayTimer = 0f;

    private float displayedHealth = 100f;
    private int targetHealth = 100;
    private int maxHealth = 100;

    private Coroutine flashCoroutine;
    private Coroutine punchCoroutine;
    private Vector3 originalContainerScale = Vector3.one;

    private void Awake()
    {
        AutoFindReferences();

        if (healthFillImage == null)
        {
            BuildDefaultUI();
        }
        else
        {
            EnsureSpritesAssigned();
        }

        if (healthContainer != null)
        {
            originalContainerScale = healthContainer.transform.localScale;
        }
    }

    private void AutoFindReferences()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();

        if (canvas != null)
        {
            Transform panel = canvas.transform.Find("PlayerHealth_Panel");
            if (panel != null)
            {
                if (healthContainer == null) healthContainer = panel.gameObject;

                Transform bg = panel.Find("Health_Bar_Bg");
                if (bg != null)
                {
                    if (trailFillImage == null)
                    {
                        Transform trail = bg.Find("Health_Trail");
                        if (trail != null) trailFillImage = trail.GetComponent<Image>();
                    }

                    if (healthFillImage == null)
                    {
                        Transform fill = bg.Find("Health_Fill");
                        if (fill != null) healthFillImage = fill.GetComponent<Image>();
                    }
                }

                if (healthText == null)
                {
                    Transform txt = panel.Find("Health_Text");
                    if (txt != null) healthText = txt.GetComponent<Text>();
                }
            }
        }

        // Si existe la barra principal pero falta la barra de rastro en tiempo de juego, crearla automáticamente
        if (Application.isPlaying && healthFillImage != null && trailFillImage == null)
        {
            CreateTrailBarBehindMain();
        }
    }

    private void CreateTrailBarBehindMain()
    {
        GameObject trailObj = Instantiate(healthFillImage.gameObject, healthFillImage.transform.parent);
        trailObj.name = "Health_Trail";
        trailObj.transform.SetSiblingIndex(healthFillImage.transform.GetSiblingIndex()); // Ubicarla justo detrás
        trailFillImage = trailObj.GetComponent<Image>();
        trailFillImage.color = trailColor;
        EnsureImageSprite(trailFillImage);
    }

    private void EnsureSpritesAssigned()
    {
        EnsureImageSprite(healthFillImage);
        EnsureImageSprite(trailFillImage);
    }

    private void EnsureImageSprite(Image img)
    {
        if (img != null && img.sprite == null)
        {
            img.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
        }
    }

    private void Start()
    {
        AutoFindReferences();
        EnsureSpritesAssigned();

        if (playerHealth == null)
        {
            playerHealth = FindFirstObjectByType<PlayerHealth>();
            if (playerHealth == null)
            {
                FPSPlayerController playerController = FindFirstObjectByType<FPSPlayerController>();
                if (playerController != null)
                {
                    playerHealth = playerController.gameObject.AddComponent<PlayerHealth>();
                    Debug.Log("[PlayerHealthUI] Componente PlayerHealth añadido automáticamente al jugador.");
                }
            }
        }

        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += HandleHealthChanged;
            playerHealth.OnDamaged += HandleDamaged;
            playerHealth.OnDeath += HandleDeath;

            // Inicialización de valores
            maxHealth = playerHealth.MaxHealth;
            targetHealth = playerHealth.CurrentHealth;
            displayedHealth = targetHealth;
            targetFill = maxHealth > 0 ? (float)targetHealth / maxHealth : 0f;
            currentFill = targetFill;
            trailFill = targetFill;

            ApplyFill(healthFillImage, currentFill);
            ApplyFill(trailFillImage, trailFill);
            if (trailFillImage != null) trailFillImage.color = trailColor;
            UpdateBarColor(currentFill);
            UpdateTextDisplay((int)displayedHealth, maxHealth);
        }
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= HandleHealthChanged;
            playerHealth.OnDamaged -= HandleDamaged;
            playerHealth.OnDeath -= HandleDeath;
        }
    }

    private void Update()
    {
        // 1. Transición suave con desaceleración (Ease-out) de la barra de vida frontal
        if (Mathf.Abs(currentFill - targetFill) > 0.0005f)
        {
            currentFill = Mathf.Lerp(currentFill, targetFill, Time.deltaTime * frontLerpSpeed);
            ApplyFill(healthFillImage, currentFill);
            UpdateBarColor(currentFill);
        }
        else if (currentFill != targetFill)
        {
            currentFill = targetFill;
            ApplyFill(healthFillImage, currentFill);
            UpdateBarColor(currentFill);
        }

        // 2. Barra de rastro / daño rezagada (Ghost Bar)
        if (trailDelayTimer > 0f)
        {
            trailDelayTimer -= Time.deltaTime;
        }
        else
        {
            if (trailFill > currentFill)
            {
                trailFill = Mathf.Lerp(trailFill, currentFill, Time.deltaTime * trailLerpSpeed);
                if (Mathf.Abs(trailFill - currentFill) < 0.001f)
                {
                    trailFill = currentFill;
                }
                ApplyFill(trailFillImage, trailFill);
            }
            else if (trailFill < currentFill)
            {
                trailFill = currentFill;
                ApplyFill(trailFillImage, trailFill);
            }
        }

        // 3. Conteo numérico dinámico del texto de vida
        if (Mathf.Abs(displayedHealth - targetHealth) > 0.05f)
        {
            displayedHealth = Mathf.MoveTowards(displayedHealth, targetHealth, textRollSpeed * Time.deltaTime);
            UpdateTextDisplay(Mathf.RoundToInt(displayedHealth), maxHealth);
        }
        else if ((int)displayedHealth != targetHealth)
        {
            displayedHealth = targetHealth;
            UpdateTextDisplay(targetHealth, maxHealth);
        }
    }

    private void ApplyFill(Image img, float fill)
    {
        if (img == null) return;

        fill = Mathf.Clamp01(fill);
        RectTransform rt = img.rectTransform;

        if (img.type == Image.Type.Filled && img.sprite != null)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            img.fillAmount = fill;
        }
        else
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(fill, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            img.fillAmount = 1f;
        }
    }

    private void UpdateBarColor(float ratio)
    {
        if (healthFillImage == null) return;

        if (ratio > 0.5f)
        {
            float t = (ratio - 0.5f) * 2f;
            healthFillImage.color = Color.Lerp(midHealthColor, fullHealthColor, t);
        }
        else
        {
            float t = ratio * 2f;
            healthFillImage.color = Color.Lerp(lowHealthColor, midHealthColor, t);
        }
    }

    private void UpdateTextDisplay(int current, int max)
    {
        if (healthText == null) return;

        if (playerHealth != null && playerHealth.IsDead && current <= 0)
        {
            healthText.text = "MUERTO";
        }
        else
        {
            healthText.text = $"{current} / {max} HP";
        }
    }

    private void HandleHealthChanged(int current, int max)
    {
        maxHealth = max;
        targetHealth = Mathf.Max(0, current);

        float newTargetFill = max > 0 ? (float)targetHealth / max : 0f;

        // Si fue daño (reducción), activar pausa de la barra de rastro
        if (newTargetFill < targetFill)
        {
            trailDelayTimer = trailDelay;
        }
        else
        {
            // Si fue curación, la barra de rastro acompaña inmediatamente
            trailDelayTimer = 0f;
            trailFill = newTargetFill;
            ApplyFill(trailFillImage, trailFill);
        }

        targetFill = newTargetFill;
    }

    private void HandleDamaged(int damageAmount)
    {
        if (damageFlashOverlay != null)
        {
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(DamageFlashRoutine());
        }

        // Rebote visual / punch al recibir impacto
        if (enablePunchEffect && healthContainer != null)
        {
            if (punchCoroutine != null) StopCoroutine(punchCoroutine);
            punchCoroutine = StartCoroutine(PunchRoutine());
        }
    }

    private void HandleDeath()
    {
        targetHealth = 0;
        targetFill = 0f;
        trailDelayTimer = trailDelay;
    }

    private IEnumerator PunchRoutine()
    {
        Transform t = healthContainer.transform;
        Vector3 punchScale = originalContainerScale * 1.07f;
        float duration = 0.18f;
        float half = duration * 0.5f;

        float elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            t.localScale = Vector3.Lerp(originalContainerScale, punchScale, elapsed / half);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            t.localScale = Vector3.Lerp(punchScale, originalContainerScale, elapsed / half);
            yield return null;
        }

        t.localScale = originalContainerScale;
    }

    private IEnumerator DamageFlashRoutine()
    {
        damageFlashOverlay.enabled = true;
        float elapsed = 0f;
        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(flashColor.a, 0f, elapsed / flashDuration);
            damageFlashOverlay.color = new Color(flashColor.r, flashColor.g, flashColor.b, alpha);
            yield return null;
        }
        damageFlashOverlay.enabled = false;
    }

    /// <summary>
    /// Construye una barra de salud visual dinámica por código dentro del Canvas.
    /// </summary>
    public void BuildDefaultUI()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();

        if (canvas == null)
        {
            Debug.LogWarning("[PlayerHealthUI] No se encontró ningún Canvas en la escena.");
            return;
        }

        Transform existing = canvas.transform.Find("PlayerHealth_Panel");
        if (existing != null)
        {
            healthContainer = existing.gameObject;
            healthFillImage = existing.Find("Health_Bar_Bg/Health_Fill")?.GetComponent<Image>();
            trailFillImage = existing.Find("Health_Bar_Bg/Health_Trail")?.GetComponent<Image>();
            healthText = existing.Find("Health_Text")?.GetComponent<Text>();
            return;
        }

        // 1. Contenedor principal
        GameObject panelObj = new GameObject("PlayerHealth_Panel", typeof(RectTransform), typeof(Image));
        panelObj.transform.SetParent(canvas.transform, false);
        healthContainer = panelObj;

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0f);
        panelRect.anchorMax = new Vector2(0f, 0f);
        panelRect.pivot = new Vector2(0f, 0f);
        panelRect.anchoredPosition = new Vector2(40f, 40f);
        panelRect.sizeDelta = new Vector2(240f, 40f);

        Image panelBg = panelObj.GetComponent<Image>();
        panelBg.color = new Color(0f, 0f, 0f, 0.65f);

        // 2. Fondo de la barra de vida
        GameObject barBgObj = new GameObject("Health_Bar_Bg", typeof(RectTransform), typeof(Image));
        barBgObj.transform.SetParent(panelObj.transform, false);
        RectTransform barBgRect = barBgObj.GetComponent<RectTransform>();
        barBgRect.anchorMin = new Vector2(0f, 0.2f);
        barBgRect.anchorMax = new Vector2(1f, 0.8f);
        barBgRect.offsetMin = new Vector2(8f, 0f);
        barBgRect.offsetMax = new Vector2(-8f, 0f);

        Image barBgImage = barBgObj.GetComponent<Image>();
        barBgImage.color = new Color(0.12f, 0.12f, 0.12f, 0.95f);

        // 3. Barra fantasma / Trail (Rastro de daño amarillo)
        GameObject trailObj = new GameObject("Health_Trail", typeof(RectTransform), typeof(Image));
        trailObj.transform.SetParent(barBgObj.transform, false);
        RectTransform trailRect = trailObj.GetComponent<RectTransform>();
        trailRect.anchorMin = Vector2.zero;
        trailRect.anchorMax = Vector2.one;
        trailRect.sizeDelta = Vector2.zero;

        trailFillImage = trailObj.GetComponent<Image>();
        trailFillImage.type = Image.Type.Filled;
        trailFillImage.fillMethod = Image.FillMethod.Horizontal;
        trailFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        trailFillImage.fillAmount = 1f;
        trailFillImage.color = trailColor;

        // 4. Barra frontal de vida
        GameObject barFillObj = new GameObject("Health_Fill", typeof(RectTransform), typeof(Image));
        barFillObj.transform.SetParent(barBgObj.transform, false);
        RectTransform barFillRect = barFillObj.GetComponent<RectTransform>();
        barFillRect.anchorMin = Vector2.zero;
        barFillRect.anchorMax = Vector2.one;
        barFillRect.sizeDelta = Vector2.zero;

        healthFillImage = barFillObj.GetComponent<Image>();
        healthFillImage.type = Image.Type.Filled;
        healthFillImage.fillMethod = Image.FillMethod.Horizontal;
        healthFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        healthFillImage.fillAmount = 1f;
        healthFillImage.color = fullHealthColor;

        // 5. Texto de vida
        GameObject textObj = new GameObject("Health_Text", typeof(RectTransform), typeof(Text));
        textObj.transform.SetParent(panelObj.transform, false);
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        healthText = textObj.GetComponent<Text>();
        healthText.text = "100 / 100 HP";
        healthText.alignment = TextAnchor.MiddleCenter;
        healthText.color = Color.white;

        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        healthText.font = defaultFont;
        healthText.fontSize = 14;

        EnsureSpritesAssigned();
        Debug.Log("[PlayerHealthUI] Interfaz de vida dinámica creada exitosamente bajo el Canvas.");
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (trailFillImage != null)
        {
            trailFillImage.color = trailColor;
        }
    }
#endif
}
