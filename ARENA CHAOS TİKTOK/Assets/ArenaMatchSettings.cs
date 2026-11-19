using UnityEngine;

public enum ArenaRoundLength { ThreeMinutes=180, FiveMinutes=300 }
[CreateAssetMenu(menuName="Arena Chaos/Match Settings")]
public sealed class ArenaMatchSettings : ScriptableObject
{
    public ArenaRoundLength roundLength=ArenaRoundLength.ThreeMinutes;
    [Range(4,20)] public int reservePopulation=10;
    [Min(1)] public float reinforcementInterval=3;
    [Range(.1f,.6f)] public float retreatHealthFraction=.35f;
    [Min(0)] public float recoveryPerSecond=2;
    [Range(0,1)] public float botScoreWeight=.25f;
    [Range(0,1)] public float dodgeChance=.45f;
    [Min(.1f)] public float dodgeCooldown=2.5f;
    [Range(0,1)] public float guardianFrontalDamageMultiplier=.65f;
    [Min(1)] public float meteorMinimumInterval=15;
}
