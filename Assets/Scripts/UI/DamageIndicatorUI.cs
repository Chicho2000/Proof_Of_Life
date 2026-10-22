using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

public class DamageIndicatorUI : MonoBehaviour
{
    [Header("Referencias de UI")]
    [Tooltip("CanvasGroup de la viñeta (opcional si se usa directamente la Image)")]
    [SerializeField] private CanvasGroup vignetteGroup;
    [Tooltip("Componente Image que muestra la textura de la viñeta de daño")]
    [SerializeField] private Image vignetteImage;

    [Header("Configuración de Transición")]
    [Tooltip("Velocidad de transición suave de la viñeta (0 para cambio instantáneo)")]
    [SerializeField] private float vignetteTransitionSpeed = 8f;

    [Header("Configuración de Indicador Direccional")]
    [SerializeField] private CanvasGroup indicatorGroup;
    [SerializeField] private Image indicatorImage;
    [SerializeField] private RectTransform indicatorRect;
    [SerializeField] private float indicatorDuration = 1.4f;
    [SerializeField] private float maxIndicatorAlpha = 0.95f;

    // Opacidades fijas exactas para cada golpe (sin sliders en el Inspector)
    private const float OPACITY_FULL_HEALTH = 0.0f; // 0 golpes (100% de vida) -> 0%
    private const float OPACITY_FIRST_HIT   = 0.25f; // 1er golpe (~75% de vida) -> 25%
    private const float OPACITY_SECOND_HIT  = 0.50f; // 2do golpe (~50% de vida) -> 50%
    private const float OPACITY_THIRD_HIT   = 0.75f; // 3er golpe (~25% de vida) -> 75%
    private const float OPACITY_DEAD        = 1.0f;  // Muerte    (0% de vida)   -> 100%

    private float currentVignetteAlpha = 0f;
    private float targetVignetteAlpha = 0f;

    private float indicatorTimer = 0f;
    private Vector3 lastAttackerPosition = Vector3.zero;
    private bool hasAttackerPosition = false;
    private Transform playerCameraTransform;

    private int cachedCurrentHealth = 100;
    private int cachedMaxHealth = 100;
    private PlayerHealth playerHealthRef;

    private void Awake()
    {
        AutoFindReferences();
        EnsureParentCanvasGroupVisible();
    }

    private void OnEnable()
    {
        PlayerHealth.OnPlayerDamagedWithSource += HandleDamageWithSource;
        PlayerHealth.OnPlayerDamaged += HandleSimpleDamage;
        PlayerHealth.OnPlayerHealthChangedStatic += HandleHealthChanged;

        HookPlayerHealthInstanceEvents();
    }

    private void OnDisable()
    {
        PlayerHealth.OnPlayerDamagedWithSource -= HandleDamageWithSource;
        PlayerHealth.OnPlayerDamaged -= HandleSimpleDamage;
        PlayerHealth.OnPlayerHealthChangedStatic -= HandleHealthChanged;

        UnhookPlayerHealthInstanceEvents();
    }

    private void Start()
    {
        AutoFindReferences();
        EnsureParentCanvasGroupVisible();

        HookPlayerHealthInstanceEvents();

        if (playerHealthRef != null)
        {
            cachedCurrentHealth = playerHealthRef.CurrentHealth;
            cachedMaxHealth = playerHealthRef.MaxHealth;
        }

        UpdateVignetteTargetAlpha();
        currentVignetteAlpha = targetVignetteAlpha;
        SetVignetteAlpha(currentVignetteAlpha);
        SetIndicatorAlpha(0f);
    }

    private void Update()
    {
        UpdateVignette();
        UpdateIndicator();
    }

    private void HookPlayerHealthInstanceEvents()
    {
        if (playerHealthRef == null)
        {
            playerHealthRef = FindFirstObjectByType<PlayerHealth>();
        }

        if (playerHealthRef != null)
        {
            playerHealthRef.OnDeath -= HandlePlayerDeath;
            playerHealthRef.OnPlayerDeath -= HandlePlayerDeath;

            playerHealthRef.OnDeath += HandlePlayerDeath;
            playerHealthRef.OnPlayerDeath += HandlePlayerDeath;
        }
    }

    private void UnhookPlayerHealthInstanceEvents()
    {
        if (playerHealthRef != null)
        {
            playerHealthRef.OnDeath -= HandlePlayerDeath;
            playerHealthRef.OnPlayerDeath -= HandlePlayerDeath;
        }
    }

    private void HandlePlayerDeath()
    {
        cachedCurrentHealth = 0;
        targetVignetteAlpha = OPACITY_DEAD;
        currentVignetteAlpha = OPACITY_DEAD;
        SetVignetteAlpha(OPACITY_DEAD);
    }

    private void HandleHealthChanged(int current, int max)
    {
        cachedCurrentHealth = Mathf.Max(0, current);
        cachedMaxHealth = Mathf.Max(1, max);

        UpdateVignetteTargetAlpha();
    }

    private void HandleSimpleDamage(int damageAmount)
    {
        // Se actualiza directamente por HandleHealthChanged
    }

    private void HandleDamageWithSource(int damageAmount, bool hasAttacker, Vector3 attackerPos)
    {
        if (hasAttacker)
        {
            lastAttackerPosition = attackerPos;
            hasAttackerPosition = true;
            indicatorTimer = indicatorDuration;
            SetIndicatorAlpha(maxIndicatorAlpha);
        }
    }

    private void UpdateVignetteTargetAlpha()
    {
        if (cachedMaxHealth <= 0)
        {
            targetVignetteAlpha = OPACITY_FULL_HEALTH;
            return;
        }

        float healthNormalized = (float)cachedCurrentHealth / cachedMaxHealth;
        targetVignetteAlpha = CalculateFixedVignetteOpacity(healthNormalized);
    }

    private float CalculateFixedVignetteOpacity(float healthNormalized)
    {
        healthNormalized = Mathf.Clamp01(healthNormalized);

        if (healthNormalized >= 1f)
        {
            return OPACITY_FULL_HEALTH; // 0%
        }
        else if (healthNormalized > 0.75f)
        {
            // Entre 100% y 75% de salud (hacia el 1er golpe)
            float t = Mathf.InverseLerp(1.0f, 0.75f, healthNormalized);
            return Mathf.Lerp(OPACITY_FULL_HEALTH, OPACITY_FIRST_HIT, t);
        }
        else if (healthNormalized > 0.50f)
        {
            // Entre 75% y 50% de salud (del 1er al 2do golpe)
            float t = Mathf.InverseLerp(0.75f, 0.50f, healthNormalized);
            return Mathf.Lerp(OPACITY_FIRST_HIT, OPACITY_SECOND_HIT, t);
        }
        else if (healthNormalized > 0.25f)
        {
            // Entre 50% y 25% de salud (del 2do al 3er golpe)
            float t = Mathf.InverseLerp(0.50f, 0.25f, healthNormalized);
            return Mathf.Lerp(OPACITY_SECOND_HIT, OPACITY_THIRD_HIT, t);
        }
        else if (healthNormalized > 0f)
        {
            // Entre 25% y 0% de salud (del 3er golpe a la muerte)
            float t = Mathf.InverseLerp(0.25f, 0.0f, healthNormalized);
            return Mathf.Lerp(OPACITY_THIRD_HIT, OPACITY_DEAD, t);
        }
        else
        {
            return OPACITY_DEAD; // 100%
        }
    }

    private void UpdateVignette()
    {
        float dt = Time.unscaledDeltaTime;

        if (vignetteTransitionSpeed > 0f)
        {
            currentVignetteAlpha = Mathf.MoveTowards(currentVignetteAlpha, targetVignetteAlpha, vignetteTransitionSpeed * dt);
        }
        else
        {
            currentVignetteAlpha = targetVignetteAlpha;
        }

        SetVignetteAlpha(currentVignetteAlpha);
    }

    private void UpdateIndicator()
    {
        if (indicatorTimer > 0f)
        {
            indicatorTimer -= Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(indicatorTimer / indicatorDuration);
            SetIndicatorAlpha(progress * maxIndicatorAlpha);

            if (playerCameraTransform == null)
            {
                FindCameraReference();
            }

            if (playerCameraTransform != null && hasAttackerPosition && indicatorRect != null)
            {
                Vector3 toAttacker = lastAttackerPosition - playerCameraTransform.position;
                toAttacker.y = 0f;

                Vector3 camForward = playerCameraTransform.forward;
                camForward.y = 0f;

                if (toAttacker.sqrMagnitude > 0.01f && camForward.sqrMagnitude > 0.01f)
                {
                    float angle = Vector3.SignedAngle(camForward.normalized, toAttacker.normalized, Vector3.up);
                    indicatorRect.localRotation = Quaternion.Euler(0f, 0f, -angle);
                }
            }
        }
        else
        {
            SetIndicatorAlpha(0f);
        }
    }

    private void SetVignetteAlpha(float alpha)
    {
        if (vignetteGroup != null)
        {
            vignetteGroup.alpha = alpha;
            if (vignetteImage != null && vignetteImage.color.a < 0.99f)
            {
                Color c = vignetteImage.color;
                c.a = 1f;
                vignetteImage.color = c;
            }
        }
        else if (vignetteImage != null)
        {
            Color c = vignetteImage.color;
            c.a = alpha;
            vignetteImage.color = c;
        }
    }

    private void SetIndicatorAlpha(float alpha)
    {
        if (indicatorGroup != null)
        {
            indicatorGroup.alpha = alpha;
            if (indicatorImage != null && indicatorImage.color.a < 0.99f)
            {
                Color c = indicatorImage.color;
                c.a = 1f;
                indicatorImage.color = c;
            }
        }
        else if (indicatorImage != null)
        {
            Color c = indicatorImage.color;
            c.a = alpha;
            indicatorImage.color = c;
        }
    }

    private void EnsureParentCanvasGroupVisible()
    {
        CanvasGroup selfGroup = GetComponent<CanvasGroup>();
        if (selfGroup != null && selfGroup != vignetteGroup && selfGroup != indicatorGroup)
        {
            selfGroup.alpha = 1f;
        }
    }

    private void AutoFindReferences()
    {
        FindCameraReference();

        if (indicatorGroup != null && indicatorGroup == vignetteGroup)
        {
            indicatorGroup = null;
        }

        if (indicatorRect != null)
        {
            indicatorRect.pivot = new Vector2(0.5f, 0.5f);
            indicatorRect.anchoredPosition = Vector2.zero;

            if (indicatorGroup == null || indicatorGroup.gameObject != indicatorRect.gameObject)
            {
                indicatorGroup = indicatorRect.GetComponent<CanvasGroup>();
            }

            if (indicatorImage == null || indicatorImage.gameObject != indicatorRect.gameObject)
            {
                indicatorImage = indicatorRect.GetComponent<Image>();
            }
        }

        if (vignetteGroup != null && vignetteImage == null)
        {
            vignetteImage = vignetteGroup.GetComponent<Image>();
        }

   // 3. Si vignetteGroup está asignado, enlazar su Image
        if (vignetteGroup != null && vignetteImage == null)
        {
            vignetteImage = vignetteGroup.GetComponent<Image>();
        }

        // 4. Búsqueda por hijos si faltó algo
        if (vignetteGroup == null || vignetteImage == null)
        {
            Transform vTr = transform.Find("Vignette_Overlay");
            if (vTr == null) vTr = transform.Find("Vignitte_Overlay");
            if (vTr == null) vTr = transform.Find("Damage_Vignette_Overlay");
            if (vTr != null)
            {
                if (vignetteGroup == null) vignetteGroup = vTr.GetComponent<CanvasGroup>();
                if (vignetteImage == null) vignetteImage = vTr.GetComponent<Image>();
            }
        }

        if (indicatorGroup == null || indicatorImage == null || indicatorRect == null)
        {
            Transform iTr = transform.Find("Direction_Indicator");
            if (iTr == null) iTr = transform.Find("Damage_Direction_Indicator");
            if (iTr != null)
            {
                if (indicatorGroup == null) indicatorGroup = iTr.GetComponent<CanvasGroup>();
                if (indicatorImage == null) indicatorImage = iTr.GetComponent<Image>();
                if (indicatorRect == null) indicatorRect = iTr.GetComponent<RectTransform>();
            }
        }

        if (indicatorRect != null)
        {
            indicatorRect.pivot = new Vector2(0.5f, 0.5f);
            indicatorRect.anchoredPosition = Vector2.zero;
        }
    }

    private void FindCameraReference()
    {
        if (playerCameraTransform != null) return;

        Camera cam = Camera.main;
        if (cam == null)
        {
            PlayerHealth ph = FindFirstObjectByType<PlayerHealth>();
            if (ph != null) cam = ph.GetComponentInChildren<Camera>();
        }
        if (cam == null)
        {
            cam = FindFirstObjectByType<Camera>();
        }

        if (cam != null)
        {
            playerCameraTransform = cam.transform;
        }
    }
}