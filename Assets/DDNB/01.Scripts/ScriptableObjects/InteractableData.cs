using UnityEngine;

[CreateAssetMenu(fileName = "NewInteractableData", menuName = "Interaction/InteractableData")]
public class InteractableData : ScriptableObject
{
    [Header("Basic Informations")]
    public string itemName;
    public ObjectType objectType;
    public string description;

    [Header("Capabilities")]
    public bool isOpened;
    public bool isConsumable;

    [Header("Interaction Settings")]
    public string animationName;

    [Header("Visual & Audio")]
    public string interactionSoundName;
}