using UnityEngine;
using UnityEngine.UI;

/// <summary>Authored vector insignia for the marked player, drawn in one UI mesh.</summary>
public sealed class ArenaBountyBadge : MaskableGraphic
{
    static readonly Color Ink=new Color(.095f,.035f,.035f);
    static readonly Color Gold=new Color(1f,.73f,.28f);
    static readonly Color Crimson=new Color(.58f,.075f,.10f);
    static readonly Color Ivory=new Color(1f,.96f,.83f);
    static readonly Vector2[] Shield={new Vector2(-36,46),new Vector2(36,46),new Vector2(41,38),
        new Vector2(34,-29),new Vector2(0,-53),new Vector2(-34,-29),new Vector2(-41,38)};

    public static void Create(Transform parent)
    {
        var root=new GameObject("Bounty target badge",typeof(RectTransform),typeof(Canvas));
        root.transform.SetParent(parent,false);
        root.transform.localScale=Vector3.one*.0095f;
        var rect=root.GetComponent<RectTransform>();rect.sizeDelta=new Vector2(100,110);
        var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;
        canvas.overrideSorting=true;canvas.sortingOrder=4;
        var icon=new GameObject("Marked player insignia",typeof(RectTransform),typeof(CanvasRenderer),typeof(ArenaBountyBadge));
        icon.transform.SetParent(root.transform,false);
        icon.GetComponent<RectTransform>().sizeDelta=rect.sizeDelta;
        icon.GetComponent<ArenaBountyBadge>().raycastTarget=false;
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Polygon(mesh,Shield,1.08f,Ink);
        Polygon(mesh,Shield,1,Gold);
        Polygon(mesh,Shield,.87f,Crimson);
        // A dark inset separates the ivory target silhouette from the red enamel.
        Disc(mesh,new Vector2(0,9),27,Ink,48);
        for(int arc=0;arc<4;arc++)Arc(mesh,new Vector2(0,9),21.5f,2.8f,arc*90+12,arc*90+78,Ivory);
        for(int i=0;i<4;i++)
        {
            float a=i*Mathf.PI*.5f;
            Stroke(mesh,new Vector2(Mathf.Cos(a),Mathf.Sin(a))*17+new Vector2(0,9),
                new Vector2(Mathf.Cos(a),Mathf.Sin(a))*29+new Vector2(0,9),3.5f,Gold);
        }
        Disc(mesh,new Vector2(0,15),5.5f,Ivory,24);
        Polygon(mesh,new[]{new Vector2(-9,1),new Vector2(-8,5),new Vector2(-4,8),
            new Vector2(4,8),new Vector2(8,5),new Vector2(9,1)},1,Ivory);
        // Bottom chevron points to the actual target instead of spinning edge-on.
        Stroke(mesh,new Vector2(-13,-31),new Vector2(0,-40),3.6f,Gold);
        Stroke(mesh,new Vector2(0,-40),new Vector2(13,-31),3.6f,Gold);
        Stroke(mesh,new Vector2(-27,38),new Vector2(-9,38),1.8f,Ivory);
    }

    static void Vertex(VertexHelper mesh,Vector2 point,Color color)
    {
        var v=UIVertex.simpleVert;v.position=point;v.color=color;mesh.AddVert(v);
    }
    static void Polygon(VertexHelper mesh,Vector2[] points,float scale,Color color)
    {
        int start=mesh.currentVertCount;
        Vector2 center=Vector2.zero;foreach(var p in points)center+=p;center/=points.Length;
        Vertex(mesh,center*scale,color);
        foreach(var point in points)Vertex(mesh,point*scale,color);
        for(int i=0;i<points.Length;i++)mesh.AddTriangle(start,start+1+i,start+1+(i+1)%points.Length);
    }
    static void Disc(VertexHelper mesh,Vector2 center,float radius,Color color,int steps)
    {
        int start=mesh.currentVertCount;Vertex(mesh,center,color);
        for(int i=0;i<steps;i++){float a=i*Mathf.PI*2/steps;Vertex(mesh,center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,color);}
        for(int i=0;i<steps;i++)mesh.AddTriangle(start,start+1+i,start+1+(i+1)%steps);
    }
    static void Stroke(VertexHelper mesh,Vector2 a,Vector2 b,float width,Color color)
    {
        var direction=(b-a).normalized;var normal=new Vector2(-direction.y,direction.x)*width*.5f;
        int n=mesh.currentVertCount;
        Vertex(mesh,a-normal,color);Vertex(mesh,a+normal,color);Vertex(mesh,b+normal,color);Vertex(mesh,b-normal,color);
        mesh.AddTriangle(n,n+1,n+2);mesh.AddTriangle(n,n+2,n+3);
    }
    static void Arc(VertexHelper mesh,Vector2 center,float radius,float width,float start,float end,Color color)
    {
        for(int i=0;i<14;i++)
        {
            float a=Mathf.Lerp(start,end,i/14f)*Mathf.Deg2Rad;
            float b=Mathf.Lerp(start,end,(i+1)/14f)*Mathf.Deg2Rad;
            Stroke(mesh,center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,
                center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,width,color);
        }
    }
}
