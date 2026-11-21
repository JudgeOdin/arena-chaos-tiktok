using System.Collections.Generic;
using UnityEngine;
#if ARENA_NORMALGON
using Normalgon.SharedAssets;
#endif

/// <summary>
/// Builds one Normalgon modular character at runtime without changing the
/// imported package prefabs. This component is used only by the PLAYER visual.
/// </summary>
public sealed class NormalgonRuntimeAssembler : MonoBehaviour
{
#if ARENA_NORMALGON
    private bool assembled;

    public void Assemble()
    {
        if (assembled) return;

        ModularCharacter character = GetComponentInChildren<ModularCharacter>(true);
        if (character == null)
        {
            Debug.LogWarning("NormalgonRuntimeAssembler: ModularCharacter bileşeni bulunamadı.");
            return;
        }

        AssembleRiggedPart(character, character.torsoOptions, "Knight", "Torso");
        AssembleRiggedPart(character, character.upperLegsOptions, "Pants", "Upper Legs");
        AssembleRiggedPart(character, character.headOptions, "Human", "Head");
        AssembleRiggedPart(character, character.handLeftOptions, "Glove", "Left Glove");
        AssembleRiggedPart(character, character.handRightOptions, "Glove", "Right Glove");
        AssembleRiggedPart(character, character.footLeftOptions, "Boot", "Left Boot");
        AssembleRiggedPart(character, character.footRightOptions, "Boot", "Right Boot");

        AssembleSocketPart(character, character.hatOptions, "Hair", "Hat");
        AssembleSocketPart(character, character.beardOptions, "Beard", "Beard");
        AssembleSocketPart(character, character.mustacheOptions, "Mustache", "Mustache");

        DisablePhysicsOnParts();
        assembled = true;
    }

    private static void AssembleRiggedPart(ModularCharacter character, List<GameObject> options, string preferredName, string label)
    {
        GameObject prefab = FindPreferred(options, preferredName);
        if (prefab == null)
        {
            Debug.LogWarning("NormalgonRuntimeAssembler: " + label + " için uygun parça bulunamadı.");
            return;
        }

        GameObject instance = Instantiate(prefab, character.transform, false);
        instance.name = "Runtime " + label;
        character.RemapBonesForPart(instance);
    }

    private static void AssembleSocketPart(ModularCharacter character, List<GameObject> options, string preferredName, string label)
    {
        GameObject prefab = FindPreferred(options, preferredName);
        if (prefab == null || character.animatedRig == null) return;

        string socketName = label == "Hat" ? "Socket_Hat" : "Socket_" + label;
        Transform socket = FindChild(character.animatedRig, socketName);
        if (socket == null) return;

        GameObject instance = Instantiate(prefab, socket, false);
        instance.name = "Runtime " + label;
        character.RemapBonesForPart(instance);
    }

    private static GameObject FindPreferred(List<GameObject> options, string preferredName)
    {
        if (options == null) return null;

        GameObject firstValid = null;
        foreach (GameObject option in options)
        {
            if (option == null) continue;
            firstValid ??= option;
            if (option.name.IndexOf(preferredName, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return option;
            }
        }

        return firstValid;
    }

    private static Transform FindChild(Transform parent, string name)
    {
        if (parent == null) return null;
        if (parent.name == name || parent.name.StartsWith(name + "_")) return parent;

        foreach (Transform child in parent)
        {
            Transform result = FindChild(child, name);
            if (result != null) return result;
        }

        return null;
    }

    private void DisablePhysicsOnParts()
    {
        foreach (Collider collider in GetComponentsInChildren<Collider>(true))
        {
            collider.enabled = false;
        }

        foreach (Rigidbody rigidbody in GetComponentsInChildren<Rigidbody>(true))
        {
            rigidbody.isKinematic = true;
            rigidbody.detectCollisions = false;
        }
    }
#else
    public void Assemble() { }
#endif
}
