using UnityEngine;

[CreateAssetMenu(menuName="Arena Chaos/Power Up Settings")]
public sealed class ArenaPowerSettings : ScriptableObject
{
    public ArenaPowerUps.Settings[] powers;
    [Header("Contact and aura tuning")]
    [Min(0)] public float burnDuration=3,burnDamagePerSecond=3,poisonDuration=5,poisonDamagePerSecond=2;
    [Min(.1f)] public float explosiveRadius=2.1f,firestormRadius=2.8f,frozenTimeRadius=6;
    [Min(0)] public float contactSlowDuration=2,auraSlowDuration=1.5f;
    [Range(.1f,1)] public float slowedMovementMultiplier=.45f,giantMovementMultiplier=.8f;
    [Range(1,2)] public float giantVisualScale=1.4f;
}
