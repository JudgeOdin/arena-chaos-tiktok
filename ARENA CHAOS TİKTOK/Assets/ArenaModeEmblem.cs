using UnityEngine;
using UnityEngine.Rendering;

public sealed class ArenaModeEmblem : MonoBehaviour
{
    public enum Shape { BountySkull, Crown, Ingot, Gate, Boss, Zombie, Archer, Guardian }

    Transform anchor;
    float height;
    float angle;
    float spinSpeed=78f;
    Material skullSkin;
    float started;
    Vector3 position;

    public static ArenaModeEmblem Create(Transform owner, Shape shape, Color accent, float height = 2.35f)
    {
        var emblem = new GameObject(shape + " emblem").AddComponent<ArenaModeEmblem>();
        emblem.transform.SetParent(owner, false);
        emblem.SetAnchor(null, height);
        emblem.Build(shape, accent);
        return emblem;
    }

    public void SetAnchor(Transform target, float offset, Vector3 ground = default)
    {
        anchor = target;
        height = offset;
        position = ground;
        transform.position = (anchor != null ? anchor.position : position) + Vector3.up * height;
    }

    void Awake() => started = Time.time;

    void LateUpdate()
    {
        float hover = Mathf.Sin((Time.time - started) * 4f) * .11f;
        angle = Mathf.Repeat(angle + Time.deltaTime * spinSpeed, 360f);
        transform.SetPositionAndRotation((anchor != null ? anchor.position : position) + Vector3.up * (height + hover),
            Quaternion.Euler(8f, angle, 0f));
    }

    void Build(Shape shape, Color accent)
    {
        Color dark = Color.Lerp(accent, new Color(.025f,.038f,.06f), .72f);
        Color light = Color.Lerp(accent, Color.white,.48f);
        switch (shape)
        {
            case Shape.BountySkull:
                BuildSkull();
                break;
            case Shape.Crown:
                Part(PrimitiveType.Cylinder,new Vector3(0,-.18f,0),new Vector3(.70f,.057f,.70f),accent);
                for (int i = 0; i < 6; i++)
                {
                    float a = i * 60f;
                    Vector3 radial = Quaternion.Euler(0,a,0)*Vector3.forward;
                    Part(PrimitiveType.Cube,radial*.27f,new Vector3(.22f,.31f,.13f),accent,new Vector3(-12,a,0));
                    Part(PrimitiveType.Sphere,radial*.30f+Vector3.up*.17f,Vector3.one*.125f,light);
                }
                Part(PrimitiveType.Sphere,new Vector3(0,-.10f,.34f),Vector3.one*.16f,dark);
                break;
            case Shape.Ingot:
                Part(PrimitiveType.Cube,Vector3.zero,new Vector3(.72f,.19f,.39f),dark);
                Part(PrimitiveType.Cube,new Vector3(0,.105f,0),new Vector3(.61f,.14f,.33f),accent);
                Part(PrimitiveType.Cube,new Vector3(0,.181f,0),new Vector3(.39f,.022f,.17f),light);
                break;
            case Shape.Gate:
                for (int i = -1; i <= 1; i++)
                    Part(PrimitiveType.Cube,new Vector3(i*.23f,0,0),new Vector3(.13f,.60f,.19f),accent);
                Part(PrimitiveType.Cube,new Vector3(0,.16f,0),new Vector3(.66f,.13f,.24f),light);
                Part(PrimitiveType.Cube,new Vector3(0,-.16f,0),new Vector3(.66f,.13f,.24f),dark);
                break;
            case Shape.Boss:
                Part(PrimitiveType.Cube,Vector3.zero,Vector3.one*.47f,dark,new Vector3(42,22,45));
                Part(PrimitiveType.Cube,Vector3.zero,Vector3.one*.31f,accent,new Vector3(0,0,45));
                Part(PrimitiveType.Sphere,Vector3.zero,Vector3.one*.24f,light);
                break;
            case Shape.Zombie:
                Part(PrimitiveType.Sphere,new Vector3(0,.06f,0),new Vector3(.46f,.43f,.32f),accent);
                Part(PrimitiveType.Cube,new Vector3(0,-.16f,0),new Vector3(.25f,.18f,.24f),light);
                foreach (int side in new[]{-1,1})
                    Part(PrimitiveType.Sphere,new Vector3(side*.10f,.08f,.133f),new Vector3(.13f,.12f,.07f),dark);
                break;
            case Shape.Archer:
                Part(PrimitiveType.Cube,Vector3.zero,new Vector3(.084f,.62f,.09f),accent);
                Part(PrimitiveType.Cube,Vector3.up*.32f,Vector3.one*.17f,light,new Vector3(0,0,45));
                Part(PrimitiveType.Cube,Vector3.down*.25f,new Vector3(.28f,.10f,.13f),dark);
                break;
            case Shape.Guardian:
                Part(PrimitiveType.Cube,Vector3.zero,new Vector3(.44f,.44f,.16f),accent,new Vector3(0,0,45));
                Part(PrimitiveType.Cube,Vector3.forward*.08f,new Vector3(.28f,.28f,.07f),dark,new Vector3(0,0,45));
                break;
        }
    }

    void BuildSkull()
    {
        var skull=Resources.Load<GameObject>("ArenaChaos/Royal/BountySkull");
        if(skull==null) return;
        var model=Instantiate(skull,transform,false);
        model.name="Ivory bounty skull";
        var renderers=model.GetComponentsInChildren<Renderer>(true);
        if(renderers.Length==0) return;
        var bounds=renderers[0].bounds;
        foreach(var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        if(bounds.size.y>.001f)
        {
            model.transform.localScale*=.78f/bounds.size.y;
            bounds=renderers[0].bounds;
            foreach(var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            model.transform.position+=transform.position-bounds.center;
        }
        spinSpeed=52f;
        var shader=Shader.Find("Universal Render Pipeline/Lit");
        if(shader==null) return;
        skullSkin=new Material(shader){color=new Color(.98f,.83f,.60f)};
        skullSkin.SetFloat("_Metallic",.08f);
        skullSkin.SetFloat("_Smoothness",.22f);
        foreach(var renderer in renderers)
        {
            renderer.sharedMaterial=skullSkin;
            renderer.shadowCastingMode=ShadowCastingMode.Off;
        }
    }

    void OnDestroy()
    {
        if(skullSkin!=null) Destroy(skullSkin);
    }

    void Part(PrimitiveType primitive, Vector3 at, Vector3 scale, Color color, Vector3 euler = default) =>
        ArenaVisualPart.Create(transform,primitive,at,scale,color,euler);
}

internal static class ArenaVisualPart
{
    public static Transform Create(Transform parent, PrimitiveType primitive, Vector3 at, Vector3 scale,
        Color color, Vector3 euler = default)
    {
        var part = GameObject.CreatePrimitive(primitive);
        part.name = primitive.ToString();
        var collider = part.GetComponent<Collider>();
        collider.enabled = false;
        Object.Destroy(collider);
        var t = part.transform;
        t.SetParent(parent,false);
        t.localPosition = at;
        t.localScale = scale;
        t.localRotation = Quaternion.Euler(euler);
        var renderer = part.GetComponent<Renderer>();
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        var properties = new MaterialPropertyBlock();
        properties.SetColor("_BaseColor", color);
        properties.SetColor("_Color", color);
        renderer.SetPropertyBlock(properties);
        return t;
    }
}

