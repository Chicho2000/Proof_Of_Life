using UnityEngine;

/// <summary>
/// Genera un cono de visión 3D procedural y translúcido en tiempo de ejecución
/// para visualizar con precisión el alcance y límites de la cámara durante el desarrollo.
/// Se sincroniza en tiempo real con el estado de la cámara (Verde -> Amarillo -> Rojo).
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class VisionConeRenderer : MonoBehaviour
{
    [Header("Configuración Visual")]
    [Tooltip("Activa o desactiva la visualización del cono 3D en el juego (útil para desarrollo/diseño de niveles).")]
    [SerializeField] private bool showVisionCone = true;

    [Tooltip("Resolución circular del cono (cantidad de segmentos).")]
    [Range(12, 48)]
    [SerializeField] private int segments = 24;

    [Header("Opacidad")]
    [Range(0.05f, 0.8f)]
    [SerializeField] private float coneAlpha = 0.2f;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh coneMesh;
    private Material coneMaterial;

    private DetectionSystem detectionSystem;
    private SecurityCamera securityCamera;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        detectionSystem = GetComponentInParent<DetectionSystem>();
        securityCamera = GetComponentInParent<SecurityCamera>();

        CreateConeMaterial();
        GenerateConeMesh();
    }

    private void Update()
    {
        meshRenderer.enabled = showVisionCone;
        if (!showVisionCone) return;

        UpdateMaterialColor();
    }

    /// <summary>
    /// Crea un material translúcido compatible con Universal Render Pipeline (URP).
    /// </summary>
    private void CreateConeMaterial()
    {
        // Buscar shader de URP Unlit o Standard Particles/Unlit
        Shader urpShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (urpShader == null)
        {
            urpShader = Shader.Find("Sprites/Default");
        }

        coneMaterial = new Material(urpShader);
        coneMaterial.name = "M_VisionCone_Runtime";

        // Configurar transparencia en URP
        if (coneMaterial.HasProperty("_Surface"))
        {
            coneMaterial.SetFloat("_Surface", 1); // Transparent
        }
        if (coneMaterial.HasProperty("_Blend"))
        {
            coneMaterial.SetFloat("_Blend", 0); // Alpha blend
        }
        if (coneMaterial.HasProperty("_ZWrite"))
        {
            coneMaterial.SetFloat("_ZWrite", 0); // No escribir en Z
        }
        if (coneMaterial.HasProperty("_Cull"))
        {
            coneMaterial.SetFloat("_Cull", 0); // Doble cara (renderiza interior y exterior)
        }

        coneMaterial.renderQueue = 3000;
        meshRenderer.material = coneMaterial;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
    }

    /// <summary>
    /// Genera la geometría tridimensional del cono de visión.
    /// </summary>
    public void GenerateConeMesh()
    {
        float distance = detectionSystem != null ? detectionSystem.ViewDistance : 12f;
        float angle = detectionSystem != null ? detectionSystem.ViewAngle : 60f;

        float halfAngleRad = angle * 0.5f * Mathf.Deg2Rad;
        float radius = distance * Mathf.Sin(halfAngleRad);
        float coneLength = distance * Mathf.Cos(halfAngleRad);

        coneMesh = new Mesh();
        coneMesh.name = "Procedural_VisionCone";

        Vector3[] vertices = new Vector3[segments + 2];
        int[] triangles = new int[segments * 3 + segments * 3]; // Caras laterales + tapa frontal

        // Vértice 0: Vértice del origen (Lente)
        vertices[0] = Vector3.zero;

        // Vértices 1 a segments: Anillo exterior
        for (int i = 0; i < segments; i++)
        {
            float theta = (2f * Mathf.PI / segments) * i;
            float x = Mathf.Sin(theta) * radius;
            float y = Mathf.Cos(theta) * radius;
            vertices[i + 1] = new Vector3(x, y, coneLength);
        }

        // Vértice centro de la tapa base
        vertices[segments + 1] = new Vector3(0f, 0f, coneLength);

        // Triángulos de las caras laterales del cono
        int triIndex = 0;
        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;
            triangles[triIndex++] = 0;
            triangles[triIndex++] = i + 1;
            triangles[triIndex++] = next + 1;
        }

        // Triángulos de la tapa frontal
        int centerIndex = segments + 1;
        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;
            triangles[triIndex++] = centerIndex;
            triangles[triIndex++] = next + 1;
            triangles[triIndex++] = i + 1;
        }

        coneMesh.vertices = vertices;
        coneMesh.triangles = triangles;
        coneMesh.RecalculateNormals();
        coneMesh.RecalculateBounds();

        meshFilter.mesh = coneMesh;
    }

    /// <summary>
    /// Sincroniza el color y la opacidad del cono con el estado de la cámara.
    /// </summary>
    private void UpdateMaterialColor()
    {
        if (coneMaterial == null) return;

        Color targetColor = new Color(0.2f, 0.85f, 0.2f, coneAlpha); // Verde normal

        if (securityCamera != null)
        {
            switch (securityCamera.CurrentState)
            {
                case SecurityCamera.CameraState.Normal:
                    targetColor = new Color(0.2f, 0.85f, 0.2f, coneAlpha);
                    break;

                case SecurityCamera.CameraState.Suspicion:
                    float progress = securityCamera.DetectionProgress;
                    Color yellow = new Color(1f, 0.85f, 0.1f, coneAlpha + 0.1f);
                    Color red = new Color(1f, 0.15f, 0.15f, coneAlpha + 0.15f);
                    targetColor = Color.Lerp(yellow, red, progress);
                    break;

                case SecurityCamera.CameraState.Alarm:
                    targetColor = new Color(1f, 0.15f, 0.15f, coneAlpha + 0.25f);
                    break;
            }
        }

        if (coneMaterial.HasProperty("_BaseColor"))
        {
            coneMaterial.SetColor("_BaseColor", targetColor);
        }
        else if (coneMaterial.HasProperty("_Color"))
        {
            coneMaterial.SetColor("_Color", targetColor);
        }
    }

    public void SetVisibility(bool visible)
    {
        showVisionCone = visible;
        if (meshRenderer != null)
        {
            meshRenderer.enabled = visible;
        }
    }
}
