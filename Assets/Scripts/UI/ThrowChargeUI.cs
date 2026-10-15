using UnityEngine;
using UnityEngine.UI;

public class ThrowChargeUI : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private PlayerThrowController throwController;
    [SerializeField] private GameObject chargePanel = null;
    [SerializeField] private Image chargeFill = null;

    [Header("Colores de Carga Dinámicos")]
    [SerializeField] private Color minChargeColor = new Color(0.2f, 0.95f, 0.35f, 1f); // Verde (tiro suave / carga mínima)
    [SerializeField] private Color midChargeColor = new Color(0.98f, 0.82f, 0.15f, 1f); // Amarillo / Ámbar (tiro medio)
    [SerializeField] private Color maxChargeColor = new Color(1.0f, 0.20f, 0.20f, 1f);  // Rojo (fuerza máxima)

    [Header("Diseño de Cono")]
    [SerializeField] private Vector2 coneSize = new Vector2(100f, 130f);
    [SerializeField] private bool pulseAtMaxCharge = true;
    [SerializeField] private float pulseSpeed = 16f;

    private Vector3 originalPanelScale = Vector3.one;

    private void Awake()
    {
        if (chargePanel != null) originalPanelScale = chargePanel.transform.localScale;
    }

    private void Start()
    {
        if (throwController == null) throwController = FindFirstObjectByType<PlayerThrowController>();
        SetupConeUI();
        if (chargePanel != null) chargePanel.SetActive(false);
    }

    private void SetupConeUI()
    {
        if (chargePanel != null && chargePanel.TryGetComponent(out Image panelImg))
        {
            panelImg.sprite = CreateConeSprite(isFrame: true);
            panelImg.type = Image.Type.Simple;
            panelImg.color = Color.white;
            if (chargePanel.TryGetComponent(out RectTransform rt)) rt.sizeDelta = coneSize;
        }

        if (chargeFill != null)
        {
            chargeFill.sprite = CreateConeSprite(isFrame: false);
            chargeFill.type = Image.Type.Filled;
            chargeFill.fillMethod = Image.FillMethod.Vertical;
            chargeFill.fillOrigin = (int)Image.OriginVertical.Bottom;
            chargeFill.fillAmount = 0f;

            RectTransform fillRt = chargeFill.rectTransform;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.sizeDelta = Vector2.zero;
            fillRt.anchoredPosition = Vector2.zero;
        }
    }

    private void Update()
    {
        bool visible = throwController != null && throwController.IsCharging && Time.timeScale > 0f;

        if (chargePanel != null && chargePanel.activeSelf != visible)
        {
            chargePanel.SetActive(visible);
            if (!visible) chargePanel.transform.localScale = originalPanelScale;
        }

        if (!visible || chargeFill == null) return;

        float p = throwController.ChargeNormalized;
        chargeFill.fillAmount = p;
        chargeFill.color = EvaluateChargeColor(p);

        if (chargePanel != null)
        {
            float bump = (pulseAtMaxCharge && p >= 0.98f) ? 1f + 0.035f * Mathf.Sin(Time.time * pulseSpeed) : 1f;
            chargePanel.transform.localScale = originalPanelScale * bump;
        }
    }

    private Color EvaluateChargeColor(float p)
    {
        Color c = p < 0.5f ? Color.Lerp(minChargeColor, midChargeColor, p / 0.5f)
                           : Color.Lerp(midChargeColor, maxChargeColor, (p - 0.5f) / 0.5f);

        if (pulseAtMaxCharge && p >= 0.98f)
            c = Color.Lerp(c, Color.white, (0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed)) * 0.2f);

        return c;
    }

    private static Sprite CreateConeSprite(bool isFrame)
    {
        const int w = 128, h = 160;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        Color[] cols = new Color[w * h];

        for (int y = 0; y < h; y++)
        {
            float v = Mathf.Clamp01((y - 4f) / (h - 8f));
            float halfW = Mathf.Lerp(0.22f, 0.86f, v);

            for (int x = 0; x < w; x++)
            {
                float u = (x / (float)w - 0.5f) * 2f;
                float dEdge = Mathf.Min(
                    (halfW - Mathf.Abs(u)) * (w * 0.5f),
                    Mathf.Min((1f - 0.06f * u * u - v) * h, (v - 0.03f * u * u) * h)
                );
                float alpha = Mathf.Clamp01(dEdge + 1f);

                if (alpha <= 0f) continue;

                if (isFrame)
                {
                    Color bg = Color.Lerp(new Color(0.05f, 0.07f, 0.1f, 0.88f), new Color(0.1f, 0.13f, 0.18f, 0.8f), v);
                    Color border = new Color(0.8f, 0.9f, 1f, 0.85f);
                    Color c = Color.Lerp(bg, border, Mathf.Clamp01((3f - dEdge) / 1.5f));

                    float dNotch = Mathf.Min(Mathf.Abs(v - 0.25f), Mathf.Min(Mathf.Abs(v - 0.5f), Mathf.Abs(v - 0.75f))) * h;
                    if (dNotch < 1.5f) c = Color.Lerp(c, new Color(0.9f, 0.96f, 1f, 0.9f), (1.5f - dNotch) * 0.45f);

                    cols[y * w + x] = new Color(c.r, c.g, c.b, c.a * alpha);
                }
                else
                {
                    float fillAlpha = Mathf.Clamp01(dEdge - 1.5f);
                    float brightness = Mathf.Lerp(0.9f, 1f, v);
                    cols[y * w + x] = new Color(brightness, brightness, brightness, fillAlpha);
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply(false, true);
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
    }
}