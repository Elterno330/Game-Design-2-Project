using UnityEngine;
using UnityEngine.UI;

// A resolution-independent clock, drawn from UI geometry rather than a font glyph.
[RequireComponent(typeof(CanvasRenderer))]
public class PrototypeClockIcon : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper vertices)
    {
        vertices.Clear();
        Rect rect = rectTransform.rect;
        Vector2 center = rect.center;
        float radius = Mathf.Min(rect.width, rect.height) * .43f;
        float thickness = Mathf.Max(1.5f, radius * .1f);
        const int segments = 48;
        for (int i = 0; i < segments; i++)
        {
            float a = i * Mathf.PI * 2f / segments, b = (i + 1) * Mathf.PI * 2f / segments;
            Vector2 first = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            Vector2 second = new Vector2(Mathf.Cos(b), Mathf.Sin(b));
            Quad(vertices, center + first * radius, center + second * radius,
                center + second * (radius - thickness), center + first * (radius - thickness));
        }
        Hand(vertices, center, center + Vector2.up * radius * .64f, thickness);
        Hand(vertices, center, center + Vector2.right * radius * .43f, thickness);
    }

    void Hand(VertexHelper vertices, Vector2 start, Vector2 end, float thickness)
    {
        Vector2 direction = (end - start).normalized;
        Vector2 normal = new Vector2(-direction.y, direction.x) * thickness * .5f;
        Quad(vertices, start - normal, end - normal, end + normal, start + normal);
    }

    void Quad(VertexHelper vertices, Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        int index = vertices.currentVertCount;
        vertices.AddVert(a, color, Vector2.zero); vertices.AddVert(b, color, Vector2.zero);
        vertices.AddVert(c, color, Vector2.zero); vertices.AddVert(d, color, Vector2.zero);
        vertices.AddTriangle(index, index + 1, index + 2); vertices.AddTriangle(index, index + 2, index + 3);
    }
}
