using UnityEngine;
using UnityEngine.UI;

public class CrosshairGraphic : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper vertices)
    {
        vertices.Clear();
        float halfSize = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * 0.5f;
        if (halfSize <= 0f) return;

        AddStroke(vertices, new Vector2(-halfSize, -halfSize), new Vector2(halfSize, halfSize));
        AddStroke(vertices, new Vector2(-halfSize, halfSize), new Vector2(halfSize, -halfSize));
    }

    private void AddStroke(VertexHelper vertices, Vector2 from, Vector2 to)
    {
        Vector2 side = new Vector2(-(to.y - from.y), to.x - from.x).normalized * 0.75f;
        int start = vertices.currentVertCount;
        vertices.AddVert(from - side, color, Vector2.zero);
        vertices.AddVert(from + side, color, Vector2.zero);
        vertices.AddVert(to + side, color, Vector2.zero);
        vertices.AddVert(to - side, color, Vector2.zero);
        vertices.AddTriangle(start, start + 1, start + 2);
        vertices.AddTriangle(start, start + 2, start + 3);
    }
}
