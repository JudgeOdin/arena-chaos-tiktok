using UnityEngine;
using UnityEngine.UI;

/// <summary>Chamfered broadcast plates with layered metal edges and engraved accents.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class ArenaPanelGraphic : MaskableGraphic
{
    public Color accent=new Color(.8f,.65f,.32f);
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();var rect=rectTransform.rect;
        Plate(mesh,rect,12,new Color(.025f,.035f,.05f,.94f));
        var inner=rect;inner.xMin+=2;inner.xMax-=2;inner.yMin+=2;inner.yMax-=2;
        Plate(mesh,inner,10,new Color(.11f,.15f,.19f,.95f));
        inner.xMin+=2;inner.xMax-=2;inner.yMin+=2;inner.yMax-=2;
        Plate(mesh,inner,8,new Color(.035f,.052f,.072f,.94f));
        Quad(mesh,new Rect(rect.xMin+14,rect.yMax-3,Mathf.Min(90,rect.width*.3f),2),accent);
        Quad(mesh,new Rect(rect.xMin+3,rect.yMin+12,2,Mathf.Max(0,rect.height-24)),accent*.7f);
        for(int i=0;i<3;i++)Quad(mesh,new Rect(rect.xMax-20-i*8,rect.yMax-10,4,2),accent);
        for(float y=rect.yMin+9;y<rect.yMax-8;y+=7)Quad(mesh,new Rect(rect.xMin+10,y,Mathf.Max(0,rect.width-20),.6f),new Color(.25f,.32f,.38f,.09f));
    }
    static void Plate(VertexHelper mesh,Rect rect,float cut,Color color)
    {
        cut=Mathf.Min(cut,rect.height*.25f,rect.width*.25f);
        int start=mesh.currentVertCount;
        Vector2[] points={new Vector2(rect.xMin+cut,rect.yMin),new Vector2(rect.xMax-cut,rect.yMin),new Vector2(rect.xMax,rect.yMin+cut),new Vector2(rect.xMax,rect.yMax-cut),new Vector2(rect.xMax-cut,rect.yMax),new Vector2(rect.xMin+cut,rect.yMax),new Vector2(rect.xMin,rect.yMax-cut),new Vector2(rect.xMin,rect.yMin+cut)};
        foreach(var point in points)mesh.AddVert(point,color,Vector2.zero);
        for(int i=1;i<7;i++)mesh.AddTriangle(start,start+i+1,start+i);
    }
    static void Quad(VertexHelper mesh,Rect rect,Color color)
    {
        int i=mesh.currentVertCount;mesh.AddVert(new Vector2(rect.xMin,rect.yMin),color,Vector2.zero);mesh.AddVert(new Vector2(rect.xMin,rect.yMax),color,Vector2.zero);mesh.AddVert(new Vector2(rect.xMax,rect.yMax),color,Vector2.zero);mesh.AddVert(new Vector2(rect.xMax,rect.yMin),color,Vector2.zero);mesh.AddTriangle(i,i+1,i+2);mesh.AddTriangle(i,i+2,i+3);
    }
}
