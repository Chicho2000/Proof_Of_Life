using UnityEngine;
using UnityEngine.UI;

public class DamageIndicatorUI : MonoBehaviour
{
    [Header("Referencias de UI")]
    [SerializeField] private CanvasGroup vignetteGroup;
    [SerializeField] private Image vignetteImage;
    [SerializeField] private CanvasGroup indicatorGroup;
    [SerializeField] private Image indicatorImage;
    [SerializeField] private RectTransform indicatorRect;

    [Header("Configuración de Viñeta (Payday 2 Flash)")]
    [SerializeField] private float vignetteDuration = 1.0f;
    [SerializeField] private float maxVignetteAlpha = 0.85f;
    [SerializeField] private float flickerSpeed = 36f;
    [SerializeField] private float flickerIntensity = 0.25f;

    [Header("Configuración de Salud Crítica (Latido Cardíaco)")]
    [Tooltip("Umbral de HP por debajo del cual comienza a palpitar la pantalla")]
    [SerializeField] private float criticalHealthThreshold = 25f;
    [SerializeField] private float heartbeatSpeed = 5.5f;
    [SerializeField] private float minHeartbeatAlpha = 0.18f;
    [SerializeField] private float maxHeartbeatAlpha = 0.52f;

    [Header("Configuración de Indicador Direccional")]
    [SerializeField] private float indicatorDuration = 1.4f;
    [SerializeField] private float maxIndicatorAlpha = 0.95f;

    private float vignetteTimer = 0f;
    private float indicatorTimer = 0f;
    private Vector3 lastAttackerPosition = Vector3.zero;
    private bool hasAttackerPosition = false;
    private Transform playerCameraTransform;

    private int cachedCurrentHealth = 100;
    private int cachedMaxHealth = 100;

    private void Awake()
    {
        AutoFindReferences();
        EnsureParentCanvasGroupVisible();
        ResetAlpha();
    }

    private void OnEnable()
    {
        PlayerHealth.OnPlayerDamagedWithSource += HandleDamageWithSource;
        PlayerHealth.OnPlayerDamaged += HandleSimpleDamage;
        PlayerHealth.OnPlayerHealthChangedStatic += HandleHealthChanged;
    }

    private void OnDisable()
    {
        PlayerHealth.OnPlayerDamagedWithSource -= HandleDamageWithSource;
        PlayerHealth.OnPlayerDamaged -= HandleSimpleDamage;
        PlayerHealth.OnPlayerHealthChangedStatic -= HandleHealthChanged;
    }

    private void Start()
    {
        AutoFindReferences();
        EnsureParentCanvasGroupVisible();
        ResetAlpha();

        PlayerHealth ph = FindFirstObjectByType<PlayerHealth>();
        if (ph != null)
        {
            cachedCurrentHealth = ph.CurrentHealth;
            cachedMaxHealth = ph.MaxHealth;
        }
    }

    private void Update()
    {
        UpdateVignette();
        UpdateIndicator();
    }

    private void HandleHealthChanged(int current, int max)
    {
        cachedCurrentHealth = current;
        cachedMaxHealth = max;
    }

    private void HandleSimpleDamage(int damageAmount)
    {
        vignetteTimer = vignetteDuration;
    }

    private void HandleDamageWithSource(int damageAmount, bool hasAttacker, Vector3 attackerPos)
    {
        vignetteTimer = vignetteDuration;

        if (hasAttacker)
        {
            lastAttackerPosition = attackerPos;
            hasAttackerPosition = true;
            indicatorTimer = indicatorDuration;
            SetIndicatorAlpha(maxIndicatorAlpha);
        }
    }

    private void UpdateVignette()
    {
        // 1. Alpha por impacto directo
        float flashAlpha = 0f;
        if (vignetteTimer > 0f)
        {
            vignetteTimer -= Time.deltaTime;
            float progress = Mathf.Clamp01(vignetteTimer / vignetteDuration);

            float flicker = 1f;
            if (progress > 0.35f)
            {
                flicker = 1f - (flickerIntensity * Mathf.Abs(Mathf.Sin(Time.time * flickerSpeed)));
            }

            flashAlpha = progress * maxVignetteAlpha * flicker;
        }

        // 2. Alpha por latido cardíaco de salud crítica
        float heartbeatAlpha = 0f;
        if (cachedCurrentHealth > 0 && cachedCurrentHealth <= criticalHealthThreshold)
        {
            // Pulso sístole/diástole (pico rápido y caída suave)
            float pulse = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Time.time * heartbeatSpeed)), 2.5f);
            heartbeatAlpha = Mathf.Lerp(minHeartbeatAlpha, maxHeartbeatAlpha, pulse);
        }

        // Se toma el mayor entre el impacto reciente y el latido continuo
        float finalAlpha = Mathf.Max(flashAlpha, heartbeatAlpha);
        SetVignetteAlpha(finalAlpha);
    }

    private void UpdateIndicator()
    {
        if (indicatorTimer > 0f)
        {
            indicatorTimer -= Time.deltaTime;
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

    private void ResetAlpha()
    {
        SetVignetteAlpha(0f);
        SetIndicatorAlpha(0f);
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

        // 1. Auto-corregir si se arrastró el mismo CanvasGroup en ambos slots
        if (indicatorGroup != null && indicatorGroup == vignetteGroup)
        {
            indicatorGroup = null;
        }

        // 2. Si indicatorRect está asignado, enlazar su propio CanvasGroup e Image
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
