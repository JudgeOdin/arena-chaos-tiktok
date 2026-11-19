using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Arena Chaos/Content")]
public sealed class ArenaContent : ScriptableObject
{
    [Serializable] public struct Model { public string key; public GameObject prefab; }
    public GameObject[] fighters;
    public Texture2D[] fighterPortraits;
    public GameObject bowPrefab,arrowPrefab,shieldPrefab;
    public GameObject[] spectators;
    public RuntimeAnimatorController controller;
    public Model[] environment;
    public Material stone, sand, timber, gold, red, blue;
    public AudioClip ambience, cheer;
    public AudioClip battleMusic;
    public AudioClip[] punches, footsteps, doors;
    public Texture2D smoke, spark, flame;
    public Material sparkMaterial, smokeMaterial, flameMaterial;
    public static ArenaContent Load()
    {
        var content = Resources.Load<ArenaContent>("ArenaChaos/Content");
        return content != null && content.fighters != null && content.fighters.Length == 6 && content.environment != null && content.environment.Length > 0 && content.spectators != null && content.spectators.Length == 6 ? content : null;
    }
    public GameObject Find(string key)
    {
        if (environment != null) foreach (Model item in environment) if (item.key == key) return item.prefab;
        return null;
    }
}
