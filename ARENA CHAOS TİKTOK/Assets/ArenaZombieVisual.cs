using UnityEngine;

public sealed class ArenaZombieVisual : MonoBehaviour
{
    ArenaFighter fighter;
    Transform torso, head;
    readonly Transform[] arms = new Transform[2], legs = new Transform[2];
    float pace;

    static readonly Color Skin = new Color(.48f,.64f,.30f);
    static readonly Color Pale = new Color(.68f,.77f,.40f);
    static readonly Color Coat = new Color(.24f,.16f,.31f);
    static readonly Color Shadow = new Color(.075f,.09f,.09f);
    static readonly Color Eyes = new Color(1f,.88f,.27f);

    public void Build(ArenaFighter owner)
    {
        fighter = owner;
        torso = Joint("Stooped torso",transform,new Vector3(0,-.20f,0));
        Part(PrimitiveType.Capsule,torso,new Vector3(0,.30f,0),new Vector3(.64f,.53f,.47f),Coat,new Vector3(12,0,-9));
        Part(PrimitiveType.Cube,torso,new Vector3(.10f,.33f,.23f),new Vector3(.27f,.34f,.07f),Skin,new Vector3(0,0,-25));
        Part(PrimitiveType.Cube,torso,new Vector3(-.17f,.08f,.17f),new Vector3(.23f,.32f,.09f),Coat,new Vector3(0,0,18));
        head = Joint("Cocked head",torso,new Vector3(.08f,1f,.13f));
        Part(PrimitiveType.Cube,head,Vector3.zero,new Vector3(.58f,.53f,.50f),Skin);
        Part(PrimitiveType.Cube,head,new Vector3(.02f,-.27f,.06f),new Vector3(.42f,.18f,.35f),Pale,new Vector3(0,0,11));
        Part(PrimitiveType.Cube,head,new Vector3(.04f,-.17f,.257f),new Vector3(.32f,.092f,.034f),Shadow,new Vector3(0,0,11));
        Part(PrimitiveType.Cube,head,new Vector3(.20f,.18f,.14f),new Vector3(.14f,.25f,.20f),Coat,new Vector3(0,0,34));
        for (int i = 0; i < 2; i++)
        {
            int side = i == 0 ? -1 : 1;
            Part(PrimitiveType.Cube,head,new Vector3(side*.145f,.025f,.252f),new Vector3(.173f,.125f,.040f),Shadow);
            Part(PrimitiveType.Cube,head,new Vector3(side*.145f,.025f,.279f),new Vector3(.102f,.054f,.023f),Eyes);
            Part(PrimitiveType.Cube,head,new Vector3(side*.07f,-.155f,.281f),new Vector3(.04f,.057f,.022f),Pale);
            arms[i] = Joint("Reaching arm",torso,new Vector3(side*.40f,.57f,.07f));
            Part(PrimitiveType.Cube,arms[i],new Vector3(0,0,.19f),new Vector3(.23f,.23f,.41f),Coat);
            Part(PrimitiveType.Capsule,arms[i],new Vector3(0,-.075f,.49f),new Vector3(.16f,.24f,.16f),Skin,new Vector3(69,0,0));
            Part(PrimitiveType.Cube,arms[i],new Vector3(0,-.14f,.70f),new Vector3(.17f,.14f,.22f),Pale,new Vector3(18,0,0));
            legs[i] = Joint("Dragging leg",transform,new Vector3(side*.19f,-.24f,0));
            Part(PrimitiveType.Cube,legs[i],new Vector3(0,-.28f,0),new Vector3(.27f,.57f,.27f),i==0?Coat:Shadow);
            Part(PrimitiveType.Cube,legs[i],new Vector3(0,-.55f,.10f),new Vector3(.30f,.18f,.43f),Shadow);
        }
    }

    void LateUpdate()
    {
        if (fighter == null || !fighter.IsAlive) return;
        pace += Time.deltaTime * (fighter.IsMoving ? 7f : 2f);
        float sway = Mathf.Sin(pace);
        float stride = fighter.IsMoving ? 23f : 2f;
        torso.localRotation = Quaternion.Euler(15f+2f*sway, 2f*sway, -8f+4f*sway);
        head.localRotation = Quaternion.Euler(-12f, sway*12f, 14f);
        for (int i = 0; i < 2; i++)
        {
            float leftRight = i==0?-1f:1f;
            float reach = fighter.IsPunching ? Mathf.Sin(fighter.AttackProgress*Mathf.PI)*28f : 0f;
            arms[i].localRotation = Quaternion.Euler(10f + sway*leftRight*8f - reach,leftRight*12f,leftRight*9f);
            legs[i].localRotation = Quaternion.Euler(sway*leftRight*stride,0,0);
        }
    }

    static Transform Joint(string name,Transform parent,Vector3 at)
    {
        var joint = new GameObject(name).transform;
        joint.SetParent(parent,false);
        joint.localPosition = at;
        return joint;
    }

    static void Part(PrimitiveType shape,Transform parent,Vector3 at,Vector3 scale,Color color,Vector3 euler=default) =>
        ArenaVisualPart.Create(parent,shape,at,scale,color,euler);
}

