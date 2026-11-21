using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class ArenaRoyalWardrobe : MonoBehaviour
{
    const int Columns = 9;
    const int Rows = 11;
    ArenaFighter owner;
    Mesh cape;
    Vector3[] points;
    readonly List<Material> materials = new List<Material>();

    public static bool Dress(ArenaFighter owner, GameObject model, Color team)
    {
        var shader=Shader.Find("Universal Render Pipeline/Lit");
        if(shader==null || !shader.isSupported) return false;
        var wardrobe=model.AddComponent<ArenaRoyalWardrobe>();
        wardrobe.owner=owner;
        bool blue=team.b>team.r;
        Color cloth=blue?new Color(.12f,.37f,.79f):new Color(.77f,.11f,.14f);
        Color gold=new Color(1f,.69f,.24f);
        var palette=new Dictionary<string,Material>();
        foreach(var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            var slots=renderer.sharedMaterials;
            for(int i=0;i<slots.Length;i++)
            {
                string key=slots[i]!=null?slots[i].name:"Metal";
                if(!palette.TryGetValue(key,out var material))
                {
                    Color shade=key switch
                    {
                        "Blue"=>cloth,
                        "Gold"=>gold,
                        "Metal"=>new Color(.42f,.49f,.58f),
                        "Metal_Dark"=>new Color(.20f,.23f,.29f),
                        "DarkBrown"=>new Color(.22f,.13f,.08f),
                        "Skin"=>new Color(.78f,.52f,.34f),
                        "Beige"=>new Color(.78f,.69f,.51f),
                        "Hair_White"=>new Color(.72f,.73f,.72f),
                        "Eye"=>new Color(.06f,.038f,.032f),
                        _=>Color.gray
                    };
                    material=wardrobe.MaterialFor(shader,key,shade,key=="Gold"||key.StartsWith("Metal")?.48f:0,.28f);
                    palette.Add(key,material);
                }
                slots[i]=material;
            }
            renderer.sharedMaterials=slots;
        }
        wardrobe.BuildCape(shader,cloth,gold);
        return palette.Count>0;
    }

    Material MaterialFor(Shader shader,string name,Color color,float metallic,float smoothness)
    {
        var material=new Material(shader) {name=name,color=color};
        material.SetFloat("_Metallic",metallic);
        material.SetFloat("_Smoothness",smoothness);
        materials.Add(material);
        return material;
    }

    void BuildCape(Shader shader,Color cloth,Color gold)
    {
        var root=new GameObject("Velvet mantle and gold hem",typeof(MeshFilter),typeof(MeshRenderer));
        root.transform.SetParent(owner.transform,false);
        cape=new Mesh {name="Tailored monarch cape"};
        cape.MarkDynamic();
        points=new Vector3[Rows*Columns];
        var fleece=MaterialFor(shader,"Royal velvet",cloth,0,.14f);
        var trim=MaterialFor(shader,"Gold braid",gold,.20f,.36f);
        fleece.SetFloat("_Cull",(float)CullMode.Off);
        trim.SetFloat("_Cull",(float)CullMode.Off);
        var clothTriangles=new List<int>();
        var hemTriangles=new List<int>();
        for(int r=0;r<Rows-1;r++)
            for(int c=0;c<Columns-1;c++)
            {
                int i=r*Columns+c;
                var indices=c==0 || c==Columns-2 || r==Rows-2?hemTriangles:clothTriangles;
                indices.Add(i);indices.Add(i+1);indices.Add(i+Columns);
                indices.Add(i+1);indices.Add(i+Columns+1);indices.Add(i+Columns);
            }
        UpdateCape(0,0);
        cape.subMeshCount=2;
        cape.SetTriangles(clothTriangles,0);
        cape.SetTriangles(hemTriangles,1);
        cape.RecalculateNormals();
        root.GetComponent<MeshFilter>().sharedMesh=cape;
        root.GetComponent<MeshRenderer>().sharedMaterials=new[]{fleece,trim};
    }

    void LateUpdate()
    {
        if(cape==null || owner==null) return;
        UpdateCape(Time.time,owner.IsMoving?.17f:.044f);
        cape.RecalculateNormals();
    }

    void UpdateCape(float time,float lift)
    {
        for(int r=0;r<Rows;r++)
        {
            float t=r/(Rows-1f);
            for(int c=0;c<Columns;c++)
            {
                float u=c/(Columns-1f)*2-1;
                float fold=Mathf.Cos(u*Mathf.PI*3)*(.022f+.037f*t);
                float sway=Mathf.Sin(time*4.4f-t*4.2f+u*1.6f)*lift*t*t;
                float y=1.20f-1.86f*t+.19f*Mathf.Abs(u)*t;
                float z=-.31f-.25f*t-.13f*(1-u*u)+fold+sway;
                points[r*Columns+c]=new Vector3(u*Mathf.Lerp(.43f,.68f,t),y,z);
            }
        }
        cape.vertices=points;
        cape.RecalculateBounds();
    }

    void OnDestroy()
    {
        if(cape!=null) Destroy(cape);
        foreach(var material in materials) Destroy(material);
    }
}

