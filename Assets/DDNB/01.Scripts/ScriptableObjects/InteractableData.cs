using UnityEngine;

[CreateAssetMenu(fileName = "NewInteractableData", menuName = "Interaction/InteractableData")]
public class InteractableData : ScriptableObject
{
    [Header("Basic Informations")]
    public string itemName;
    public ObjectType objectType;
    public string description;

    [Header("Capabilities")]
    public bool isConsumable;

    [Header("Audio")]
    public string interactionSoundName;
}