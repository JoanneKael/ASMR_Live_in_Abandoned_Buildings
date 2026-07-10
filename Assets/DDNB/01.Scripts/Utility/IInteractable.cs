using UnityEngine;

public interface IInteractable
{
    /// <summary>
    /// 스크립터블 오브젝트
    /// </summary>
    InteractableData Data { get; }

    /// <summary>
    /// 일반 상호작용
    /// </summary>
    void Interact();

    /// <summary>
    /// 홀드 상호작용
    /// </summary>
    void HoldInteract(float mouseDelta);
}
