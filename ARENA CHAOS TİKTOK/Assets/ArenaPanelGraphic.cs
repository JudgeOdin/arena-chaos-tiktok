using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class ArenaPanelGraphic : MaskableGraphic
{
    public Color accent = new Color(.88f, .65f, .30f);
    public bool fadeUp;
    public bool backdrop;

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect rect = rectTransform.rect;
        if (rect.width < 1 || rect.height < 1) return;

        if (backdrop)
        {
            var shade = new Color(.017f, .025f, .043f, .98f);
            var clear = new Color(shade.r, shade.g, shade.b, 0f);
            Quad(mesh, rect, fadeUp ? shade : clear, fadeUp ? clear : shade);
            return;
        }

        float cut = Mathf.Min(20, rect.height * .24f, rect.width * .17f);
        Plate(mesh, rect, cut, new Color(.013f, .019f, .032f, .98f),
            Color.Lerp(new Color(.23f, .29f, .37f), accent, .52f));

        var face = Rect.MinMaxRect(rect.xMin + 2, rect.yMin + 2, rect.xMax - 2, rect.yMax - 2);
        Plate(mesh, face, Mathf.Max(0, cut - 1),
            new Color(.026f, .038f, .063f, .98f),
            Color.Lerp(new Color(.09f, .13f, .20f, .98f), accent, .12f));

        float rail = Mathf.Min(rect.width * .36f, 114);
        Quad(mesh, new Rect(rect.xMin + cut, rect.yMax - 3, rail, 2), accent, accent);
        Quad(mesh, new Rect(rect.xMax - cut - rail * .35f, rect.yMin + 1, rail * .35f, 1),
            accent * .55f, accent * .55f);
    }

    static void Plate(VertexHelper mesh, Rect rect, float cut, Color bottom, Color top)
    {
        int start = mesh.currentVertCount;
        Vertex(mesh, rect.xMin + cut, rect.yMin, bottom);
        Vertex(mesh, rect.xMax - cut, rect.yMin, bottom);
        Vertex(mesh, rect.xMax, rect.yMin + cut, bottom);
        Vertex(mesh, rect.xMax, rect.yMax - cut, top);
        Vertex(mesh, rect.xMax - cut, rect.yMax, top);
        Vertex(mesh, rect.xMin + cut, rect.yMax, top);
        Vertex(mesh, rect.xMin, rect.yMax - cut, top);
        Vertex(mesh, rect.xMin, rect.yMin + cut, bottom);
        for (int i = 1; i < 7; i++) mesh.AddTriangle(start, start + i + 1, start + i);
    }

    static void Quad(VertexHelper mesh, Rect rect, Color bottom, Color top)
    {
        int start = mesh.currentVertCount;
        Vertex(mesh, rect.xMin, rect.yMin, bottom);
        Vertex(mesh, rect.xMin, rect.yMax, top);
        Vertex(mesh, rect.xMax, rect.yMax, top);
        Vertex(mesh, rect.xMax, rect.yMin, bottom);
        mesh.AddTriangle(start, start + 1, start + 2);
        mesh.AddTriangle(start, start + 2, start + 3);
    }

    static void Vertex(VertexHelper mesh, float x, float y, Color color) =>
        mesh.AddVert(new Vector3(x, y), color, Vector2.zero);
}
