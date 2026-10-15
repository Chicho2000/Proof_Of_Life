using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class TakeDamage : MonoBehaviour
{
    [Header("Configuración de Viñeta")]
    [Tooltip("Intensidad máxima de la viñeta cuando la vida es 0 o al recibir daño máximo.")]
    [Range(0.1f, 1f)]
    [SerializeField] private float maxIntensity = 0.65f;

    [Tooltip("Suavidad de la viñeta en las esquinas.")]
    [Range(0.1f, 1f)]
    [SerializeField] private float smoothness = 0.45f;

    [Tooltip("Color de la viñeta.")]
    [SerializeField] private Color color = Color.black;

    [Tooltip("Velocidad de transición de la viñeta.")]
    [SerializeField] private float transitionSpeed = 5f;

    [Header("Pruebas")]
    [Tooltip("Permite probar el efecto con clic izquierdo.")]
    [SerializeField] private bool testWithLeftClick = true;

    private PostProcessVolume _volume;
    private Vignette _vignette;
    private float targetIntensity = 0f;
    private float currentIntensity = 0f;
    private Coroutine _flashCoroutine;

    private void Awake()
    {
        SetupVolumeAndVignette();
        FixCameraPostProcessLayer();
    }

    private void OnEnable()
    {
        PlayerHealth.OnPlayerDamaged += HandlePlayerDamaged;
        PlayerHealth.OnPlayerHealthChangedStatic += HandleHealthChanged;

        PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            HandleHealthChanged(playerHealth.CurrentHealth, playerHealth.MaxHealth);
        }
    }

    private void OnDisable()
    {
        PlayerHealth.OnPlayerDamaged -= HandlePlayerDamaged;
        PlayerHealth.OnPlayerHealthChangedStatic -= HandleHealthChanged;
    }

    private void Start()
    {
        if (_vignette == null)
        {
            SetupVolumeAndVignette();
        }
        FixCameraPostProcessLayer();
    }

    private void Update()
    {
        // Prueba manual opcional con clic
        if (testWithLeftClick && Input.GetMouseButtonDown(0))
        {
            TriggerHitFlash(0.4f);
        }

        if (_vignette == null) return;

        // Suavizar la intensidad hacia el valor objetivo
        if (!Mathf.Approximately(currentIntensity, targetIntensity))
        {
            currentIntensity = Mathf.MoveTowards(currentIntensity, targetIntensity, Time.deltaTime * transitionSpeed);
            _vignette.intensity.Override(currentIntensity);
        }
    }

    private void SetupVolumeAndVignette()
    {
        if (_volume == null)
        {
            _volume = GetComponent<PostProcessVolume>();
            if (_volume == null) _volume = GetComponentInChildren<PostProcessVolume>();
            if (_volume == null) _volume = FindFirstObjectByType<PostProcessVolume>();
        }

        if (_volume == null)
        {
            Debug.LogWarning("[TakeDamage] No se encontró ningún PostProcessVolume en la escena.");
            return;
        }

        // Asegurar que el volumen sea global y tenga peso activo
        _volume.isGlobal = true;
        _volume.weight = 1f;

        // Obtener o instanciar perfil
        PostProcessProfile profile = _volume.profile ?? _volume.sharedProfile;
        if (profile == null) return;

        if (!profile.TryGetSettings(out _vignette))
        {
            _vignette = profile.AddSettings<Vignette>();
        }

        if (_vignette != null)
        {
            _vignette.enabled.Override(true);
            _vignette.color.Override(color);
            _vignette.smoothness.Override(smoothness);
            _vignette.roundness.Override(1f);
            _vignette.intensity.Override(currentIntensity);
        }
    }

    /// <summary>
    /// Corrige la cámara si su PostProcessLayer tiene LayerMask en 0 (Nothing), lo cual impediría ver los efectos.
    /// </summary>
    private void FixCameraPostProcessLayer()
    {
        Camera cam = Camera.main ?? FindFirstObjectByType<Camera>();
        if (cam != null && cam.TryGetComponent(out PostProcessLayer ppLayer))
        {
            if (ppLayer.volumeLayer.value == 0)
            {
                // Si la capa está en Nothing, habilitar todas las capas para que el volumen sea visible
                ppLayer.volumeLayer = ~0;
                Debug.Log("[TakeDamage] PostProcessLayer tenía volumeLayer en 0 (Nothing). Se corrigió automáticamente a Everything.");
            }
        }
    }

    private void HandleHealthChanged(int currentHealth, int maxHealth)
    {
        if (maxHealth <= 0) return;

        // Calcula el daño acumulado (0% al inicio, 25% con golpe 1, 50% golpe 2, 75% golpe 3, 100% muerte)
        float damageRatio = 1f - Mathf.Clamp01((float)currentHealth / maxHealth);
        targetIntensity = damageRatio * maxIntensity;

        if (_vignette == null) SetupVolumeAndVignette();
    }

    private void HandlePlayerDamaged(int damageAmount)
    {
        // Flash de impacto al recibir un golpe
        TriggerHitFlash(Mathf.Clamp01(targetIntensity + 0.25f));
    }

    private void TriggerHitFlash(float peakIntensity)
    {
        if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
        _flashCoroutine = StartCoroutine(HitFlashRoutine(peakIntensity));
    }

    private IEnumerator HitFlashRoutine(float peak)
    {
        if (_vignette == null) yield break;

        currentIntensity = peak;
        _vignette.intensity.Override(currentIntensity);

        yield return new WaitForSeconds(0.15f);
        _flashCoroutine = null;
    }
}


