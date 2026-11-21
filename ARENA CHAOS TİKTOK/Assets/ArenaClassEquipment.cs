using UnityEngine;

public sealed class ArenaClassEquipment : MonoBehaviour
{
    GameObject equippedItem;
    public Transform ArrowOrigin {get;private set;}
    public void Build(ArenaFighter fighter)
    {
        if (equippedItem != null)
        {
            equippedItem.SetActive(false);
            Destroy(equippedItem);
        }
        ArrowOrigin = null;
        var content=ArenaContent.Load();if(content==null)return;
        Transform hand=null;foreach(Transform bone in fighter.GetComponentsInChildren<Transform>(true))if(bone.name=="hand.l"){hand=bone;break;}
        if(hand==null)return;
        var prefab=fighter.Role==ArenaFighterRole.Archer?content.bowPrefab:fighter.Role==ArenaFighterRole.Guardian?content.shieldPrefab:null;
        if(prefab==null)return;
        var item=equippedItem=Instantiate(prefab);item.name=fighter.Role==ArenaFighterRole.Archer?"Equipped bow":"Equipped heavy shield";
        Bounds bounds=new Bounds();bool found=false;foreach(var renderer in item.GetComponentsInChildren<Renderer>()){if(!found){bounds=renderer.bounds;found=true;}else bounds.Encapsulate(renderer.bounds);}
        float size=fighter.Role==ArenaFighterRole.Archer?1.1f:1;
        if(found)item.transform.localScale*=size/Mathf.Max(.01f,bounds.size.magnitude);
        item.transform.SetParent(hand,true);item.transform.position=hand.position;item.transform.rotation=hand.rotation*Quaternion.Euler(0,90,0);
        if(fighter.Role==ArenaFighterRole.Archer)
        {
            var origin=new GameObject("Bow release point");origin.transform.SetParent(item.transform,false);
            ArrowOrigin=origin.transform;ArrowOrigin.position=hand.position+fighter.transform.forward*.12f;
        }
        foreach(var collider in item.GetComponentsInChildren<Collider>())Destroy(collider);
    }
}
