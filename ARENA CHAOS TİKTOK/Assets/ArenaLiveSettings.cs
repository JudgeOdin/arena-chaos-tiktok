using System;
using UnityEngine;

[CreateAssetMenu(menuName="Arena Chaos/LIVE Settings")]
public sealed class ArenaLiveSettings : ScriptableObject
{
    [Serializable] public class Binding {public string giftId,label;public GiftEventType type;}
    public bool connectOnStart;
    public string endpoint="ws://127.0.0.1:21213/";
    public string channel;
    public Binding[] gifts=Array.Empty<Binding>();
}
