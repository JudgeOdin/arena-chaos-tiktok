using System;
using UnityEngine;

public enum ArenaFighterRole { Basic, Archer, Guardian }
[CreateAssetMenu(menuName="Arena Chaos/Gift Character Roster")]
public sealed class ArenaGiftRoster : ScriptableObject
{
    [Serializable] public class Tier
    {
        public ArenaFighterRole role;
        public string label;
        [Tooltip("Fill from the selected LIVE provider; empty means not mapped.")] public string tiktokGiftId;
        public int modelIndex;
        public float health=100,damage=16,speed=2.8f,range=1.2f,cooldown=.9f,shield;
    }
    public Tier[] tiers={
        new Tier{role=ArenaFighterRole.Basic,label="TEMEL DÖVÜŞÇÜ",modelIndex=0},
        new Tier{role=ArenaFighterRole.Archer,label="OKÇU",modelIndex=3,health=140,damage=22,speed=3,range=7,cooldown=1.4f},
        new Tier{role=ArenaFighterRole.Guardian,label="KALKANLI MUHAFIZ",modelIndex=1,health=240,damage=24,speed=2.2f,range=1.25f,cooldown=1.05f,shield=80}
    };
    public Tier Find(ArenaFighterRole role){foreach(var tier in tiers)if(tier.role==role)return tier;return null;}
}
