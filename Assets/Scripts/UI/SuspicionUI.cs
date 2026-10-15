using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SuspicionUI : MonoBehaviour
{
    [Header("Barra Superior en Pantalla")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image fillImage;
    [SerializeField] private TMP_Text iconText;

    [Header("Marcador sobre el Enemigo (Opcional - En este mismo Canvas)")]
    [Tooltip("Un objeto o texto dentro de este Canvas que flotará sobre la cabeza del enemigo que te ve.")]
    [SerializeField] private RectTransform enemyMarker;
    [SerializeField] private TMP_Text enemyMarkerText;

    [Header("Colores")]
    [SerializeField] private Color level1Color = new Color(0.2f, 0.65f, 1f, 1f); // Azul Nivel 1 (?)
    [SerializeField] private Color level2StartColor = new Color(1f, 0.65f, 0.1f, 1f); // Naranja inicio Nivel 2
    [SerializeField] private Color level2EndColor = new Color(1f, 0.05f, 0.05f, 1f); // Rojo intenso Nivel 2 (!)

    [Header("Animación")]
    [SerializeField] private float fadeSpeed = 8f;
    [SerializeField] private float fillSpeed = 10f;

    private float targetAlpha = 0f;
    private float targetFill = 0f;
    private int currentLevel = 0;
    private Transform currentEnemy = null;
    private Color currentColor;

    private void Awake()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }

        if (enemyMarker != null)
        {
            enemyMarker.gameObject.SetActive(false);
        }

        currentColor = level1Color;
    }

    private void OnEnable()
    {
        NPCSuspicionSystem.OnGlobalSuspicionUpdated += HandleSuspicionUpdated;
        AlarmManager.OnAlarmTriggered += HideInstant;
        AlarmManager.OnAlarmStopped += HideInstant;
    }

    private void OnDisable()
    {
        NPCSuspicionSystem.OnGlobalSuspicionUpdated -= HandleSuspicionUpdated;
        AlarmManager.OnAlarmTriggered -= HideInstant;
        AlarmManager.OnAlarmStopped -= HideInstant;
    }

    private void Update()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, fadeSpeed * Time.deltaTime);
        }

        if (fillImage != null)
        {
            fillImage.fillAmount = Mathf.MoveTowards(fillImage.fillAmount, targetFill, fillSpeed * Time.deltaTime);
        }

        // Posicionar el marcador flotante en la cabeza del enemigo que te ve
        if (enemyMarker != null)
        {
            if (currentLevel > 0 && currentEnemy != null && Camera.main != null)
            {
                Vector3 screenPos = Camera.main.WorldToScreenPoint(currentEnemy.position + Vector3.up * 2.1f);
                if (screenPos.z > 0f)
                {
                    enemyMarker.gameObject.SetActive(true);
                    enemyMarker.position = screenPos;
                }
                else
                {
                    enemyMarker.gameObject.SetActive(false);
                }
            }
            else
            {
                enemyMarker.gameObject.SetActive(false);
            }
        }
    }

    private void HandleSuspicionUpdated(float progress, int level, Transform enemySource)
    {
        currentLevel = level;
        currentEnemy = enemySource;

        if (currentLevel <= 0)
        {
            targetAlpha = 0f;
            targetFill = 0f;
            if (enemyMarker != null)
            {
                enemyMarker.gameObject.SetActive(false);
            }
            return;
        }

        targetAlpha = 1f;
        targetFill = Mathf.Clamp01(progress);

        string symbol;

        if (currentLevel == 1)
        {
            currentColor = level1Color;
            symbol = "?";
        }
        else
        {
            currentColor = Color.Lerp(level2StartColor, level2EndColor, targetFill);
            symbol = "!";
        }

        if (iconText != null)
        {
            iconText.text = symbol;
            iconText.color = currentColor;
        }

        if (fillImage != null)
        {
            fillImage.color = currentColor;
        }

        if (enemyMarkerText != null)
        {
            enemyMarkerText.text = symbol;
            enemyMarkerText.color = currentColor;
        }
    }

    private void HideInstant()
    {
        targetAlpha = 0f;
        targetFill = 0f;
        currentLevel = 0;
        currentEnemy = null;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }

        if (fillImage != null)
        {
            fillImage.fillAmount = 0f;
        }

        if (enemyMarker != null)
        {
            enemyMarker.gameObject.SetActive(false);
        }
    }
}
