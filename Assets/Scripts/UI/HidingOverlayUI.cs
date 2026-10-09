using UnityEngine;

public class HidingOverlayUI : MonoBehaviour
{
    [Header("Componente de UI")]
    [SerializeField] private CanvasGroup hidingCanvasGroup;

    [Header("Configuración")]
    [Range(0f, 1f)]
    [SerializeField] private float maxDarknessAlpha = 0.75f;
    [SerializeField] private float fadeSpeed = 4.5f;

    private float targetAlpha = 0f;
    private float currentAlpha = 0f;

    private void Awake()
    {
        if (hidingCanvasGroup == null)
        {
            hidingCanvasGroup = GetComponent<CanvasGroup>();
        }

        if (hidingCanvasGroup != null)
        {
            hidingCanvasGroup.alpha = 0f;
            hidingCanvasGroup.blocksRaycasts = false;
            hidingCanvasGroup.interactable = false;
        }
    }

    private void OnEnable()
    {
        PlayerHidingSystem.OnPlayerEnteredHiding += HandlePlayerEnteredHiding;
        PlayerHidingSystem.OnPlayerExitedHiding += HandlePlayerExitedHiding;
    }

    private void OnDisable()
    {
        PlayerHidingSystem.OnPlayerEnteredHiding -= HandlePlayerEnteredHiding;
        PlayerHidingSystem.OnPlayerExitedHiding -= HandlePlayerExitedHiding;
    }

    private void Update()
    {
        if (hidingCanvasGroup == null) return;

        if (Mathf.Abs(currentAlpha - targetAlpha) > 0.001f)
        {
            currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, fadeSpeed * Time.deltaTime);
            hidingCanvasGroup.alpha = currentAlpha;
        }
        else if (hidingCanvasGroup.alpha != targetAlpha)
        {
            hidingCanvasGroup.alpha = targetAlpha;
        }
    }

    private void HandlePlayerEnteredHiding(HideSpot spot)
    {
        targetAlpha = maxDarknessAlpha;
    }

    private void HandlePlayerExitedHiding()
    {
        targetAlpha = 0f;
    }
}
