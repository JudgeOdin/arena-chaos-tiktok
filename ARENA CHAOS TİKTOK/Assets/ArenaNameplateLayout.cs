using System.Collections.Generic;
using UnityEngine;

/// <summary>Screen-space spacing and a hard label budget keep crowded broadcasts readable.</summary>
public sealed class ArenaNameplateLayout : MonoBehaviour
{
    readonly List<ArenaFighter> candidates=new List<ArenaFighter>();
    readonly List<Rect> occupied=new List<Rect>();
    BattleArenaManager arena;
    float nextLayout;
    [SerializeField,Range(4,20)] int labelBudget=12;
    void Start(){arena=GetComponent<BattleArenaManager>();}
    void LateUpdate()
    {
        if(arena==null||Camera.main==null||Time.unscaledTime<nextLayout)return;
        nextLayout=Time.unscaledTime+.12f;candidates.Clear();occupied.Clear();
        foreach(var actor in arena.Fighters)if(actor!=null){actor.SetNameplateVisible(false);if(actor.IsAlive&&!actor.IsEntering)candidates.Add(actor);}
        candidates.Sort((a,b)=>{int c=b.IsRoyal.CompareTo(a.IsRoyal);if(c==0)c=a.IsBot.CompareTo(b.IsBot);return c!=0?c:a.Health.CompareTo(b.Health);});
        foreach(var actor in candidates)
        {
            if(occupied.Count>=labelBudget)break;
            var point=Camera.main.WorldToScreenPoint(actor.NameplatePosition);
            if(point.z<0||point.x<30||point.x>Screen.width-30||point.y<30||point.y>Screen.height-30)continue;
            float scale=Screen.height/1080f;var rect=new Rect(point.x-65*scale,point.y-10*scale,130*scale,26*scale);
            bool overlaps=false;foreach(var previous in occupied)if(previous.Overlaps(rect)){overlaps=true;break;}
            if(overlaps)continue;occupied.Add(rect);actor.SetNameplateVisible(true);
        }
    }
}
